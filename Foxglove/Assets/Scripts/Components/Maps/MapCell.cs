using Unity.Entities;

namespace Foxglove.Maps {
    public enum CellType {
        None, Room, Hallway,
    }

    public struct MapCell : IBufferElementData {
        public CellType Type;
        public MapCell(CellType type) => Type = type;

        public static implicit operator MapCell(CellType type) => new(type);
    }
}
