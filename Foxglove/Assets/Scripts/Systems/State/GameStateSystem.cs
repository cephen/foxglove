using System;
using Foxglove.Maps;
using Foxglove.State;
using Unity.Burst;
using Unity.Entities;
using Unity.Logging;
using Random = Unity.Mathematics.Random;

[assembly: RegisterGenericComponentType(typeof(CurrentState<GameState>))]
[assembly: RegisterGenericComponentType(typeof(NextState<GameState>))]

namespace Foxglove.State {
    public enum GameState : byte { Initialize, Generate, Play }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct GameStateSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tick>();
            state.EntityManager.AddComponentData<CurrentState<GameState>>(state.SystemHandle, GameState.Initialize);
            state.EntityManager.AddComponent<NextState<GameState>>(state.SystemHandle);
            state.EntityManager.AddComponent<ChangedStateAt>(state.SystemHandle);
        }

        public void OnUpdate(ref SystemState state) {
            CheckTransitions(ref state);
            RunStateUpdate(ref state);
        }

        public void OnDestroy(ref SystemState state) { }

        private void CheckTransitions(ref SystemState state) {
            // If no transition is queued, do nothing.
            if (!SystemAPI.HasComponent<NextState<GameState>>(state.SystemHandle)
                || !SystemAPI.IsComponentEnabled<NextState<GameState>>(state.SystemHandle)) return;

            GameState next = SystemAPI.GetComponent<NextState<GameState>>(state.SystemHandle);
            SetCurrentState(ref state, next);
        }

        private void RunStateUpdate(ref SystemState state) {
            if (!SystemAPI.HasComponent<CurrentState<GameState>>(state.SystemHandle)) return;

            GameState current = SystemAPI.GetComponent<CurrentState<GameState>>(state.SystemHandle);

            switch (current) {
                case GameState.Initialize:
                    Log.Info("[GameStateSystem] Initializing game");
                    SetNextState(ref state, GameState.Generate);
                    break;
                case GameState.Generate:
                    MapConfig config = new() {
                        Seed = new Random((uint)DateTimeOffset.UtcNow.GetHashCode()).NextUInt(),
                        Radius = 50,
                        MinRoomSize = 5,
                        MaxRoomSize = 10,
                        RoomsToGenerate = 30,
                    };

                    SystemHandle mapGenSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<MapGeneratorSystem>();
                    SystemAPI.SetComponent<ShouldGenerateMap>(mapGenSystem, config);
                    SystemAPI.SetComponentEnabled<ShouldGenerateMap>(mapGenSystem, true);

                    SetNextState(ref state, GameState.Play);
                    break;
                case GameState.Play:
                    // Something should definitely go here at some point
                    uint currentTick = SystemAPI.GetSingleton<Tick>();
                    uint changedAtTick = SystemAPI.GetComponent<ChangedStateAt>(state.SystemHandle);
                    if (currentTick - changedAtTick > 50)
                        SetNextState(ref state, GameState.Generate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetCurrentState(ref SystemState state, GameState current) {
            Log.Debug("[GameStateSystem] Setting current state to {0} State", current);
            if (!SystemAPI.HasComponent<CurrentState<GameState>>(state.SystemHandle))
                state.EntityManager.AddComponent<CurrentState<GameState>>(state.SystemHandle);

            SystemAPI.SetComponent<CurrentState<GameState>>(state.SystemHandle, current);
            SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, false);

            uint tick = SystemAPI.GetSingleton<Tick>();
            SystemAPI.SetComponent<ChangedStateAt>(state.SystemHandle, tick);
        }

        private void SetNextState(ref SystemState state, GameState next) {
            if (!SystemAPI.HasComponent<NextState<GameState>>(state.SystemHandle))
                state.EntityManager.AddComponent<NextState<GameState>>(state.SystemHandle);

            SystemAPI.SetComponent<NextState<GameState>>(state.SystemHandle, next);
            SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, true);
        }
    }
}
