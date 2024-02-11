using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Foxglove.Input {
    /// <summary>
    /// This system is responsible for parsing and storing input state.
    /// It runs before any gameplay simulation code
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    internal partial class InputReaderSystem : SystemBase {
        private FoxgloveActions _actions;
        private Entity _inputReaderEntity;

        protected override void OnCreate() {
            _actions = new FoxgloveActions();
            RequireForUpdate<InputState>();

            _inputReaderEntity = EntityManager.CreateEntity();
            EntityManager.SetName(_inputReaderEntity, "Input State");
            EntityManager.AddComponent<InputState>(_inputReaderEntity);
        }

        protected override void OnStartRunning() {
            _actions.Enable();
        }

        protected override void OnUpdate() {
            float2 move = _actions.Gameplay.Move.ReadValue<Vector2>();
            // Normalize input values with a length greater than 1
            // This preserves partial joystick inputs
            if (math.lengthsq(move) > 1f) move = math.normalize(move);


            SystemAPI.SetSingleton(new InputState {
                Move = move,
                Aim = new AimState {
                    Value = _actions.Gameplay.Aim.ReadValue<Vector2>(),
                    IsMouseAim = _actions.KBMScheme.SupportsDevice(_actions.Gameplay.Aim.activeControl.device),
                },
                Interact = _actions.Gameplay.Interact.IsPressed(),
                Sword = _actions.Gameplay.Sword.IsPressed(),
                Roll = _actions.Gameplay.Roll.IsPressed(),
                Flask = _actions.Gameplay.Flask.IsPressed(),
                Spell1 = _actions.Gameplay.Spell1.IsPressed(),
                Spell2 = _actions.Gameplay.Spell2.IsPressed(),
                Spell3 = _actions.Gameplay.Spell3.IsPressed(),
                Spell4 = _actions.Gameplay.Spell4.IsPressed(),
                Pause = _actions.Gameplay.Pause.IsPressed(),
            });
        }

        protected override void OnStopRunning() {
            _actions.Disable();
        }
    }
}
