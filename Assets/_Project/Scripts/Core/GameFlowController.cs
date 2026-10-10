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
        [SerializeField] private AccusationView accusation;
        [SerializeField] private EndingView ending;

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
            Session.Clues.ClueRemoved += OnClueRemoved;

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

            // ---- 1차 인물 선택 → 1차 심문 → 추론 ----
            // 추론의 「돌아가기」로 인물 선택에 돌아와 아직 심문하지 않은 사람을 고를 수 있고,
            // 인물 선택의 「추론 진행하기」로 같은 추론 상태에 다시 들어간다 (2026-10-11 제작자 지시).
            yield return RunFirstRoundAndDeduction(null, startAtDeduction: false, preInterrogated: null);
        }

        /// <summary>
        /// 1차 인물 선택 · 1차 심문 · 추론을 오간다. 추리를 마치면 2차 심문 → 지목 → 결말.
        /// firstPick 이 있으면 인물 선택을 건너뛰고 그 사람부터, startAtDeduction 이면 추론부터(디버그).
        /// </summary>
        private IEnumerator RunFirstRoundAndDeduction(SuspectDefinition firstPick, bool startAtDeduction, IEnumerable<string> preInterrogated)
        {
            var interrogated = new HashSet<string>();
            if (preInterrogated != null)
                foreach (var id in preInterrogated) if (!string.IsNullOrEmpty(id)) interrogated.Add(id);

            var talkDone = firstPick != null || startAtDeduction;   // 두 사람을 앉히는 대화는 처음 한 번만 (디버그 점프는 이미 지난 것으로)
            var deductionEntered = false;  // 한 번이라도 추론에 들어갔으면 인물 선택에 「추론 진행하기」가 보인다
            var resumeDeduction = false;   // 「돌아가기」로 떠난 추론을 이어 간다
            var next = firstPick;
            var toDeduction = startAtDeduction;

            while (true)
            {
                if (!toDeduction)
                {
                    if (next == null)
                    {
                        if (suspectSelect == null)
                        {
                            next = FirstLeftToInterrogate(interrogated);
                            if (next == null) { toDeduction = true; continue; }
                        }
                        else
                        {
                            SuspectDefinition picked = null;
                            var wantDeduction = false;
                            void OnPicked(SuspectDefinition s) => picked = s;
                            void OnWantDeduction() => wantDeduction = true;
                            suspectSelect.Picked += OnPicked;
                            suspectSelect.DeductionRequested += OnWantDeduction;

                            yield return Transition(GamePhase.SuspectSelect);
                            suspectSelect.BeginRound(chapter, chapter.FirstRoundSuspects, interrogated, chapter.SuspectSelectPrompt,
                                skipTalk: talkDone, allowDeduction: deductionEntered);
                            talkDone = true;

                            while (picked == null && !wantDeduction) yield return null;
                            suspectSelect.Picked -= OnPicked;
                            suspectSelect.DeductionRequested -= OnWantDeduction;

                            if (wantDeduction) { toDeduction = true; continue; }
                            next = picked;
                        }
                    }

                    // ---- 1차 심문 — 고른 한 사람 ----
                    yield return Transition(GamePhase.Interrogation1);
                    yield return RunInterrogationRound(new List<SuspectDefinition> { next }, 1);
                    interrogated.Add(next.Id);
                    next = null;
                    if (Session.IsGameOver)
                    {
                        yield return ShowGameOver(allowRetry: false, retryText: null);
                        yield break;
                    }
                }
                toDeduction = false;

                // ---- 추론 ----
                yield return Transition(GamePhase.Deduction);
                if (deduction == null) break;

                DeductionExit? exit = null;
                void OnExit(DeductionExit e) => exit = e;
                deduction.Exited += OnExit;
                deduction.Begin(chapter, Session.Clues, canReturn: FirstLeftToInterrogate(interrogated) != null, resume: resumeDeduction);
                deductionEntered = true;
                while (exit == null) yield return null;
                deduction.Exited -= OnExit;

                if (exit == DeductionExit.Completed) break;

                if (exit == DeductionExit.Returned)
                {
                    resumeDeduction = true;   // 다음에 추론에 들어오면 잉크·가설·실패한 짝을 그대로 이어 간다
                    continue;
                }

                // 잉크가 다 마른 채 가설이 없다 — 자기 비하 게임오버. 복귀 지점은 추론의 시작(새 잉크).
                Session.TriggerGameOver(GameOverKind.SelfLoathing);
                var retry = false;
                yield return ShowGameOver(allowRetry: true, retryText: "단서 조합부터 다시", onRetry: () => retry = true);
                if (!retry) yield break;
                Session.ClearGameOver();
                resumeDeduction = false;
                toDeduction = true;
            }

            yield return RunSecondRoundAndAccusation();
        }

        /// <summary>아직 1차 심문을 하지 않았고, 고를 수 있는 사람. 없으면 null.</summary>
        private SuspectDefinition FirstLeftToInterrogate(ISet<string> interrogated)
        {
            var list = chapter.FirstRoundSuspects;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s != null && s.Selectable && !interrogated.Contains(s.Id)) return s;
            }
            return null;
        }

        /// <summary>2차 심문 → 지목 → 결말.</summary>
        private IEnumerator RunSecondRoundAndAccusation()
        {
            // ---- 2차 심문 ----
            // 2026-09-23 기획: 상대를 고르고, 저장된 가설 중 하나를 들고 심문한다. 인물당 한 번.
            // 타임 토큰·무반응 판정은 없다(제작자 확정). 다른 가설로 여는 선택지는 이번 심문에 나타나지 않는다.
            var secondRound = chapter.SecondRoundSuspects != null && chapter.SecondRoundSuspects.Count > 0
                ? new List<SuspectDefinition>(chapter.SecondRoundSuspects)
                : new List<SuspectDefinition>(chapter.FirstRoundSuspects);
            var visited = new HashSet<string>();

            while (true)
            {
                var anySelectable = false;
                for (int i = 0; i < secondRound.Count; i++)
                {
                    var s = secondRound[i];
                    if (s != null && s.Selectable && !visited.Contains(s.Id)) { anySelectable = true; break; }
                }
                if (!anySelectable) break;

                SuspectDefinition target = null;
                ClueDefinition hypothesis = null;
                if (suspectSelect != null)
                {
                    void OnPicked2(SuspectDefinition s) => target = s;
                    void OnHyp(ClueDefinition c) => hypothesis = c;
                    suspectSelect.Picked += OnPicked2;
                    suspectSelect.HypothesisPicked += OnHyp;
                    yield return Transition(GamePhase.SuspectSelect);
                    suspectSelect.BeginRound(chapter, secondRound, visited, chapter.SecondRoundSelectPrompt, skipTalk: true);
                    while (target == null) yield return null;

                    var hyps = new List<ClueDefinition>(Session.Clues.OfKind(ClueKind.Deduced));
                    suspectSelect.BeginHypothesisPick(hyps, id => Session.Clues.DisplayName(id), chapter.HypothesisPickPrompt);
                    while (hypothesis == null && hyps.Count > 0) yield return null;
                    suspectSelect.Picked -= OnPicked2;
                    suspectSelect.HypothesisPicked -= OnHyp;
                }
                else
                {
                    for (int i = 0; i < secondRound.Count && target == null; i++)
                        if (secondRound[i] != null && secondRound[i].Selectable && !visited.Contains(secondRound[i].Id)) target = secondRound[i];
                    if (target == null) break;
                    foreach (var c in Session.Clues.OfKind(ClueKind.Deduced)) { hypothesis = c; break; }
                }

                yield return Transition(GamePhase.Interrogation2);

                var finished2 = false;
                var reason2 = InterrogationEndReason.PlayerEnded;
                void OnFinished2(InterrogationEndReason r) { reason2 = r; finished2 = true; }

                interrogation.Finished += OnFinished2;
                interrogation.Begin(chapter, target, Session.Clues, ResolveView(target.Id), 2,
                    untimed: true, activeHypothesisId: hypothesis != null ? hypothesis.Id : null);
                visited.Add(target.Id);
                while (!finished2) yield return null;
                interrogation.Finished -= OnFinished2;

                Session.AddTrust(interrogation.Session.TrustDelta);
                if (clueTray != null) clueTray.Rebuild(Session.Clues);

                if (reason2 == InterrogationEndReason.GameOverByErraticTone)
                {
                    Session.TriggerGameOver(GameOverKind.Standard);
                    yield return ShowGameOver(allowRetry: false, retryText: null);
                    yield break;
                }
            }

            // ---- 범인 지목 ----
            // 가설 → 용의자 → 추론 두 갈래 → 주인공의 추론 · 반응 → (억울함 호소 → 단서로 반박) → 결과.
            yield return Transition(GamePhase.Accusation);
            if (chapter.BeforeAccusation != null && !chapter.BeforeAccusation.IsEmpty)
                yield return dialogue.Play(chapter.BeforeAccusation);

            SuspectDefinition accused = null;
            var resultTitle = string.Empty;
            if (accusation != null)
            {
                var done = false;
                void OnAccused(SuspectDefinition s, string title) { accused = s; resultTitle = title; done = true; }
                accusation.Finished += OnAccused;
                accusation.Begin(chapter, Session.Clues);
                while (!done) yield return null;
                accusation.Finished -= OnAccused;
            }
            else
            {
                // 지목 UI 가 없으면 Accuse() 를 부를 때까지 기다린다.
                while (string.IsNullOrEmpty(Session.AccusedSuspectId)) yield return null;
            }

            if (accused != null) Session.Accuse(accused.Id);

            yield return Transition(GamePhase.Ending);
            ending?.Show(resultTitle, accused != null ? accused.DisplayName : Session.AccusedSuspectId, Session.Clues.Count);
            ChapterCompleted?.Invoke(Session.AccusedSuspectId);
        }

        private Coroutine _chapterRoutine;

        /// <summary>
        /// 디버그 — 앞 페이즈를 건너뛰고 단서 조합부터 시작한다. 첫 인물은 심문한 것으로 친다. 씬의 모든 수집 오브젝트 단서와
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

            SuspectDefinition first = null;
            if (chapter.FirstRoundSuspects != null && chapter.FirstRoundSuspects.Count > 0)
                first = chapter.FirstRoundSuspects[0];
            // 추론부터면 첫 사람은 이미 심문한 것으로 친다(예전 디버그 점프와 같은 상태).
            _chapterRoutine = StartCoroutine(fromInterrogation
                ? RunFirstRoundAndDeduction(first, startAtDeduction: false, preInterrogated: null)
                : RunFirstRoundAndDeduction(null, startAtDeduction: true, preInterrogated: first != null ? new[] { first.Id } : null));
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
        private void OnClueRemoved(ClueDefinition clue)
        {
            if (clueTray != null) clueTray.Rebuild(Session.Clues);
        }

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
            var split = phase == GamePhase.Interrogation1 || phase == GamePhase.Interrogation2;
            dialogue?.SetSplitBySpeaker(split);

            // 평범한 대화 구간은 화면 전체를 쓰는 시네마틱 대사창을, 나머지는 수첩형 대사창을 쓴다.
            // 추론도 시네마틱 — 서류철 위에 아래 그라데이션 + 이름표로 얹힌다(2026-10-11 스케치).
            var cinematic = phase == GamePhase.PreIncidentTalk || phase == GamePhase.Cutscene || phase == GamePhase.Intro
                         || phase == GamePhase.Deduction;
            dialogue?.SetCinematic(cinematic);

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
                Session.Clues.ClueRemoved -= OnClueRemoved;
            }
        }
    }
}
