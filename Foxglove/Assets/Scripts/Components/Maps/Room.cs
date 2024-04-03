using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Maps {
    public struct Room : IComponentData {
        public int2 Position;
        public int2 Size;

        public readonly float2 Center => (float2)(Position + Size) / 2;
    }
}
