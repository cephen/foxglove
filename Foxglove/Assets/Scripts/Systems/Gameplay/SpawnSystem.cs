using System;
using Foxglove.Camera;
using Foxglove.Character;
using Foxglove.Maps;
using Foxglove.Player;
using SideFX.Events;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Gameplay {
    /// <summary>
    /// Spawns entities from prefabs.
    /// </summary>
    internal sealed partial class SpawnSystem : SystemBase {
        private NativeQueue<SpawnRequest> _spawnQueue;
        private EventBinding<SpawnRequest> _spawnBinding;

        protected override void OnCreate() {
            // Dependencies
            RequireForUpdate<SpawnablePrefabs>();
            RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();

            _spawnQueue = new NativeQueue<SpawnRequest>(Allocator.Persistent);

            _spawnBinding = new EventBinding<SpawnRequest>(OnSpawnCharacter);
            EventBus<SpawnRequest>.Register(_spawnBinding);
        }

        protected override void OnDestroy() {
            EventBus<SpawnRequest>.Deregister(_spawnBinding);
            _spawnQueue.Dispose();
        }

        private void OnSpawnCharacter(SpawnRequest e) => _spawnQueue.Enqueue(e);

        protected override void OnUpdate() {
            if (_spawnQueue.Count == 0) return;

            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            var prefabs = SystemAPI.GetSingleton<SpawnablePrefabs>();

            while (_spawnQueue.Count > 0) {
                SpawnRequest e = _spawnQueue.Dequeue();

                switch (e.Spawnable) {
                    case Spawnable.Player:
                        SpawnPlayer(commands, e.Position);
                        continue;
                    case Spawnable.Wisp:
                        Entity wisp = commands.Instantiate(prefabs.WispPrefab);
                        commands.SetComponent(wisp, LocalTransform.FromPosition(e.Position));
                        continue;
                    case Spawnable.Teleporter:
                        Entity mapRoot = SystemAPI.GetSingletonEntity<Map>();
                        Entity teleporter = commands.Instantiate(prefabs.TeleporterPrefab);
                        commands.AddComponent(teleporter, new Parent { Value = mapRoot });
                        commands.SetComponent(teleporter, LocalTransform.FromPosition(e.Position));
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SpawnPlayer(EntityCommandBuffer commands, float3 position) {
            var prefabStore = SystemAPI.GetSingleton<SpawnablePrefabs>();

            Entity player = commands.Instantiate(prefabStore.PlayerPrefab);
            commands.SetComponent(player, LocalTransform.FromPosition(position));

            Entity camera = SystemAPI.HasSingleton<MainCameraTag>()
                                ? SystemAPI.GetSingletonEntity<MainCameraTag>()
                                : commands.Instantiate(prefabStore.OrbitCamera);

            // Set up player controller
            if (SystemAPI.HasSingleton<PlayerController>()) {
                Entity controller = SystemAPI.GetSingletonEntity<PlayerController>();
                commands.SetComponent(
                    controller,
                    new PlayerController {
                        ControlledCamera = camera,
                        ControlledCharacter = player,
                    }
                );
            }
            else {
                Entity controller = commands.CreateEntity();
                commands.AddComponent(
                    controller,
                    new PlayerController {
                        ControlledCamera = camera,
                        ControlledCharacter = player,
                    }
                );
            }
        }
    }
}
