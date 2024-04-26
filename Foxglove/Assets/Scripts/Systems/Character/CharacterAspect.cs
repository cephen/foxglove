using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Foxglove.Character {
    // Aspects are used to query the ECS world for entities with a given set of components.
    // implementing IKinematicCharacterProcessor on this aspect allows the aspect to process physics updates for characters
    public readonly partial struct CharacterAspect : IAspect,
        IKinematicCharacterProcessor<FoxgloveCharacterUpdateContext> {
        // having another aspect as a field means components from that aspect are required to satisfy this aspect.
        public readonly KinematicCharacterAspect KinematicCharacter;

        // These additional component types are needed to simulate character motion
        public readonly RefRW<CharacterSettings> CharacterSettings;
        public readonly RefRW<CharacterController> CharacterControl;

        /// <summary>
        /// This method is like GameObject.FixedUpdate, and will process every entity that has the required components.
        /// Called by <see cref="CharacterPhysicsUpdateSystem.CharacterPhysicsUpdateJob" />
        /// </summary>
        /// <param name="foxgloveContext">Contains global game state</param>
        /// <param name="physicsContext">Contains global physics state</param>
        public void PhysicsUpdate(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext physicsContext
        ) {
            ref CharacterSettings characterSettings = ref CharacterSettings.ValueRW;
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref float3 characterPosition = ref KinematicCharacter.LocalTransform.ValueRW.Position;

#region First phase of physics update

            KinematicCharacter.Update_Initialize(
                in this,
                ref foxgloveContext,
                ref physicsContext,
                ref characterBody,
                physicsContext.Time.DeltaTime
            );
            KinematicCharacter.Update_ParentMovement(
                in this,
                ref foxgloveContext,
                ref physicsContext,
                ref characterBody,
                ref characterPosition,
                characterBody.WasGroundedBeforeCharacterUpdate
            );
            KinematicCharacter.Update_Grounding(
                in this,
                ref foxgloveContext,
                ref physicsContext,
                ref characterBody,
                ref characterPosition
            );

#endregion

            // Update desired character velocity after grounding was detected
            // but before doing additional processing that depends on velocity
            HandleVelocityControl(ref foxgloveContext, ref physicsContext);

#region Second phase of default character update

            KinematicCharacter.Update_PreventGroundingFromFutureSlopeChange(
                in this,
                ref foxgloveContext,
                ref physicsContext,
                ref characterBody,
                in characterSettings.StepAndSlopeHandling
            );
            KinematicCharacter.Update_GroundPushing(
                in this,
                ref foxgloveContext,
                ref physicsContext,
                characterSettings.Gravity
            );
            KinematicCharacter.Update_MovementAndDecollisions(
                in this,
                ref foxgloveContext,
                ref physicsContext,
                ref characterBody,
                ref characterPosition
            );
            KinematicCharacter.Update_MovingPlatformDetection(ref physicsContext, ref characterBody);
            KinematicCharacter.Update_ParentMomentum(ref physicsContext, ref characterBody);
            KinematicCharacter.Update_ProcessStatefulCharacterHits();

#endregion
        }

        /// <summary>
        /// Transforms a character's control input into a target velocity.
        /// Called between the first and second phases of the default character update in <see cref="PhysicsUpdate" />
        /// </summary>
        /// <param name="foxgloveContext">Contains global game state</param>
        /// <param name="physicsContext">Contains global physics state</param>
        private void HandleVelocityControl(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext physicsContext
        ) {
            float deltaTime = physicsContext.Time.DeltaTime;
            CharacterSettings characterSettings = CharacterSettings.ValueRO;
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref CharacterController characterController = ref CharacterControl.ValueRW;

            // If the character has a parent, rotate the move input and velocity to take into account the parent's rotation
            if (characterBody.ParentEntity != Entity.Null) {
                characterController.MoveVector =
                    math.rotate(characterBody.RotationFromParent, characterController.MoveVector);
                characterBody.RelativeVelocity =
                    math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
            }

            if (characterBody.IsGrounded) {
                HandleGroundedMovement(
                    ref characterController,
                    ref characterBody,
                    in characterSettings,
                    deltaTime
                );
            }
            else { // character is in air
                float3 airAcceleration = characterController.MoveVector * characterSettings.AirAcceleration;
                // if the magnitude of the air acceleration is greater than the smallest representable floating point value
                // (filters out noisy inputs)
                if (math.lengthsq(airAcceleration) > math.EPSILON)
                    HandleAirMovement(
                        ref foxgloveContext,
                        ref physicsContext,
                        ref characterBody,
                        characterSettings,
                        airAcceleration,
                        deltaTime
                    );

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(
                    ref characterBody.RelativeVelocity,
                    characterSettings.Gravity,
                    deltaTime
                );

                // Air Drag
                CharacterControlUtilities.ApplyDragToVelocity(
                    ref characterBody.RelativeVelocity,
                    deltaTime,
                    characterSettings.AirDrag
                );
            }
        }

        private void HandleGroundedMovement(
            ref CharacterController characterController,
            ref KinematicCharacterBody characterBody,
            in CharacterSettings characterSettings,
            float deltaTime
        ) {
            float3 targetVelocity = characterController.MoveVector * characterSettings.GroundMaxSpeed;

            // CharacterControlUtilities comes from Unity.CharacterController,
            // and implements a bunch of functionality I was trying to do manually :'(
            CharacterControlUtilities.StandardGroundMove_Interpolated(
                ref characterBody.RelativeVelocity,
                targetVelocity,
                characterSettings.GroundedAcceleration,
                deltaTime,
                characterBody.GroundingUp,
                characterBody.GroundHit.Normal
            );

            // Handle jump input
            if (characterController.Jump)
                CharacterControlUtilities.StandardJump(
                    ref characterBody,
                    characterBody.GroundingUp * characterSettings.JumpForce, // jump velocity
                    true, // reset velocity before jump?
                    characterBody.GroundingUp // if resetting velocity,  provide up direction
                );
        }

        private void HandleAirMovement(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext kinematicContext,
            ref KinematicCharacterBody characterBody,
            in CharacterSettings characterSettings,
            float3 airAcceleration,
            float deltaTime
        ) {
            float3 tmpVelocity = characterBody.RelativeVelocity;
            // Thanks again, unity
            CharacterControlUtilities.StandardAirMove(
                ref characterBody.RelativeVelocity,
                airAcceleration,
                characterSettings.AirMaxSpeed,
                characterBody.GroundingUp,
                deltaTime,
                false // clamp velocity magnitude to max value?
            );

            // Cancel air acceleration from input if character would hit a non-grounded surface
            // (prevents air-climbing slopes/walls)
            if (characterSettings.PreventAirAccelerationAgainstUngroundedHits
                && KinematicCharacter.MovementWouldHitNonGroundedObstruction( // d(*-* ')b
                    in this, // Kinematic Character Processor
                    ref foxgloveContext,
                    ref kinematicContext,
                    characterBody.RelativeVelocity * deltaTime,
                    out ColliderCastHit _ // hit isn't used so discard it
                )
            ) characterBody.RelativeVelocity = tmpVelocity;
        }

        /// <summary>
        /// Counterpart to <see cref="PhysicsUpdate" />, called every frame for each character.
        /// </summary>
        public void FrameUpdate(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext kinematicContext
        ) {
            CharacterController characterController = CharacterControl.ValueRO;
            CharacterSettings characterSettings = CharacterSettings.ValueRO;
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref quaternion characterRotation = ref KinematicCharacter.LocalTransform.ValueRW.Rotation;

            // Add rotation from parent body to the character rotation
            // (this allows a rotating moving platform to rotate riding characters as well, and handle interpolation properly)
            // Thanks, Unity
            KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(
                ref characterRotation, // Rotation to modify
                characterBody.RotationFromParent, // Rotation amount
                kinematicContext.Time.DeltaTime,
                characterBody.LastPhysicsUpdateDeltaTime // time since last physics update
            );

            // Rotate towards move direction
            if (math.lengthsq(characterController.MoveVector) > 0f)
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(
                    ref characterRotation,
                    kinematicContext.Time.DeltaTime,
                    math.normalizesafe(characterController.MoveVector),
                    MathUtilities.GetUpFromRotation(characterRotation),
                    characterSettings.RotationSharpness
                );
        }

#region Character Processor Callbacks

        public void UpdateGroundingUp(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext
        ) {
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;

            KinematicCharacter.Default_UpdateGroundingUp(ref characterBody);
        }

        public bool CanCollideWithHit(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit
        ) => PhysicsUtilities.IsCollidable(hit.Material);

        public bool IsGroundedOnHit(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit,
            int groundingEvaluationType
        ) {
            CharacterSettings characterSettings = CharacterSettings.ValueRO;

            return KinematicCharacter.Default_IsGroundedOnHit(
                in this,
                ref context,
                ref baseContext,
                in hit,
                in characterSettings.StepAndSlopeHandling,
                groundingEvaluationType
            );
        }

        public void OnMovementHit(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref KinematicCharacterHit hit,
            ref float3 remainingMovementDirection,
            ref float remainingMovementLength,
            float3 originalVelocityDirection,
            float hitDistance
        ) {
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref float3 characterPosition = ref KinematicCharacter.LocalTransform.ValueRW.Position;
            CharacterSettings characterSettings = CharacterSettings.ValueRO;

            KinematicCharacter.Default_OnMovementHit(
                in this,
                ref context,
                ref baseContext,
                ref characterBody,
                ref characterPosition,
                ref hit,
                ref remainingMovementDirection,
                ref remainingMovementLength,
                originalVelocityDirection,
                hitDistance,
                characterSettings.StepAndSlopeHandling.StepHandling,
                characterSettings.StepAndSlopeHandling.MaxStepHeight,
                characterSettings.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck
            );
        }

        public void OverrideDynamicHitMasses(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref PhysicsMass characterMass,
            ref PhysicsMass otherMass,
            BasicHit hit
        ) {
            // Custom mass overrides
        }

        public void ProjectVelocityOnHits(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref float3 velocity,
            ref bool characterIsGrounded,
            ref BasicHit characterGroundHit,
            in DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits,
            float3 originalVelocityDirection
        ) {
            CharacterSettings characterSettings = CharacterSettings.ValueRO;

            KinematicCharacter.Default_ProjectVelocityOnHits(
                ref velocity,
                ref characterIsGrounded,
                ref characterGroundHit,
                in velocityProjectionHits,
                originalVelocityDirection,
                characterSettings.StepAndSlopeHandling.ConstrainVelocityToGroundPlane
            );
        }

#endregion
    }
}
