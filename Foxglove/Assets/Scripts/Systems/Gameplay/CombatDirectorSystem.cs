using System;
using Foxglove.Character;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Maps;
using Foxglove.Player;
using SideFX.Events;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Foxglove.Gameplay {
    /// <summary>
    /// This system manages the spawning of enemies.
    /// It accumulates credits over time, which it can periodically spend to spawn enemies in the world.
    /// This approach is inspired by Risk of Rain 2's Director system
    /// </summary>
    [UpdateInGroup(typeof(CheckpointUpdateGroup))]
    public sealed partial class CombatDirectorSystem : SystemBase {
        private const float MinSpawnDistance = 10f;
        private const float MaxSpawnDistance = 25f;
        private const float CreditMultiplier = 0.75f;

        private EventBinding<GameReady> _startGameBinding;
        private EventBinding<PauseGame> _pauseBinding;
        private EventBinding<ResumeGame> _resumeBinding;

        private Random _rng;
        private float _credits;

        protected override void OnCreate() {
            _rng = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());
            Enabled = false;

            RequireForUpdate<State<GameState>>();
            RequireForUpdate<PlayerCharacterTag>();
            RequireForUpdate<Map>();

            // Initialize event bindings
            _startGameBinding = new EventBinding<GameReady>(OnStartGame);
            _pauseBinding = new EventBinding<PauseGame>(OnPause);
            _resumeBinding = new EventBinding<ResumeGame>(OnResume);

            // Register bindings
            EventBus<GameReady>.Register(_startGameBinding);
            EventBus<PauseGame>.Register(_pauseBinding);
            EventBus<ResumeGame>.Register(_resumeBinding);
        }

        protected override void OnDestroy() {
            EventBus<GameReady>.Deregister(_startGameBinding);
            EventBus<PauseGame>.Deregister(_pauseBinding);
            EventBus<ResumeGame>.Deregister(_resumeBinding);
        }

        protected override void OnUpdate() {
            GameState gameState = SystemAPI.GetSingleton<State<GameState>>().Current;
            if (gameState is not GameState.Playing or GameState.Paused) {}
                // Only run in playing state
                if (gameState is not GameState.Playing)
                    return;

            int difficulty = 4; // TODO: replace with current level number

            float creditsPerSecond = CreditMultiplier * (1 + 0.4f * difficulty);

            _credits += creditsPerSecond;

            if (_credits < 250) return;

            uint creaturesToSpawn = _rng.NextUInt(1, _credits / 50);

            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerCharacterTag>();
            float3 playerPosition = SystemAPI.GetComponentRO<LocalToWorld>(playerEntity).ValueRO.Position;

            Entity mapEntity = SystemAPI.GetSingletonEntity<Map>();
            MapConfig mapConfig = SystemAPI.GetComponentRO<MapConfig>(mapEntity).ValueRO;
            DynamicBuffer<MapTile> mapCells = SystemAPI.GetBuffer<MapTile>(mapEntity);

            uint spawnedCreatures = 0;
            while (spawnedCreatures < creaturesToSpawn) {
                float3 spawnPosition = playerPosition;
                float spawnDistance = math.lerp(MinSpawnDistance, MaxSpawnDistance, _rng.NextFloat());
                spawnPosition.xz += _rng.NextFloat2Direction() * spawnDistance;

                // Spawn wisps floating in the air
                spawnPosition.y = 1;

                int2 spawnCoords = mapConfig.CoordsFromPosition(spawnPosition);
                TileType tileType = mapCells[mapConfig.IndexFromCoords(spawnCoords)].Type;

                // make sure creature is spawned inside the dungeon
                if (tileType is TileType.None) continue;

                EventBus<SpawnRequest>.Raise(
                    new SpawnRequest {
                        Spawnable = Spawnable.Wisp,
                        Position = spawnPosition,
                    }
                );
                spawnedCreatures++;
                _credits -= 50;
            }
        }

        private void OnStartGame(GameReady e) {
            Log.Debug("[CombatDirector] Initializing");
            _credits = 0;
            Enabled = true;
        }

        private void OnPause(PauseGame e) {
            Log.Debug("[CombatDirector] Disabling spawners");
            Enabled = false;
        }

        private void OnResume(ResumeGame e) {
            Log.Debug("[CombatDirector] Enabling spawners");
            Enabled = true;
        }
    }
}
