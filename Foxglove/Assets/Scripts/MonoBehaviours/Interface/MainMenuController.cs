using Foxglove.Gameplay;
using SideFX.Events;
using SideFX.SceneManagement;
using SideFX.SceneManagement.Events;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foxglove.Interface {
    [RequireComponent(typeof(UIDocument))]
    internal sealed class MainMenuController : MonoBehaviour {
        [SerializeField] private GameplayScene _gameplayScene;
        private UIDocument _doc;
        private Button _playButton;
        private Button _exitButton;

        private void Awake() {
            _doc = GetComponent<UIDocument>();

            _playButton = _doc.rootVisualElement.Q<Button>("start-game");
            if (_playButton is null) {
                Log.Error("[MainMenuController] Failed to find button 'start-game'");
                enabled = false;
                return;
            }

            _exitButton = _doc.rootVisualElement.Q<Button>("exit-game");
            if (_exitButton is null) {
                Log.Error("[MainMenuController] Failed to find button 'exit-game'");
                enabled = false;
            }
        }

        private void OnEnable() {
            _playButton.clicked += OnPlayClicked;
            _exitButton.clicked += OnExitClicked;
        }

        private void OnDisable() {
            _playButton.clicked -= OnPlayClicked;
            _exitButton.clicked -= OnExitClicked;
        }

        private void OnPlayClicked() => EventBus<LoadRequest>.Raise(new LoadRequest(_gameplayScene));
        private static void OnExitClicked() => EventBus<Shutdown>.Raise(new Shutdown());
    }
}
