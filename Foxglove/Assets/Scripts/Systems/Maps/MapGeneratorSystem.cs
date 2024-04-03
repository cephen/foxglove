using System;
using Foxglove.Maps.Delaunay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Transforms;

namespace Foxglove.Maps {
    [BurstCompile]
    internal partial struct MapGeneratorSystem : ISystem {
        private NativeList<Room> _rooms;
        private NativeList<Edge> _edges;
        private NativeArray<CellType> _cellTypes;
        private State _generatorState;

        private enum State {
            Idle, Generating, Spawning,
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _generatorState = State.Idle;
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.EntityManager.AddComponent<ShouldGenerateMap>(state.SystemHandle);
            state.EntityManager.SetComponentEnabled<ShouldGenerateMap>(state.SystemHandle, false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            switch (_generatorState) {
                case State.Idle:
                    if (!SystemAPI.IsComponentEnabled<ShouldGenerateMap>(state.SystemHandle)) return;
                    StartGeneration(ref state);
                    _generatorState = State.Generating;
                    return;
                case State.Generating:
                    if (!state.Dependency.IsCompleted) {
                        Log.Debug("[MapGeneratorSystem] Map still generating");
                        return;
                    }

                    Log.Debug("[MapGeneratorSystem] Map generation complete");
                    _generatorState = State.Spawning;
                    return;
                case State.Spawning:
                    Log.Debug("[MapGeneratorSystem] Spawning map");
                    SpawnMap(ref state);
                    _generatorState = State.Idle;
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

            _rooms.Dispose();
            _cellTypes.Dispose();
        }
    }
}
