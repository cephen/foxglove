using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Input {
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal partial struct InputSettingsSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.EntityManager.AddComponent<LookSensitivity>(state.SystemHandle);
            SystemAPI.SetComponent(state.SystemHandle, new LookSensitivity { Gamepad = 1f, Mouse = 0.3f });
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) { }
    }
}
