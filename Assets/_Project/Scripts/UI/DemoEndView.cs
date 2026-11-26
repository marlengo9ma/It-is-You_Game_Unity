using System.Collections;
using ItIsYou.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 데모 종료 화면 (2026-11-27) — 컷씬(임시) 위에 어두운 상자를 올리고 감사 문장과
    /// '타이틀로 가기' · '불러오기' 버튼을 띄운다. 1챕터 인트로가 끝난 뒤에 나온다.
    /// 그림 · 문장은 챕터 에셋의 '데모 종료' 칸에서 바꾼다. 자리 · 크기는 UI 편집기로.
    /// </summary>
    [AddComponentMenu("It is You/Demo End View")]
    public class DemoEndView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [Tooltip("어두운 상자 + 문장 + 버튼 묶음. 배경이 먼저 뜨고 이것이 천천히 나타난다.")]
        [SerializeField] private CanvasGroup box;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private SaveSlotView slots;
        [SerializeField] private float backgroundFadeSeconds = 0.8f;
        [SerializeField] private float boxDelaySeconds = 0.6f;
        [SerializeField] private float boxFadeSeconds = 0.9f;

        private CanvasGroup _rootGroup;

        private void Awake()
        {
            if (titleButton != null) titleButton.onClick.AddListener(() => { PauseMenu.ForceUnpause(); SceneManager.LoadScene(SceneRoutes.Title); });
            if (loadButton != null) loadButton.onClick.AddListener(() => slots?.Open(SaveSlotView.Mode.Load));
            if (root != null && root != gameObject) root.SetActive(false);
        }

        public bool IsShown => root != null && root.activeSelf;

        public void Show(Sprite image, string text)
        {
            if (root == null) return;
            root.SetActive(true);
            if (background != null)
            {
                background.sprite = image;
                background.color = image != null ? Color.white : new Color32(0x0F, 0x11, 0x14, 0xFF);
                var fitter = background.GetComponent<AspectRatioFitter>();
                if (fitter != null && image != null && image.rect.height > 0f) fitter.aspectRatio = image.rect.width / image.rect.height;
            }
            if (message != null) message.text = text ?? string.Empty;
            if (loadButton != null) loadButton.interactable = SaveSystem.AnyExists;
            StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            _rootGroup = root.GetComponent<CanvasGroup>();
            if (_rootGroup == null) _rootGroup = root.AddComponent<CanvasGroup>();
            if (box != null) { box.alpha = 0f; box.interactable = false; box.blocksRaycasts = false; }

            yield return FadeGroup(_rootGroup, backgroundFadeSeconds);
            if (boxDelaySeconds > 0f) yield return new WaitForSecondsRealtime(boxDelaySeconds);
            if (box != null)
            {
                yield return FadeGroup(box, boxFadeSeconds);
                box.interactable = true;
                box.blocksRaycasts = true;
            }
        }

        private static IEnumerator FadeGroup(CanvasGroup g, float seconds)
        {
            if (g == null) yield break;
            var t = 0f;
            g.alpha = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / Mathf.Max(0.01f, seconds));
                g.alpha = k * k * (3f - 2f * k);
                yield return null;
            }
            g.alpha = 1f;
        }
    }
}
