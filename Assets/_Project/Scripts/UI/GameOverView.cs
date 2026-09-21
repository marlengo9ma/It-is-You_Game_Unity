using System;
using ItIsYou.Core;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 게임오버 화면. 컷씬 #GameOver 가 오기 전까지의 자리.
    ///
    /// 개선안: 게임오버는 남기되, 복귀 지점은 실패한 페이즈의 시작이다.
    /// 조합 실패(자기 비하) → 조합 시작으로. 통상 게임오버는 아직 타이틀로만.
    /// </summary>
    [AddComponentMenu("It is You/Game Over View")]
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button retryButton;
        [SerializeField] private TMP_Text retryLabel;
        [SerializeField] private Button titleButton;

        private bool _showRequested;

        /// <summary>실패한 페이즈의 시작으로 되돌아간다.</summary>
        public event Action RetryRequested;

        private void Awake()
        {
            if (!_showRequested && root != null && root != gameObject) root.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            if (titleButton != null) titleButton.onClick.AddListener(() => SceneManager.LoadScene(SceneRoutes.Title));
        }

        public void Show(GameOverKind kind, bool allowRetry, string retryText)
        {
            _showRequested = true;
            if (root != null) root.SetActive(true);

            if (title != null) title.text = "게임 오버";
            if (body != null)
                body.text = kind == GameOverKind.SelfLoathing
                    ? "단서가 모자라 사건을 추합하지 못했다.\n(컷씬 #GameOver — 자기 비하 · 아트 대기)"
                    : "신뢰를 잃었다. 이 상태로는 어떤 진술도 받아낼 수 없다.\n(컷씬 #GameOver — 통상 · 아트 대기)";

            if (retryButton != null) retryButton.gameObject.SetActive(allowRetry);
            if (retryLabel != null) retryLabel.text = retryText ?? "다시 시도";
        }

        public void Hide()
        {
            _showRequested = false;
            if (root != null) root.SetActive(false);
        }
    }
}
