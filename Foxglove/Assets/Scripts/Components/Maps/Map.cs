using Unity.Entities;

namespace Foxglove.Maps {
    public struct Map : IComponentData { }

    public readonly struct MapTheme : IComponentData {
        public Entity RoomTile { get; init; }
        public Entity HallTile { get; init; }
        public Entity WallTile { get; init; }
    }
}
