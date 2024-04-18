using System;
using Foxglove.Character;
using Foxglove.Core;
using Foxglove.Maps;
using Foxglove.Player;
using SideFX.Events;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Gameplay {
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    public sealed partial class CombatDirectorSystem : SystemBase {
        private EventBinding<ToggleSpawnersEvent> _toggleSpawnersBinding;
        private uint _credits;
        private Random _rng;
        private const float MinSpawnDistance = 10f;
        private const float MaxSpawnDistance = 25f;

        protected override void OnCreate() {
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            _toggleSpawnersBinding = new EventBinding<ToggleSpawnersEvent>(OnToggleSpawners);
            EventBus<ToggleSpawnersEvent>.Register(_toggleSpawnersBinding);
            Enabled = false;
        }

        protected override void OnDestroy() {
            EventBus<ToggleSpawnersEvent>.Deregister(_toggleSpawnersBinding);
        }

        protected override void OnUpdate() {
            _credits += 25; // Gain 25 credits per second

            if (_credits < 250) return;

            uint creaturesToSpawn = _rng.NextUInt(1, _credits / 50);

            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerCharacterTag>();
            float3 playerPosition = SystemAPI.GetComponentRO<LocalToWorld>(playerEntity).ValueRO.Position;

            Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
            MapConfig mapConfig = SystemAPI.GetComponentRO<MapConfig>(mapEntity).ValueRO;
            DynamicBuffer<MapCell> mapCells = SystemAPI.GetBuffer<MapCell>(mapEntity);

            uint spawnedCreatures = 0;
            while (spawnedCreatures < creaturesToSpawn) {
                float3 spawnPosition = playerPosition;
                float spawnDistance = math.lerp(MinSpawnDistance, MaxSpawnDistance, _rng.NextFloat());
                spawnPosition.xz += _rng.NextFloat2Direction() * spawnDistance;

                // Spawn wisps floating in the air
                spawnPosition.y = 1;

                int2 spawnCoords = mapConfig.CoordsFromPosition(spawnPosition);
                CellType cellType = mapCells[mapConfig.IndexFromCoords(spawnCoords)].Type;

                // make sure creature is spawned inside the dungeon
                if (cellType is CellType.None) continue;

                EventBus<SpawnCharacterEvent>.Raise(
                    new SpawnCharacterEvent {
                        Character = SpawnableCharacter.Wisp,
                        Position = spawnPosition,
                    }
                );
                spawnedCreatures++;
                _credits -= 50;
            }
        }

        private void OnToggleSpawners(ToggleSpawnersEvent e) {
            Log.Debug(e.Enabled ? "[CombatDirector] Turning spawners on" : "[CombatDirector] Turning spawners off");
            Enabled = e.Enabled;
        }
    }
}
