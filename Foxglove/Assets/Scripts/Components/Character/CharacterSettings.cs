using System;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    /// <summary>
    /// Settings for character movement
    /// </summary>
    [Serializable]
    public struct CharacterSettings : IComponentData {
        public float RotationSharpness;
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float JumpSpeed;
        public float3 Gravity;
        public bool PreventAirAccelerationAgainstUngroundedHits;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;
    }
}
