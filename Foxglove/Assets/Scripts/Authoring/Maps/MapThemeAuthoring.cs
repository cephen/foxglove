using Foxglove.Maps;
using Unity.Entities;
using UnityEngine;

namespace Foxglove.Authoring.Maps {
    public sealed class MapThemeAuthoring : MonoBehaviour {
        public MapThemeConfig Config;

        private sealed class Baker : Baker<MapThemeAuthoring> {
            public override void Bake(MapThemeAuthoring authoring) {
                Entity room = GetEntity(authoring.Config.RoomTile, TransformUsageFlags.None);
                Entity hall = GetEntity(authoring.Config.HallTile, TransformUsageFlags.None);
                Entity wall = GetEntity(authoring.Config.WallTile, TransformUsageFlags.None);
                Entity store = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    store,
                    new MapTheme {
                        RoomTile = room,
                        HallTile = hall,
                        WallTile = wall,
                    }
                );
            }
        }
    }
}
