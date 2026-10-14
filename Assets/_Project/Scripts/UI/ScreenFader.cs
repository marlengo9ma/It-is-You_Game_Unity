using System.Collections;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 화면 전환용 검은 막.
    ///
    /// 기획서가 &lt;화면 전환&gt; 이라고 적어 둔 자리마다 한 번씩 덮었다 걷는다.
    /// 장면이 툭 바뀌면 같은 장소에서 시간만 흐른 것처럼 보이기 때문에,
    /// 끊어 주는 한 박자가 있어야 "다른 장면"으로 읽힌다.
    /// </summary>
    [AddComponentMenu("It is You/Screen Fader")]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeSeconds = 0.4f;

        [Tooltip("완전히 덮인 채로 머무는 시간(초).")]
        [SerializeField] private float holdSeconds = 0.25f;

        private void Awake() => Clear();

        private void Clear()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

#if UNITY_EDITOR
        // 이 막은 화면 전체를 덮는 검은 판이다. 알파가 1인 채로 씬에 저장되면
        // 플레이를 누르기 전의 게임 뷰가 통째로 까맣게 보인다 — 실제로 한 번 그랬다.
        // 에디터에서는 항상 걷힌 상태로 둔다.
        private void Reset() => Clear();
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            Clear();
        }
#endif

        /// <summary>덮었다가 걷는다. 사이에 화면을 갈아 끼우려면 <paramref name="between"/> 을 넘긴다.</summary>
        public IEnumerator Transition(System.Action between = null)
        {
            yield return To(1f);
            if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);
            between?.Invoke();
            yield return To(0f);
        }

        /// <summary>바로 까맣게 덮는다. 씬이 시작되는 첫 프레임에 부르면 화면이 덮인 채로 뜬다.</summary>
        public void Cover()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        /// <summary>덮인 화면을 <paramref name="seconds"/> 초에 걸쳐 걷는다 (씬 첫 진입용 — 보통 전환보다 길게).</summary>
        public IEnumerator FadeIn(float seconds)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) yield break;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            // 씬을 막 불러온 직후의 첫 프레임은 로딩 시간이 통째로 delta 에 들어와 페이드가 한 번에 끝나 버린다.
            // 한 프레임 쉬고, 한 프레임에 흐르는 시간도 1/30초로 묶는다.
            yield return null;
            var t = 0f;
            while (t < seconds)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                // 시작과 끝을 부드럽게 (smoothstep)
                var k = Mathf.Clamp01(t / seconds);
                group.alpha = 1f - k * k * (3f - 2f * k);
                yield return null;
            }
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public IEnumerator To(float target)
        {
            if (group == null) yield break;

            group.blocksRaycasts = target > 0.01f;

            var from = group.alpha;
            if (fadeSeconds <= 0f)
            {
                group.alpha = target;
                group.blocksRaycasts = target > 0.99f;
                yield break;
            }

            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, target, t / fadeSeconds);
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.99f;
        }
    }
}
