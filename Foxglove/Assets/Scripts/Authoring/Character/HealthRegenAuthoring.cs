using Foxglove.Combat;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Character {
    internal sealed class HealthRegenAuthoring : MonoBehaviour {
        public float RegenRate = 2.5f;

        private sealed class Baker : Baker<HealthRegenAuthoring> {
            public override void Bake(HealthRegenAuthoring authoring) {
                Entity e = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent(e, new HealthRegen(authoring.RegenRate));
            }
        }
    }
}
