using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Maps {
    /// <summary>
    /// Settings for map generation
    /// </summary>
    [BurstCompile]
    public readonly struct MapConfig : IComponentData {
        public readonly uint Seed;
        public readonly int RoomsToGenerate;
        public readonly int MinRoomSize;
        public readonly int MaxRoomSize;

        // The map is actually a square centered on the world origin
        // But these names made more sense for the purposes of the algorithm
        public readonly int Radius; // Distance in tiles from world origin to map border on any axis
        public int Diameter => Radius * 2; // Side length of the map

        public MapConfig(uint seed, int rooms = 40, int minRoomSize = 4, int maxRoomSize = 12, int radius = 64) {
            Seed = seed;
            RoomsToGenerate = rooms;
            MinRoomSize = minRoomSize;
            MaxRoomSize = maxRoomSize;
            Radius = radius;
        }

        /// <summary>
        /// Converts an array index to a world space coordinate
        /// </summary>
        [BurstCompile]
        public int2 CoordsFromIndex(in int index) => new int2(index % Diameter, index / Diameter) - Radius;

        /// <summary>
        /// Converts field space coordinate to an array index
        /// </summary>
        [BurstCompile]
        public int IndexFromCoords(in int2 coords) => coords.x + Radius + (coords.y + Radius) * Diameter;

        /// <summary>
        /// Converts an array index to a world space position.
        /// The returned position is the southwest corner of the tile
        /// </summary>
        [BurstCompile]
        public float3 PositionFromIndex(in int index) {
            int2 coords = CoordsFromIndex(index);
            return new float3(coords.x, 0, coords.y);
        }

        /// <summary>
        /// Converts a world space position to world space coordinates
        /// </summary>
        [BurstCompile]
        public int2 CoordsFromPosition(in float3 position) => new((int)position.x, (int)position.z);
    }

    /// <summary>
    /// Attached to a map to specify what prefabs should be used for each tile type
    /// </summary>
    public readonly struct MapTheme : IComponentData {
        public Entity RoomTile { get; init; }
        public Entity HallTile { get; init; }
        public Entity WallTile { get; init; }
    }
}
