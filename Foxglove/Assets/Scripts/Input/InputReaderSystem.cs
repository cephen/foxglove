using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
            RequireForUpdate<FoxgloveGameplayInput>();

            // Only one instance of InputState should exist
            if (SystemAPI.HasSingleton<FoxgloveGameplayInput>()) return;

            _inputStateEntity = EntityManager.CreateEntity();
            EntityManager.SetName(_inputStateEntity, "Input State");
            EntityManager.AddComponent<FoxgloveGameplayInput>(_inputStateEntity);
        }

        protected override void OnStartRunning() {
            _actions.Enable();
        }

        protected override void OnStopRunning() {
            _actions.Disable();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            float2 move = _actions.Gameplay.Move.ReadValue<Vector2>();
            // Normalize input values with a length greater than 1
            // This preserves partial joystick inputs
            if (math.lengthsq(move) > 1f) move = math.normalize(move);

            // When game isn't focused, this is null and causes exceptions when constructing AimState
            bool aimDeviceExists = _actions.Gameplay.Aim.activeControl is not null;

            ref FoxgloveGameplayInput state = ref SystemAPI.GetSingletonRW<FoxgloveGameplayInput>().ValueRW;

            state.Move = move;
            state.Aim = new AimState {
                Value = _actions.Gameplay.Aim.ReadValue<Vector2>(),
                IsMouseAim = aimDeviceExists
                             && _actions.KBMScheme.SupportsDevice(_actions.Gameplay.Aim.activeControl.device),
            };
            state.Interact = _actions.Gameplay.Interact.IsPressed();
            state.Sword = _actions.Gameplay.Sword.IsPressed();
            state.Roll = _actions.Gameplay.Roll.IsPressed();
            state.Flask = _actions.Gameplay.Flask.IsPressed();
            state.Spell1 = _actions.Gameplay.Spell1.IsPressed();
            state.Spell2 = _actions.Gameplay.Spell2.IsPressed();
            state.Spell3 = _actions.Gameplay.Spell3.IsPressed();
            state.Spell4 = _actions.Gameplay.Spell4.IsPressed();
            state.Pause = _actions.Gameplay.Pause.IsPressed();
        }
    }
}
