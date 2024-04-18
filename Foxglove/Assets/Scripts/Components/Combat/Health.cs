using Unity.Entities;

namespace Foxglove.Combat {
    public struct Health : IComponentData {
        public float Current;
        public float Max;

        public Health(float max) {
            Max = max;
            Current = max;
        }

        public void Reset() {
            Current = Max;
        }
    }

    public struct HealthRegen : IComponentData, IEnableableComponent {
        public float Rate;
    }
}
