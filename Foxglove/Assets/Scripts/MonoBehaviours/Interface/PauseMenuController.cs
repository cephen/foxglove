using Foxglove.Gameplay;
using SideFX.Events;
using SideFX.SceneManagement;
using SideFX.SceneManagement.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foxglove.Interface {
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuController : MonoBehaviour {
        [SerializeField] private MainMenuScene _mainMenuScene;
        private UIDocument _doc;
        private Button _resumeButton;
        private Button _exitButton;
        private EventBinding<PauseEvent> _pauseBinding;
        private EventBinding<ResumeEvent> _resumeBinding;

        private void Awake() {
            _doc = GetComponent<UIDocument>();
            _pauseBinding = new EventBinding<PauseEvent>(OnPauseEvent);
            _resumeBinding = new EventBinding<ResumeEvent>(OnResumeEvent);
        }

        private void OnEnable() {
            EventBus<PauseEvent>.Register(_pauseBinding);
            EventBus<ResumeEvent>.Register(_resumeBinding);

            _resumeButton = _doc.rootVisualElement.Q<Button>("resume-button");
            _exitButton = _doc.rootVisualElement.Q<Button>("exit-button");
            _resumeButton.clicked += OnResumeClicked;
            _exitButton.clicked += OnExitClicked;
        }

        private void OnDisable() {
            EventBus<PauseEvent>.Deregister(_pauseBinding);
            EventBus<ResumeEvent>.Deregister(_resumeBinding);

            _resumeButton.clicked -= OnResumeClicked;
            _exitButton.clicked -= OnExitClicked;
        }

#region Button bindings

        private static void OnResumeClicked() => EventBus<ResumeEvent>.Raise(new ResumeEvent());
        private void OnExitClicked() => EventBus<LoadRequest>.Raise(new LoadRequest(_mainMenuScene));

#endregion

#region EventBus Bindings

        private void OnPauseEvent() => _doc.enabled = true;
        private void OnResumeEvent() => _doc.enabled = false;

#endregion
    }
}
