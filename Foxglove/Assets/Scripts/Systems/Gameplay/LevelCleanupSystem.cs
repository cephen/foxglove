using Foxglove.Agent;
using Foxglove.Maps;
using Foxglove.Player;
using SideFX.Events;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Gameplay {
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed partial class LevelCleanupSystem : SystemBase {
        private EntityQuery _levelEntities;
        private bool _shouldRun;
        private EventBinding<TeleporterTriggered> _teleporterTriggeredBinding;

        protected override void OnCreate() {
            RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

            _levelEntities = SystemAPI.QueryBuilder().WithAny<PlayerCharacterTag, Wisp, Teleporter>().Build();

            _teleporterTriggeredBinding = new EventBinding<TeleporterTriggered>(OnTeleporterTriggered);
            EventBus<TeleporterTriggered>.Register(_teleporterTriggeredBinding);
        }

        protected override void OnDestroy() => EventBus<TeleporterTriggered>.Deregister(_teleporterTriggeredBinding);

        protected override void OnUpdate() {
            if (!_shouldRun) return;

            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            commands.DestroyEntity(_levelEntities.ToEntityArray(Allocator.Temp));

            Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
            NativeArray<Entity> mapCells = SystemAPI.GetBuffer<Child>(mapEntity).Reinterpret<Entity>().AsNativeArray();
            commands.DestroyEntity(mapCells);

            _shouldRun = false;
        }

        private void OnTeleporterTriggered() => _shouldRun = true;
    }
}
