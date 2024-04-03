using Unity.Entities;

namespace Foxglove.State {
    public struct ChangedStateAt : IComponentData {
        public uint Value;
        public static implicit operator uint(ChangedStateAt t) => t.Value;
        public static implicit operator ChangedStateAt(uint t) => new() { Value = t };
    }
}
