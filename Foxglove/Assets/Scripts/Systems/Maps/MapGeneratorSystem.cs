using System;
using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Transforms;
#if UNITY_EDITOR
using Foxglove.Maps.Editor;
using UnityEngine;
#endif

namespace Foxglove.Maps {
    [BurstCompile]
    internal partial struct MapGeneratorSystem : ISystem {
        private NativeList<Room> _rooms;
        private NativeList<Edge> _edges;
        private NativeArray<CellType> _cellTypes;
        private State _currentState;
        private Entity _mapRoot;

        private enum State {
            Initialize,
            Idle,
            Generating,
            Spawning,
#if UNITY_EDITOR
            DrawDebug,
#endif
            Dispose,
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _currentState = State.Initialize;
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.EntityManager.AddComponent<ShouldGenerateMap>(state.SystemHandle);
            state.EntityManager.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            switch (_currentState) {
                case State.Initialize:
                    SpawnMapRoot(ref state);
                    _currentState = State.Idle;
                    break;
                case State.Idle:
                    if (!SystemAPI.IsComponentEnabled<ShouldGenerateMap>(state.SystemHandle)) return;
                    StartGeneration(ref state);
                    _currentState = State.Generating;
                    return;
                case State.Generating:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGeneratorSystem] Map generation complete");
                    _currentState = State.Spawning;
                    return;
                case State.Spawning:
                    Log.Debug("[MapGeneratorSystem] Spawning map");
                    SpawnMap(ref state);
                    _currentState = State.DrawDebug;
                    return;
#if UNITY_EDITOR
                case State.DrawDebug:
                    if (!state.Dependency.IsCompleted) return;

                    Log.Debug("[MapGeneratorSystem] Drawing debug lines");

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

                    _currentState = State.Dispose;
                    return;
#endif
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SpawnMapRoot(ref SystemState state) {
            _mapRoot = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(_mapRoot, "Map Root");
            state.EntityManager.AddComponent<Map>(_mapRoot);
            state.EntityManager.AddComponentData(_mapRoot, new LocalToWorld { Value = float4x4.identity });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        private void StartGeneration(ref SystemState state) {
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(state.SystemHandle);

            Log.Debug(
                "[MapGeneratorSystem] Generating map with diameter {diameter} and seed {seed}",
                config.Diameter,
                config.Seed
            );

            // Ensure map root has a world space transform
            if (!SystemAPI.HasComponent<LocalToWorld>(config.MapRoot))
                commands.AddComponent<LocalToWorld>(config.MapRoot);

            _cellTypes = new NativeArray<CellType>(config.Diameter * config.Diameter, Allocator.TempJob);
            _rooms = new NativeList<Room>(config.RoomsToGenerate, Allocator.TempJob);
            _edges = new NativeList<Edge>(Allocator.TempJob);

            // Configure Jobs
            PlaceRoomsJob placeRooms = new() {
                Config = config,
                Rooms = _rooms,
                Cells = _cellTypes,
            };

            TriangulateMapJob triangulate = new() {
                Rooms = _rooms,
                Edges = _edges,
            };

            // Schedule jobs
            state.Dependency = placeRooms.Schedule(state.Dependency);
            state.Dependency = triangulate.Schedule(state.Dependency);
            // Create Hallways
            // Set up flow fields

            SystemAPI.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);
        }

        private void SpawnMap(ref SystemState state) {
            MapConfig config = SystemAPI.GetComponent<ShouldGenerateMap>(state.SystemHandle);

            EntityCommandBuffer cmd = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (Room room in _rooms.AsReadOnly()) {
                Entity e = cmd.CreateEntity();
                cmd.AddComponent(e, room);
                cmd.AddComponent(e, LocalTransform.FromPosition(room.Position.x, 0f, room.Position.y));
                cmd.AddComponent(e, new Parent { Value = config.MapRoot });
            }

            state.Dependency.Complete();


            state.Dependency = new DrawRoomDebugLinesJob {
                DeltaTime = 10f,
                Colour = Color.yellow,
                Rooms = _rooms.AsArray(),
            }.Schedule(_rooms.Length, state.Dependency);
            state.Dependency = new DrawEdgeDebugLinesJob {
                DeltaTime = 10f,
                Colour = Color.red,
                Edges = _edges.AsArray(),
            }.Schedule(_edges.Length, state.Dependency);

            _rooms.Dispose(state.Dependency);
            _edges.Dispose(state.Dependency);
            _cellTypes.Dispose(state.Dependency);
        }
    }
}
