using UnityEngine;

namespace Foxglove.Camera {
    public sealed class MainGameObjectCamera : MonoBehaviour {
        public static UnityEngine.Camera Instance;

        private void Awake() {
            Instance = GetComponent<UnityEngine.Camera>();
        }
    }
}
