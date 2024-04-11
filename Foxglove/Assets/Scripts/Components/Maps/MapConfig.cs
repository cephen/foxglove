using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Maps {
    [BurstCompile]
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

        [BurstCompile]
        public float3 PositionFromIndex(in int index) {
            int2 coords = new int2(index % Diameter, index / Diameter) - Radius;
            var to3d = new float3(coords.x, 0f, coords.y);
            return to3d;
        }

        [BurstCompile]
        public int IndexFromPosition(in float3 position) {
            int2 coords = (int2)math.floor(position.xz) + Radius;
            return coords.x + coords.y * Diameter;
        }
    }
}
