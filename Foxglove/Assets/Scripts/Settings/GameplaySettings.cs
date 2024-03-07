using System;
using Unity.Entities;

namespace Foxglove.Settings {
    [Serializable]
    public struct LookSensitivity : IComponentData {
        public float Mouse;
        public float Gamepad;
    }
}
