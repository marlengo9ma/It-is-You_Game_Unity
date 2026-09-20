using System.Collections.Generic;
using ItIsYou.Characters;
using ItIsYou.Data;
using ItIsYou.Interrogation;
using ItIsYou.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Core
{
    /// <summary>
    /// 심문 화면 하나만 띄워 보기 위한 시험용 진행자.
    ///
    /// 정식 흐름은 <see cref="GameFlowController"/> 가 인트로부터 순서대로 돌린다.
    /// 이건 그 중간을 건너뛰고 심문 화면만 바로 보기 위한 것이라,
    /// 단서 수집 페이즈에서 얻었어야 할 단서를 미리 지급한 상태로 시작한다.
    ///
    /// 전체 루프가 붙으면 지워도 되는 파일이다.
    /// </summary>
    [AddComponentMenu("It is You/Interrogation Test Driver")]
    public class InterrogationTestDriver : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private ChapterDefinition chapter;
        [SerializeField] private SuspectDefinition suspect;

        [Tooltip("단서 수집 페이즈를 건너뛰므로, 여기 넣은 단서를 가진 상태로 시작한다.")]
        [SerializeField] private List<ClueDefinition> startingClues = new List<ClueDefinition>();

        [Header("연결")]
        [SerializeField] private InterrogationController interrogation;
        [SerializeField] private PlaceholderCharacterView characterView;
        [SerializeField] private CharacterClickTarget characterClickTarget;
        [SerializeField] private ClueTrayView clueTray;
        [SerializeField] private DialoguePresenter dialogue;

        [Header("상태 표시")]
        [SerializeField] private TMP_Text tokenLabel;
        [SerializeField] private TMP_Text suspectLabel;
        [SerializeField] private Image idleFill;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button endButton;

        private GameSession _session;

        private void Start()
        {
            if (chapter == null || suspect == null || interrogation == null)
            {
                Debug.LogError("[테스트 드라이버] 챕터 · 용의자 · 심문 컨트롤러가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            _session = new GameSession();
            foreach (var clue in startingClues) _session.Clues.Add(clue);

            if (suspectLabel != null) suspectLabel.text = suspect.DisplayName;

            interrogation.Finished += OnFinished;
            interrogation.TutorialLessonRequested += OnTutorialLesson;

            if (dialogue != null)
                dialogue.ReactionWindowChanged += OnReactionWindowChanged;

            if (characterClickTarget != null)
                characterClickTarget.ReactionClicked += interrogation.TryCollectReaction;

            if (clueTray != null)
            {
                clueTray.Rebuild(_session.Clues);
                clueTray.TooltipOpened += OnTooltipOpened;
                clueTray.TooltipClosed += OnTooltipClosed;
            }

            if (endButton != null)
                endButton.onClick.AddListener(() => interrogation.EndManually());

            _session.Clues.ClueAdded += _ => clueTray?.Rebuild(_session.Clues);

            interrogation.Begin(chapter, suspect, _session.Clues, characterView);
            SetStatus("심문 시작 — 좌측 선택지를 고르세요");
        }

        private void OnDestroy()
        {
            if (interrogation != null)
            {
                interrogation.Finished -= OnFinished;
                interrogation.TutorialLessonRequested -= OnTutorialLesson;
            }
            if (dialogue != null) dialogue.ReactionWindowChanged -= OnReactionWindowChanged;
            if (clueTray != null)
            {
                clueTray.TooltipOpened -= OnTooltipOpened;
                clueTray.TooltipClosed -= OnTooltipClosed;
            }
        }

        private void Update()
        {
            var session = interrogation != null ? interrogation.Session : null;
            if (session == null) return;

            if (tokenLabel != null)
                tokenLabel.text = $"{session.Tokens.Remaining} / {session.Tokens.Capacity}";

            // 무반응 게이지. 실제 게임에서는 초침 소리가 이 역할을 하고,
            // 이 막대는 지금 규칙이 도는지 눈으로 확인하기 위한 것이다.
            if (idleFill != null)
                idleFill.fillAmount = session.Idle.Progress;
        }

        private void OnReactionWindowChanged(bool open, string clueId)
        {
            characterClickTarget?.SetArmed(open);
            if (open) SetStatus("지금 — 조수를 클릭하세요");
        }

        private void OnTutorialLesson(PressureVerdict verdict)
        {
            // 튜토리얼이라 심문이 끊기지 않았다. 정식 구현에서는 설명 팝업이 뜬다.
            var what = verdict switch
            {
                PressureVerdict.RefusedByFear =>
                    "강압이 과했습니다. 본 챕터였다면 여기서 심문이 끝났습니다.",
                PressureVerdict.RefusedByAnger =>
                    "질문 없이 주변만 살폈습니다. 본 챕터였다면 상대가 자리를 떴습니다.",
                PressureVerdict.GameOverByToneAlternation =>
                    "태도를 번복했습니다. 본 챕터였다면 게임오버입니다.",
                _ => verdict.ToString()
            };
            SetStatus("[튜토리얼 유예] " + what);
            Debug.Log("[튜토리얼 유예] " + what);
        }

        private void OnFinished(InterrogationEndReason reason)
        {
            var text = reason switch
            {
                InterrogationEndReason.PlayerEnded => "심문을 마쳤습니다.",
                InterrogationEndReason.TokensExhausted => "시간이 다했습니다. 심문 종료.",
                InterrogationEndReason.RefusedByFear => "상대가 공포로 심문을 거부했습니다.",
                InterrogationEndReason.RefusedByAnger => "상대가 분노로 심문을 거부했습니다.",
                InterrogationEndReason.GameOverByErraticTone => "게임오버 — 태도 번복으로 신뢰를 잃었습니다.",
                _ => reason.ToString()
            };
            SetStatus(text + $"  (수집 단서 {_session.Clues.Count}개)");
            if (endButton != null) endButton.interactable = false;
        }

        private void OnTooltipOpened()
        {
            if (interrogation.IsRunning) interrogation.Session.Idle.Hold();
        }

        private void OnTooltipClosed()
        {
            if (interrogation.IsRunning)
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
