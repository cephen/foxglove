using Unity.Entities;

namespace Foxglove.Maps {
    public enum CellType {
        None, Room, Hallway,
    }

    public struct MapCell : IBufferElementData {
        public CellType Type;
    }
}
