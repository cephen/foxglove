using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Foxglove.Input {
    /// <summary>
    /// This system is responsible for parsing and storing input state.
    /// It runs before any gameplay simulation code
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal sealed partial class InputReaderSystem : SystemBase {
        private FoxgloveActions _actions;
        private Entity _inputStateEntity;

        protected override void OnCreate() {
            _actions = new FoxgloveActions();
            RequireForUpdate<FixedTickSystem.Singleton>();
        }

        protected override void OnStartRunning() {
            _actions.Enable();
            EntityManager.CreateOrAddSingleton<FoxgloveGameplayInput>();
        }

        protected override void OnStopRunning() {
            _actions.Disable();
            EntityManager.RemoveSingletonComponentIfExists<FoxgloveGameplayInput>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            uint tick = EntityManager.GetSingleton<FixedTickSystem.Singleton>().Tick;
            var input = EntityManager.GetSingleton<FoxgloveGameplayInput>();

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

            EntityManager.CreateOrSetSingleton(input);
        }
    }
}
