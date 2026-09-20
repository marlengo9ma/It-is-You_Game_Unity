using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.Core
{
    /// <summary>
    /// 스타트 화면.
    ///
    /// 기획서: "레트로타자기를 2/3지점에 배치 / 배경의 움직이는 요소".
    /// 배치는 씬 빌더가 잡고, 여기서는 그 '움직이는 요소'를 돌린다.
    /// 정지 화면은 죽은 화면처럼 보이기 때문에, 아트가 없는 지금도 뭔가는 움직여야 한다.
    /// </summary>
    [AddComponentMenu("It is You/Title Controller")]
    public class TitleController : MonoBehaviour
    {
        [Header("메뉴")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("표시")]
        [SerializeField] private TMP_Text versionLabel;
        [SerializeField] private TMP_Text caret;

        [Header("배경 움직임")]
        [Tooltip("천천히 흘러가는 요소들. 기차역의 증기를 대신한다.")]
        [SerializeField] private List<RectTransform> driftingLayers = new List<RectTransform>();

        [Tooltip("각 층이 한 바퀴 흐르는 데 걸리는 시간(초). 층마다 다르게 두면 깊이가 생긴다.")]
        [SerializeField] private float baseDriftSeconds = 26f;

        [SerializeField] private float caretBlinkSeconds = 0.62f;

        private readonly List<float> _startX = new List<float>();
        private float _caretTimer;
        private bool _caretOn = true;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            // 아직 없는 기능은 눌리지 않게 둔다. 눌러도 아무 일 없는 버튼보다 낫다.
            if (continueButton != null) continueButton.interactable = false;
            if (settingsButton != null) settingsButton.interactable = false;

            if (versionLabel != null)
                versionLabel.text = $"v{Application.version}  ·  개발 빌드";

            for (int i = 0; i < driftingLayers.Count; i++)
                _startX.Add(driftingLayers[i] != null ? driftingLayers[i].anchoredPosition.x : 0f);
        }

        private void OnDestroy()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void Update()
        {
            DriveDrift();
            DriveCaret();
        }

        /// <summary>층마다 조금씩 다른 속도로 옆으로 흘려보낸다.</summary>
        private void DriveDrift()
        {
            for (int i = 0; i < driftingLayers.Count; i++)
            {
                var layer = driftingLayers[i];
                if (layer == null) continue;

                var period = baseDriftSeconds * (1f + i * 0.45f);
                var t = Mathf.Repeat(Time.time / period, 1f);
                var width = layer.rect.width;

                var p = layer.anchoredPosition;
                p.x = _startX[i] - t * width;
                layer.anchoredPosition = p;
            }
        }

        private void DriveCaret()
        {
            if (caret == null) return;
            _caretTimer += Time.unscaledDeltaTime;
            if (_caretTimer < caretBlinkSeconds) return;

            _caretTimer = 0f;
            _caretOn = !_caretOn;
            caret.alpha = _caretOn ? 1f : 0f;
        }

        private void OnNewGame() => SceneManager.LoadScene(SceneRoutes.Prologue);

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
