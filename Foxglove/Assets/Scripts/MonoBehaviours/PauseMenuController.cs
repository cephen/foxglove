using Foxglove.Gameplay;
using SideFX.Events;
using SideFX.SceneManagement;
using SideFX.SceneManagement.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foxglove {
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuController : MonoBehaviour {
        [SerializeField] private MainMenuScene _mainMenuScene;
        private UIDocument _doc;
        private Button _resumeButton;
        private Button _exitButton;
        private EventBinding<PauseEvent> _pauseBinding;

        private void Awake() {
            _doc = GetComponent<UIDocument>();
            _resumeButton = _doc.rootVisualElement.Q<Button>("resume-button");
            _exitButton = _doc.rootVisualElement.Q<Button>("exit-button");
            _pauseBinding = new EventBinding<PauseEvent>(OnPause);
        }

        private void OnEnable() {
            EventBus<PauseEvent>.Register(_pauseBinding);
            _resumeButton.clicked += OnResumeClicked;
            _exitButton.clicked += OnExitClicked;
        }

        private void OnDisable() {
            EventBus<PauseEvent>.Deregister(_pauseBinding);
            _resumeButton.clicked -= OnResumeClicked;
            _exitButton.clicked -= OnExitClicked;
        }

        private static void OnResumeClicked() => EventBus<ResumeEvent>.Raise(new ResumeEvent());
        private void OnExitClicked() => EventBus<LoadRequest>.Raise(new LoadRequest(_mainMenuScene));

        private void OnPause() => _doc.enabled = true;
        private void OnResume() => _doc.enabled = false;
    }
}
