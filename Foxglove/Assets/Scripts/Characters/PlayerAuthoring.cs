using Foxglove.Motion;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Characters {
    public class PlayerAuthoring : MonoBehaviour {
        public MotionSettings MotionSettings;

        public class PlayerBaker : Baker<PlayerAuthoring> {
            /// <inheritdoc />
            public override void Bake(PlayerAuthoring authoring) {
                Entity player = GetEntity(authoring.gameObject,
                    TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

                AddComponent<PlayerTag>(player);
                AddComponent(player, authoring.MotionSettings);
                AddComponent<TargetVelocity>(player);
            }
        }
    }

    public struct PlayerTag : IComponentData { }
}
