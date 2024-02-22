using Foxglove.Camera;
using Foxglove.Characters;
using Foxglove.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Motion {
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerOrientationSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<InputState>();
            RequireForUpdate<CameraPosition>();
            RequireForUpdate<CameraTarget>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            var input = SystemAPI.GetSingleton<InputState>();

            Entities
                .WithAll<PlayerTag>()
                .ForEach((ref Heading heading, in CameraPosition cameraPosition, in CameraTarget cameraTarget) => {
                    float3 lookDirection =
                        math.normalizesafe(cameraTarget.Value - cameraPosition.Value, math.forward());
                    float forwardDotLook = math.dot(math.forward(), lookDirection);
                    float multiplied = forwardDotLook / math.length(lookDirection);
                    heading.Radians = math.acos(multiplied);
                    // Log.Info("Look Direction: {Direction}, Dot Forward {DotF}, Player Heading: {HeadingDegrees}",
                    //     lookDirection, forwardDotLook, heading.Degrees);
                })
                .Run();
        }
    }
}
