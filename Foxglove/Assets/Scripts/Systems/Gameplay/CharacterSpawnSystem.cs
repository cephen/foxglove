using System;
using Foxglove.Camera;
using Foxglove.Character;
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
    internal sealed partial class CharacterSpawnSystem : SystemBase {
        private NativeQueue<SpawnCharacterEvent> _spawnQueue;
        private EventBinding<SpawnCharacterEvent> _spawnBinding;

        protected override void OnCreate() {
            // Dependencies
            RequireForUpdate<SpawnablePrefabs>();
            RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

            _spawnQueue = new NativeQueue<SpawnCharacterEvent>(Allocator.Persistent);

            _spawnBinding = new EventBinding<SpawnCharacterEvent>(OnSpawnCharacter);
            EventBus<SpawnCharacterEvent>.Register(_spawnBinding);
        }

        protected override void OnDestroy() {
            EventBus<SpawnCharacterEvent>.Deregister(_spawnBinding);
            _spawnQueue.Dispose();
        }

        private void OnSpawnCharacter(SpawnCharacterEvent e) => _spawnQueue.Enqueue(e);

        protected override void OnUpdate() {
            if (_spawnQueue.Count == 0) return;

            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            var prefabs = SystemAPI.GetSingleton<SpawnablePrefabs>();

            while (_spawnQueue.Count > 0) {
                SpawnCharacterEvent e = _spawnQueue.Dequeue();

                switch (e.Character) {
                    case SpawnableCharacter.Player:
                        SpawnPlayer(commands, e.Position);
                        continue;
                    case SpawnableCharacter.Wisp:
                        Entity wisp = commands.Instantiate(prefabs.WispPrefab);
                        commands.SetComponent(wisp, LocalTransform.FromPosition(e.Position));
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
