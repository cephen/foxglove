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

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct MapGeneratorSystem : ISystem, IStateMachineSystem<GeneratorState> {
        private Entity _mapRoot;
        private MapConfig _mapConfig;
        private Random _random;

        private GenerateRoomsJob _generateRooms;
        private TriangulateMapJob _triangulateMap;
        private FilterEdgesJob _filterEdges;

        public void OnCreate(ref SystemState ecsState) {
            _random = new Random((uint)DateTimeOffset.UtcNow.GetHashCode());

            ecsState.RequireForUpdate<Tick>();
            ecsState.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            ecsState.EntityManager.AddComponent<GenerateMapRequest>(ecsState.SystemHandle);
            ecsState.EntityManager.SetComponentEnabled<GenerateMapRequest>(ecsState.SystemHandle, true);

            StateMachine.Init(ecsState, GeneratorState.Idle);

            SpawnMapRoot(ref ecsState);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState ecsState) {
            if (StateMachine.IsTransitionQueued<GeneratorState>(ecsState)) Transition(ref ecsState);
            HandleStateUpdate(ref ecsState);
        }

        /// <summary>
        /// Implementation required by ISystem, but there's nothing to do for this system.
        /// </summary>
        public void OnDestroy(ref SystemState state) { }

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

                    StateMachine.SetNextState(ecsState, GeneratorState.PlaceRooms);

                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Placing Rooms");

                    // Schedule room generation
                    // Rooms will be extracted later and stored in the map
                    _generateRooms = new GenerateRoomsJob {
                        Config = _mapConfig,
                        Rooms = new NativeList<Room>(Allocator.TempJob),
                    };

                    ecsState.Dependency = _generateRooms.Schedule(ecsState.Dependency);

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
                    ecsState.Dependency = _triangulateMap.Schedule(ecsState.Dependency);

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
        private void HandleStateUpdate(ref SystemState ecsState) {
            EntityCommandBuffer commands;

            State<GeneratorState> state = StateMachine.GetState<GeneratorState>(ecsState);

            switch (state.Current) {
                case GeneratorState.Idle:
                    uint now = SystemAPI.GetSingleton<Tick>();
                    uint enteredAt = state.EnteredAt;
                    uint ticksInState = now - enteredAt;

                    bool requested = SystemAPI.IsComponentEnabled<GenerateMapRequest>(ecsState.SystemHandle);

                    // If map generation specifically requested or if Idle for 10 seconds
                    if (requested || ticksInState > 500) StateMachine.SetNextState(ecsState, GeneratorState.Initialize);

                    return;
                case GeneratorState.Initialize:
                    // Initialize is a one-shot state and all it's behaviour happens in OnEnter
                    return;
                case GeneratorState.PlaceRooms:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for GenerateRoomsJob to complete

                    commands = CreateCommandBuffer(ref ecsState);
                    // this buffer will be attached to the _mapRoot entity at the end of the frame
                    // and will replace any existing DynamicBuffer<Room> component on that entity
                    commands.SetBuffer<Room>(_mapRoot).CopyFrom(_generateRooms.Rooms.AsArray());

                    StateMachine.SetNextState(ecsState, GeneratorState.Triangulate);

                    return;
                case GeneratorState.Triangulate:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for TriangulateMapJob to complete

                    commands = CreateCommandBuffer(ref ecsState);
                    // this buffer will be attached to the _mapRoot entity at the end of the frame
                    // and will replace any existing DynamicBuffer<Edge> component on that entity
                    commands.SetBuffer<Edge>(_mapRoot).CopyFrom(_triangulateMap.Edges.AsArray());

                    StateMachine.SetNextState(ecsState, GeneratorState.FilterEdges);

                    return;
                case GeneratorState.FilterEdges:
                    if (!ecsState.Dependency.IsCompleted) return; // wait for FilterEdgesJob to complete

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
        public void OnExit(ref SystemState ecsState, State<GeneratorState> fsmState) {
            switch (fsmState.Current) {
                case GeneratorState.Idle:
                    break;
                case GeneratorState.Initialize:
                    Log.Debug("[MapGenerator] Done initializing");
                    break;
                case GeneratorState.PlaceRooms:
                    Log.Debug("[MapGenerator] Done placing rooms");

                    // Clean up temporary data
                    _generateRooms.Rooms.Dispose(ecsState.Dependency);

                    break;
                case GeneratorState.Triangulate:
                    Log.Debug("[MapGenerator] Done building room graph");

                    // Clean up temporary data
                    _triangulateMap.Edges.Dispose(ecsState.Dependency);

                    return;
                case GeneratorState.FilterEdges:
                    Log.Debug("[MapGenerator] Done filtering edges");

                    // Clean up temporary data
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
            _mapRoot = ecsState.EntityManager.CreateEntity();
            ecsState.EntityManager.SetName(_mapRoot, "Map Root");

            ecsState.EntityManager.AddBuffer<Room>(_mapRoot);
            ecsState.EntityManager.AddBuffer<Edge>(_mapRoot);
            ecsState.EntityManager.AddBuffer<MapCell>(_mapRoot);

            ecsState.EntityManager.AddComponent<Map>(_mapRoot);
            ecsState.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }

        private EntityCommandBuffer CreateCommandBuffer(ref SystemState ecsState) =>
            SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(ecsState.WorldUnmanaged);
    }
}
