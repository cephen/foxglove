﻿using Foxglove.Gameplay;
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
        private EventBinding<PauseGame> _pauseBinding;
        private EventBinding<ResumeGame> _resumeBinding;

        private void Awake() {
            _doc = GetComponent<UIDocument>();
            _pauseBinding = new EventBinding<PauseGame>(OnPauseEvent);
            _resumeBinding = new EventBinding<ResumeGame>(OnResumeEvent);
        }

        private void OnEnable() {
            EventBus<PauseGame>.Register(_pauseBinding);
            EventBus<ResumeGame>.Register(_resumeBinding);

            _resumeButton = _doc.rootVisualElement.Q<Button>("resume-button");
            _exitButton = _doc.rootVisualElement.Q<Button>("exit-button");
            _resumeButton.clicked += OnResumeClicked;
            _exitButton.clicked += OnExitClicked;
        }

        private void OnDisable() {
            EventBus<PauseGame>.Deregister(_pauseBinding);
            EventBus<ResumeGame>.Deregister(_resumeBinding);

            _resumeButton.clicked -= OnResumeClicked;
            _exitButton.clicked -= OnExitClicked;
        }

#region Button bindings

        private static void OnResumeClicked() => EventBus<ResumeGame>.Raise(new ResumeGame());
        private void OnExitClicked() => EventBus<LoadRequest>.Raise(new LoadRequest(_mainMenuScene));

#endregion

#region EventBus Bindings

        private void OnPauseEvent() => _doc.enabled = true;
        private void OnResumeEvent() => _doc.enabled = false;

#endregion
    }
}
