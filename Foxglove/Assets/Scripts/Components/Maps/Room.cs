using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Maps {
    public struct Room : IComponentData {
        public int2 Position;
        public int2 Size;

        public readonly int2 Center => Position + Size / 2;
    }
}
