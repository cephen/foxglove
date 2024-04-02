using Unity.Entities;

namespace Foxglove.Maps {
    public struct MapConfig {
        public Entity MapRoot;
        public uint Seed;
        public int RoomsToGenerate;
        public int MinRoomSize;
        public int MaxRoomSize;
        public int Radius;
        public readonly int Diameter => Radius * 2;
    }
}
