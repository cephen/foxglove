using Foxglove.Agent;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Agent {
    /// <summary>
    /// Added to the wisp prefab, when instantiated this component is converted to a <see cref="Wisp" />
    /// </summary>
    internal sealed class WispAuthoring : MonoBehaviour {
        public uint MinAttackCooldown = 50 * 4; // 4 Seconds @ 50 ticks per second
        public uint MaxAttackCooldown = 50 * 10; // 10 seconds

        /// <summary>
        /// When a GameObject is instantiated, any MonoBehaviours attached to it that have a baker
        /// will be converted into an Entity. This baker handles the conversion of the Wisp prefab into a Wisp Entity.
        /// </summary>
        private sealed class Baker : Baker<WispAuthoring> {
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
            }
        }
    }
}
