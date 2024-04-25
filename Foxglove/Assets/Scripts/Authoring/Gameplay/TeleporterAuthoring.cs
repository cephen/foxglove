using Foxglove.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Gameplay {
    internal sealed class TeleporterAuthoring : MonoBehaviour {
        private sealed class Baker : Baker<TeleporterAuthoring> {
            public override void Bake(TeleporterAuthoring authoring) {
                Entity e = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<Teleporter>(e);
            }
        }
    }
}
