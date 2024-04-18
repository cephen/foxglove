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
            int2 coords = Config.CoordsFromIndex(index);
            switch (Cells[index].Type) {
                case CellType.None when CountFilledNeighbours(coords) is 4:
                    SpawnTile(index, Theme.HallTile);
                    return;
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
        /// Counts the number of neighbour cells that are not CellType.None
        /// </summary>
        private readonly int CountFilledNeighbours(int2 coords) {
            NativeArray<int2> neighbours = NeighboursOf(coords);
            int filled = 0;

            foreach (int2 neighbour in neighbours) {
                // Skip out of bounds cells
                bool xInBounds = neighbour.x >= -Config.Radius && neighbour.x < Config.Radius;
                bool yInBounds = neighbour.y >= -Config.Radius && neighbour.y < Config.Radius;
                if (!(xInBounds && yInBounds)) continue;

                CellType neighbourType = Cells[Config.IndexFromCoords(neighbour)].Type;
                if (neighbourType is not CellType.None) filled++;
            }

            neighbours.Dispose();
            return filled;
        }

        /// <summary>
        /// Get an array of potential neighbour coordinates
        /// </summary>
        private readonly NativeArray<int2> NeighboursOf(in int2 position) {
            var array = new NativeArray<int2>(4, Allocator.Temp);

            array[0] = position + new int2(0, 1); // North
            array[1] = position + new int2(1, 0); // East
            array[2] = position + new int2(0, -1); // South
            array[3] = position + new int2(-1, 0); // West

            return array;
        }

        /// <summary>
        /// Spawns a wall if the cell has at least one neighbour
        /// </summary>
        private void TrySpawnWall(int index) {
            int2 coords = Config.CoordsFromIndex(index);
            if (CountFilledNeighbours(coords) is 0) return;
            SpawnTile(index, Theme.WallTile);
        }

        private void SpawnTile(int index, Entity prefab) {
            Entity entity = Commands.Instantiate(_threadIndex, prefab);
            Commands.AddComponent(_threadIndex, entity, new Parent { Value = MapRoot });
            Commands.AddComponent(_threadIndex, entity, LocalTransform.FromPosition(Config.PositionFromIndex(index)));
        }
    }
}
