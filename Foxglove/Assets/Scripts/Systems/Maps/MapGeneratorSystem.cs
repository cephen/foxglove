using System;
using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Jobs;
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
        private Entity _mapRoot;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tick>(); // How many ticks since the game started
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();

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
            GeneratorState current = StateMachine.GetState<GeneratorState>(ref ecs).Current;
            GeneratorState next = StateMachine.GetNextState<GeneratorState>(ref ecs).Value;

            SystemAPI.SetComponentEnabled<NextState<GeneratorState>>(ecs.SystemHandle, false);

            OnExit(ref ecs, current);
            OnEnter(ref ecs, next);
            StateMachine.SetState(ref ecs, next);
        }

        public void OnEnter(ref SystemState ecs, State<GeneratorState> systemState) {
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(ecs.WorldUnmanaged);

            MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(ecs.SystemHandle).Config;

            switch (systemState.Current) {
                case GeneratorState.Idle:
                    Log.Debug("[MapGenerator] Idle");
                    break;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Initializing");
                    StateMachine.SetNextState(ref ecs, GeneratorState.PlaceRooms);
                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Starting room placement");

                    ecs.Dependency = new PlaceRoomsJob {
                        Config = config,
                        Rooms = commands.SetBuffer<Room>(_mapRoot),
                        Cells = commands.SetBuffer<MapCell>(_mapRoot),
                    }.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Starting map triangulation");
                    DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(_mapRoot);

                    ecs.Dependency = new TriangulateMapJob {
                        Rooms = rooms.AsNativeArray().AsReadOnly(),
                        Edges = commands.SetBuffer<Edge>(_mapRoot),
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
                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Done triangulating map");
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


        private void HandleStateUpdate(ref SystemState ecs) {
            switch (StateMachine.GetState<GeneratorState>(ref ecs).Current) {
                case GeneratorState.Idle:
#if UNITY_EDITOR
                    // If rooms exist and this is an editor build
                    // Draw debug lines for map components

                    DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(_mapRoot);
                    JobHandle drawRooms = new DrawRoomDebugLinesJob {
                        DeltaTime = SystemAPI.Time.DeltaTime,
                        Colour = Color.yellow,
                        Rooms = rooms.ToNativeArray(Allocator.TempJob).AsReadOnly(),
                    }.Schedule(rooms.Length, ecs.Dependency);

                    DynamicBuffer<Edge> edges = SystemAPI.GetBuffer<Edge>(_mapRoot);
                    JobHandle drawEdges = new DrawEdgeDebugLinesJob {
                        DeltaTime = SystemAPI.Time.DeltaTime,
                        Colour = Color.red,
                        Edges = edges.ToNativeArray(Allocator.TempJob).AsReadOnly(),
                    }.Schedule(edges.Length, drawRooms);

                    ecs.Dependency = JobHandle.CombineDependencies(drawRooms, drawEdges);
#endif
                    if (!SystemAPI.IsComponentEnabled<ShouldGenerateMap>(ecs.SystemHandle)) return;

                    // If the component is activated, a map
                    MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(ecs.SystemHandle).Config;
                    Log.Debug("[MapGenerator] Starting map generator with seed {seed}", config.Seed);
                    StateMachine.SetNextState(ref ecs, GeneratorState.Initialize);

                    return;
                case GeneratorState.Initialize:
                    // Initialize is a one-shot state and all it's behaviour happens in OnEnter
                    return;
                case GeneratorState.PlaceRooms:
                    if (!ecs.Dependency.IsCompleted) return; // wait for jobs to complete

                    StateMachine.SetNextState(ref ecs, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ref ecs, GeneratorState.CreateHallways);

                    return;
                case GeneratorState.CreateHallways:
                    if (!ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ref ecs, GeneratorState.OptimizeHallways);

                    return;
                case GeneratorState.OptimizeHallways:
                    if (!ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ref ecs, GeneratorState.Spawning);

                    return;
                case GeneratorState.Spawning:
                    if (!ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ref ecs, GeneratorState.Cleanup);

                    return;
                case GeneratorState.Cleanup:
                    if (!ecs.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Cleaning up");
                    SystemAPI.SetComponentEnabled<ShouldGenerateMap>(ecs.SystemHandle, false);

                    StateMachine.SetNextState(ref ecs, GeneratorState.Idle);
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
            state.EntityManager.AddBuffer<MapCell>(_mapRoot);

            state.EntityManager.AddComponent<Map>(_mapRoot);
            state.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }
    }
}
