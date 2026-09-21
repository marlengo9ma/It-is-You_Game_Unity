using System.Collections;
using TMPro;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 화면 가운데 잠깐 떠오르는 제목. 심문 진입의 "&lt;용의자 ‘세피’ 심문 시작&gt;
    /// *텍스트 이미지 애니메이션 및 소리 연출" 자리다.
    ///
    /// 지금은 글자가 살짝 커지며 떠올랐다 사라진다. 텍스트 이미지와 소리가 오면 여기만 바꾼다.
    /// 컴포넌트가 루트 자체에 붙어 꺼진 채 저장되므로, Awake 에서 루트를 다시 끄지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Title Card View")]
    public class TitleCardView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text label;
        [SerializeField] private AudioSource stinger;

        [SerializeField] private float fadeInSeconds = 0.35f;
        [SerializeField] private float holdSeconds = 1.1f;
        [SerializeField] private float fadeOutSeconds = 0.4f;
        [SerializeField] private float scaleFrom = 0.92f;

        private bool _playing;

        private void Awake()
        {
            if (group == null || _playing) return;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            if (group.gameObject != gameObject) group.gameObject.SetActive(false);
        }

        public IEnumerator Play(string text)
        {
            if (group == null || string.IsNullOrEmpty(text)) yield break;

            _playing = true;
            if (label != null) label.text = text;
            group.gameObject.SetActive(true);
            group.alpha = 0f;
            group.blocksRaycasts = true;
            if (stinger != null) stinger.Play();

            yield return Fade(0f, 1f, fadeInSeconds, scaleFrom, 1f);
            if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Fade(1f, 0f, fadeOutSeconds, 1f, 1.03f);

            group.blocksRaycasts = false;
            _playing = false;
            group.gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to, float seconds, float scaleA, float scaleB)
        {
            var t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                var k = seconds <= 0f ? 1f : Mathf.Clamp01(t / seconds);
                var eased = 1f - (1f - k) * (1f - k);
                group.alpha = Mathf.Lerp(from, to, eased);
                if (card != null) card.localScale = Vector3.one * Mathf.Lerp(scaleA, scaleB, eased);
                yield return null;
            }
            group.alpha = to;
            if (card != null) card.localScale = Vector3.one * scaleB;
        }
    }
}
