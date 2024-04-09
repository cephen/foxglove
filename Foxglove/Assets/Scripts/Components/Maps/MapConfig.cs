using Unity.Entities;

namespace Foxglove.Maps {
    public readonly struct MapConfig : IComponentData {
        public readonly uint Seed;
        public readonly int RoomsToGenerate;
        public readonly int MinRoomSize;
        public readonly int MaxRoomSize;
        public readonly int Radius;
        public int Diameter => Radius * 2;

        public MapConfig(uint seed, int rooms = 32, int minRoomSize = 3, int maxRoomSize = 10, int radius = 64) {
            Seed = seed;
            RoomsToGenerate = rooms;
            MinRoomSize = minRoomSize;
            MaxRoomSize = maxRoomSize;
            Radius = radius;
        }
    }
}
