using System;
using Foxglove.Maps.Delaunay;
using Foxglove.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;
#if UNITY_EDITOR
using Foxglove.Maps.Editor;
using UnityEngine;
#endif

namespace Foxglove.Maps {
    internal enum GeneratorState {
        Idle,
        Initialize,
        PlaceRooms,
        Triangulate,
        CreateHallways,
        OptimizeHallways,
        Spawning,
        Cleanup,
    }

    [BurstCompile]
    internal partial struct MapGeneratorSystem : ISystem, IStateMachineSystem<GeneratorState> {
        private NativeArray<CellType> _cells;
        private NativeList<Edge> _edges;
        private Entity _mapRoot;
        private EntityArchetype _roomArchetype;
        private NativeList<Room> _rooms;


        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tick>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            state.EntityManager.AddComponent<ShouldGenerateMap>(state.SystemHandle);
            state.EntityManager.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);

            StateMachine.Init(ref state, GeneratorState.Idle);

            SpawnMapRoot(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (StateMachine.IsTransitionQueued<GeneratorState>(ref state)) Transition(ref state);
            HandleStateUpdate(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        public void Transition(ref SystemState ecs) {
            SystemAPI.SetComponentEnabled<NextState<GeneratorState>>(ecs.SystemHandle, false);

            OnExit(ref ecs, StateMachine.GetState<GeneratorState>(ref ecs));
            StateMachine.SetState(ref ecs, StateMachine.GetNextState<GeneratorState>(ref ecs).Value);
            OnEnter(ref ecs, StateMachine.GetState<GeneratorState>(ref ecs));
        }

        public void OnEnter(ref SystemState ecs, State<GeneratorState> systemState) {
            MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(ecs.SystemHandle).Config;
            switch (systemState.Current) {
                case GeneratorState.Idle:
                    Log.Debug("[MapGenerator] Idle");
                    break;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Initializing");

                    SystemAPI.GetBuffer<Room>(_mapRoot).Clear();

                    _cells = new NativeArray<CellType>(config.Diameter * config.Diameter, Allocator.TempJob);
                    _rooms = new NativeList<Room>(Allocator.TempJob);
                    _edges = new NativeList<Edge>(Allocator.TempJob);

                    StateMachine.SetNextState(ref ecs, GeneratorState.PlaceRooms);
                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Starting room placement");

                    ecs.Dependency = new PlaceRoomsJob {
                        Config = config,
                        Rooms = _rooms,
                        Cells = _cells,
                    }.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Starting map triangulation");
                    DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(_mapRoot);

                    ecs.Dependency = new TriangulateMapJob {
                        Rooms = rooms.AsNativeArray().AsReadOnly(),
                        Edges = _edges,
                    }.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.CreateHallways:
                    Log.Debug("[MapGenerator] Starting hallway generation");
                    break;
                case GeneratorState.OptimizeHallways:
                    Log.Debug("[MapGenerator] Starting hallway optimization");
                    break;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Spawning map objects");
                    break;
                case GeneratorState.Cleanup:
                    Log.Debug("[MapGenerator] Cleaning up");
                    StateMachine.SetNextState(ref ecs, GeneratorState.Idle);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnExit(ref SystemState ecs, State<GeneratorState> state) {
            switch (state.Current) {
                case GeneratorState.Idle:
                    break;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Done initializing");
                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Done placing rooms");
                    _rooms.Dispose(ecs.Dependency);
                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Done triangulating map");
                    _edges.Dispose(ecs.Dependency);
                    _cells.Dispose(ecs.Dependency);
                    break;
                case GeneratorState.CreateHallways:
                    Log.Debug("[MapGenerator] Done creating hallways");
                    break;
                case GeneratorState.OptimizeHallways:
                    Log.Debug("[MapGenerator] Done optimizing hallways");
                    break;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Done spawning map objects");
                    break;
                case GeneratorState.Cleanup:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void HandleStateUpdate(ref SystemState state) {
            MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(state.SystemHandle).Config;

            switch (StateMachine.GetState<GeneratorState>(ref state).Current) {
                case GeneratorState.Idle:
                    // If rooms exist, draw debug lines for them
                    DynamicBuffer<Room> roomBuffer = SystemAPI.GetBuffer<Room>(_mapRoot);
                    if (roomBuffer.Length != 0)
                        state.Dependency = new DrawRoomDebugLinesJob {
                            DeltaTime = SystemAPI.Time.DeltaTime,
                            Colour = Color.yellow,
                            Rooms = roomBuffer.ToNativeArray(Allocator.TempJob).AsReadOnly(),
                        }.Schedule(roomBuffer.Length, state.Dependency);

                    if (!SystemAPI.IsComponentEnabled<ShouldGenerateMap>(state.SystemHandle)) return;

                    Log.Debug("[MapGenerator] Starting map generator with seed {seed}", config.Seed);
                    StateMachine.SetNextState(ref state, GeneratorState.Initialize);

                    return;
                case GeneratorState.Initialize:
                    // Initialize is a oneshot state and all it's behaviour happens in OnEnter
                    return;
                case GeneratorState.PlaceRooms:
                    if (!state.Dependency.IsCompleted) return; // wait for job to complete

                    AddRoomsToMap(ref state);
                    StateMachine.SetNextState(ref state, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (!state.Dependency.IsCompleted) return;

                    StateMachine.SetNextState(ref state, GeneratorState.CreateHallways);
                    return;
                case GeneratorState.CreateHallways:
                    if (!state.Dependency.IsCompleted) return;

                    StateMachine.SetNextState(ref state, GeneratorState.OptimizeHallways);
                    return;
                case GeneratorState.OptimizeHallways:
                    if (!state.Dependency.IsCompleted) return;

                    StateMachine.SetNextState(ref state, GeneratorState.Spawning);
                    return;
                case GeneratorState.Spawning:
                    if (!state.Dependency.IsCompleted) return;

                    StateMachine.SetNextState(ref state, GeneratorState.Cleanup);
                    return;
                case GeneratorState.Cleanup:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Cleaning up");
                    SystemAPI.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);

                    StateMachine.SetNextState(ref state, GeneratorState.Idle);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [BurstCompile]
        private void SpawnMapRoot(ref SystemState state) {
            _mapRoot = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(_mapRoot, "Map Root");
            state.EntityManager.AddBuffer<Room>(_mapRoot);
            state.EntityManager.AddBuffer<Edge>(_mapRoot);
            state.EntityManager.AddComponent<Map>(_mapRoot);
            state.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }

        [BurstCompile]
        private void AddRoomsToMap(ref SystemState state) {
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            DynamicBuffer<Room> roomBuffer = commands.SetBuffer<Room>(_mapRoot);
            foreach (Room room in _rooms) roomBuffer.Add(room);
        }
    }
}
