using Foxglove.Camera.OrbitCamera;
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
            state.RequireForUpdate<ThirdPersonPlayer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var input = SystemAPI.GetSingleton<FoxgloveGameplayInput>();
            var sensitivity = SystemAPI.GetSingleton<LookSensitivity>();

            foreach (ThirdPersonPlayer player in SystemAPI.Query<ThirdPersonPlayer>().WithAll<Simulate>()) {
                if (!SystemAPI.HasComponent<OrbitCameraControl>(player.ControlledCamera)) continue;

                var cameraControl = SystemAPI.GetComponent<OrbitCameraControl>(player.ControlledCamera);

                cameraControl.FollowedCharacterEntity = player.ControlledCharacter;
                cameraControl.LookDegreesDelta = input.Aim.IsMouseAim switch {
                    true => input.Aim.Value * sensitivity.Mouse,
                    false => input.Aim.Value * sensitivity.Gamepad,
                };

                SystemAPI.SetComponent(player.ControlledCamera, cameraControl);
            }
        }
    }
}