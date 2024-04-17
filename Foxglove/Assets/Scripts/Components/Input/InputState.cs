using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Input {
    public struct InputState : IComponentData {
        /// <summary>
        /// Input move vector with a max length of 1
        /// </summary>
        public float2 Move;

        public AimState Aim;
        public FixedInputEvent Interact;
        public FixedInputEvent Jump;
        public FixedInputEvent Flask;
        public FixedInputEvent Sword;
        public FixedInputEvent Spell1;
        public FixedInputEvent Spell2;
        public FixedInputEvent Spell3;
        public FixedInputEvent Spell4;
        public FixedInputEvent Pause;
    }
}
