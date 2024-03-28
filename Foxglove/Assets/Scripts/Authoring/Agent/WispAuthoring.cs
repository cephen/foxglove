using Foxglove.Agent;
using Foxglove.Character;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Agent {
    /// <summary>
    /// Added to the wisp prefab, when instantiated this component is converted to a <see cref="Wisp" />
    /// </summary>
    public sealed class WispAuthoring : MonoBehaviour {
        public int MaxHealth = 100;
        public uint MinAttackCooldown = 50 * 4; // 4 Seconds @ 50 ticks per second
        public uint MaxAttackCooldown = 50 * 10; // 10 seconds

        public sealed class Baker : Baker<WispAuthoring> {
            public override void Bake(WispAuthoring authoring) {
                Entity wisp = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(
                    wisp,
                    new Wisp {
                        CanAttackAt = 0,
                        MinAttackCooldown = authoring.MinAttackCooldown,
                        MaxAttackCooldown = authoring.MaxAttackCooldown,
                    }
                );
                AddComponent(wisp, WispState.Default());
                AddComponent(
                    wisp,
                    new Health {
                        Max = authoring.MaxHealth,
                        Current = authoring.MaxHealth,
                    }
                );
                AddComponent<DespawnTimer>(wisp);
                SetComponentEnabled<DespawnTimer>(wisp, false);
            }
        }
    }
}
