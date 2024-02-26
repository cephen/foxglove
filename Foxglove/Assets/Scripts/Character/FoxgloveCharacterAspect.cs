using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Foxglove.Character {
    // Aspects are used to query the ECS world for entities with a given set of components.
    // implementing IKinematicCharacterProcessor on this aspect allows centralised physics integration of any character
    public readonly partial struct FoxgloveCharacterAspect : IAspect,
        IKinematicCharacterProcessor<FoxgloveCharacterUpdateContext> {
        // having another aspect as a field means components from that aspect are required to satisfy this aspect.
        public readonly KinematicCharacterAspect KinematicCharacter;

        // These additional component types are needed to simulate character motion
        public readonly RefRW<FoxgloveCharacterSettings> CharacterSettings;
        public readonly RefRW<FoxgloveCharacterControl> CharacterControl;

        /// <summary>
        /// This method is like GameObject.FixedUpdate, and will process every entity that has the required components.
        /// Called by <see cref="CharacterPhysicsUpdateSystem.CharacterPhysicsUpdateJob" />
        /// </summary>
        /// <param name="foxgloveContext">Contains global game state</param>
        /// <param name="kinematicContext">Contains global physics state</param>
        public void PhysicsUpdate(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext kinematicContext
        ) {
            ref FoxgloveCharacterSettings characterSettings = ref CharacterSettings.ValueRW;
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref float3 characterPosition = ref KinematicCharacter.LocalTransform.ValueRW.Position;

#region First phase of default character update

            KinematicCharacter.Update_Initialize(
                in this,
                ref foxgloveContext,
                ref kinematicContext,
                ref characterBody,
                kinematicContext.Time.DeltaTime
            );
            KinematicCharacter.Update_ParentMovement(
                in this,
                ref foxgloveContext,
                ref kinematicContext,
                ref characterBody,
                ref characterPosition,
                characterBody.WasGroundedBeforeCharacterUpdate
            );
            KinematicCharacter.Update_Grounding(
                in this,
                ref foxgloveContext,
                ref kinematicContext,
                ref characterBody,
                ref characterPosition
            );

#endregion

            // Update desired character velocity after grounding was detected, but before doing additional processing that depends on velocity
            HandleVelocityControl(ref foxgloveContext, ref kinematicContext);

#region Second phase of default character update

            KinematicCharacter.Update_PreventGroundingFromFutureSlopeChange(
                in this,
                ref foxgloveContext,
                ref kinematicContext,
                ref characterBody,
                in characterSettings.StepAndSlopeHandling
            );
            KinematicCharacter.Update_GroundPushing(
                in this,
                ref foxgloveContext,
                ref kinematicContext,
                characterSettings.Gravity
            );
            KinematicCharacter.Update_MovementAndDecollisions(
                in this,
                ref foxgloveContext,
                ref kinematicContext,
                ref characterBody,
                ref characterPosition
            );
            KinematicCharacter.Update_MovingPlatformDetection(ref kinematicContext, ref characterBody);
            KinematicCharacter.Update_ParentMomentum(ref kinematicContext, ref characterBody);
            KinematicCharacter.Update_ProcessStatefulCharacterHits();

#endregion
        }

        /// <summary>
        /// Transforms a character's control input into a target velocity.
        /// Called between the first and second phases of the default character update in <see cref="PhysicsUpdate" />
        /// </summary>
        private void HandleVelocityControl(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext kinematicContext
        ) {
            float deltaTime = kinematicContext.Time.DeltaTime;
            FoxgloveCharacterSettings characterSettings = CharacterSettings.ValueRO;
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref FoxgloveCharacterControl characterControl = ref CharacterControl.ValueRW;

            // Rotate move input and velocity to take into account parent rotation
            if (characterBody.ParentEntity != Entity.Null) {
                characterControl.MoveVector =
                    math.rotate(characterBody.RotationFromParent, characterControl.MoveVector);
                characterBody.RelativeVelocity =
                    math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
            }

            if (characterBody.IsGrounded) {
                // Move on ground
                float3 targetVelocity = characterControl.MoveVector * characterSettings.GroundMaxSpeed;
                // CharacterControlUtilities comes from Unity.CharacterController,
                // and implements a bunch of functionality I was trying to do manually ;-;
                CharacterControlUtilities.StandardGroundMove_Interpolated(
                    ref characterBody.RelativeVelocity,
                    targetVelocity,
                    characterSettings.GroundedMovementSharpness,
                    deltaTime,
                    characterBody.GroundingUp,
                    characterBody.GroundHit.Normal
                );

                // Jump
                // The player doesn't have a jump button but this is here on the off chance I add NPCs that can
                if (characterControl.Jump)
                    CharacterControlUtilities.StandardJump(
                        ref characterBody,
                        characterBody.GroundingUp * characterSettings.JumpSpeed, // jump velocity
                        true, // reset velocity before jump
                        characterBody.GroundingUp // if resetting velocity,  provide up direction
                    );
            }
            else {
                // Move in air
                float3 airAcceleration = characterControl.MoveVector * characterSettings.AirAcceleration;
                if (math.lengthsq(airAcceleration) > math.EPSILON) {
                    float3 tmpVelocity = characterBody.RelativeVelocity;
                    CharacterControlUtilities.StandardAirMove(
                        ref characterBody.RelativeVelocity,
                        airAcceleration,
                        characterSettings.AirMaxSpeed,
                        characterBody.GroundingUp,
                        deltaTime,
                        false
                    );

                    // Cancel air acceleration from input if we would hit a non-grounded surface
                    // (prevents air-climbing slopes at high air accelerations)
                    if (
                        characterSettings.PreventAirAccelerationAgainstUngroundedHits
                        && KinematicCharacter.MovementWouldHitNonGroundedObstruction(
                            in this,
                            ref foxgloveContext,
                            ref kinematicContext,
                            characterBody.RelativeVelocity * deltaTime,
                            out ColliderCastHit hit
                        )
                    )
                        characterBody.RelativeVelocity = tmpVelocity;
                }

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(
                    ref characterBody.RelativeVelocity,
                    characterSettings.Gravity,
                    deltaTime
                );

                // Drag
                CharacterControlUtilities.ApplyDragToVelocity(
                    ref characterBody.RelativeVelocity,
                    deltaTime,
                    characterSettings.AirDrag
                );
            }
        }

        /// <summary>
        /// Counterpart to <see cref="PhysicsUpdate" />, called every frame for each character.
        /// </summary>
        public void FrameUpdate(
            ref FoxgloveCharacterUpdateContext foxgloveContext,
            ref KinematicCharacterUpdateContext kinematicContext
        ) {
            FoxgloveCharacterControl characterControl = CharacterControl.ValueRO;
            FoxgloveCharacterSettings characterSettings = CharacterSettings.ValueRO;
            ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
            ref quaternion characterRotation = ref KinematicCharacter.LocalTransform.ValueRW.Rotation;

            // Add rotation from parent body to the character rotation
            // (this allows a rotating moving platform to rotate riding characters as well, and handle interpolation properly)
            // Thanks, Unity
            KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(
                ref characterRotation, // Rotation to modify
                characterBody.RotationFromParent, // Modification amount
                kinematicContext.Time.DeltaTime,
                characterBody.LastPhysicsUpdateDeltaTime // time since last physics update
            );

            // Rotate towards move direction
            if (math.lengthsq(characterControl.MoveVector) > 0f)
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(
                    ref characterRotation,
                    kinematicContext.Time.DeltaTime,
                    math.normalizesafe(characterControl.MoveVector),
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
        ) {
            return PhysicsUtilities.IsCollidable(hit.Material);
        }

        public bool IsGroundedOnHit(
            ref FoxgloveCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit,
            int groundingEvaluationType
        ) {
            FoxgloveCharacterSettings characterSettings = CharacterSettings.ValueRO;

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
            FoxgloveCharacterSettings characterSettings = CharacterSettings.ValueRO;

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
            FoxgloveCharacterSettings characterSettings = CharacterSettings.ValueRO;

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