using Unity.Entities;

namespace Foxglove.Combat {
    public struct Health : IComponentData {
        public float Current;
        public float Max;
        public uint LastDamagedAt;

        public Health(float max) {
            Max = max;
            Current = max;
            LastDamagedAt = 0;
        }

        public void Reset() {
            Current = Max;
            LastDamagedAt = 0;
        }
    }

    public struct HealthRegen : IComponentData, IEnableableComponent {
        public float Rate;

        public HealthRegen(float rate) => Rate = rate;
    }
}
