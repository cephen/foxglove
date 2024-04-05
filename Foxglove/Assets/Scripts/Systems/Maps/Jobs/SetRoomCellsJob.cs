using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    [BurstCompile]
    internal partial struct SetRoomCellsJob : IJobEntity {
        internal MapConfig Config;

        [BurstCompile]
        private readonly void Execute(ref DynamicBuffer<MapCell> cells, in DynamicBuffer<Room> rooms) {
            foreach (Room room in rooms) {
                // Shift position northeast by one quadrant
                int2 position = room.Position + Config.Radius;

                // For each world unit grid square that is part of the room
                for (int x = position.x; x < position.x + room.Size.x; x++) {
                    for (int y = position.y; y < position.y + room.Size.y; y++) {
                        // Set the cell type to Room
                        int i = y + x * Config.Diameter;
                        cells[i] = new MapCell { Type = CellType.Room };
                    }
                }
            }
        }
    }
}
