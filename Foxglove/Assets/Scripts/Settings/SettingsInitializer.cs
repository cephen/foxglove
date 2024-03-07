using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Settings {
    [BurstCompile]
    public sealed partial class SettingsInitializer : SystemBase {
        private Entity _singletonEntity;

        [BurstCompile]
        protected override void OnCreate() {
            EntityManager.CreateOrSetSingleton(
                new LookSensitivity {
                    Gamepad = 1f, Mouse = 0.3f,
                }
            );
        }

        protected override void OnUpdate() { }
    }
}
