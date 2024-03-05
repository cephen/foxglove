using System;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    [Serializable]
    public struct FoxgloveCharacterSettings : IComponentData {
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

    [Serializable]
    public struct FoxgloveCharacterControl : IComponentData {
        public float3 MoveVector;
        public bool Jump;
    }
}