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
        public float RotationSharpness; // How quickly does this character rotate?
        public float GroundMaxSpeed;
        public float GroundedAcceleration;
        public float AirMaxSpeed;
        public float AirAcceleration;
        public float AirDrag;
        public float JumpForce;
        public float3 Gravity;

        // If true, the character cannot accelerate into slopes that are too steep to be considered ground
        public bool PreventAirAccelerationAgainstUngroundedHits;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;
    }
}
