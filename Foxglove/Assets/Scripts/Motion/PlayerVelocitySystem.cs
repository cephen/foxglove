using Foxglove.Characters;
using Foxglove.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Motion {
    [BurstCompile]
    [UpdateAfter(typeof(PlayerOrientationSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerVelocitySystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<InputState>();
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<MotionSettings>();
            RequireForUpdate<Heading>();
            RequireForUpdate<TargetVelocity>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            var input = SystemAPI.GetSingleton<InputState>();

            Entities
                .WithAll<PlayerTag>()
                .ForEach((ref TargetVelocity targetVelocity, in Heading heading, in MotionSettings settings) => {
                    float3 forward = math.forward() * input.Move.y;
                    float3 right = math.right() * input.Move.x;
                    float3 totalMove = (forward + right) * settings.MaxHorizontalSpeed;

                    quaternion rotation = quaternion.RotateY(heading.Radians);
                    targetVelocity.Value = math.rotate(rotation, totalMove);
                })
                .Run();
        }
    }
}
