using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 인트로 한 페이지 (C1 ~ C5). 자식으로 놓인 컷(<see cref="ComicCutView"/>)을 순번대로 띄운다.
    ///
    /// 재생 규칙
    /// · 같은 순번의 컷은 함께 나온다. 순번 사이에는 각 컷의 「기다리는 시간」만큼 쉰다.
    /// · 컷이 들어오는 중에 화면을 누르면 그 컷이 바로 제자리에 놓인다(다음 컷은 정해진 대로 이어진다).
    /// · 다 나오면 「화면을 누르면 다음」 안내가 뜨고, 누르면 페이지가 사라진다.
    /// · 페이지에 <see cref="ComicEnvelope"/> 가 붙어 있으면 컷이 다 나온 뒤 봉투를 눌러야 넘어간다.
    /// </summary>
    [AddComponentMenu("It is You/Comic Page View")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class ComicPageView : MonoBehaviour
    {
        [Tooltip("첫 컷이 나오기 전에 비어 있는 페이지로 머무는 시간(초).")]
        [SerializeField] private float startDelay = 0.2f;

        [Tooltip("다 나온 뒤 화면을 눌러야 다음 페이지로 넘어가는가. 끄면 아래 시간만큼 머문 뒤 저절로 넘어간다.")]
        [SerializeField] private bool waitClickAtEnd = true;

        [Tooltip("자동으로 넘어갈 때 다 나온 화면에 머무는 시간(초).")]
        [SerializeField] private float holdSeconds = 1.2f;

        [Tooltip("페이지가 사라지는 데 걸리는 시간(초).")]
        [SerializeField] private float exitFadeSeconds = 0.35f;

        private CanvasGroup _group;

        public IReadOnlyList<ComicCutView> Cuts => GetComponentsInChildren<ComicCutView>(true);

        private CanvasGroup Group
        {
            get
            {
                if (_group == null) _group = GetComponent<CanvasGroup>();
                return _group;
            }
        }

        /// <summary>재생 전 상태 — 페이지는 켜고, 컷은 전부 출발 자리에 숨긴다.</summary>
        public void Prepare()
        {
            gameObject.SetActive(true);
            Group.alpha = 1f;
            Group.blocksRaycasts = true;
            foreach (var cut in Cuts) cut.ResetToStart();
            var envelope = GetComponent<ComicEnvelope>();
            if (envelope != null) envelope.Prepare();
        }

        public void Hide()
        {
            Group.alpha = 0f;
            gameObject.SetActive(false);
        }

        public IEnumerator Play(IComicDirector d)
        {
            yield return Wait(d, startDelay, skippable: true);

            // 순번별로 묶는다.
            var groups = new SortedDictionary<int, List<ComicCutView>>();
            foreach (var cut in Cuts)
            {
                if (!cut.gameObject.activeSelf) continue;   // 꺼 둔 컷은 건너뛴다
                if (!groups.TryGetValue(cut.Order, out var list)) groups[cut.Order] = list = new List<ComicCutView>();
                list.Add(cut);
            }

            foreach (var pair in groups)
            {
                if (d.Skipping) yield break;
                var cuts = pair.Value;

                var needClick = false;
                var delay = 0f;
                foreach (var c in cuts) { needClick |= c.WaitForClick; delay = Mathf.Max(delay, c.Delay); }

                if (needClick)
                {
                    // 클릭으로 나오는 컷은 누르자마자 움직인다 — 기다리는 시간(Delay)은 자동으로 이어질 때만 쓴다.
                    d.SetAdvanceHint(true);
                    d.ClearClicks();
                    while (!d.ConsumeClick() && !d.Skipping) yield return null;
                    d.SetAdvanceHint(false);
                }
                else
                {
                    yield return Wait(d, delay, skippable: true);
                }

                yield return Animate(d, cuts);
            }

            if (d.Skipping) yield break;

            var envelope = GetComponent<ComicEnvelope>();
            if (envelope != null)
            {
                yield return envelope.Run(d);
                if (d.Skipping) yield break;
            }
            else if (waitClickAtEnd)
            {
                d.SetAdvanceHint(true);
                d.ClearClicks();
                while (!d.ConsumeClick() && !d.Skipping) yield return null;
                d.SetAdvanceHint(false);
            }
            else
            {
                yield return Wait(d, holdSeconds, skippable: true);
            }

            yield return FadeOut(d);
        }

        private static IEnumerator Animate(IComicDirector d, List<ComicCutView> cuts)
        {
            var longest = 0f;
            foreach (var c in cuts) longest = Mathf.Max(longest, c.Duration);

            d.ClearClicks();
            var t = 0f;
            while (t < longest)
            {
                if (d.Skipping) break;
                if (d.ConsumeClick()) break;           // 누르면 이 순번의 컷이 바로 제자리에 놓인다
                if (!d.Paused) t += Time.unscaledDeltaTime;
                foreach (var c in cuts) c.Apply(c.Duration <= 0f ? 1f : t / c.Duration);
                yield return null;
            }
            foreach (var c in cuts) c.Complete();
        }

        private IEnumerator FadeOut(IComicDirector d)
        {
            var t = 0f;
            while (t < exitFadeSeconds)
            {
                if (!d.Paused) t += Time.unscaledDeltaTime;
                Group.alpha = 1f - Mathf.Clamp01(t / exitFadeSeconds);
                yield return null;
            }
            Hide();
        }

        /// <summary>시간을 기다린다. skippable 이면 화면을 눌러 건너뛸 수 있다.</summary>
        private static IEnumerator Wait(IComicDirector d, float seconds, bool skippable)
        {
            if (seconds <= 0f) yield break;
            d.ClearClicks();
            var t = 0f;
            while (t < seconds)
            {
                if (d.Skipping) yield break;
                if (skippable && d.ConsumeClick()) yield break;
                if (!d.Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
