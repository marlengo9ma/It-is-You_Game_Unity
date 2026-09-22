using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Characters;
using ItIsYou.Clues;
using ItIsYou.Data;
using ItIsYou.Deduction;
using ItIsYou.Interrogation;
using ItIsYou.Investigation;
using ItIsYou.UI;
using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>
    /// 챕터 한 편의 페이즈 진행을 맡는다.
    ///
    /// Intro → 단서 수집 → 1차 심문 → 조합 → 2차 심문 → 범인 지목 → 엔딩.
    /// 각 페이즈의 내부 규칙은 각자의 컨트롤러가 알고 있고,
    /// 이 클래스는 "언제 다음으로 넘어가는가"만 판단한다.
    /// </summary>
    [AddComponentMenu("It is You/Game Flow Controller")]
    public class GameFlowController : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private GameDatabase database;
        [SerializeField] private ChapterDefinition chapter;

        [Header("페이즈 컨트롤러")]
        [SerializeField] private InvestigationController investigation;
        [SerializeField] private SuspectSelectController suspectSelect;
        [SerializeField] private InterrogationController interrogation;
        [SerializeField] private DeductionController deduction;

        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [Tooltip("<화면 전환> 표기가 있는 자리에서 화면을 덮었다 걷는다.")]
        [SerializeField] private ScreenFader fader;
        [SerializeField] private ClueTrayView clueTray;
        [SerializeField] private CursorController cursor;
        [Tooltip("게임오버 화면. 실패한 페이즈의 시작으로 되돌아가는 버튼이 있다.")]
        [SerializeField] private GameOverView gameOverView;

        [Header("캐릭터")]
        [Tooltip("용의자 id 와 화면 위 캐릭터 뷰를 짝지어 둔다.")]
        [SerializeField] private List<CharacterBinding> characterBindings = new List<CharacterBinding>();

        [Serializable]
        private struct CharacterBinding
        {
            public string suspectId;
            public MonoBehaviour view; // ICharacterView 를 구현한 컴포넌트
        }

        private readonly Queue<SuspectDefinition> _pending = new Queue<SuspectDefinition>();

        public ChapterDefinition Chapter => chapter;
        public GameSession Session { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.None;

        /// <summary>페이즈가 바뀌었다.</summary>
        public event Action<GamePhase> PhaseChanged;

        /// <summary>게임오버가 확정됐다.</summary>
        public event Action<GameOverKind> GameOver;

        /// <summary>챕터를 끝까지 마쳤다. (지목한 용의자 id)</summary>
        public event Action<string> ChapterCompleted;

        /// <summary>새 단서를 얻었다. HUD 가 피드백을 띄운다.</summary>
        public event Action<ClueDefinition> ClueCollected;

        private void Start()
        {
            if (chapter != null) BeginChapter(chapter);
        }

        public void BeginChapter(ChapterDefinition target)
        {
            chapter = target;
            Session = new GameSession();
            Session.GameOverTriggered += OnGameOver;

            if (clueTray != null)
            {
                clueTray.TooltipOpened += OnTooltipOpened;
                clueTray.TooltipClosed += OnTooltipClosed;
            }

            // 단서가 늘면 그 자리에서 목록을 다시 그린다.
            // 심문이 끝난 뒤에만 갱신하면, 밑줄을 눌러 증언을 주웠는데도
            // 화면에는 아무 일도 일어나지 않은 것처럼 보인다 — 실제로 그랬다.
            Session.Clues.ClueAdded += OnClueAdded;

            _chapterRoutine = StartCoroutine(RunChapter());
        }

        private IEnumerator RunChapter()
        {
            // ---- 사건 이전: [게임플레이 배경_캐릭터 씬] ----
            SetPhase(GamePhase.PreIncidentTalk);
            if (chapter.PreIncidentTalk != null && !chapter.PreIncidentTalk.IsEmpty)
                yield return dialogue.Play(chapter.PreIncidentTalk);

            // ---- [일러스트 컷씬#1] ----
            // 화장실 칸으로 자리를 옮기는 장면이다. 같은 배경 위에서 대사만 이어지면
            // 장소가 바뀐 것이 읽히지 않으므로, 화면을 한 번 덮었다 걷는다.
            if (chapter.Cutscene != null && !chapter.Cutscene.IsEmpty)
            {
                yield return Transition(GamePhase.Cutscene);
                yield return dialogue.Play(chapter.Cutscene);
            }

            // ---- 컷씬에서 돌아온 뒤: 다시 [게임플레이 배경_캐릭터 씬] ----
            if (chapter.AfterCutsceneTalk != null && !chapter.AfterCutsceneTalk.IsEmpty)
            {
                yield return Transition(GamePhase.PreIncidentTalk);
                yield return dialogue.Play(chapter.AfterCutsceneTalk);
            }

            // ---- <화면 전환> 사건 진행 #1 ----
            yield return Transition(GamePhase.Intro);
            if (chapter.Intro != null && !chapter.Intro.IsEmpty)
                yield return dialogue.Play(chapter.Intro);

            // ---- 단서 수집 ----
            // 기획서의 <튜토리얼 시작> <사건 시작/진입 연출> 자리.
            yield return Transition(GamePhase.ClueCollection);
            var collectionDone = false;
            void OnReady(CollectionGate _) => collectionDone = true;

            investigation.ReadyToAdvance += OnReady;
            investigation.Begin(chapter, Session.Clues);

            while (!collectionDone) yield return null;
            investigation.ReadyToAdvance -= OnReady;

            // ---- 누구부터 심문할지 고른다 ----
            // 두 사람을 자리에 앉히는 대화도 이 화면에서 이어진다.
            SuspectDefinition picked = null;
            if (suspectSelect != null)
            {
                void OnPicked(SuspectDefinition s) => picked = s;
                suspectSelect.Picked += OnPicked;

                yield return Transition(GamePhase.SuspectSelect);
                suspectSelect.Begin(chapter);

                while (picked == null) yield return null;
                suspectSelect.Picked -= OnPicked;
            }

            // ---- 1차 심문 ----
            // 고른 상대만 상대한다. 나머지 인물의 심문 스크립트는 아직 없다.
            var round = picked != null
                ? new List<SuspectDefinition> { picked }
                : new List<SuspectDefinition>(chapter.FirstRoundSuspects);

            yield return RunFromInterrogation(round);
        }

        /// <summary>1차 심문 → 조합 → 2차 심문 → 지목 → 결말.</summary>
        private IEnumerator RunFromInterrogation(List<SuspectDefinition> round)
        {
            yield return Transition(GamePhase.Interrogation1);
            yield return RunInterrogationRound(round, 1);
            if (Session.IsGameOver)
            {
                yield return ShowGameOver(allowRetry: false, retryText: null);
                yield break;
            }

            yield return RunFromDeduction(round);
        }

        /// <summary>단서 조합 → 2차 심문 → 지목 → 결말. 디버그 점프의 진입점이기도 하다.</summary>
        private IEnumerator RunFromDeduction(List<SuspectDefinition> round)
        {
            // ---- 단서 조합 ----
            // 잉크가 다 마른 채 또 틀리면 자기 비하 게임오버. 게임오버는 남되, 복귀 지점은 조합의 시작이다.
            while (true)
            {
                yield return Transition(GamePhase.Deduction);
                var deductionDone = false;
                var deductionOk = false;
                void OnDeduced(bool ok) { deductionOk = ok; deductionDone = true; }

                if (deduction != null)
                {
                    deduction.Finished += OnDeduced;
                    deduction.Begin(chapter, Session.Clues);
                    while (!deductionDone) yield return null;
                    deduction.Finished -= OnDeduced;
                }
                else deductionOk = true;

                if (deductionOk) break;

                Session.TriggerGameOver(GameOverKind.SelfLoathing);
                var retry = false;
                yield return ShowGameOver(allowRetry: true, retryText: "단서 조합부터 다시", onRetry: () => retry = true);
                if (!retry) yield break;
                Session.ClearGameOver();
            }

            // ---- 2차 심문 ----
            // 1차와 같은 규칙. 조합에서 만든 추론이 단서 제시 선택지를 연다.
            var secondRound = chapter.SecondRoundSuspects != null && chapter.SecondRoundSuspects.Count > 0
                ? new List<SuspectDefinition>(chapter.SecondRoundSuspects)
                : round;
            yield return Transition(GamePhase.Interrogation2);
            yield return RunInterrogationRound(secondRound, 2);
            if (Session.IsGameOver)
            {
                yield return ShowGameOver(allowRetry: false, retryText: null);
                yield break;
            }

            // ---- 범인 지목 ----
            SetPhase(GamePhase.Accusation);
            // 지목 UI 가 Accuse() 를 부를 때까지 기다린다.
            while (string.IsNullOrEmpty(Session.AccusedSuspectId)) yield return null;

            SetPhase(GamePhase.Ending);
            ChapterCompleted?.Invoke(Session.AccusedSuspectId);
        }

        private Coroutine _chapterRoutine;

        /// <summary>
        /// 디버그 — 앞 페이즈를 건너뛰고 단서 조합부터 시작한다. 씬의 모든 수집 오브젝트 단서와
        /// 지정한 추가 단서를 가진 상태로 만든다. 에디터·테스트용.
        /// </summary>
        public void DebugJumpToDeduction(IEnumerable<ClueDefinition> extraClues = null, bool giveAllSceneClues = true)
            => DebugJump(false, extraClues, giveAllSceneClues);

        /// <summary>디버그 — 1차 심문부터 시작한다.</summary>
        public void DebugJumpToInterrogation(IEnumerable<ClueDefinition> extraClues = null, bool giveAllSceneClues = true)
            => DebugJump(true, extraClues, giveAllSceneClues);

        private void DebugJump(bool fromInterrogation, IEnumerable<ClueDefinition> extraClues, bool giveAllSceneClues)
        {
            if (_chapterRoutine != null) StopCoroutine(_chapterRoutine);
            StopAllCoroutines();
            dialogue?.Stop();

            if (giveAllSceneClues && chapter.SceneObjects != null)
            {
                for (int i = 0; i < chapter.SceneObjects.Count; i++)
                {
                    var obj = chapter.SceneObjects[i];
                    if (obj != null && obj.GrantedClue != null) Session.Clues.Add(obj.GrantedClue);
                }
            }
            if (extraClues != null)
            {
                foreach (var c in extraClues) if (c != null) Session.Clues.Add(c);
            }

            var round = new List<SuspectDefinition>();
            if (chapter.FirstRoundSuspects != null && chapter.FirstRoundSuspects.Count > 0)
                round.Add(chapter.FirstRoundSuspects[0]);
            _chapterRoutine = StartCoroutine(fromInterrogation ? RunFromInterrogation(round) : RunFromDeduction(round));
        }

        /// <summary>
        /// 게임오버 화면을 띄우고 플레이어의 선택을 기다린다. 재시도가 허용되면 그 버튼을 눌렀을 때 돌아온다.
        /// 타이틀로 가는 버튼은 씬을 갈아 끼우므로 이 코루틴은 그냥 죽는다.
        /// </summary>
        private IEnumerator ShowGameOver(bool allowRetry, string retryText, Action onRetry = null)
        {
            if (gameOverView == null) yield break;

            var chosen = false;
            void OnRetry() => chosen = true;
            gameOverView.RetryRequested += OnRetry;
            gameOverView.Show(Session.GameOver, allowRetry, retryText);

            while (!chosen) yield return null;
            gameOverView.RetryRequested -= OnRetry;
            gameOverView.Hide();
            onRetry?.Invoke();
        }

        private IEnumerator RunInterrogationRound(IReadOnlyList<SuspectDefinition> suspects, int round)
        {
            _pending.Clear();
            if (suspects != null)
            {
                for (int i = 0; i < suspects.Count; i++)
                {
                    if (suspects[i] != null) _pending.Enqueue(suspects[i]);
                }
            }

            while (_pending.Count > 0)
            {
                var suspect = _pending.Dequeue();
                var view = ResolveView(suspect.Id);

                var finished = false;
                var reason = InterrogationEndReason.PlayerEnded;
                void OnFinished(InterrogationEndReason r) { reason = r; finished = true; }

                interrogation.Finished += OnFinished;
                interrogation.Begin(chapter, suspect, Session.Clues, view, round);

                while (!finished) yield return null;
                interrogation.Finished -= OnFinished;

                Session.AddTrust(interrogation.Session.TrustDelta);

                if (reason == InterrogationEndReason.GameOverByErraticTone)
                {
                    Session.TriggerGameOver(GameOverKind.Standard);
                    yield break;
                }

                if (clueTray != null) clueTray.Rebuild(Session.Clues);
            }
        }

        /// <summary>범인 지목 UI 가 호출한다.</summary>
        public void Accuse(string suspectId)
        {
            if (Phase != GamePhase.Accusation) return;
            Session.Accuse(suspectId);
        }

        private ICharacterView ResolveView(string suspectId)
        {
            for (int i = 0; i < characterBindings.Count; i++)
            {
                if (characterBindings[i].suspectId != suspectId) continue;
                return characterBindings[i].view as ICharacterView;
            }
            return null;
        }

        /// <summary>화면을 덮은 채로 페이즈를 갈아 끼우고 다시 걷는다.</summary>
        private IEnumerator Transition(GamePhase next)
        {
            if (fader == null)
            {
                SetPhase(next);
                yield break;
            }
            yield return fader.Transition(() => SetPhase(next));
        }

        /// <summary>새 단서를 얻었다. 목록을 즉시 갱신하고 바깥에도 알린다.</summary>
        private void OnClueAdded(ClueDefinition clue)
        {
            if (clueTray != null) clueTray.Rebuild(Session.Clues);
            ClueCollected?.Invoke(clue);
        }

        private void SetPhase(GamePhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            cursor?.ResetState();

            // 상대가 무대에 있는 구간에서는 상대의 말이 위 말풍선으로 간다.
            var split = phase == GamePhase.Interrogation1 || phase == GamePhase.Interrogation2 || phase == GamePhase.Deduction;
            dialogue?.SetSplitBySpeaker(split);

            PhaseChanged?.Invoke(phase);
        }

        private void OnGameOver(GameOverKind kind)
        {
            SetPhase(GamePhase.GameOver);
            GameOver?.Invoke(kind);
        }

        // 열람창이 떠 있는 동안 무반응 타이머를 멈춘다. 닫으면 3초 뒤(ClueTrayView 의 유예) 멈춘 자리에서 이어 간다.
        // 읽는 행동은 침묵을 끊는 게 아니라 잠시 미루는 것이라, 0으로 되돌리지 않는다.
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
            }
        }

        private void OnDestroy()
        {
            if (clueTray != null)
            {
                clueTray.TooltipOpened -= OnTooltipOpened;
                clueTray.TooltipClosed -= OnTooltipClosed;
            }
            if (Session != null)
            {
                Session.GameOverTriggered -= OnGameOver;
                Session.Clues.ClueAdded -= OnClueAdded;
            }
        }
    }
}
