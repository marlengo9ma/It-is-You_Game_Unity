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

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        /// <summary>덮었다가 걷는다. 사이에 화면을 갈아 끼우려면 <paramref name="between"/> 을 넘긴다.</summary>
        public IEnumerator Transition(System.Action between = null)
        {
            yield return To(1f);
            if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);
            between?.Invoke();
            yield return To(0f);
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
