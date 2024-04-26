using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Maps {
    /// <summary>
    /// Tag that identifies map entities (there should only be one or zero at any given point in time)
    /// </summary>
    public struct Map : IComponentData { }

    /// <summary>
    /// Attached to map entities, indicates if a map should be built/rebuilt
    /// </summary>
    public struct ShouldBuild : IComponentData, IEnableableComponent { }

    public enum TileType {
        None, Room, Hallway,
    }

    /// <summary>
    /// After generation, but before spawning, the map is represented in memory as an array of MapTiles.
    /// </summary>
    public struct MapTile : IBufferElementData {
        public TileType Type;
        public MapTile(TileType type) => Type = type;

        public static implicit operator MapTile(TileType type) => new(type);
    }

    /// <summary>
    /// Rooms are placed around the map
    /// After generation two rooms are selected to spawn the player and teleporter in
    /// </summary>
    public struct Room : IBufferElementData {
        public int2 Position; // of the southwest corner of this room
        public int2 Size; // width and depth in tiles

        public readonly float2 Center => Position + Size / 2;
    }
}
