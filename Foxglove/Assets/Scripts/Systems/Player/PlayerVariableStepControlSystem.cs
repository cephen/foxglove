using Foxglove.Camera;
using Foxglove.Input;
using Foxglove.Settings;
using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Player {
    /// <summary>
    /// Apply inputs that need to be read at a variable rate
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PlayerVariableStepControlSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<FoxgloveGameplayInput>();
            state.RequireForUpdate<LookSensitivity>();
            state.RequireForUpdate<PlayerController>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var input = SystemAPI.GetSingleton<FoxgloveGameplayInput>();
            var sensitivity = SystemAPI.GetSingleton<LookSensitivity>();

            foreach (RefRO<PlayerController> player in
                SystemAPI.Query<RefRO<PlayerController>>().WithAll<Simulate>()) {
                Entity controlledCamera = player.ValueRO.ControlledCamera;
                Entity controlledCharacter = player.ValueRO.ControlledCharacter;
                if (!SystemAPI.HasComponent<OrbitCameraControl>(controlledCamera)) continue;

                var cameraControl = SystemAPI.GetComponent<OrbitCameraControl>(controlledCamera);

                cameraControl.FollowedCharacterEntity = controlledCharacter;
                cameraControl.LookDegreesDelta = input.Aim.IsMouseAim switch {
                    true => input.Aim.Value * sensitivity.Mouse,
                    false => input.Aim.Value * sensitivity.Gamepad,
                };

                SystemAPI.SetComponent(controlledCamera, cameraControl);
            }
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
