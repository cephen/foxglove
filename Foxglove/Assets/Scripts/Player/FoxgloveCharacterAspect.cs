using Foxglove.Player.Systems;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Foxglove.Player {
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
        /// Called every frame for each character.
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
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref characterRotation,
                    kinematicContext.Time.DeltaTime, math.normalizesafe(characterControl.MoveVector),
                    MathUtilities.GetUpFromRotation(characterRotation), characterSettings.RotationSharpness);
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
                groundingEvaluationType);
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
                characterSettings.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck);
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
                characterSettings.StepAndSlopeHandling.ConstrainVelocityToGroundPlane);
        }

#endregion
    }
}
