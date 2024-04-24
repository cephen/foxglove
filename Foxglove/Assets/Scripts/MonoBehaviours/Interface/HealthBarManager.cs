using Foxglove.Character;
using Foxglove.Gameplay;
using SideFX.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foxglove.Interface {
    /// <summary>
    /// Listens for events to update the fill amount of the health bar
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    internal sealed class HealthBarManager : MonoBehaviour {
        private UIDocument _uiDocument;

        private EventBinding<GameReady> _gameReadyBinding;
        private EventBinding<PlayerDamaged> _playerDamagedBinding;

        private void Start() {
            _uiDocument = GetComponent<UIDocument>();
            _uiDocument.enabled = false;
        }

        private void OnEnable() {
            _gameReadyBinding = new EventBinding<GameReady>(OnGameReady);
            _playerDamagedBinding = new EventBinding<PlayerDamaged>(OnPlayerDamaged);

            EventBus<GameReady>.Register(_gameReadyBinding);
            EventBus<PlayerDamaged>.Register(_playerDamagedBinding);
        }

        private void OnDisable() {
            EventBus<GameReady>.Deregister(_gameReadyBinding);
            EventBus<PlayerDamaged>.Deregister(_playerDamagedBinding);
        }

        private void OnGameReady(GameReady evt) {
            _uiDocument.enabled = true;

            var bar = _uiDocument.rootVisualElement.Q<VisualElement>("bar");
            bar.style.width = new Length(100, LengthUnit.Percent);
        }

        private void OnPlayerDamaged(PlayerDamaged evt) {
            var bar = _uiDocument.rootVisualElement.Q<VisualElement>("bar");

            float percent = evt.Health.Current / evt.Health.Max * 100;
            var width = new Length(percent, LengthUnit.Percent);

            bar.style.width = width;
        }
    }
}
