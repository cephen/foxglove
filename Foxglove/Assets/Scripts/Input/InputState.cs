using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Input {
    public struct InputState : IComponentData {
        /// <summary>
        /// Input move vector with a max length of 1
        /// </summary>
        public float2 Move;

        public AimState Aim;
        public bool Attack;
    }

    public struct AimState {
        public float2 Target;
        public bool IsMouseAim;
    }
}
