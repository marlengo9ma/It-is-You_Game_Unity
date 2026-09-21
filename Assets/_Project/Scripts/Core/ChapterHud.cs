using ItIsYou.Data;
using ItIsYou.Interrogation;
using ItIsYou.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Core
{
    /// <summary>
    /// 챕터 내내 화면 위에 떠 있는 표시들 — 페이즈 이름, 타임 토큰, 무반응 게이지, 상태 줄.
    ///
    /// 각 페이즈 컨트롤러는 자기 규칙만 알면 되도록, 화면 공통 표시는 여기로 모았다.
    /// 버튼 연결도 여기서 한다 — 에디터에서 붙인 연결은 씬에 저장되지 않기 때문이다.
    /// </summary>
    [AddComponentMenu("It is You/Chapter HUD")]
    public class ChapterHud : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private GameFlowController flow;
        [SerializeField] private InterrogationController interrogation;
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private CharacterClickTarget characterClickTarget;
        [SerializeField] private ClueTrayView clueTray;

        [Header("표시")]
        [SerializeField] private TMP_Text chapterTitle;
        [SerializeField] private TMP_Text phaseLabel;
        [SerializeField] private TMP_Text tokenLabel;
        [SerializeField] private TMP_Text tokenCaption;
        [SerializeField] private Image idleFill;
        [SerializeField] private TMP_Text statusLabel;

        [Header("버튼")]
        [SerializeField] private Button endInterrogationButton;

        private void Awake()
        {
            if (endInterrogationButton != null)
                endInterrogationButton.onClick.AddListener(OnEndInterrogation);

            if (characterClickTarget != null && interrogation != null)
                characterClickTarget.ReactionClicked += interrogation.TryCollectReaction;

            if (dialogue != null)
                dialogue.ReactionWindowChanged += OnReactionWindowChanged;

            if (interrogation != null)
            {
                interrogation.Finished += OnInterrogationFinished;
                interrogation.TutorialLessonRequested += OnTutorialLesson;
            }

            if (flow != null)
            {
                flow.PhaseChanged += OnPhaseChanged;
                flow.GameOver += OnGameOver;
                flow.ChapterCompleted += OnChapterCompleted;
                flow.ClueCollected += OnClueCollected;
            }

            if (clueTray != null)
            {
                clueTray.TooltipOpened += OnTooltipOpened;
                clueTray.TooltipClosed += OnTooltipClosed;
                clueTray.CluePromoted += OnCluePromoted;
            }

            SetTokenVisible(false);
            SetStatus(string.Empty);
        }

        private void Start()
        {
            if (chapterTitle != null && flow != null && flow.Chapter != null)
                chapterTitle.text = flow.Chapter.DisplayName;
        }

        private void OnDestroy()
        {
            if (endInterrogationButton != null)
                endInterrogationButton.onClick.RemoveListener(OnEndInterrogation);
            if (characterClickTarget != null && interrogation != null)
                characterClickTarget.ReactionClicked -= interrogation.TryCollectReaction;
            if (dialogue != null) dialogue.ReactionWindowChanged -= OnReactionWindowChanged;
            if (interrogation != null)
            {
                interrogation.Finished -= OnInterrogationFinished;
                interrogation.TutorialLessonRequested -= OnTutorialLesson;
            }
            if (flow != null)
            {
                flow.PhaseChanged -= OnPhaseChanged;
                flow.GameOver -= OnGameOver;
                flow.ChapterCompleted -= OnChapterCompleted;
                flow.ClueCollected -= OnClueCollected;
            }
            if (clueTray != null)
            {
                clueTray.TooltipOpened -= OnTooltipOpened;
                clueTray.TooltipClosed -= OnTooltipClosed;
                clueTray.CluePromoted -= OnCluePromoted;
            }
        }

        private void Update()
        {
            var running = interrogation != null && interrogation.IsRunning;
            var session = running ? interrogation.Session : null;

            if (session != null)
            {
                if (tokenLabel != null)
                    tokenLabel.text = $"{session.Tokens.Remaining} / {session.Tokens.Capacity}";

                // 무반응 게이지. 완성된 게임에서는 초침 소리가 이 역할을 하고,
                // 이 막대는 지금 규칙이 도는지 눈으로 확인하기 위한 것이다.
                if (idleFill != null) idleFill.fillAmount = session.Idle.Progress;
            }
            else if (idleFill != null && idleFill.fillAmount != 0f)
            {
                idleFill.fillAmount = 0f;
            }
        }

        // ------------------------------------------------------------ 페이즈

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phaseLabel != null) phaseLabel.text = PhaseName(phase);

            var inInterrogation = phase == GamePhase.Interrogation1 || phase == GamePhase.Interrogation2;
            SetTokenVisible(inInterrogation);

            SetStatus(phase switch
            {
                GamePhase.Intro => "화면을 눌러 넘기세요",
                GamePhase.ClueCollection => "살펴볼 곳을 눌러 수집하고, 우측 목록에서 읽어 보세요",
                GamePhase.Interrogation1 => "좌측 선택지를 고르세요",
                GamePhase.Deduction => "단서를 정리하는 중…",
                GamePhase.Interrogation2 => "좌측 선택지를 고르세요",
                GamePhase.Accusation => "누가 가져갔는지 지목하세요",
                _ => string.Empty
            });
        }

        private static string PhaseName(GamePhase phase) => phase switch
        {
            GamePhase.Intro => "사건 발생",
            GamePhase.ClueCollection => "단서 수집",
            GamePhase.Interrogation1 => "1차 심문",
            GamePhase.Deduction => "단서 조합",
            GamePhase.Interrogation2 => "2차 심문",
            GamePhase.Accusation => "범인 지목",
            GamePhase.Ending => "결말",
            GamePhase.GameOver => "게임 오버",
            _ => string.Empty
        };

        private void SetTokenVisible(bool visible)
        {
            if (tokenLabel != null) tokenLabel.gameObject.SetActive(visible);
            if (tokenCaption != null) tokenCaption.gameObject.SetActive(visible);
            if (idleFill != null) idleFill.transform.parent.gameObject.SetActive(visible);
            if (endInterrogationButton != null) endInterrogationButton.gameObject.SetActive(visible);
        }

        // -------------------------------------------------------------- 심문

        private void OnEndInterrogation()
        {
            if (interrogation != null && interrogation.IsRunning) interrogation.EndManually();
        }

        private void OnReactionWindowChanged(bool open, string clueId)
        {
            characterClickTarget?.SetArmed(open);
            if (open) SetStatus("지금 — 상대를 클릭하세요");
        }

        /// <summary>
        /// 단서를 주운 순간의 피드백.
        ///
        /// 목록에 칸이 하나 느는 것만으로는 우측 구석이라 눈에 안 띈다.
        /// 무엇을 주웠는지 종류까지 적어 줘야 "방금 그 클릭이 먹혔다"가 읽힌다.
        /// </summary>
        private void OnClueCollected(ClueDefinition clue)
        {
            if (clue == null) return;
            var kind = clue.Kind switch
            {
                ClueKind.Testimony => "증언",
                ClueKind.Reaction => "정황",
                ClueKind.Deduced => "추론",
                _ => "수집품"
            };
            SetStatus($"+ {kind} — 우측 목록에 추가되었습니다");
        }

        private void OnCluePromoted(ClueDefinition clue)
        {
            if (clue == null) return;
            SetStatus($"‘{clue.ReadName}’ 을(를) 단서로 정리했습니다");
        }

        private void OnTutorialLesson(PressureVerdict verdict)
        {
            var what = verdict switch
            {
                PressureVerdict.RefusedByFear => "강압이 과했습니다. 본 챕터였다면 여기서 심문이 끝났습니다.",
                PressureVerdict.RefusedByAnger => "질문 없이 주변만 살폈습니다. 본 챕터였다면 상대가 자리를 떴습니다.",
                PressureVerdict.GameOverByToneAlternation => "태도를 번복했습니다. 본 챕터였다면 게임오버입니다.",
                _ => verdict.ToString()
            };
            SetStatus("[튜토리얼 유예] " + what);
        }

        private void OnInterrogationFinished(InterrogationEndReason reason)
        {
            SetStatus(reason switch
            {
                InterrogationEndReason.PlayerEnded => "심문을 마쳤습니다.",
                InterrogationEndReason.TokensExhausted => "시간이 다했습니다.",
                InterrogationEndReason.RefusedByFear => "상대가 공포로 심문을 거부했습니다.",
                InterrogationEndReason.RefusedByAnger => "상대가 분노로 심문을 거부했습니다.",
                InterrogationEndReason.GameOverByErraticTone => "신뢰를 잃었습니다.",
                _ => string.Empty
            });
        }

        // ------------------------------------------------------------ 마무리

        private void OnGameOver(GameOverKind kind)
        {
            SetStatus(kind == GameOverKind.SelfLoathing
                ? "게임 오버 — 단서가 모자라 사건을 추합할 수 없었습니다."
                : "게임 오버 — 신뢰를 잃었습니다.");
        }

        private void OnChapterCompleted(string accusedId)
        {
            var session = flow != null ? flow.Session : null;
            var clues = session != null ? session.Clues.Count : 0;
            SetStatus($"챕터 종료 — 지목: {accusedId} · 수집한 단서 {clues}개");
        }

        // ------------------------------------------------------------ 타이머

        private void OnTooltipOpened()
        {
            if (interrogation != null && interrogation.IsRunning)
                interrogation.Session.Idle.Hold();
        }

        private void OnTooltipClosed()
        {
            if (interrogation != null && interrogation.IsRunning)
            {
                interrogation.Session.Idle.Release();
                interrogation.Session.Idle.Restart();
            }
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }
    }
}
