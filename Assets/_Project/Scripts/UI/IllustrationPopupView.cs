using System.Collections;
using ItIsYou.Core;
using ItIsYou.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 대사 중에 뜨는 일러스트 컷 — 예: 객실로 돌아왔을 때의 Scene#2.
    ///
    /// 대사 한 줄에 그림(<see cref="DialogueLine.Illustration"/>)이 붙어 있으면 그 줄이 나올 때 떠오르고,
    /// 다음 대사 묶음이 시작되거나 화면(페이즈)이 바뀌면 내려간다. 대사창보다 아래에 놓여 글이 그림 위로 읽힌다.
    /// 2026-10-24 2차부터 컷씬(#1)처럼 화면 전체에 꽉 차게 뜬다(fitter 가 그림 비율로 화면을 덮는다).
    /// 자리 · 크기는 UI 편집기로 바꾼다(IllustrationPopup › Frame).
    /// </summary>
    [AddComponentMenu("It is You/Illustration Popup View")]
    public class IllustrationPopupView : MonoBehaviour
    {
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private GameFlowController flow;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform frame;
        [SerializeField] private Image image;
        [Tooltip("그림 비율에 맞춰 화면을 꽉 덮는 판. 비어 있으면 그림을 판 안에 비율대로 넣는다.")]
        [SerializeField] private AspectRatioFitter fitter;
        [SerializeField] private float fadeSeconds = 0.45f;
        [Tooltip("떠오를 때 이 배율에서 1 로.")]
        [SerializeField] private float startScale = 0.94f;

        private Coroutine _anim;
        private bool _shown;

        private void Awake()
        {
            if (dialogue != null)
            {
                dialogue.LineStarted += OnLine;
                dialogue.SequenceStarted += OnSequence;
            }
            if (flow != null) flow.PhaseChanged += OnPhase;
            SetHidden();
        }

        private void OnDestroy()
        {
            if (dialogue != null)
            {
                dialogue.LineStarted -= OnLine;
                dialogue.SequenceStarted -= OnSequence;
            }
            if (flow != null) flow.PhaseChanged -= OnPhase;
        }

        private void OnLine(DialogueLine line)
        {
            if (line != null && line.Illustration != null) Show(line.Illustration);
        }

        private void OnSequence(DialogueSequence _) => Hide();
        private void OnPhase(GamePhase _) => SetHidden();

        public void Show(Sprite sprite)
        {
            if (image != null) { image.sprite = sprite; image.preserveAspect = fitter == null; image.color = Color.white; }
            if (fitter != null && sprite != null && sprite.rect.height > 0f) fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            if (_shown) return;
            _shown = true;
            gameObject.SetActive(true);
            Run(Animate(1f));
        }

        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            if (!isActiveAndEnabled) { SetHidden(); return; }
            Run(Animate(0f));
        }

        private void SetHidden()
        {
            _shown = false;
            if (_anim != null && isActiveAndEnabled) StopCoroutine(_anim);
            _anim = null;
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
        }

        private void Run(IEnumerator routine)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(routine);
        }

        private IEnumerator Animate(float target)
        {
            if (group == null) yield break;
            var from = group.alpha;
            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
                k = k * k * (3f - 2f * k);
                var a = Mathf.Lerp(from, target, k);
                group.alpha = a;
                if (frame != null && target > 0f) frame.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, k);
                yield return null;
            }
            group.alpha = target;
            if (frame != null) frame.localScale = Vector3.one;
            _anim = null;
        }
    }
}
