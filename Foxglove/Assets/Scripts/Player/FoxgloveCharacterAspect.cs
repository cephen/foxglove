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
        public readonly KinematicCharacterAspect KinematicCharacter;
        public readonly RefRW<FoxgloveCharacterSettings> CharacterSettings;

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
