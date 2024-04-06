using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Foxglove.Maps.Jobs {
    [BurstCompile]
    internal struct GenerateRoomsJob : IJob {
        [ReadOnly] internal MapConfig Config;
        internal NativeList<Room> Rooms;

        private Random _random;

        [BurstCompile]
        public void Execute() {
            _random = new Random(Config.Seed);

            while (Rooms.Length < Config.RoomsToGenerate) {
                int2 position = _random.NextInt2(-Config.Radius, Config.Radius - Config.MaxRoomSize);
                int2 roomSize = _random.NextInt2(Config.MinRoomSize, Config.MaxRoomSize);

                Room proposed = new() {
                    Position = position,
                    Size = roomSize,
                };
                Room padding = new() {
                    Position = position - 1,
                    Size = roomSize + 2,
                };

                var add = true;

                foreach (Room room in Rooms) {
                    // Discard proposed room if it overlaps existing room
                    if (!Intersects(room, padding)) continue;
                    add = false;
                    break;
                }

                // Discard proposed room if out of bounds
                if (position.x + roomSize.x > Config.Radius
                    || position.y + roomSize.y > Config.Radius) add = false;

                if (!add) continue;

                // Add to room buffer
                Rooms.Add(proposed);
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
