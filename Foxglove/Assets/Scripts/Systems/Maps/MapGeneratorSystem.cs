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
        FilterEdges,
        PathfindHallways,
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

            state.EntityManager.AddComponent<GenerateMapRequest>(state.SystemHandle);
            state.EntityManager.SetComponentEnabled<GenerateMapRequest>(state.SystemHandle, false);

            StateMachine.Init(state, GeneratorState.Idle);

            SpawnMapRoot(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (StateMachine.IsTransitionQueued<GeneratorState>(state)) Transition(ref state);
            HandleStateUpdate(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        public void Transition(ref SystemState ecs) {
            GeneratorState current = StateMachine.GetState<GeneratorState>(ecs).Current;
            GeneratorState next = StateMachine.GetNextState<GeneratorState>(ecs).Value;

            SystemAPI.SetComponentEnabled<NextState<GeneratorState>>(ecs.SystemHandle, false);

            OnExit(ref ecs, current);
            OnEnter(ref ecs, next);
            StateMachine.SetState(ecs, next);
        }

        public void OnEnter(ref SystemState ecs, State<GeneratorState> systemState) {
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(ecs.WorldUnmanaged);

            var request = SystemAPI.GetComponent<GenerateMapRequest>(ecs.SystemHandle);

            switch (systemState.Current) {
                case GeneratorState.Idle:
                    Log.Debug("[MapGenerator] Idle");
                    return;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Initializing");

                    StateMachine.SetNextState(ecs, GeneratorState.PlaceRooms);

                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Placing Rooms");

                    // This job has no dependencies
                    // but should only run after this systems other dependencies are satisfied
                    ecs.Dependency = new GenerateRoomsJob {
                        Config = request.Config,
                        Rooms = commands.SetBuffer<Room>(_mapRoot),
                    }.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Building room graph");

                    DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(_mapRoot);

                    ecs.Dependency = new TriangulateMapJob {
                        Rooms = rooms.AsNativeArray().AsReadOnly(),
                        Edges = commands.SetBuffer<Edge>(_mapRoot),
                    }.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Filtering edges");

                    DynamicBuffer<Edge> edges = SystemAPI.GetBuffer<Edge>(_mapRoot);
                    if (edges.Length == 0) {
                        Log.Error("[MapGenerator] No edges found, map generation failed");
                        StateMachine.SetNextState(ecs, GeneratorState.Idle);
                        return;
                    }

                    var selectedEdges = new NativeList<Edge>(Allocator.TempJob);

                    JobHandle mstJob = new MinimumSpanningTreeJob {
                        Start = edges[0].A,
                        Edges = edges,
                        Results = selectedEdges,
                    }.Schedule(ecs.Dependency);

                    ecs.Dependency = new AddHallwaysJob {
                        Config = request.Config,
                        SelectedEdges = selectedEdges,
                    }.Schedule(mstJob);


                    return;
                case GeneratorState.PathfindHallways:
                    Log.Debug("[MapGenerator] Starting hallway optimization");
                    return;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Spawning map objects");

                    return;
                case GeneratorState.Cleanup:
                    Log.Debug("[MapGenerator] Cleaning up");
                    StateMachine.SetNextState(ecs, GeneratorState.Idle);
                    return;
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
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Done filtering edges");
                    break;
                case GeneratorState.PathfindHallways:
                    Log.Debug("[MapGenerator] Done pathfinding hallways");
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
#if UNITY_EDITOR
            // If rooms exist and this is an editor build
            // Draw debug lines for map components

            JobHandle drawRooms = new DrawRoomDebugLinesJob {
                DrawTime = SystemAPI.Time.DeltaTime,
                Color = Color.yellow,
            }.Schedule(ecs.Dependency);

            JobHandle drawEdges = new DrawEdgeDebugLinesJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Colour = Color.red,
            }.Schedule(drawRooms);

            ecs.Dependency = JobHandle.CombineDependencies(drawRooms, drawEdges);
#endif

            var request = SystemAPI.GetComponent<GenerateMapRequest>(ecs.SystemHandle);

            switch (StateMachine.GetState<GeneratorState>(ecs).Current) {
                case GeneratorState.Idle:
                    if (!SystemAPI.IsComponentEnabled<GenerateMapRequest>(ecs.SystemHandle)) return;

                    // If the component is activated, a map
                    Log.Debug("[MapGenerator] Starting map generator with seed {seed}", request.Config.Seed);
                    StateMachine.SetNextState(ecs, GeneratorState.Initialize);

                    return;
                // Initialize is a one-shot state and all it's behaviour happens in OnEnter
                case GeneratorState.Initialize: return;
                case GeneratorState.PlaceRooms:
                    // wait for jobs to complete
                    if (!ecs.Dependency.IsCompleted) return;

                    // This job depends on the DynamicBuffer<Room> component on _mapRoot being filled by the GenerateRoomsJob
                    ecs.Dependency = new SetRoomCellsJob {
                        Config = request.Config,
                    }.Schedule(ecs.Dependency);

                    StateMachine.SetNextState(ecs, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ecs, GeneratorState.FilterEdges);
                    return;
                case GeneratorState.FilterEdges:
                    if (ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ecs, GeneratorState.PathfindHallways);
                    return;
                case GeneratorState.PathfindHallways:
                    if (ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ecs, GeneratorState.Spawning);
                    return;
                case GeneratorState.Spawning:
                    if (ecs.Dependency.IsCompleted)
                        StateMachine.SetNextState(ecs, GeneratorState.Cleanup);
                    return;
                case GeneratorState.Cleanup:
                    if (!ecs.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Cleaning up");
                    SystemAPI.SetComponentEnabled<GenerateMapRequest>(ecs.SystemHandle, false);

                    StateMachine.SetNextState(ecs, GeneratorState.Idle);
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
