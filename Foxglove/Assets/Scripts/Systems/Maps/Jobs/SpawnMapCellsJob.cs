using System;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Maps.Jobs {
    [BurstCompile]
    public struct SpawnMapCellsJob : IJobParallelFor {
        [ReadOnly] internal Entity MapRoot;
        [ReadOnly] internal MapTheme Theme;
        [ReadOnly] internal MapConfig Config;
        internal NativeArray<MapCell>.ReadOnly Cells;

        internal EntityCommandBuffer.ParallelWriter Commands;

        [NativeSetThreadIndex] private int _threadIndex;

        public void Execute(int index) {
            switch (Cells[index].Type) {
                case CellType.None:
                    TrySpawnWall(index);
                    return;
                case CellType.Room:
                    SpawnTile(index, Theme.RoomTile);
                    return;
                case CellType.Hallway:
                    SpawnTile(index, Theme.HallTile);
                    return;
            }
        }

        /// <summary>
        /// Spawns a wall if any of the cell's neighbours is not CellType.None
        /// </summary>
        private void TrySpawnWall(int index) {
            int2 coords = Config.CoordsFromIndex(index);
            NativeArray<int2> neighbours = NeighboursOf(coords);

            foreach (int2 neighbour in neighbours) {
                bool xInBounds = neighbour.x >= -Config.Radius && neighbour.x < Config.Radius;
                bool yInBounds = neighbour.y >= -Config.Radius && neighbour.y < Config.Radius;
                if (!xInBounds || !yInBounds) continue;

                CellType neighbourType = Cells[Config.IndexFromCoords(neighbour)].Type;

                if (neighbourType is not CellType.None) {
                    // Walls don't need to be rotated bc they're cubes
                    LocalTransform transform = LocalTransform.FromPosition(Config.PositionFromIndex(index));

                    Entity entity = Commands.Instantiate(_threadIndex, Theme.WallTile);
                    Commands.AddComponent(_threadIndex, entity, new Parent { Value = MapRoot });
                    Commands.SetComponent(_threadIndex, entity, transform);
                    break;
                }
            }

            neighbours.Dispose();
        }

        private void SpawnTile(int index, Entity prefab) {
            // room and hall tiles are quads, and need to be rotated
            LocalTransform transform =
                LocalTransform
                    .FromPosition(Config.PositionFromIndex(index))
                    .WithRotation(quaternion.RotateX(math.radians(90)));

            Entity entity = Commands.Instantiate(_threadIndex, prefab);
            Commands.AddComponent(_threadIndex, entity, new Parent { Value = MapRoot });
            Commands.SetComponent(_threadIndex, entity, transform);

        }

        private readonly NativeArray<int2> NeighboursOf(in int2 position) {
            var array = new NativeArray<int2>(4, Allocator.Temp);

            array[0] = position + new int2(1, 0); // North
            array[1] = position + new int2(0, 1); // East
            array[2] = position + new int2(-1, 0); // South
            array[3] = position + new int2(0, -1); // West

            return array;
        }
    }
}
