using System;
using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Maps.Delaunay;
using Foxglove.Maps.Jobs;
using SideFX.Events;
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
        SetMapCells,
        Spawning,
        Finished,
        Despawn,
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
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal sealed partial class MapGeneratorSystem : SystemBase, IStateMachineSystem<GeneratorState> {
        private Entity _mapRoot;
        private bool _hasCells;
        private Random _random;

        private EventBinding<BuildMapEvent> _buildMapBinding;

        // These jobs are used to incrementally build the map over several frames.
        // Each state in the state machine will be responsible for one of these jobs
        // (not all states are implemented yet though)
        private GenerateRoomsJob _generateRooms;
        private TriangulateMapJob _triangulateMap;
        private FilterEdgesJob _filterEdges;
        private SetMapCells _setMapCells;
        private SpawnMapCellsJob _spawnMapCells;

        /// <summary>
        /// Called by the ECS framework when the system is created.
        /// Used to define data dependencies, and to add components to the system.
        /// </summary>
        protected override void OnCreate() {
            uint initialSeed = (uint)DateTimeOffset.UtcNow.GetHashCode();
            Log.Debug("[MapGenerator] Initial seed: {seed}", initialSeed);
            _random = new Random(initialSeed);

            RequireForUpdate<Tick>();
            RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            RequireForUpdate<MapTheme>();

            EntityManager.AddComponent<GenerateMapRequest>(SystemHandle);
            EntityManager.SetComponentEnabled<GenerateMapRequest>(SystemHandle, false);

            StateMachine.Init(CheckedStateRef, GeneratorState.Idle);

            SpawnMapRoot();

            _buildMapBinding = new EventBinding<BuildMapEvent>(OnBuildMapRequest);
            EventBus<BuildMapEvent>.Register(_buildMapBinding);
        }

        protected override void OnDestroy() {
            EventBus<BuildMapEvent>.Deregister(_buildMapBinding);
        }

        /// <summary>
        /// Called once per frame by the ECS framework.
        /// Checks for state transitions, and calls the state update function
        /// </summary>
        protected override void OnUpdate() {
            if (StateMachine.IsTransitionQueued<GeneratorState>(CheckedStateRef)) Transition(ref CheckedStateRef);
            HandleStateUpdate(ref CheckedStateRef);
        }

        private void OnBuildMapRequest() => SystemAPI.SetComponentEnabled<GenerateMapRequest>(SystemHandle, true);

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
                    bool requested = SystemAPI.IsComponentEnabled<GenerateMapRequest>(ecsState.SystemHandle);
                    if (!requested) return;

                    Log.Debug("[MapGenerator] Scheduling map generation");
                    StateMachine.SetNextState(ecsState, GeneratorState.Initialize);

                    return;
                case GeneratorState.Initialize:
                    // Initialize is a one-shot state and all it's behaviour happens in OnEnter
                    return;
                case GeneratorState.PlaceRooms:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for GenerateRoomsJob to complete

                    Log.Debug("[MapGenerator] GenerateRoomsJob finished, extracting rooms");
                    ecsState.Dependency.Complete();
                    CreateCommandBuffer(ref ecsState)
                        .SetBuffer<Room>(_mapRoot)
                        .CopyFrom(_generateRooms.Rooms.AsArray());

                    Log.Debug("[MapGenerator] Transitioning to Triangulate State");
                    StateMachine.SetNextState(ecsState, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for TriangulateMapJob to complete

                    Log.Debug("[MapGenerator] TriangulateMapJob finished, extracting edges");
                    // Copy generated edges into persistent map buffer
                    CreateCommandBuffer(ref ecsState)
                        .SetBuffer<Edge>(_mapRoot)
                        .CopyFrom(_triangulateMap.Edges.AsArray());

                    Log.Debug("[MapGenerator] Transitioning to FilterEdges State");
                    StateMachine.SetNextState(ecsState, GeneratorState.FilterEdges);

                    return;
                case GeneratorState.FilterEdges:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for FilterEdgesJob to complete

                    Log.Debug("[MapGenerator] FilterEdgesJob finished, extracting edges");
                    CreateCommandBuffer(ref ecsState)
                        .SetBuffer<Edge>(_mapRoot)
                        .CopyFrom(_filterEdges.Results.AsArray());

                    Log.Debug("[MapGenerator] Transitioning to PlaceHallways State");
                    StateMachine.SetNextState(ecsState, GeneratorState.SetMapCells);

                    return;
                case GeneratorState.SetMapCells:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for PlaceHallwaysJob to complete

                    Log.Debug("[MapGenerator] SetMapCellsJob finished, extracting cells");
                    CreateCommandBuffer(ref ecsState)
                        .SetBuffer<MapCell>(_mapRoot)
                        .CopyFrom(_setMapCells.Results);

                    StateMachine.SetNextState(ecsState, GeneratorState.Spawning);

                    return;
                case GeneratorState.Spawning:
                    // wait for spawning job
                    ecsState.Dependency.Complete();

                    StateMachine.SetNextState(ecsState, GeneratorState.Finished);

                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [BurstCompile]
        private void SpawnMapRoot() {
            Log.Debug("[MapGenerator] Creating Map Root");
            _mapRoot = EntityManager.CreateEntity();
            EntityManager.SetName(_mapRoot, "Map Root");

            EntityManager.AddBuffer<Room>(_mapRoot);
            EntityManager.AddBuffer<Edge>(_mapRoot);
            EntityManager.AddBuffer<MapCell>(_mapRoot);

            EntityManager.AddComponent<Map>(_mapRoot);
            EntityManager.AddComponent<MapConfig>(_mapRoot);
            EntityManager.AddComponentData(_mapRoot, LocalTransform.FromScale(1));
            EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }

        private EntityCommandBuffer CreateCommandBuffer(ref SystemState ecsState) =>
            SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(ecsState.WorldUnmanaged);

#region IStateMachineSystem implementation

        public void Transition(ref SystemState ecsState) {
            GeneratorState current = StateMachine.GetState<GeneratorState>(ecsState).Current;
            GeneratorState next = StateMachine.GetNextState<GeneratorState>(ecsState).Value;

            SystemAPI.SetComponentEnabled<NextState<GeneratorState>>(ecsState.SystemHandle, false);

            OnExit(ref ecsState, current);
            OnEnter(ref ecsState, next);
            StateMachine.SetState(ecsState, next);
        }

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
                    ecsState.EntityManager.SetComponentData(_mapRoot, new MapConfig(seed));

                    if (_hasCells) StateMachine.SetNextState(ecsState, GeneratorState.Despawn);

                    Log.Debug("[MapGenerator] Generating map with seed {seed}", seed);
                    StateMachine.SetNextState(ecsState, GeneratorState.PlaceRooms);

                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Configuring GenerateRoomsJob");
                    _generateRooms = new GenerateRoomsJob {
                        Config = SystemAPI.GetComponent<MapConfig>(_mapRoot),
                        Rooms = new NativeList<Room>(Allocator.Persistent),
                    };

                    Log.Debug("[MapGenerator] Scheduling GenerateRoomsJob");
                    ecsState.Dependency = _generateRooms.Schedule(ecsState.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Building room graph");

                    Log.Debug("[MapGenerator] Configuring TriangulateMapJob");
                    _triangulateMap = new TriangulateMapJob {
                        Rooms = SystemAPI.GetBuffer<Room>(_mapRoot).AsNativeArray().AsReadOnly(),
                        Edges = new NativeList<Edge>(Allocator.Persistent),
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
                        Random = new Random(_random.NextUInt()),
                        Results = new NativeList<Edge>(Allocator.Persistent),
                    };

                    Log.Debug("[MapGenerator] Scheduling FilterEdgesJob");
                    ecsState.Dependency = _filterEdges.Schedule(ecsState.Dependency);

                    return;
                case GeneratorState.SetMapCells:
                    Log.Debug("[MapGenerator] Configuring SetMapCellsJob");
                    var config = SystemAPI.GetComponent<MapConfig>(_mapRoot);
                    int cellCount = config.Diameter * config.Diameter;

                    _setMapCells = new SetMapCells {
                        Config = config,
                        Rooms = SystemAPI.GetBuffer<Room>(_mapRoot).AsNativeArray().AsReadOnly(),
                        Hallways = SystemAPI.GetBuffer<Edge>(_mapRoot).AsNativeArray().AsReadOnly(),
                        Results = new NativeArray<MapCell>(cellCount, Allocator.Persistent),
                    };

                    Log.Debug("[MapGenerator] Scheduling SetMapCellsJob");
                    ecsState.Dependency = _setMapCells.Schedule(cellCount, 1024, ecsState.Dependency);

                    return;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Configuring SpawnMapCellsJob");

                    NativeArray<MapCell>.ReadOnly mapCells =
                        SystemAPI.GetBuffer<MapCell>(_mapRoot).AsNativeArray().AsReadOnly();
                    _spawnMapCells = new SpawnMapCellsJob {
                        MapRoot = _mapRoot,
                        Theme = SystemAPI.GetSingleton<MapTheme>(),
                        Config = SystemAPI.GetComponent<MapConfig>(_mapRoot),
                        Cells = mapCells,
                        Commands = CreateCommandBuffer(ref ecsState).AsParallelWriter(),
                    };

                    Log.Debug("[MapGenerator] Scheduling SpawnMapCellsJob");
                    ecsState.Dependency = _spawnMapCells.Schedule(mapCells.Length, 1024, ecsState.Dependency);

                    return;
                case GeneratorState.Finished:
                    Log.Debug("[MapGenerator] Finished Spawning map, disposing intermediate buffers");

                    _generateRooms.Rooms.Dispose();
                    _triangulateMap.Edges.Dispose();
                    _filterEdges.Results.Dispose();
                    _setMapCells.Results.Dispose();
                    return;
                case GeneratorState.Despawn:
                    if (!SystemAPI.HasBuffer<Child>(_mapRoot)) return;

                    EntityCommandBuffer commands = CreateCommandBuffer(ref ecsState);

                    NativeArray<Entity> children =
                        SystemAPI
                            .GetBuffer<Child>(_mapRoot)
                            .Reinterpret<Entity>()
                            .AsNativeArray();

                    if (children.Length > 0) {
                        Log.Debug("[MapGenerator] Despawning map cells");
                        commands.DestroyEntity(children);
                    }

                    Log.Debug("[MapGenerator] Clearing map buffers");
                    commands.SetBuffer<Room>(_mapRoot);
                    commands.SetBuffer<Edge>(_mapRoot);
                    commands.SetBuffer<MapCell>(_mapRoot);

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
                    return;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Initialized");
                    return;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Rooms placed");
                    return;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Built map graph");
                    return;
                case GeneratorState.SetMapCells:
                    Log.Debug("[MapGenerator] Map tiles configured");
                    return;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Map objects spawned");
                    return;
                case GeneratorState.Despawn:
                    Log.Debug("[MapGenerator] Done cleaning up");
                    return;
                default:
                    return;
            }
        }

#endregion
    }
}
