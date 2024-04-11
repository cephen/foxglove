using UnityEngine;

namespace Foxglove.Authoring.Maps {
    [CreateAssetMenu(menuName = "Foxglove/Maps/Theme", fileName = "New Map Theme")]
    public sealed class MapThemeConfig : ScriptableObject {
        public GameObject RoomTile;
        public GameObject HallTile;
        public GameObject WallTile;
    }
}
