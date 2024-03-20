using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Foxglove.Input {
    /// <summary>
    /// This system is responsible for parsing and storing input state.
    /// It runs before any gameplay simulation code.
    /// This system stores a managed type (<see cref="FoxgloveActions" />) as a field,
    /// so must be implemented as a class inheriting from <see cref="SystemBase" />
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed partial class InputReaderSystem : SystemBase {
        private FoxgloveActions _actions;
        private Entity _inputStateEntity;

        protected override void OnCreate() {
            _actions = new FoxgloveActions();
            RequireForUpdate<FixedTickSystem.State>();
        }

        protected override void OnStartRunning() {
            _actions.Enable();
            EntityManager.CreateOrAddSingleton<State>();
        }

        protected override void OnStopRunning() {
            _actions.Disable();
            EntityManager.RemoveSingletonComponentIfExists<State>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            uint tick = EntityManager.GetSingleton<FixedTickSystem.State>().Tick;
            ref State input = ref SystemAPI.GetSingletonRW<State>().ValueRW;

            float2 move = _actions.Gameplay.Move.ReadValue<Vector2>();
            // Normalize input values with a length greater than 1
            // This preserves partial joystick inputs
            if (math.lengthsq(move) > 1f) move = math.normalize(move);

            // When game isn't focused (PC only), this is null and causes exceptions when constructing AimState
            InputControl aimControl = _actions.Gameplay.Aim.activeControl;

            input.Move = move;
            input.Aim = new AimState {
                Value = _actions.Gameplay.Aim.ReadValue<Vector2>(),
                IsMouseAim = aimControl is not null && _actions.KBMScheme.SupportsDevice(aimControl.device),
            };
            if (_actions.Gameplay.Interact.IsPressed()) input.Interact.Set(tick);
            if (_actions.Gameplay.Sword.IsPressed()) input.Sword.Set(tick);
            if (_actions.Gameplay.Jump.IsPressed()) input.Jump.Set(tick);
            if (_actions.Gameplay.Flask.IsPressed()) input.Flask.Set(tick);
            if (_actions.Gameplay.Spell1.IsPressed()) input.Spell1.Set(tick);
            if (_actions.Gameplay.Spell2.IsPressed()) input.Spell2.Set(tick);
            if (_actions.Gameplay.Spell3.IsPressed()) input.Spell3.Set(tick);
            if (_actions.Gameplay.Spell4.IsPressed()) input.Spell4.Set(tick);
            if (_actions.Gameplay.Pause.IsPressed()) input.Pause.Set(tick);
        }

        public struct State : IComponentData {
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
}
