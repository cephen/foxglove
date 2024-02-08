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

            float2 aim = _actions.Gameplay.Aim.ReadValue<Vector2>();
            bool isMouseAim = _actions.KBMScheme.SupportsDevice(_actions.Gameplay.Aim.activeControl.device);

            bool attack = _actions.Gameplay.Sword.IsPressed();

            SystemAPI.SetSingleton(new InputState {
                Move = move,
                Aim = (aim, isMouseAim),
                Attack = attack,
            });
        }

        protected override void OnStopRunning() {
            _actions.Disable();
        }
    }
}
