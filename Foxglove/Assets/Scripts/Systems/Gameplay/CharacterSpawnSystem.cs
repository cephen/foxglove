using System;
using Foxglove.Character;
using SideFX.Events;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Gameplay {
    internal sealed partial class CharacterSpawnSystem : SystemBase {
        private NativeQueue<SpawnCharacterEvent> _spawnQueue;
        private EventBinding<SpawnCharacterEvent> _spawnBinding;

        protected override void OnCreate() {
            // Dependencies
            RequireForUpdate<CharacterPrefabStore>();
            RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

            _spawnBinding = new EventBinding<SpawnCharacterEvent>(OnSpawnCharacter);
            EventBus<SpawnCharacterEvent>.Register(_spawnBinding);
            _spawnQueue = new NativeQueue<SpawnCharacterEvent>(Allocator.Persistent);
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

            var prefabs = SystemAPI.GetSingleton<CharacterPrefabStore>();

            while (_spawnQueue.Count > 0) {
                SpawnCharacterEvent e = _spawnQueue.Dequeue();

                Entity character = e.Character switch {
                    SpawnableCharacter.Player => prefabs.PlayerPrefab,
                    SpawnableCharacter.Wisp => prefabs.WispPrefab,
                    _ => throw new ArgumentOutOfRangeException(),
                };

                Entity spawned = commands.Instantiate(character);
                commands.SetComponent(spawned, LocalTransform.FromPosition(e.Position));
            }
        }
    }
}
