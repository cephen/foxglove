using System;
using Unity.Mathematics;

namespace Foxglove.Input {
    [Serializable]
    public struct AimState {
        /// <summary>
        /// IsMouseAim true: mouse position delta
        /// IsMouseAim false: right stick tilt, normalized
        /// </summary>
        public float2 Value;

        public bool IsMouseAim;
    }
}
