using System;
using ItIsYou.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// ESC 로 여는 일시 정지 메뉴. 계속하기 · 설정 · 타이틀로.
    ///
    /// 기획: "게임 일시 정지 - 타이머 일시 정지". 열려 있는 동안 <see cref="IsPaused"/> 가 참이고,
    /// 무반응 타이머와 대사 출력은 이 값을 보고 멈춘다.
    /// </summary>
    [AddComponentMenu("It is You/Pause Menu")]
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private SettingsMenu settings;

        private bool _open;
        private bool _wired;

        /// <summary>어느 씬에서든 읽을 수 있는 정지 상태.</summary>
        public static bool IsPaused { get; private set; }

        /// <summary>정지 상태가 바뀌었다. (정지 여부)</summary>
        public event Action<bool> PauseChanged;

        public bool IsOpen => _open;

        private void Awake()
        {
            Wire();
            IsPaused = false;
            if (!_open && root != null && root != gameObject) root.SetActive(false);
        }

        private void OnDestroy()
        {
            IsPaused = false;
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (titleButton != null) titleButton.onClick.AddListener(ToTitle);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            // 설정 창이 떠 있으면 그쪽이 먼저 닫힌다.
            if (settings != null && settings.IsOpen) return;

            if (_open) Close();
            else Open();
        }

        public void Open()
        {
            if (_open) return;
            Wire();
            _open = true;
            IsPaused = true;
            if (root != null) root.SetActive(true);
            PauseChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            IsPaused = false;
            if (root != null) root.SetActive(false);
            PauseChanged?.Invoke(false);
        }

        private void OpenSettings()
        {
            if (settings != null) settings.Open();
        }

        private void ToTitle()
        {
            IsPaused = false;
            SceneManager.LoadScene(SceneRoutes.Title);
        }
    }
}
