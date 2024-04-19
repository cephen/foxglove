using Foxglove.Gameplay;
using SideFX.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foxglove {
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuController : MonoBehaviour {
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
        }

        private void OnDisable() {
            EventBus<PauseEvent>.Deregister(_pauseBinding);
            _resumeButton.clicked -= OnResumeClicked;
        }

        private void OnResumeClicked() {
            _doc.enabled = false;
            EventBus<ResumeEvent>.Raise(new ResumeEvent());
        }

        private void OnPause() => _doc.enabled = true;
    }
}
