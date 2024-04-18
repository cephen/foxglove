using Foxglove.Combat;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Character {
    internal sealed class HealthAuthoring : MonoBehaviour {
        public int MaxHealth = 100;

        private sealed class Baker : Baker<HealthAuthoring> {
            public override void Bake(HealthAuthoring authoring) {
                Entity e = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent(e, new Health(authoring.MaxHealth));
            }
        }
    }
}
