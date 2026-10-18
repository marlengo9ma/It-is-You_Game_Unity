using System.Collections;
using System.Collections.Generic;
using ItIsYou.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 챕터 엔딩.
    ///
    /// 1. 엔딩 컷씬 — 지목 결과의 엔딩 분기 컷(예: EA#1~#4) 뒤에 챕터의 엔딩 컷씬(#End)이 이어진다.
    ///    화면 전체에 한 장씩, 누를 때마다(스페이스 · 엔터도) 다음 장으로 부드럽게 넘어간다.
    /// 2. 결과 — 마지막 컷 위에 지목 결과 한 줄과 '타이틀로'.
    /// </summary>
    [AddComponentMenu("It is You/Ending View")]
    public class EndingView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button titleButton;

        [Header("엔딩 컷씬")]
        [Tooltip("컷 그림 두 장 — 번갈아 쓰며 겹쳐 넘긴다.")]
        [SerializeField] private Image cutA;
        [SerializeField] private Image cutB;
        [Tooltip("화면 전체를 덮는 투명 버튼. 누르면 다음 장.")]
        [SerializeField] private Button advanceArea;
        [SerializeField] private GameObject advanceHint;
        [Tooltip("결과 글자 · 타이틀 버튼 묶음. 컷씬이 다 끝난 뒤 나타난다.")]
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private float crossFadeSeconds = 0.6f;
        [SerializeField] private float resultFadeSeconds = 0.8f;

        private bool _advance;

        private void Awake()
        {
            if (titleButton != null) titleButton.onClick.AddListener(() => SceneManager.LoadScene(SceneRoutes.Title));
            if (advanceArea != null) advanceArea.onClick.AddListener(() => { if (!PauseMenu.IsPaused) _advance = true; });
            if (root != null) root.SetActive(false);
            if (cutA != null) cutA.gameObject.SetActive(false);
            if (cutB != null) cutB.gameObject.SetActive(false);
            if (advanceArea != null) advanceArea.gameObject.SetActive(false);
            if (advanceHint != null) advanceHint.SetActive(false);
        }

        private void Update()
        {
            if (PauseMenu.IsPaused) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) _advance = true;
        }

        /// <summary>컷을 차례로 보여 준다. 장마다 한 번 누르면 다음으로, 마지막 장은 결과 뒤 배경으로 남는다.</summary>
        public IEnumerator PlayCutscenes(IList<Sprite> cuts)
        {
            if (root != null) root.SetActive(true);
            if (resultGroup != null) { resultGroup.alpha = 0f; resultGroup.interactable = false; resultGroup.blocksRaycasts = false; }
            if (cuts == null || cuts.Count == 0 || cutA == null || cutB == null) yield break;

            if (advanceArea != null) advanceArea.gameObject.SetActive(true);
            Image front = null;
            var back = cutA;
            for (int i = 0; i < cuts.Count; i++)
            {
                back.sprite = cuts[i];
                var fitter = back.GetComponent<AspectRatioFitter>();
                if (fitter != null && cuts[i] != null && cuts[i].rect.height > 0f)
                    fitter.aspectRatio = cuts[i].rect.width / cuts[i].rect.height;
                else back.preserveAspect = true;
                back.gameObject.SetActive(true);
                back.transform.SetSiblingIndex(Mathf.Max(cutA.transform.GetSiblingIndex(), cutB.transform.GetSiblingIndex()));   // 새 장이 위
                yield return Fade(back, front);
                front = back;
                back = front == cutA ? cutB : cutA;

                _advance = false;
                if (advanceHint != null) advanceHint.SetActive(true);
                while (!_advance) yield return null;
                _advance = false;
                if (advanceHint != null) advanceHint.SetActive(false);
            }
            if (advanceArea != null) advanceArea.gameObject.SetActive(false);
        }

        private IEnumerator Fade(Image incoming, Image outgoing)
        {
            var t = 0f;
            SetAlpha(incoming, 0f);
            while (t < crossFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / Mathf.Max(0.01f, crossFadeSeconds));
                SetAlpha(incoming, k * k * (3f - 2f * k));
                yield return null;
            }
            SetAlpha(incoming, 1f);
            if (outgoing != null) outgoing.gameObject.SetActive(false);
        }

        private static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }

        public void Show(string resultTitle, string accusedName, int clueCount)
        {
            if (root != null) root.SetActive(true);
            if (title != null) title.text = string.IsNullOrEmpty(resultTitle) ? "엔딩" : resultTitle;
            if (body != null) body.text = $"지목: {accusedName}\n수집한 단서 {clueCount}개";
            if (resultGroup != null) StartCoroutine(FadeResult());
        }

        private IEnumerator FadeResult()
        {
            resultGroup.interactable = true;
            resultGroup.blocksRaycasts = true;
            var t = 0f;
            while (t < resultFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                resultGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, resultFadeSeconds));
                yield return null;
            }
            resultGroup.alpha = 1f;
        }
    }
}
