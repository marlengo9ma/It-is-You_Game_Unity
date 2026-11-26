using System.Collections;
using System.Collections.Generic;
using ItIsYou.Core;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 챕터 엔딩 화면.
    ///
    /// 2026-11-27 — 엔딩(End_) 에셋의 단계를 GameFlowController 가 하나씩 넘기고, 이 화면은 그림과 연출만 맡는다:
    /// 컷 그림을 겹쳐 바꾸기(<see cref="ShowCut"/>), 까만 화면, 흔들림 · 번쩍임 · 어두워짐(<see cref="PlayEffect"/>),
    /// 엔딩이 끝난 뒤의 제목 카드(<see cref="ShowTitleCard"/>). 대사는 대사창이 이 화면 위에 띄운다.
    ///
    /// 예전 방식(<see cref="PlayCutscenes"/> + <see cref="Show"/>)은 엔딩 에셋이 없을 때를 위해 남겨 둔다.
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

        [Header("연출 (2026-11-27)")]
        [Tooltip("어두워짐 · 번쩍임 막이 들어갈 자리 — 이 오브젝트 바로 아래(뒤)에 끼운다. 대사창이 막 위에 남도록 대사창을 넣는다. 비우면 캔버스에서 'DialoguePanel' 을 찾는다.")]
        [SerializeField] private RectTransform overlayBelow;
        [SerializeField] private float shakeSeconds = 0.45f;
        [SerializeField] private float shakePixels = 18f;
        [SerializeField] private float flashSeconds = 0.5f;
        [SerializeField] private float darkenSeconds = 1.6f;
        [Range(0f, 1f)]
        [SerializeField] private float darkenAlpha = 0.82f;

        private bool _advance;
        private Image _front;
        private Image _overlay;

        private void Awake()
        {
            if (titleButton != null) titleButton.onClick.AddListener(() => { PauseMenu.ForceUnpause(); SceneManager.LoadScene(SceneRoutes.Title); });
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

        // ================================================================ 단계별 (2026-11-27)

        /// <summary>엔딩 화면을 연다. 결과 묶음은 숨긴다.</summary>
        public void Open()
        {
            if (root != null) root.SetActive(true);
            HideResult();
        }

        /// <summary>컷을 곧바로 깐다(화면 전환 막 뒤에서 부를 때).</summary>
        public void SetCutInstant(Sprite sprite)
        {
            Open();
            if (cutA == null) return;
            if (cutB != null) cutB.gameObject.SetActive(false);
            if (sprite == null) { cutA.gameObject.SetActive(false); _front = null; return; }
            Place(cutA, sprite);
            SetAlpha(cutA, 1f);
            _front = cutA;
        }

        /// <summary>지금 컷 위로 새 컷을 겹쳐 바꾼다. 같은 그림이면 아무 일도 없다.</summary>
        public IEnumerator ShowCut(Sprite sprite)
        {
            Open();
            if (cutA == null || cutB == null || sprite == null) yield break;
            if (_front != null && _front.gameObject.activeSelf && _front.sprite == sprite) yield break;
            var back = _front == cutA ? cutB : cutA;
            Place(back, sprite);
            back.transform.SetSiblingIndex(Mathf.Max(cutA.transform.GetSiblingIndex(), cutB.transform.GetSiblingIndex()));   // 새 장이 위
            var outgoing = _front != null && _front.gameObject.activeSelf ? _front : null;
            yield return Fade(back, outgoing);
            _front = back;
        }

        /// <summary>컷을 걷는다 — 엔딩 화면의 바탕만 남는다.</summary>
        public void ClearCuts()
        {
            if (cutA != null) cutA.gameObject.SetActive(false);
            if (cutB != null) cutB.gameObject.SetActive(false);
            _front = null;
        }

        /// <summary>클릭(스페이스 · 엔터)을 기다린다 — 대사 없이 컷만 보여 주는 단계.</summary>
        public IEnumerator WaitClick()
        {
            if (advanceArea != null) advanceArea.gameObject.SetActive(true);
            _advance = false;
            yield return null;
            _advance = false;
            if (advanceHint != null) advanceHint.SetActive(true);
            while (!_advance) yield return null;
            _advance = false;
            if (advanceHint != null) advanceHint.SetActive(false);
            if (advanceArea != null) advanceArea.gameObject.SetActive(false);
        }

        /// <summary>단계 시작의 연출.</summary>
        public IEnumerator PlayEffect(EndingEffect effect)
        {
            switch (effect)
            {
                case EndingEffect.Shake: yield return Shake(); break;
                case EndingEffect.Flash: yield return Flash(); break;
                case EndingEffect.Darken: yield return OverlayTo(new Color(0f, 0f, 0f, darkenAlpha), darkenSeconds); break;
                case EndingEffect.Brighten: yield return OverlayTo(new Color(0f, 0f, 0f, 0f), darkenSeconds * 0.6f); break;
            }
        }

        /// <summary>까만 화면 — 컷을 걷고 막을 완전히 까맣게.</summary>
        public void SetBlack()
        {
            Open();
            ClearCuts();
            var o = Overlay();
            if (o != null) { o.color = Color.black; o.gameObject.SetActive(true); }
        }

        /// <summary>어두워짐 · 까만 막을 걷는다. 화면이 바뀌는 단계마다 부른다.</summary>
        public void ResetEffects()
        {
            if (_overlay != null) { _overlay.color = new Color(0f, 0f, 0f, 0f); _overlay.gameObject.SetActive(false); }
        }

        /// <summary>엔딩이 끝난 뒤의 제목 카드. 누르면 걷힌다.</summary>
        public IEnumerator ShowTitleCard(string heading, string sub)
        {
            Open();
            if (resultGroup == null) yield break;
            if (title != null) title.text = heading ?? string.Empty;
            if (body != null) body.text = sub ?? string.Empty;
            if (titleButton != null) titleButton.gameObject.SetActive(false);
            yield return FadeResult(1f);
            yield return WaitClick();
            yield return FadeResult(0f);
            HideResult();
        }

        /// <summary>엔딩 화면을 닫는다.</summary>
        public void Close()
        {
            ResetEffects();
            ClearCuts();
            HideResult();
            if (root != null) root.SetActive(false);
        }

        // ---------------------------------------------------------------- 연출 보조

        private Image Overlay()
        {
            if (_overlay != null) return _overlay;
            var canvasRoot = root != null ? root.transform.parent : transform;
            if (canvasRoot == null) return null;
            var below = overlayBelow != null ? overlayBelow : canvasRoot.Find("DialoguePanel") as RectTransform;
            var go = new GameObject("EndingOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            if (below != null && below.parent == canvasRoot) rt.SetSiblingIndex(below.GetSiblingIndex());
            _overlay = go.GetComponent<Image>();
            _overlay.raycastTarget = false;
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            go.SetActive(false);
            return _overlay;
        }

        private IEnumerator OverlayTo(Color target, float seconds)
        {
            var o = Overlay();
            if (o == null) yield break;
            o.gameObject.SetActive(true);
            var from = o.color;
            if (from.a <= 0.001f) from = new Color(target.r, target.g, target.b, 0f);
            var t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / Mathf.Max(0.01f, seconds));
                o.color = Color.Lerp(from, target, k * k * (3f - 2f * k));
                yield return null;
            }
            o.color = target;
            if (target.a <= 0.001f) o.gameObject.SetActive(false);
        }

        /// <summary>하얗게 번쩍였다가 원래 막(어두워진 상태 포함)으로 돌아온다.</summary>
        private IEnumerator Flash()
        {
            var o = Overlay();
            if (o == null) yield break;
            var keep = o.gameObject.activeSelf ? o.color : new Color(0f, 0f, 0f, 0f);
            o.gameObject.SetActive(true);
            var white = new Color(1f, 1f, 1f, 0.9f);
            var t = 0f;
            while (t < flashSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / Mathf.Max(0.01f, flashSeconds));
                o.color = Color.Lerp(white, keep, k);
                yield return null;
            }
            o.color = keep;
            if (keep.a <= 0.001f) o.gameObject.SetActive(false);
        }

        /// <summary>켜져 있는 화면(Screen_*)들을 잠깐 흔든다.</summary>
        private IEnumerator Shake()
        {
            var canvasRoot = root != null ? root.transform.parent : null;
            if (canvasRoot == null) yield break;
            var targets = new List<RectTransform>();
            var origins = new List<Vector2>();
            foreach (Transform child in canvasRoot)
            {
                if (!child.gameObject.activeInHierarchy || !child.name.StartsWith("Screen_")) continue;
                var rt = child as RectTransform;
                if (rt == null) continue;
                targets.Add(rt);
                origins.Add(rt.anchoredPosition);
            }
            var t = 0f;
            while (t < shakeSeconds)
            {
                t += Time.unscaledDeltaTime;
                var fall = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, shakeSeconds));
                var off = Random.insideUnitCircle * shakePixels * fall;
                for (int i = 0; i < targets.Count; i++) targets[i].anchoredPosition = origins[i] + off;
                yield return null;
            }
            for (int i = 0; i < targets.Count; i++) targets[i].anchoredPosition = origins[i];
        }

        private static void Place(Image img, Sprite sprite)
        {
            img.sprite = sprite;
            var fitter = img.GetComponent<AspectRatioFitter>();
            if (fitter != null && sprite != null && sprite.rect.height > 0f)
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            else img.preserveAspect = true;
            img.gameObject.SetActive(true);
        }

        private void HideResult()
        {
            if (resultGroup != null) { resultGroup.alpha = 0f; resultGroup.interactable = false; resultGroup.blocksRaycasts = false; }
        }

        private IEnumerator FadeResult(float target)
        {
            if (resultGroup == null) yield break;
            var from = resultGroup.alpha;
            var t = 0f;
            while (t < resultFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                resultGroup.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / Mathf.Max(0.01f, resultFadeSeconds)));
                yield return null;
            }
            resultGroup.alpha = target;
        }

        // ================================================================ 예전 방식

        /// <summary>컷을 차례로 보여 준다. 장마다 한 번 누르면 다음으로, 마지막 장은 결과 뒤 배경으로 남는다.</summary>
        public IEnumerator PlayCutscenes(IList<Sprite> cuts)
        {
            if (root != null) root.SetActive(true);
            HideResult();
            if (cuts == null || cuts.Count == 0 || cutA == null || cutB == null) yield break;
            for (int i = 0; i < cuts.Count; i++)
            {
                yield return ShowCut(cuts[i]);
                yield return WaitClick();
            }
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
            if (titleButton != null) titleButton.gameObject.SetActive(true);
            if (resultGroup != null) StartCoroutine(ShowResult());
        }

        private IEnumerator ShowResult()
        {
            resultGroup.interactable = true;
            resultGroup.blocksRaycasts = true;
            yield return FadeResult(1f);
        }
    }
}
