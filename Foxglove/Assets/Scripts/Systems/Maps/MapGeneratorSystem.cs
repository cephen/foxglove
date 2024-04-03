using System;
using Foxglove.Maps.Delaunay;
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
    [BurstCompile]
    internal partial struct MapGeneratorSystem : ISystem {
        private const int MapSize = 100;
        private NativeList<Room> _rooms;
        private NativeList<Edge> _edges;
        private NativeArray<CellType> _cells;
        private State _currentState;
        private EntityArchetype _roomArchetype;
        private Entity _mapRoot;

        private enum State {
            Idle,
            Initialize,
            PlaceRooms,
            Triangulate,
            CreateHallways,
            OptimizeHallways,
            Spawning,
#if UNITY_EDITOR
            DrawDebug,
#endif
            Cleanup,
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            LoadArchetypes(ref state);
            SpawnMapRoot(ref state);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.EntityManager.AddComponent<ShouldGenerateMap>(state.SystemHandle);
            state.EntityManager.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);
            _rooms = new NativeList<Room>(Allocator.Persistent);
            _edges = new NativeList<Edge>(Allocator.Persistent);
            _cells = new NativeArray<CellType>(MapSize * MapSize, Allocator.Persistent);
            _currentState = State.Idle;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {
            _rooms.Dispose(state.Dependency);
            _edges.Dispose(state.Dependency);
            _cells.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(state.SystemHandle);

            switch (_currentState) {
                case State.Idle:
                    if (!SystemAPI.IsComponentEnabled<ShouldGenerateMap>(state.SystemHandle)) return;

                    Log.Debug("[MapGenerator] Starting map generator with seed {seed}", config.Seed);

                    _currentState = State.Initialize;
                    return;
                case State.Initialize:
                    Log.Debug("[MapGenerator] Initializing");

                    DespawnRooms(ref state);

                    _edges.Clear();
                    _rooms.Clear();
                    for (var i = 0; i < _cells.Length; i++)
                        _cells[i] = CellType.None;

                    _currentState = State.PlaceRooms;
                    return;
                case State.PlaceRooms:
                    Log.Debug("[MapGenerator] Placing rooms");
                    state.Dependency = new PlaceRoomsJob {
                        Config = config,
                        Rooms = _rooms,
                        Cells = _cells,
                    }.Schedule(state.Dependency);

                    _currentState = State.Triangulate;
                    return;
                case State.Triangulate:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Triangulating map");
                    state.Dependency = new TriangulateMapJob {
                        Rooms = _rooms,
                        Edges = _edges,
                    }.Schedule(state.Dependency);

                    _currentState = State.CreateHallways;
                    return;
                case State.CreateHallways:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Creating hallways");
                    // Schedule job to create hallways

                    _currentState = State.OptimizeHallways;
                    return;
                case State.OptimizeHallways:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Optimizing hallways");
                    // Schedule job to optimize hallways

                    _currentState = State.Spawning;
                    return;
                case State.Spawning:

                    Log.Debug("[MapGenerator] Spawning map");
                    SpawnMap(ref state);

                    _currentState = State.DrawDebug;
                    return;
#if UNITY_EDITOR
                case State.DrawDebug:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Drawing debug lines");

                    state.Dependency = new DrawRoomDebugLinesJob {
                        DeltaTime = 1f,
                        Colour = Color.yellow,
                        Rooms = _rooms.AsArray().AsReadOnly(),
                    }.Schedule(_rooms.Length, state.Dependency);

                    state.Dependency = new DrawEdgeDebugLinesJob {
                        DeltaTime = 1f,
                        Colour = Color.red,
                        Edges = _edges.AsArray().AsReadOnly(),
                    }.Schedule(_edges.Length, state.Dependency);

                    _currentState = State.Cleanup;
                    return;
#endif
                case State.Cleanup:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGenerator] Cleaning up");
                    SystemAPI.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);

                    _currentState = State.Idle;
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [BurstCompile]
        private void LoadArchetypes(ref SystemState state) {
            SystemHandle archetypeManager =
                state.WorldUnmanaged.GetExistingUnmanagedSystem<MapArchetypeInitializer>();
            var archetypes = SystemAPI.GetComponent<MapArchetypes>(archetypeManager);
            _roomArchetype = archetypes.Room;
        }

        [BurstCompile]
        private void SpawnMapRoot(ref SystemState state) {
            _mapRoot = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(_mapRoot, "Map Root");
            state.EntityManager.AddComponent<Map>(_mapRoot);
            state.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }

        [BurstCompile]
        private void SpawnMap(ref SystemState state) {
            EntityCommandBuffer cmd = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (Room room in _rooms) {
                Entity e = cmd.CreateEntity(_roomArchetype);
                cmd.SetComponent(e, room);
                cmd.SetComponent(e, LocalTransform.FromPosition(room.Position.x, 0f, room.Position.y));
                cmd.SetComponent(e, new Parent { Value = _mapRoot });
            }
        }

        [BurstCompile]
        private void DespawnRooms(ref SystemState state) {
            EntityCommandBuffer cmd = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            cmd.DestroyEntity(SystemAPI.QueryBuilder().WithAll<Room>().Build(), EntityQueryCaptureMode.AtRecord);
        }
    }
}
