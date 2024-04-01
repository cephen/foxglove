using System;
using Foxglove.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;

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
        public PlaceRoomsJob PlaceRooms;
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

            state.EntityManager.AddComponentData<NextState<GameState>>(state.SystemHandle, GameState.Initialize);
            SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, true);
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            // Check Transitions
            CheckTransitions(ref state);
            // Perform State actions
        }

        private void CheckTransitions(ref SystemState state) {
            // If no transition queued, exit early.
            if (!SystemAPI.IsComponentEnabled<NextState<GameState>>(state.SystemHandle)) return;

            GameState next = SystemAPI.GetComponent<NextState<GameState>>(state.SystemHandle);

            switch (next) {
                case GameState.Initialize: // Set up required gameplay components & systems
                    Log.Debug("[GameStateSystem] Entering Initialize State");
                    state.EntityManager.AddComponentData<CurrentLevel>(state.SystemHandle, 1);
                    SystemAPI.SetComponent<NextState<GameState>>(state.SystemHandle, GameState.Generate);
                    break;
                case GameState.Generate: // Generate the level
                    Log.Debug("[GameStateSystem] Entering Generate State");

                    var rooms = new NativeArray<Room>(15, Allocator.Persistent);

                    var placeRooms = new PlaceRoomsJob {
                        Generator = SystemAPI.GetSingleton<RandomNumberGenerators>().Base,
                        LevelRadius = 50,
                        MinRoomSize = 3,
                        MaxRoomSize = 8,
                        Rooms = new NativeList<Room>(15, Allocator.TempJob),
                    };

                    var levelJobs = new LevelGenerationJobs { PlaceRooms = placeRooms };
                    state.EntityManager.AddComponentData(state.SystemHandle, levelJobs);
                    state.Dependency = placeRooms.Schedule(state.Dependency);

                    // Triangulate
                    // Create Hallways
                    // Simplify Hallways
                    SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, false);
                    break;
                case GameState.Play:
                    Log.Debug("[GameStateSystem] Entering Play State");
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
                    break;
                case GameState.Play:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TransitionTo(ref SystemState state, GameState nextState) {
            if (!SystemAPI.HasComponent<CurrentState<GameState>>(state.SystemHandle))
                state.EntityManager.AddComponent<CurrentState<GameState>>(state.SystemHandle);

            SystemAPI.SetComponent<NextState<GameState>>(state.SystemHandle, nextState);
            SystemAPI.SetComponentEnabled<NextState<GameState>>(state.SystemHandle, false);
        }
    }
}
