using UnityEngine;

namespace Foxglove.Interface {
    internal sealed class MainMenuModelAnimator : MonoBehaviour {
        public float BounceFrequency = 0.5f;
        public float BounceAmplitude = 0.5f;
        public float RotateSpeed = 15f;
        private float _baseElevation;

        private void Start() {
            _baseElevation = transform.position.y;
        }

        private void Update() {
            transform.Rotate(Vector3.up, RotateSpeed * Time.deltaTime);

            Vector3 position = transform.position;
            float t = Time.time * BounceFrequency;
            position.y = _baseElevation + BounceAmplitude * Mathf.Sin(t);
            transform.position = position;
        }
    }
}
