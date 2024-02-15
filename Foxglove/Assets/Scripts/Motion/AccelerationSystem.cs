using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Foxglove.Motion {
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsInitializeGroup))]
    public partial struct AccelerationSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<MotionSettings>();
            state.RequireForUpdate<TargetVelocity>();
            state.RequireForUpdate<PhysicsVelocity>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            float deltaTime = SystemAPI.Time.fixedDeltaTime;
            foreach (PhysicsCharacterAspect character in SystemAPI.Query<PhysicsCharacterAspect>()) {
                ref PhysicsVelocity current = ref character.PhysicsVelocity.ValueRW;
                float3 targetVelocity = character.TargetVelocity.ValueRO.Value;
                float3 newVelocity = current.Linear
                                     + targetVelocity * character.MotionSettings.ValueRO.AccelerationRate * deltaTime;

                float3 direction = math.normalizesafe(newVelocity);
                float speed = math.min(character.MotionSettings.ValueRO.MaxHorizontalSpeed, math.length(newVelocity));
                float3 finalVelocity = speed < math.EPSILON ? float3.zero : direction * speed;
                current.Linear = finalVelocity;
            }
        }
    }
}
