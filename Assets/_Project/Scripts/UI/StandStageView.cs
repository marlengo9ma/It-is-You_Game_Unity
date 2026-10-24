using System.Collections;
using ItIsYou.Data;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 대화 화면에 서 있는 인물(세피 · 고양이)을 대사 묶음마다 켜고 끈다.
    ///
    /// 대사 묶음의 '스탠드'(<see cref="DialogueSequence.Stand"/>)를 따르고, '화면 기본값'이면 이 화면의 기본 구성을 쓴다.
    /// 대사 한 줄에 스탠드가 적혀 있으면 그 줄부터 바꾼다(묶음이 끝날 때까지).
    /// 화면 전환 전에 미리 알려 주면(<see cref="DialoguePresenter.Prepare"/>) 보이기 전에 바로 맞추고,
    /// 화면이 보이는 중에 바뀌면 부드럽게 나타나고 사라진다.
    /// 화면(Screen_Talk 등)은 꺼져 있는 동안이 많으므로 이 컴포넌트는 늘 켜져 있는 오브젝트(StandStages)에 둔다.
    /// </summary>
    [AddComponentMenu("It is You/Stand Stage View")]
    public class StandStageView : MonoBehaviour
    {
        [SerializeField] private DialoguePresenter dialogue;
        [Tooltip("세피 자리(스탠드 일러스트 / Live2D). CanvasGroup 이 붙어 있다.")]
        [SerializeField] private CanvasGroup sepi;
        [Tooltip("고양이 자리.")]
        [SerializeField] private CanvasGroup cat;
        [Tooltip("대사 묶음이 '화면 기본값'일 때 서 있을 인물.")]
        [SerializeField] private DialogueStand defaultStand = DialogueStand.SepiAndCat;
        [SerializeField] private float fadeSeconds = 0.3f;

        private Coroutine _sepiFade, _catFade;

        public DialogueStand Current { get; private set; } = DialogueStand.Default;

        private void Awake()
        {
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>(FindObjectsInactive.Include);
            if (dialogue != null)
            {
                dialogue.SequencePreparing += OnPreparing;
                dialogue.SequenceStarted += OnSequence;
                dialogue.LineStarted += OnLine;
            }
            Apply(defaultStand, instant: true);
        }

        private void OnDestroy()
        {
            if (dialogue == null) return;
            dialogue.SequencePreparing -= OnPreparing;
            dialogue.SequenceStarted -= OnSequence;
            dialogue.LineStarted -= OnLine;
        }

        private DialogueStand Resolve(DialogueStand s) => s == DialogueStand.Default ? defaultStand : s;

        private void OnPreparing(DialogueSequence seq) => Apply(Resolve(seq.Stand), instant: true);

        private void OnSequence(DialogueSequence seq)
        {
            if (seq == null) return;
            Apply(Resolve(seq.Stand), instant: false);
        }

        private void OnLine(DialogueLine line)
        {
            if (line == null || line.Stand == DialogueStand.Default) return;
            Apply(line.Stand, instant: false);
        }

        /// <summary>서 있는 인물을 바꾼다. instant 면 바로, 아니면 fadeSeconds 동안.</summary>
        public void Apply(DialogueStand stand, bool instant)
        {
            stand = Resolve(stand);
            Current = stand;
            Set(sepi, stand.HasSepi(), instant, ref _sepiFade);
            Set(cat, stand.HasCat(), instant, ref _catFade);
        }

        private void Set(CanvasGroup g, bool on, bool instant, ref Coroutine running)
        {
            if (g == null) return;
            if (running != null) { StopCoroutine(running); running = null; }
            g.blocksRaycasts = on;
            // 화면이 안 보이는 중이면 기다릴 것 없이 바로 맞춘다.
            if (instant || !isActiveAndEnabled || !g.gameObject.activeInHierarchy || fadeSeconds <= 0f) { g.alpha = on ? 1f : 0f; return; }
            running = StartCoroutine(Fade(g, on ? 1f : 0f));
        }

        private IEnumerator Fade(CanvasGroup g, float target)
        {
            var from = g.alpha;
            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            g.alpha = target;
        }
    }
}
