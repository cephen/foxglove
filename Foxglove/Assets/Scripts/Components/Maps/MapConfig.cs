using Unity.Entities;

namespace Foxglove.Maps {
    public struct MapConfig {
        public uint Seed;
        public int RoomsToGenerate;
        public int MinRoomSize;
        public int MaxRoomSize;
        public int Radius;
        public readonly int Diameter => Radius * 2;
    }
}
