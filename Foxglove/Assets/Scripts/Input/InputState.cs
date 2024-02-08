using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Input {
    public struct InputState : IComponentData {
        /// <summary>
        /// Input move vector with a max length of 1
        /// </summary>
        public float2 Move;

        /// <summary>
        /// Tuple representation of aim direction and input device type.
        /// when the bool is true, the float2 represents the mouse position in screen space
        /// when false, the float2 represents the normalised joystick tilt direction
        /// </summary>
        public (float2, bool) Aim;

        /// <summary>
        /// Whether the attack button is pressed
        /// </summary>
        public bool Attack;
    }
}
