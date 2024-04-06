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
using Random = Unity.Mathematics.Random;

namespace Foxglove.Maps {
    internal enum GeneratorState {
        Idle,
        Initialize,
        PlaceRooms,
        Triangulate,
        FilterEdges,
        PlaceHallways,
        Spawning,
        Cleanup,
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct MapGeneratorSystem : ISystem, IStateMachineSystem<GeneratorState> {
        private Entity _mapRoot;
        private MapConfig _mapConfig;
        private Random _random;

        private GenerateRoomsJob _generateRooms;
        private TriangulateMapJob _triangulateMap;
        private FilterEdgesJob _filterEdges;

        public void OnCreate(ref SystemState state) {
            _random = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            state.RequireForUpdate<Tick>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            state.EntityManager.AddComponent<GenerateMapRequest>(state.SystemHandle);
            state.EntityManager.SetComponentEnabled<GenerateMapRequest>(state.SystemHandle, true);

            StateMachine.Init(state, GeneratorState.Idle);

            SpawnMapRoot(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (StateMachine.IsTransitionQueued<GeneratorState>(state)) Transition(ref state);
            HandleStateUpdate(ref state);
        }

        /// <summary>
        /// Implementation required by ISystem, but there's nothing to do for this system.
        /// </summary>
        public void OnDestroy(ref SystemState state) { }

        public void Transition(ref SystemState ecs) {
            GeneratorState current = StateMachine.GetState<GeneratorState>(ecs).Current;
            GeneratorState next = StateMachine.GetNextState<GeneratorState>(ecs).Value;

            SystemAPI.SetComponentEnabled<NextState<GeneratorState>>(ecs.SystemHandle, false);

            OnExit(ref ecs, current);
            OnEnter(ref ecs, next);
            StateMachine.SetState(ecs, next);
        }

        /// <summary>
        /// Called when transitioning into a state
        /// Used to set up temporary buffers and schedule jobs
        /// </summary>
        public void OnEnter(ref SystemState ecs, State<GeneratorState> systemState) {
            switch (systemState.Current) {
                case GeneratorState.Idle:
                    Log.Debug("[MapGenerator] Idle");
                    return;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Initializing");

                    uint seed = _random.NextUInt();
                    _mapConfig = new MapConfig(seed);

                    Log.Debug("[MapGenerator] Generating map with seed {seed}", seed);

                    StateMachine.SetNextState(ecs, GeneratorState.PlaceRooms);

                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Placing Rooms");

                    // Schedule room generation
                    // Rooms will be extracted later and stored in the map
                    _generateRooms = new GenerateRoomsJob {
                        Config = _mapConfig,
                        Rooms = new NativeList<Room>(Allocator.TempJob),
                    };

                    ecs.Dependency = _generateRooms.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Building room graph");

                    // Get generated rooms
                    DynamicBuffer<Room> rooms = SystemAPI.GetBuffer<Room>(_mapRoot);

                    // Schedule triangulation
                    // Edges will be extracted later and stored in the map
                    _triangulateMap = new TriangulateMapJob {
                        Rooms = rooms.AsNativeArray().AsReadOnly(),
                        Edges = new NativeList<Edge>(Allocator.TempJob),
                    };
                    ecs.Dependency = _triangulateMap.Schedule(ecs.Dependency);

                    break;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Filtering edges");

                    // Get generated edges
                    DynamicBuffer<Edge> edges = SystemAPI.GetBuffer<Edge>(_mapRoot);

                    // Schedule job to calculate minimum required edges to connect all rooms
                    // The buffer of edges already attached to the map will be replaced with the output of this job
                    _filterEdges = new FilterEdgesJob {
                        Start = edges.ElementAt(0).A,
                        Edges = edges.AsNativeArray().AsReadOnly(),
                        Results = new NativeList<Edge>(Allocator.TempJob),
                    };

                    ecs.Dependency = _filterEdges.Schedule(ecs.Dependency);

                    return;
                case GeneratorState.PlaceHallways:
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

        /// <summary>
        /// Called every frame, used to wait for generation jobs to complete
        /// When a state's given jobs are complete, this function extracts job output
        /// and stores it in the map
        /// </summary>
        private void HandleStateUpdate(ref SystemState ecs) {
            EntityCommandBuffer commands;

            State<GeneratorState> state = StateMachine.GetState<GeneratorState>(ecs);

            switch (state.Current) {
                case GeneratorState.Idle:
                    uint now = SystemAPI.GetSingleton<Tick>();
                    uint enteredAt = state.EnteredAt;
                    uint ticksInState = now - enteredAt;

                    bool requested = SystemAPI.IsComponentEnabled<GenerateMapRequest>(ecs.SystemHandle);

                    // If map generation specifically requested or if Idle for 10 seconds
                    if (requested || ticksInState > 500) StateMachine.SetNextState(ecs, GeneratorState.Initialize);

                    return;
                case GeneratorState.Initialize:
                    // Initialize is a one-shot state and all it's behaviour happens in OnEnter
                    return;
                case GeneratorState.PlaceRooms:
                    if (!ecs.Dependency.IsCompleted) return; // wait for GenerateRoomsJob to complete

                    commands = CreateCommandBuffer(ref ecs);
                    // this buffer will be attached to the _mapRoot entity at the end of the frame
                    // and will replace any existing DynamicBuffer<Room> component on that entity
                    commands.SetBuffer<Room>(_mapRoot).CopyFrom(_generateRooms.Rooms.AsArray());

                    StateMachine.SetNextState(ecs, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (!ecs.Dependency.IsCompleted) return; // wait for TriangulateMapJob to complete

                    commands = CreateCommandBuffer(ref ecs);
                    // this buffer will be attached to the _mapRoot entity at the end of the frame
                    // and will replace any existing DynamicBuffer<Edge> component on that entity
                    commands.SetBuffer<Edge>(_mapRoot).CopyFrom(_triangulateMap.Edges.AsArray());

                    StateMachine.SetNextState(ecs, GeneratorState.FilterEdges);

                    return;
                case GeneratorState.FilterEdges:
                    if (!ecs.Dependency.IsCompleted) return; // wait for FilterEdgesJob to complete

                    // Add some edges back
                    DynamicBuffer<Edge> allEdges = SystemAPI.GetBuffer<Edge>(_mapRoot);
                    NativeList<Edge> selectedEdges = _filterEdges.Results;

                    NativeHashSet<Edge> remainingEdges = new(allEdges.Length, Allocator.Temp);
                    foreach (Edge edge in allEdges) remainingEdges.Add(edge);

                    remainingEdges.ExceptWith(selectedEdges.AsArray()); // remove selected edges

                    // add 12.5% of remaining edges back
                    foreach (Edge edge in remainingEdges) {
                        if (_random.NextDouble() < 0.125)
                            selectedEdges.Add(edge);
                    }


                    allEdges.Clear();
                    allEdges.CopyFrom(selectedEdges.AsArray());
                    remainingEdges.Dispose();

                    StateMachine.SetNextState(ecs, GeneratorState.PlaceHallways);

                    return;
                case GeneratorState.PlaceHallways:
                    if (!ecs.Dependency.IsCompleted) return; // wait for PlaceHallwaysJob to complete

                    StateMachine.SetNextState(ecs, GeneratorState.Spawning);

                    return;
                case GeneratorState.Spawning:
                    if (!ecs.Dependency.IsCompleted) return; // wait for SpawnMapObjectsJob to complete

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

        /// <summary>
        /// Called when transitioning out of a state
        /// Used to deallocate temporary buffers
        /// </summary>
        public void OnExit(ref SystemState ecs, State<GeneratorState> state) {
            switch (state.Current) {
                case GeneratorState.Idle:
                    break;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Done initializing");
                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Done placing rooms");

                    // Clean up temporary data
                    _generateRooms.Rooms.Dispose(ecs.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Done building room graph");

                    // Clean up temporary data
                    _triangulateMap.Edges.Dispose(ecs.Dependency);

                    return;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Done filtering edges");

                    // Clean up temporary data
                    _filterEdges.Results.Dispose(ecs.Dependency);

                    break;
                case GeneratorState.PlaceHallways:
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

        private EntityCommandBuffer CreateCommandBuffer(ref SystemState ecs) =>
            SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(ecs.WorldUnmanaged);
    }
}
