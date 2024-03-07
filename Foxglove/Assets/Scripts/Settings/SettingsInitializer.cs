using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Settings {
    public struct SettingsStoreTag : IComponentData { }

    [BurstCompile]
    public sealed partial class SettingsInitializer : SystemBase {
        private Entity _singletonEntity;

        [BurstCompile]
        protected override void OnCreate() {
            if (SystemAPI.HasSingleton<SettingsStoreTag>()) return;

            _singletonEntity = EntityManager.CreateSingleton<SettingsStoreTag>();
            EntityManager.AddComponentData(
                _singletonEntity,
                new LookSensitivity {
                    Gamepad = 1f, Mouse = 0.3f,
                }
            );
        }

        protected override void OnUpdate() { }
    }
}
