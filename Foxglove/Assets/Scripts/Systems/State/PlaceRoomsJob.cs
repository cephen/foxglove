using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.State {
    internal struct Room : IComponentData {
        public int2 Position;
        public int2 Size;
    }

    [BurstCompile]
    internal struct PlaceRoomsJob : IJob {
        public Random Generator;
        public ushort LevelRadius;
        public byte MinRoomSize;
        public byte MaxRoomSize;

        // Buffer should have a capacity equal to the number of rooms to generate
        public NativeList<Room> Rooms;

        [BurstCompile]
        public void Execute() {
            while (Rooms.Length < Rooms.Capacity) {
                int2 position = Generator.NextInt2(-LevelRadius, LevelRadius);
                int2 roomSize = Generator.NextInt2(MinRoomSize, MaxRoomSize);

                var proposed = new Room {
                    Position = position,
                    Size = roomSize,
                };

                var add = true;

                // try again if room overlaps existing room
                foreach (Room room in Rooms) {
                    if (!Intersects(proposed, room)) continue;
                    add = false;
                    break;
                }

                // try again if room is out of bounds
                if (position.x + roomSize.x > LevelRadius
                    || position.y + roomSize.y > LevelRadius) add = false;

                if (add) Rooms.Add(proposed);
            }
        }

        /// <summary>
        /// Tests if a room intersects another
        /// </summary>
        [BurstCompile]
        private static bool Intersects(in Room a, in Room b) =>
            !(a.Position.x >= b.Position.x + b.Size.x
              || a.Position.x + a.Size.x <= b.Position.x
              || a.Position.y >= b.Position.y + b.Size.y
              || a.Position.y + a.Size.y <= b.Position.y);
    }
}
