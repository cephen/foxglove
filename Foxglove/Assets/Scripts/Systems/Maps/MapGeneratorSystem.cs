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
    internal partial struct MapGeneratorSystem : ISystem, IStateMachineSystem<GeneratorState> {
        private Entity _mapRoot;
        private bool _hasCells;
        private Random _random;

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
        public void OnCreate(ref SystemState state) {
            uint initialSeed = (uint)DateTimeOffset.UtcNow.GetHashCode();
            Log.Debug("[MapGenerator] Initial seed: {seed}", initialSeed);
            _random = new Random(initialSeed);

            state.RequireForUpdate<Tick>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MapTheme>();


            StateMachine.Init(state, GeneratorState.Idle);

            SpawnMapRoot(ref state);
        }

        /// <summary>
        /// Called once per frame by the ECS framework.
        /// Checks for state transitions, and calls the state update function
        /// </summary>
        public void OnUpdate(ref SystemState state) {
            if (StateMachine.IsTransitionQueued<GeneratorState>(state)) Transition(ref state);
            HandleStateUpdate(ref state);
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
                case GeneratorState.Idle: // Idle Update, wait for map generation to be requested
                    if (!SystemAPI.IsComponentEnabled<ShouldBuild>(_mapRoot)) return;

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
                        .SetBuffer<MapTile>(_mapRoot)
                        .CopyFrom(_setMapCells.Results);

                    StateMachine.SetNextState(ecsState, GeneratorState.Spawning);

                    return;
                case GeneratorState.Spawning:
                    // wait for spawning job
                    ecsState.Dependency.Complete();

                    StateMachine.SetNextState(ecsState, GeneratorState.Finished);

                    return;
                default:
                    return;
            }
        }

        [BurstCompile]
        private void SpawnMapRoot(ref SystemState state) {
            Log.Debug("[MapGenerator] Creating Map Root");
            _mapRoot = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(_mapRoot, "Map Root");

            state.EntityManager.AddBuffer<Room>(_mapRoot);
            state.EntityManager.AddBuffer<Edge>(_mapRoot);
            state.EntityManager.AddBuffer<MapTile>(_mapRoot);

            state.EntityManager.AddComponent<ShouldBuild>(_mapRoot);
            state.EntityManager.SetComponentEnabled<ShouldBuild>(_mapRoot, false);

            state.EntityManager.AddComponent<Map>(_mapRoot);
            state.EntityManager.AddComponent<MapConfig>(_mapRoot);
            state.EntityManager.AddComponentData(_mapRoot, LocalTransform.FromScale(1));
            state.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
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
                        Results = new NativeArray<MapTile>(cellCount, Allocator.Persistent),
                    };

                    Log.Debug("[MapGenerator] Scheduling SetMapCellsJob");
                    ecsState.Dependency = _setMapCells.Schedule(cellCount, 1024, ecsState.Dependency);

                    return;
                case GeneratorState.Spawning:
                    Log.Debug("[MapGenerator] Configuring SpawnMapCellsJob");

                    NativeArray<MapTile>.ReadOnly mapCells =
                        SystemAPI.GetBuffer<MapTile>(_mapRoot).AsNativeArray().AsReadOnly();
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

                    DynamicBuffer<Room> generatedRooms = SystemAPI.GetBuffer<Room>(_mapRoot);
                    int numRooms = generatedRooms.Length;

                    var availableRooms = new NativeHashSet<int>(numRooms, Allocator.Temp);
                    for (int i = 0; i < numRooms; i++) availableRooms.Add(i);

                    int playerRoomIndex = _random.NextInt(0, numRooms);
                    availableRooms.Remove(playerRoomIndex);
                    Room playerRoom = generatedRooms[playerRoomIndex];


                    Room? teleporterRoom = null;

                    while (availableRooms.Count > 0) {
                        // Pick a random room that hasn't been considered yet
                        int i = _random.NextInt(0, availableRooms.Count);
                        if (!availableRooms.Contains(i)) continue;
                        availableRooms.Remove(i);

                        // If the room is at least 25 units away from the player room, use it
                        Room prospect = generatedRooms[i];
                        if (math.distance(playerRoom.Center, prospect.Center) > 25f) {
                            teleporterRoom = prospect;
                            break;
                        }
                    }

                    availableRooms.Dispose();

                    if (teleporterRoom is null) {
                        Log.Error("[MapGenerator] Failed to place teleporter, restarting map generation");
                        StateMachine.SetNextState(ecsState, GeneratorState.Initialize);
                        return;
                    }

                    var playerSpawnPosition = new float3(playerRoom.Center.x, 1f, playerRoom.Center.y);

                    var teleporterSpawnPosition = new float3(
                        ((Room)teleporterRoom).Center.x,
                        0f,
                        ((Room)teleporterRoom).Center.y
                    );

                    Log.Debug("[MapGenerator] Player spawn position: " + playerSpawnPosition);
                    Log.Debug("[MapGenerator] Teleporter spawn position: " + teleporterSpawnPosition);


                    EventBus<MapReadyEvent>.Raise(
                        new MapReadyEvent {
                            PlayerLocation = playerSpawnPosition,
                            TeleporterLocation = teleporterSpawnPosition,
                        }
                    );

                    _generateRooms.Rooms.Dispose();
                    _triangulateMap.Edges.Dispose();
                    _filterEdges.Results.Dispose();
                    _setMapCells.Results.Dispose();

                    SystemAPI.SetComponentEnabled<ShouldBuild>(_mapRoot, false);

                    StateMachine.SetNextState(ecsState, GeneratorState.Idle);
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
                    commands.SetBuffer<MapTile>(_mapRoot);

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
