using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Foxglove.Motion {
    [Serializable]
    public struct MotionSettings : IComponentData {
        public float MaxHorizontalSpeed;
        public float AccelerationRate;
    }

    public struct TargetVelocity : IComponentData {
        public float3 Value;
    }

    public readonly partial struct PhysicsCharacterAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRO<MotionSettings> MotionSettings;
        public readonly RefRO<LocalTransform> LocalTransform;
        public readonly RefRW<PhysicsVelocity> PhysicsVelocity;
        public readonly RefRW<TargetVelocity> TargetVelocity;
    }
}
