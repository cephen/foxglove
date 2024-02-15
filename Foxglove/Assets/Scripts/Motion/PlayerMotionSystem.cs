using Foxglove.Characters;
using Foxglove.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Motion {
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerMotionSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<MotionSettings>();
            state.RequireForUpdate<TargetVelocity>();
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.HasSingleton<PlayerTag>()) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var player = SystemAPI.GetAspect<PhysicsCharacterAspect>(playerEntity);
            var input = SystemAPI.GetSingleton<InputState>();
            float3 targetVelocity = float3.zero;

            // Calculate target velocity
            targetVelocity.xz = input.Move * player.MotionSettings.ValueRO.MaxHorizontalSpeed;
            // Calculate orientation
            float heading = player.Heading;
            // Rotate target velocity
            quaternion rotation = quaternion.AxisAngle(math.up(), heading);
            targetVelocity = math.rotate(rotation, targetVelocity);

            // Accelerate
            player.TargetVelocity.ValueRW.Value = targetVelocity;
        }
    }
}
