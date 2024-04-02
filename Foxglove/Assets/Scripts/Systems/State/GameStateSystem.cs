using System;
using Foxglove.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Transforms;

[assembly: RegisterGenericComponentType(typeof(CurrentState<GameState>))]
[assembly: RegisterGenericComponentType(typeof(NextState<GameState>))]

namespace Foxglove.State {
    public enum GameState : byte { Initialize, Generate, Play }

    public struct CurrentLevel : IComponentData {
        public byte Value; // Lemme know if you get past level 255 :D
        public static implicit operator byte(CurrentLevel level) => level.Value;
        public static implicit operator CurrentLevel(byte level) => new() { Value = level };
    }

    internal struct LevelGenerationJobs : IComponentData {
        public PlaceRoomsJob PlaceRoomsJob;
    }

    internal struct LevelItems : IComponentData {
        public NativeArray<Room> Rooms;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GameStateSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<NextState<GameState>>();
            state.RequireForUpdate<RandomNumberGenerators>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            state.EntityManager.AddComponentData<NextState<GameState>>(state.SystemHandle, GameState.Initialize);
            SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, true);
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            CheckTransitions(ref state, ref ecb);
        }

        private void CheckTransitions(ref SystemState state, ref EntityCommandBuffer ecb) {
            Log.Debug("[GameStateSystem] Checking transitions");
            if (!SystemAPI.IsComponentEnabled<NextState<GameState>>(state.SystemHandle)) return;

            GameState next = SystemAPI.GetComponent<NextState<GameState>>(state.SystemHandle);

            switch (next) {
                case GameState.Initialize: // Set up required gameplay components & systems
                    Log.Debug("[GameStateSystem] Entering Initialize State");
                    state.EntityManager.AddComponentData<CurrentLevel>(state.SystemHandle, 1);
                    SetNextState(ref state, GameState.Generate);
                    break;
                case GameState.Generate: // Generate the level
                    Log.Debug("[GameStateSystem] Entering Generate State");

                    Log.Debug("[GameStateSystem] Spawning Level Root");
                    Entity levelRoot = ecb.CreateEntity();
                    ecb.SetName(levelRoot, "Level Root");
                    ecb.AddComponent<LevelRoot>(levelRoot);
                    ecb.AddComponent<LocalToWorld>(levelRoot);

                    ref RandomNumberGenerators generator =
                        ref SystemAPI.GetSingletonRW<RandomNumberGenerators>().ValueRW;

                    Log.Debug("[GameStateSystem] Configuring Room Placement Job");
                    var placeRoomsJob = new PlaceRoomsJob {
                        Generator = new Random(generator.Base.NextUInt()),
                        LevelRadius = 50,
                        RoomsToPlace = 30,
                        MinRoomSize = 3,
                        MaxRoomSize = 10,
                        Commands = ecb,
                        LevelRoot = levelRoot,
                    };

                    Log.Debug("[GameStateSystem] Scheduling PlaceRoomsJob");
                    state.Dependency = placeRoomsJob.Schedule(state.Dependency);

                    var levelJobs = new LevelGenerationJobs {
                        PlaceRoomsJob = placeRoomsJob,
                    };
                    state.EntityManager.AddComponentData(state.SystemHandle, levelJobs);

                    // Triangulate
                    // Create Hallways
                    // Simplify Hallways
                    SetCurrentState(ref state, GameState.Generate);
                    break;
                case GameState.Play:
                    Log.Debug("[GameStateSystem] Entering Play State");
                    SetCurrentState(ref state, GameState.Play);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleCurrentState(ref SystemState state) {
            GameState current = SystemAPI.GetComponent<CurrentState<GameState>>(state.SystemHandle);

            switch (current) {
                case GameState.Initialize:
                    // Unreachable
                    break;
                case GameState.Generate:
                    if (!state.Dependency.IsCompleted) return;
                    // Place calculated objects
                    var levelJobs = SystemAPI.GetComponent<LevelGenerationJobs>(state.SystemHandle);
                    levelJobs.PlaceRooms.Rooms.Dispose();

                    state.EntityManager.RemoveComponent<LevelGenerationJobs>(state.SystemHandle);
                    SetNextState(ref state, GameState.Play);
                    break;
                case GameState.Play:
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
        }

        private void SetNextState(ref SystemState state, GameState next) {
            Log.Debug("[GameStateSystem] Setting next state to {0} State", next);
            if (!SystemAPI.HasComponent<NextState<GameState>>(state.SystemHandle))
                state.EntityManager.AddComponent<NextState<GameState>>(state.SystemHandle);

            SystemAPI.SetComponent<NextState<GameState>>(state.SystemHandle, next);
            SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, true);
        }
    }
}
