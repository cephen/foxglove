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

        /// <summary>
        /// Extract yaw rotation from rotation quaternion
        /// Source: https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Source_code_2
        /// </summary>
        public float Heading {
            get {
                float4 q = LocalTransform.ValueRO.Rotation.value;
                float siny_cosp = 2 * (q.w * q.z + q.x * q.y);
                float cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
                return math.atan2(siny_cosp, cosy_cosp);
            }
        }
    }
}
