using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Input {
    [BurstCompile]
    public sealed partial class InputSettingsSystem : SystemBase {
        protected override void OnStartRunning() {
            EntityManager.CreateOrSetSingleton(
                new LookSensitivity {
                    Gamepad = 1f, Mouse = 0.3f,
                }
            );
        }

        protected override void OnUpdate() { }
    }
}
