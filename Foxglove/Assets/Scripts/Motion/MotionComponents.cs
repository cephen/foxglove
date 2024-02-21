using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Motion {
    [Serializable]
    public struct MotionSettings : IComponentData {
        public float MaxHorizontalSpeed;
        public float AccelerationRate;
    }

    public struct TargetVelocity : IComponentData {
        public float3 Value;
    }
}
