using System;
using Unity.Mathematics;

namespace Foxglove.Input {
    /// <summary>
    /// Represents the aim action state for the current tick.
    /// Since Aim can be triggered by both mouse and gamepad bindings,
    /// and the input system processes those bindings differently,
    /// consuming systems need to be aware of which device triggered the action.
    /// </summary>
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
