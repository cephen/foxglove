using System;
using Foxglove.Core.State;
using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Jobs;
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

    /// <summary>
    /// This system generates maps using an algorithm similar to the one used in TinyKeep.
    /// The algorithm is described here: https://www.reddit.com/r/gamedev/comments/1dlwc4
    /// ---
    /// I've made several modifications to this algorithm:
    /// - I'm generating far fewer rooms per map
    /// - I use the Uniform distribution instead of the Park-Miller normal distribution
    /// ---
    /// The system is implemented as a state machine, with a circular state flowchart.
    /// States are modeled with the GeneratorState enum, and the system flows through states in the order they are defined.
    /// Once generation is complete, and the system has finished cleaning up, it returns to the Idle state
    /// and waits for a new map generation request.
    /// ---
    /// For debugging purposes it also starts a new generation cycle after idling for 10 seconds.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct MapGeneratorSystem : ISystem, IStateMachineSystem<GeneratorState> {
        private Entity _mapRoot;
        private MapConfig _mapConfig;
        private Random _random;

        // These jobs are used to incrementally build the map over several frames.
        // Each state in the state machine will be responsible for one of these jobs
        // (not all states are implemented yet though)
        private GenerateRoomsJob _generateRooms;
        private TriangulateMapJob _triangulateMap;
        private FilterEdgesJob _filterEdges;

        /// <summary>
        /// Called by the ECS framework when the system is created.
        /// Used to define data dependencies, and to add components to the system.
        /// </summary>
        public void OnCreate(ref SystemState ecsState) {
            var initialSeed = (uint)DateTimeOffset.UtcNow.GetHashCode();
            Log.Debug("[MapGenerator] Initial seed: {seed}", initialSeed);
            _random = new Random(initialSeed);

            ecsState.RequireForUpdate<Tick>();

            ecsState.EntityManager.AddComponent<GenerateMapRequest>(ecsState.SystemHandle);
            ecsState.EntityManager.SetComponentEnabled<GenerateMapRequest>(ecsState.SystemHandle, true);

            StateMachine.Init(ecsState, GeneratorState.Idle);

            SpawnMapRoot(ref ecsState);
        }

        /// <summary>
        /// Called once per frame by the ECS framework.
        /// Checks for state transitions, and calls the state update function
        /// </summary>
        /// <param name="ecsState"></param>
        public void OnUpdate(ref SystemState ecsState) {
            if (StateMachine.IsTransitionQueued<GeneratorState>(ecsState))
                this.Transition<MapGeneratorSystem, GeneratorState>(ref ecsState);
            HandleStateUpdate(ref ecsState);
        }

        /// <summary>
        /// Implementation required by ISystem, but there's nothing to do for this system.
        /// </summary>
        public void OnDestroy(ref SystemState state) { }

        /// <summary>
        /// Called when transitioning into a state
        /// Used to set up temporary buffers and schedule jobs
        /// </summary>
        [BurstCompile]
        public void OnEnter(ref SystemState ecsState, State<GeneratorState> fsmState) {
            switch (fsmState.Current) {
                case GeneratorState.Idle:
                    Log.Debug("[MapGenerator] Idle");
                    return;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Initializing");

                    uint seed = _random.NextUInt();
                    _mapConfig = new MapConfig(seed);

                    Log.Debug("[MapGenerator] Generating map with seed {seed}", seed);

                    Log.Debug("[MapGenerator] Clearing map buffers");
                    SystemAPI.GetBuffer<Room>(_mapRoot).Clear();
                    SystemAPI.GetBuffer<Edge>(_mapRoot).Clear();
                    SystemAPI.GetBuffer<MapCell>(_mapRoot).Clear();

                    Log.Debug("[MapGenerator] Transitioning to PlaceRooms State");
                    StateMachine.SetNextState(ecsState, GeneratorState.PlaceRooms);

                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Configuring GenerateRoomsJob");
                    _generateRooms = new GenerateRoomsJob {
                        Config = _mapConfig,
                        Rooms = new NativeList<Room>(Allocator.TempJob),
                    };

                    Log.Debug("[MapGenerator] Scheduling GenerateRoomsJob");
                    ecsState.Dependency = _generateRooms.Schedule(ecsState.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Building room graph");

                    Log.Debug("[MapGenerator] Configuring TriangulateMapJob");
                    _triangulateMap = new TriangulateMapJob {
                        Rooms = SystemAPI.GetBuffer<Room>(_mapRoot).AsNativeArray().AsReadOnly(),
                        Edges = new NativeList<Edge>(Allocator.TempJob),
                    };

                    Log.Debug("[MapGenerator] Scheduling TriangulateMapJob");
                    ecsState.Dependency = _triangulateMap.Schedule(ecsState.Dependency);

                    break;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Configuring FilterEdgesJob");
                    DynamicBuffer<Edge> edges = SystemAPI.GetBuffer<Edge>(_mapRoot);
                    _filterEdges = new FilterEdgesJob {
                        Start = edges.ElementAt(0).A,
                        Edges = edges.AsNativeArray().AsReadOnly(),
                        Results = new NativeList<Edge>(Allocator.TempJob),
                    };

                    Log.Debug("[MapGenerator] Scheduling FilterEdgesJob");
                    ecsState.Dependency = _filterEdges.Schedule(ecsState.Dependency);

                    return;
                case GeneratorState.PlaceHallways:
                    Log.Debug("[MapGenerator] Starting hallway optimization");

                    return;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Spawning map objects");
                    return;
                case GeneratorState.Cleanup:
                    Log.Debug("[MapGenerator] Cleaning up");
                    StateMachine.SetNextState(ecsState, GeneratorState.Idle);
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
        [BurstCompile]
        private void HandleStateUpdate(ref SystemState ecsState) {
            State<GeneratorState> state = StateMachine.GetState<GeneratorState>(ecsState);

            switch (state.Current) {
                case GeneratorState.Idle:
                    uint now = SystemAPI.GetSingleton<Tick>();
                    uint enteredAt = state.EnteredAt;
                    uint ticksInState = now - enteredAt;

                    Log.Debug("[MapGenerator] Idle for {ticks} ticks", ticksInState);

                    bool requested = SystemAPI.IsComponentEnabled<GenerateMapRequest>(ecsState.SystemHandle);

                    // If map generation specifically requested or if Idle for 10 seconds
                    if (requested || ticksInState > 500) {
                        Log.Debug("[MapGenerator] Scheduling map generation");
                        StateMachine.SetNextState(ecsState, GeneratorState.Initialize);
                    }

                    return;
                case GeneratorState.Initialize:
                    // Initialize is a one-shot state and all it's behaviour happens in OnEnter
                    return;
                case GeneratorState.PlaceRooms:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for GenerateRoomsJob to complete

                    Log.Debug("[MapGenerator] GenerateRoomsJob finished, extracting rooms");
                    // The buffer stored in the job needs to be deallocated
                    // So copy the rooms into a persistent buffer stored on the map entity
                    SystemAPI.GetBuffer<Room>(_mapRoot).CopyFrom(_generateRooms.Rooms.AsArray());

                    Log.Debug("[MapGenerator] Transitioning to Triangulate State");
                    StateMachine.SetNextState(ecsState, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for TriangulateMapJob to complete

                    Log.Debug("[MapGenerator] TriangulateMapJob finished, extracting edges");
                    // Copy generated edges into persistent map buffer
                    SystemAPI.GetBuffer<Edge>(_mapRoot).CopyFrom(_triangulateMap.Edges.AsArray());

                    Log.Debug("[MapGenerator] Transitioning to FilterEdges State");
                    StateMachine.SetNextState(ecsState, GeneratorState.FilterEdges);

                    return;
                case GeneratorState.FilterEdges:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for FilterEdgesJob to complete

                    Log.Debug("[MapGenerator] FilterEdgesJob finished, extracting edges");
                    DynamicBuffer<Edge> allEdges = SystemAPI.GetBuffer<Edge>(_mapRoot);
                    NativeList<Edge> selectedEdges = _filterEdges.Results;

                    NativeHashSet<Edge> remainingEdges = new(allEdges.Length, Allocator.Temp);
                    foreach (Edge edge in allEdges) remainingEdges.Add(edge);

                    Log.Debug("[MapGenerator] Add additional connections");
                    remainingEdges.ExceptWith(selectedEdges.AsArray()); // remove selected edges
                    // add 12.5% of remaining edges back
                    foreach (Edge edge in remainingEdges) {
                        if (_random.NextDouble() < 0.125)
                            selectedEdges.Add(edge);
                    }

                    Log.Debug("[MapGenerator] Storing Edges in map buffer");
                    allEdges.Clear();
                    allEdges.CopyFrom(selectedEdges.AsArray());
                    remainingEdges.Dispose();

                    Log.Debug("[MapGenerator] Transitioning to PlaceHallways State");
                    StateMachine.SetNextState(ecsState, GeneratorState.PlaceHallways);

                    return;
                case GeneratorState.PlaceHallways:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for PlaceHallwaysJob to complete

                    StateMachine.SetNextState(ecsState, GeneratorState.Spawning);

                    return;
                case GeneratorState.Spawning:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for SpawnMapObjectsJob to complete

                    StateMachine.SetNextState(ecsState, GeneratorState.Cleanup);

                    return;
                case GeneratorState.Cleanup:
                    if (!ecsState.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Cleaning up");
                    SystemAPI.SetComponentEnabled<GenerateMapRequest>(ecsState.SystemHandle, false);

                    StateMachine.SetNextState(ecsState, GeneratorState.Idle);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Called when transitioning out of a state
        /// Used to deallocate temporary buffers
        /// </summary>
        [BurstCompile]
        public void OnExit(ref SystemState ecsState, State<GeneratorState> fsmState) {
            switch (fsmState.Current) {
                case GeneratorState.Idle:
                    break;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Done initializing");
                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Done placing rooms");

                    Log.Debug("[MapGenerator] Disposing GenerateRoomsJob buffers");
                    _generateRooms.Rooms.Dispose(ecsState.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Done building room graph");

                    Log.Debug("[MapGenerator] Disposing TriangulateMapJob buffers");
                    _triangulateMap.Edges.Dispose(ecsState.Dependency);

                    return;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Done filtering edges");

                    Log.Debug("[MapGenerator] Disposing FilterEdgesJob buffers");
                    _filterEdges.Results.Dispose(ecsState.Dependency);

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
        private void SpawnMapRoot(ref SystemState ecsState) {
            Log.Debug("[MapGenerator] Creating Map Root");
            _mapRoot = ecsState.EntityManager.CreateEntity();
            ecsState.EntityManager.SetName(_mapRoot, "Map Root");

            ecsState.EntityManager.AddBuffer<Room>(_mapRoot);
            ecsState.EntityManager.AddBuffer<Edge>(_mapRoot);
            ecsState.EntityManager.AddBuffer<MapCell>(_mapRoot);

            ecsState.EntityManager.AddComponent<Map>(_mapRoot);
            ecsState.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }
    }
}
