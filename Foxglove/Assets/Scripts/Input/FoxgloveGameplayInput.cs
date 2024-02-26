using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Input {
    public struct FoxgloveGameplayInput : IComponentData {
        /// <summary>
        /// Input move vector with a max length of 1
        /// </summary>
        public float2 Move;

        public AimState Aim;
        public bool Interact;
        public bool Roll;
        public bool Flask;
        public bool Sword;
        public bool Spell1;
        public bool Spell2;
        public bool Spell3;
        public bool Spell4;
        public bool Pause;
    }

    public struct AimState {
        /// <summary>
        /// IsMouseAim true: mouse position delta
        /// IsMouseAim false: right stick tilt, normalized
        /// </summary>
        public float2 Value;

        public bool IsMouseAim;
    }
}
