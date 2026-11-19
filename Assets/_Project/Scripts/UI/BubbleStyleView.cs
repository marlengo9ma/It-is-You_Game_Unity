using ItIsYou.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 심문 말풍선의 모양을 대사마다 바꾼다 — 일반 / 짜증(분노) / 불안(당황) / 기쁨.
    ///
    /// 어떤 모양을 쓸지는 대사 줄의 '말풍선' 칸이 정한다(데이터 에셋에서 줄마다 고른다).
    /// '자동'이면 그 줄의 표정을 보고 고른다. 말풍선에 나오는 줄(조수 · 고양이 · 용의자)일 때만 바꾼다.
    /// </summary>
    [AddComponentMenu("It is You/Bubble Style View")]
    public class BubbleStyleView : MonoBehaviour
    {
        [SerializeField] private DialoguePresenter dialogue;
        [Tooltip("말풍선 판.")]
        [SerializeField] private Image panel;
        [Tooltip("말풍선 꼬리.")]
        [SerializeField] private Image tail;

        [Header("판 — 일반 / 짜증 / 불안 / 기쁨")]
        [SerializeField] private Sprite normal;
        [SerializeField] private Sprite angry;
        [SerializeField] private Sprite anxious;
        [SerializeField] private Sprite joy;

        [Header("꼬리 — 일반 / 짜증 / 불안 / 기쁨")]
        [SerializeField] private Sprite tailNormal;
        [SerializeField] private Sprite tailAngry;
        [SerializeField] private Sprite tailAnxious;
        [SerializeField] private Sprite tailJoy;

        /// <summary>지금 보이는 모양 — 확인용.</summary>
        public BubbleStyle Current { get; private set; } = BubbleStyle.Normal;

        private void Awake()
        {
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>(FindObjectsInactive.Include);
            if (dialogue != null) dialogue.LineStarted += OnLine;
            Apply(BubbleStyle.Normal);
        }

        private void OnDestroy()
        {
            if (dialogue != null) dialogue.LineStarted -= OnLine;
        }

        private void OnLine(DialogueLine line)
        {
            if (line == null || !IsBubbleSpeaker(line.Speaker)) return;
            Apply(BubbleStyles.Resolve(line.Bubble, line.Expression));
        }

        private static bool IsBubbleSpeaker(SpeakerId s)
            => s == SpeakerId.Assistant || s == SpeakerId.Cat
            || s == SpeakerId.SuspectA || s == SpeakerId.SuspectB || s == SpeakerId.SuspectC;

        /// <summary>모양을 바로 바꾼다. 그림이 빈 모양은 일반으로.</summary>
        public void Apply(BubbleStyle style)
        {
            Current = style;
            var p = Pick(style, normal, angry, anxious, joy);
            var t = Pick(style, tailNormal, tailAngry, tailAnxious, tailJoy);
            if (panel != null && p != null) { panel.sprite = p; panel.type = Image.Type.Sliced; }
            if (tail != null && t != null) tail.sprite = t;
        }

        private static Sprite Pick(BubbleStyle s, Sprite n, Sprite a, Sprite x, Sprite j)
        {
            var r = s switch
            {
                BubbleStyle.Angry => a,
                BubbleStyle.Anxious => x,
                BubbleStyle.Joy => j,
                _ => n,
            };
            return r != null ? r : n;
        }
    }
}
