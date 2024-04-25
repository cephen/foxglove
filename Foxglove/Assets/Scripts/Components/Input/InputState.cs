using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Input {
    /// <summary>
    /// Represents user input, accessed using the singleton pattern
    /// ---
    /// A lot of these fields are unused at present, but have been left in for future development
    /// </summary>
    public struct InputState : IComponentData {
        /// <summary>
        /// Input move vector with a max length of 1
        /// </summary>
        public float2 Move;

        public AimState Aim;
        public FixedInputEvent Jump;
        public FixedInputEvent Pause;

        // Unused :'c
        public FixedInputEvent Interact;
        public FixedInputEvent Flask;
        public FixedInputEvent Sword;
        public FixedInputEvent Spell1;
        public FixedInputEvent Spell2;
        public FixedInputEvent Spell3;
        public FixedInputEvent Spell4;
    }
}
