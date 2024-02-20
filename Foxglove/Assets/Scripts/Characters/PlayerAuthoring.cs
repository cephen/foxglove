using Foxglove.Camera;
using Foxglove.Motion;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Characters {
    public class PlayerAuthoring : MonoBehaviour {
        public MotionSettings MotionSettings;

        [Header("Camera")]
        public float3 CameraOffset;

        public float CameraDistance = 1f;

        public class PlayerBaker : Baker<PlayerAuthoring> {
            /// <inheritdoc />
            public override void Bake(PlayerAuthoring authoring) {
                Entity player = GetEntity(authoring.gameObject,
                    TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

                AddComponent<PlayerTag>(player);
                // AddComponent<TargetVelocity>(player);
                // AddComponent(player, authoring.MotionSettings);
                AddComponent(player, new CameraPosition());
                AddComponent(player, new CameraTarget());
                AddComponent(player, new CameraOffset { Value = authoring.CameraOffset });
                AddComponent(player, new CameraDistance { Value = authoring.CameraDistance });
            }
        }
    }

    public struct PlayerTag : IComponentData { }
}
