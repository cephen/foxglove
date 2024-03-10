using System;
using Unity.Entities;

namespace Foxglove.Input {
    [Serializable]
    public struct LookSensitivity : IComponentData {
        public float Mouse;
        public float Gamepad;
    }
}
