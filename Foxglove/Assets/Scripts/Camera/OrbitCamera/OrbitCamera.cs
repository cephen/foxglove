using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Camera.OrbitCamera {
    [Serializable]
    public struct OrbitCamera : IComponentData {
        public float RotationSpeed;
        public float MaxPitchAngle;
        public float MinPitchAngle;
        public bool RotateWithCharacterParent;

        public float MinDistance;
        public float MaxDistance;
        public float DistanceMovementSpeed;
        public float DistanceMovementSharpness;

        public float ObstructionRadius;
        public float ObstructionInnerSmoothingSharpness;
        public float ObstructionOuterSmoothingSharpness;
        public bool PreventFixedUpdateJitter;

        public float TargetDistance;
        public float SmoothedTargetDistance;
        public float ObstructedDistance;
        public float PitchAngle;
        public float3 PlanarForward;
    }

    [Serializable]
    public struct OrbitCameraControl : IComponentData {
        public float2 LookDegreesDelta;
        public Entity FollowedCharacterEntity;
    }

    [Serializable]
    public struct OrbitCameraIgnoredEntityBufferElement : IBufferElementData {
        public Entity Entity;
    }
}
