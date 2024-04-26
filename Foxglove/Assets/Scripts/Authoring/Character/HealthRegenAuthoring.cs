using Foxglove.Combat;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Character {
    /// <summary>
    /// Adds health regen capability to entities
    /// </summary>
    internal sealed class HealthRegenAuthoring : MonoBehaviour {
        /// <summary>
        /// Amount of health applied per second
        /// </summary>
        public float RegenRate = 2.5f;

        private sealed class Baker : Baker<HealthRegenAuthoring> {
            public override void Bake(HealthRegenAuthoring authoring) {
                Entity e = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent(e, new HealthRegen(authoring.RegenRate));
            }
        }
    }
}
