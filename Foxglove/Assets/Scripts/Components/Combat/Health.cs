using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Combat {
    [Serializable]
    public struct Health : IComponentData {
        [field: SerializeReference] public float Max { get; }
        [field: SerializeField] public float Current { get; private set; }
        [field: SerializeField] public uint LastDamagedAt { get; private set; }

        public Health(float max) {
            Max = max;
            Current = max;
            LastDamagedAt = 0;
        }

        public void ApplyDamage(uint tick, float damage) {
            Current -= damage;
            LastDamagedAt = tick;
        }

        public void ApplyRegen(float amount) => Current = math.min(Current + amount, Max);

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
