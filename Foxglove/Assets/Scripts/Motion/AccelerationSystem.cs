using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Foxglove.Motion {
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsInitializeGroup))]
    public partial class AccelerationSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<MotionSettings>();
            RequireForUpdate<TargetVelocity>();
            RequireForUpdate<PhysicsVelocity>();
        }

        protected override void OnDestroy() { }

        [BurstCompile]
        protected override void OnUpdate() {
            Entities
                .ForEach((ref PhysicsVelocity currentVelocity,
                    in TargetVelocity targetVelocity, in MotionSettings settings) => {
                    float deltaTime = SystemAPI.Time.fixedDeltaTime;
                    float3 accelerated = currentVelocity.Linear
                                         + targetVelocity.Value * settings.AccelerationRate * deltaTime;
                    float3 direction = math.normalizesafe(accelerated);
                    float speed = math.min(math.length(accelerated), settings.MaxHorizontalSpeed);
                    currentVelocity.Linear = speed > math.EPSILON ? direction * speed : float3.zero;
                    currentVelocity.Angular = float3.zero;
                })
                .Run();
        }
    }
}
