using Foxglove.Agent;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Agent {
    /// <summary>
    /// Added to the wisp prefab, when instantiated this component is converted to a <see cref="WispTag" />
    /// </summary>
    public sealed class WispAuthoring : MonoBehaviour {
        public sealed class Baker : Baker<WispAuthoring> {
            public override void Bake(WispAuthoring authoring) {
                Entity wisp = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<WispTag>(wisp);
                AddComponent(wisp, WispState.Default());
            }
        }
    }
}
