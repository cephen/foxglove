using Unity.Entities;
using UnityEngine;

namespace Foxglove.Character {
    public struct WispTag : IComponentData { }

    public sealed class WispAuthoring : MonoBehaviour {
        public sealed class Baker : Baker<WispAuthoring> {
            public override void Bake(WispAuthoring authoring) {
                Entity wisp = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<WispTag>(wisp);
            }
        }
    }
}
