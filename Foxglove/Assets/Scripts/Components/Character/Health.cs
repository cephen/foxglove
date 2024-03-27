using Unity.Entities;

namespace Foxglove.Character {
    public struct Health : IComponentData {
        public float Current;
        public float Max;

        public Health(float max) {
            Max = max;
            Current = max;
        }
    }
}
