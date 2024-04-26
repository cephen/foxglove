using Foxglove.Character;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = Foxglove.Character.CharacterController;

namespace Foxglove.Authoring.Character {
    /// <summary>
    /// Authoring component for configuring a character via the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class CharacterAuthoring : MonoBehaviour {
        public float RotationSharpness = 25f;
        public float GroundMaxSpeed = 10f;
        public float GroundedMovementSharpness = 15f;
        public float AirAcceleration = 50f;
        public float AirMaxSpeed = 10f;
        public float AirDrag;
        public float JumpSpeed = 10f;

        public bool PreventAirAccelerationAgainstUngroundedHits = true;

        public float3 Gravity = math.up() * -30f;

        // Physics properties of the character
        public AuthoringKinematicCharacterProperties CharacterProperties =
            AuthoringKinematicCharacterProperties.GetDefault();

        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling =
            BasicStepAndSlopeHandlingParameters.GetDefault();

        private sealed class Baker : Baker<CharacterAuthoring> {
            public override void Bake(CharacterAuthoring authoring) {
                KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.CharacterProperties);

                Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

                AddComponent(
                    entity,
                    new CharacterSettings {
                        RotationSharpness = authoring.RotationSharpness,
                        GroundMaxSpeed = authoring.GroundMaxSpeed,
                        GroundedAcceleration = authoring.GroundedMovementSharpness,
                        AirAcceleration = authoring.AirAcceleration,
                        AirMaxSpeed = authoring.AirMaxSpeed,
                        AirDrag = authoring.AirDrag,
                        JumpForce = authoring.JumpSpeed,
                        Gravity = authoring.Gravity,
                        PreventAirAccelerationAgainstUngroundedHits =
                            authoring.PreventAirAccelerationAgainstUngroundedHits,
                        StepAndSlopeHandling = authoring.StepAndSlopeHandling,
                    }
                );
                AddComponent<CharacterController>(entity);
            }
        }
    }
}
