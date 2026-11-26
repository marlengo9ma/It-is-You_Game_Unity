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
    /// Intro → 단서 수집 → 1차 심문 → 조합 → 2차 심문 → 범인 지목 → 엔딩 → 1챕터 인트로 → 데모 종료.
    /// 각 페이즈의 내부 규칙은 각자의 컨트롤러가 알고 있고,
    /// 이 클래스는 "언제 다음으로 넘어가는가"만 판단한다.
    ///
    /// 2026-11-27 — 세이브: 페이즈의 시작(단서 수집 · 심문 · 추론 · 지목)과 스토리 장면의 대사 한 줄마다
    /// 자동 저장 칸에 자리를 적고(<see cref="Checkpoint"/>), 세이브를 불러오면 그 자리부터 이어 간다(<see cref="BeginFromSave"/>).
    /// 게임오버는 단서 수집의 시작으로 되돌아간다.
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

        [Tooltip("챕터 씬이 시작될 때(인트로 카툰 다음) 까만 화면에서 밝아지는 시간(초).")]
        [SerializeField] private float openingFadeSeconds = 1.2f;
        [Tooltip("컷씬 화면의 그림·이름판. 회상씬(E#1 ~ E#3)이 같은 화면을 빌려 쓴다.")]
        [SerializeField] private CutsceneScreenView cutsceneView;
        [SerializeField] private ClueTrayView clueTray;
        [SerializeField] private CursorController cursor;
        [Tooltip("게임오버 화면. '단서 수집부터 다시' · '불러오기' · '타이틀로'.")]
        [SerializeField] private GameOverView gameOverView;
        [SerializeField] private AccusationView accusation;
        [SerializeField] private EndingView ending;
        [Tooltip("1챕터 인트로 뒤의 데모 종료 화면.")]
        [SerializeField] private DemoEndView demoEnd;

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

        // ---- 세이브 ----
        private SaveData _current;      // 지금 자리 — 수동 저장이 이것을 적는다
        private string _retryJson;      // 단서 수집 시작의 세이브 — 게임오버의 '단서 수집부터 다시'
        private string _storyStep;      // 지금 재생 중인 스토리 장면
        private bool _storyActive;
        private bool _wired;
        private Coroutine _chapterRoutine;

        /// <summary>지금 자리의 세이브 내용(가장 최근의 자동 저장 지점). 없으면 null.</summary>
        public SaveData CurrentSave => _current;

        private void Start()
        {
            if (chapter == null) return;
            var pending = SaveSystem.TakePending();
            if (pending != null && (string.IsNullOrEmpty(pending.chapterId) || pending.chapterId == chapter.Id)) BeginFromSave(pending);
            else BeginChapter(chapter);
        }

        public void BeginChapter(ChapterDefinition target)
        {
            Setup(target);
            _chapterRoutine = StartCoroutine(RunChapter(null, 0));
        }

        /// <summary>세이브에서 이어 간다. 판 상태를 되살리고, 적힌 자리(페이즈의 시작 / 스토리 대사 한 줄)부터 시작한다.</summary>
        public void BeginFromSave(SaveData data)
        {
            Setup(chapter);
            Session.RestoreFrom(data, database);
            _retryJson = data.retryJson;
            if (clueTray != null) clueTray.Rebuild(Session.Clues);
            fader?.Cover();
            _chapterRoutine = StartCoroutine(RunFromSave(data));
        }

        private void Setup(ChapterDefinition target)
        {
            chapter = target;
            Session = new GameSession();
            Session.GameOverTriggered += OnGameOver;

            if (!_wired)
            {
                _wired = true;
                if (clueTray != null)
                {
                    clueTray.TooltipOpened += OnTooltipOpened;
                    clueTray.TooltipClosed += OnTooltipClosed;
                }
                if (dialogue != null) dialogue.LineStarted += OnStoryLine;
            }

            // 단서가 늘면 그 자리에서 목록을 다시 그린다.
            // 심문이 끝난 뒤에만 갱신하면, 밑줄을 눌러 증언을 주웠는데도
            // 화면에는 아무 일도 일어나지 않은 것처럼 보인다 — 실제로 그랬다.
            Session.Clues.ClueAdded += OnClueAdded;
            Session.Clues.ClueRemoved += OnClueRemoved;

            SaveSystem.CurrentProvider = ProvideSave;
        }

        private SaveData ProvideSave() => _current;

        private IEnumerator RunFromSave(SaveData d)
        {
            switch (d.point)
            {
                case SavePoint.ClueCollection:
                    yield return RunFromCollection();
                    break;
                case SavePoint.Interrogation1:
                    yield return RunFirstRoundAndDeduction(null, false, d.interrogated, d.selectTalkDone, d.deductionEntered, RestoreOf(d));
                    break;
                case SavePoint.Deduction:
                    yield return RunFirstRoundAndDeduction(null, true, d.interrogated, true, d.deductionEntered, RestoreOf(d));
                    break;
                case SavePoint.Interrogation2:
                    yield return RunSecondRoundAndAccusation(d.visited2);
                    break;
                case SavePoint.Accusation:
                    yield return RunAccusationAndEnding();
                    break;
                case SavePoint.DemoEnd:
                    yield return RunDemoEnd();
                    break;
                default:
                    yield return RunStoryFromSave(d.storyStep, d.storyLine);
                    break;
            }
        }

        private static DeductionRestore RestoreOf(SaveData d)
            => d != null && d.deductionInk >= 0
                ? new DeductionRestore { ink = d.deductionInk, failCount = d.deductionFails, triedPairs = d.deductionTried }
                : null;

        // ================================================================ 스토리 (대사 단위 저장)

        private static bool IsEmpty(DialogueSequence seq) => seq == null || seq.IsEmpty;

        /// <summary>스토리 장면의 순서. 세이브에서 이 자리부터 다시 시작할 때 쓴다.</summary>
        private static int OrderOf(string id)
        {
            switch (id)
            {
                case "prelude": return 0;
                case "preIncident": return 1;
                case "cutscene": return 2;
                case "afterCutscene": return 3;
                case "afterFlashback": return 100;
                case "intro": return 101;
            }
            if (id != null && id.StartsWith("flashback") && int.TryParse(id.Substring(9), out var n)) return 10 + n;
            return 0;
        }

        private IEnumerator RunChapter(string startId, int startLine)
        {
            // 인트로 카툰에서 넘어오면 화면이 까만 채로 시작해 천천히 밝아진다 — 카툰 → 대화창이 툭 바뀌지 않게.
            // 첫 화면(기차역 / 객실)을 먼저 켜 둔 뒤 걷어야 한다. 페이즈가 None 인 채로 걷으면 빈 화면이 밝아지고
            // 배경은 다 밝아진 뒤에야 툭 나타난다(2026-10-14 확인). 세이브에서 이어 갈 때도 같은 방식으로 첫 화면을 연다.
            var from = startId == null ? 0 : OrderOf(startId);
            var first = true;
            int LineOf(string id) => id == startId ? startLine : 0;
            bool Due(string id) => OrderOf(id) >= from;

            // ---- 인트로 대화: 기차역 (노션 <대화 씬으로 화면 전환> ~ 튜토리얼 챕터 시작점) ----
            // 인트로 카툰이 끝나면 이 씬으로 넘어온다. 대화가 끝나면 <화면 배경 전환> — 덮었다 걷으며 기차 안으로.
            if (!IsEmpty(chapter.PreludeTalk) && Due("prelude"))
            {
                yield return Enter(GamePhase.Prelude, chapter.PreludeTalk, null, first); first = false;
                yield return PlayStory("prelude", chapter.PreludeTalk, LineOf("prelude"));
            }

            // ---- 사건 이전: [게임플레이 배경_캐릭터 씬] ----
            if (Due("preIncident") && (!IsEmpty(chapter.PreIncidentTalk) || first))
            {
                yield return Enter(GamePhase.PreIncidentTalk, chapter.PreIncidentTalk, null, first); first = false;
                yield return PlayStory("preIncident", chapter.PreIncidentTalk, LineOf("preIncident"));
            }

            // ---- [일러스트 컷씬#1] ----
            // 화장실 칸으로 자리를 옮기는 장면이다. 같은 배경 위에서 대사만 이어지면
            // 장소가 바뀐 것이 읽히지 않으므로, 화면을 한 번 덮었다 걷는다.
            if (!IsEmpty(chapter.Cutscene) && Due("cutscene"))
            {
                yield return Enter(GamePhase.Cutscene, null, () => cutsceneView?.Show(chapter.CutsceneName, chapter.CutsceneImage), first); first = false;
                yield return PlayStory("cutscene", chapter.Cutscene, LineOf("cutscene"));
            }

            // ---- 컷씬에서 돌아온 뒤: 다시 [게임플레이 배경_캐릭터 씬] ----
            if (!IsEmpty(chapter.AfterCutsceneTalk) && Due("afterCutscene"))
            {
                yield return Enter(GamePhase.PreIncidentTalk, chapter.AfterCutsceneTalk, null, first); first = false;
                yield return PlayStory("afterCutscene", chapter.AfterCutsceneTalk, LineOf("afterCutscene"));
            }

            // ---- <회상씬으로 전환 E#1 ~ E#3> → <게임플레이 배경_캐릭터 일반 대화 씬으로 전환> ----
            var flashbacks = chapter.Flashbacks;
            if (flashbacks != null && flashbacks.Count > 0)
            {
                for (int i = 0; i < flashbacks.Count; i++)
                {
                    var fb = flashbacks[i];
                    var id = "flashback" + i;
                    if (fb == null || fb.IsEmpty || !Due(id)) continue;
                    yield return Enter(GamePhase.Cutscene, null, () => cutsceneView?.Show(fb.Name, fb.Image, recall: true), first); first = false;
                    yield return PlayStory(id, fb.Sequence, LineOf(id));
                }
                if (!IsEmpty(chapter.AfterFlashbackTalk) && Due("afterFlashback"))
                {
                    yield return Enter(GamePhase.PreIncidentTalk, chapter.AfterFlashbackTalk, null, first); first = false;
                    yield return PlayStory("afterFlashback", chapter.AfterFlashbackTalk, LineOf("afterFlashback"));
                }
            }

            // ---- <화면 전환> 사건 진행 #1 ----
            yield return Enter(GamePhase.Intro, chapter.Intro, null, first);
            yield return PlayStory("intro", chapter.Intro, LineOf("intro"));

            yield return RunFromCollection();
        }

        /// <summary>
        /// 장면 하나로 들어간다. first 면(씬의 첫 화면) 까만 화면에서 천천히 밝아지고, 아니면 덮었다 걷는다.
        /// 대화 화면에 서 있을 인물은 화면이 보이기 전에 맞춘다(DialogueSequence.Stand) — 전환 뒤에 바뀌면 툭 사라져 보인다.
        /// </summary>
        private IEnumerator Enter(GamePhase phase, DialogueSequence prepare, Action before, bool first)
        {
            if (prepare != null) dialogue?.Prepare(prepare);
            if (first && fader != null && openingFadeSeconds > 0f)
            {
                fader.Cover();
                before?.Invoke();
                SetPhase(phase);
                yield return fader.FadeIn(openingFadeSeconds);
                yield break;
            }
            yield return TransitionWith(phase, before);
        }

        /// <summary>스토리 장면의 대사 묶음을 재생한다. 한 줄마다 자동 저장 칸에 자리를 적는다(autosave 일 때).</summary>
        private IEnumerator PlayStory(string stepId, DialogueSequence seq, int startLine, bool autosave = true)
        {
            if (IsEmpty(seq) || dialogue == null) yield break;
            _storyStep = autosave ? stepId : null;
            _storyActive = autosave;
            dialogue.SetPacing(slowSpeech: false, allowSkip: true);
            yield return startLine > 0 ? dialogue.PlayFrom(seq, startLine) : dialogue.Play(seq);
            _storyActive = false;
        }

        private void OnStoryLine(DialogueLine _)
        {
            if (!_storyActive || dialogue == null || string.IsNullOrEmpty(_storyStep)) return;
            Checkpoint(SavePoint.Story, _storyStep, dialogue.CurrentLineIndex);
        }

        private IEnumerator RunStoryFromSave(string step, int line)
        {
            if (!string.IsNullOrEmpty(step) && step.StartsWith("ending:"))
            {
                // ending:엔딩id:단계
                var parts = step.Split(':');
                EndingDefinition def = null;
                var index = 0;
                if (parts.Length >= 3) { def = chapter.FindEndingById(parts[1]); int.TryParse(parts[2], out index); }
                if (def != null)
                {
                    if (def == chapter.ChapterOneIntro)
                    {
                        yield return PlayEndingSteps(def, index, line, autosave: true);
                        yield return RunDemoEnd();
                    }
                    else yield return PlayEndingFlow(def, index, line);
                    yield break;
                }
                Debug.LogWarning($"[It is You] 세이브의 엔딩 '{step}' 을 찾지 못해 범인 지목부터 이어 갑니다.");
                yield return RunAccusationAndEnding();
                yield break;
            }
            yield return RunChapter(step, line);
        }

        // ================================================================ 페이즈

        private IEnumerator RunFromCollection()
        {
            // ---- 단서 수집 ----
            // 기획서의 <튜토리얼 시작> <사건 시작/진입 연출> 자리. 게임오버의 '단서 수집부터 다시'도 여기로 돌아온다.
            Checkpoint(SavePoint.ClueCollection);
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
            yield return RunFirstRoundAndDeduction(null, false, null, false, false, null);
        }

        /// <summary>
        /// 1차 인물 선택 · 1차 심문 · 추론을 오간다. 추리를 마치면 2차 심문 → 지목 → 결말.
        /// firstPick 이 있으면 인물 선택을 건너뛰고 그 사람부터, startAtDeduction 이면 추론부터(디버그 · 세이브).
        /// </summary>
        private IEnumerator RunFirstRoundAndDeduction(SuspectDefinition firstPick, bool startAtDeduction, IEnumerable<string> preInterrogated,
            bool selectTalkDone, bool deductionEnteredBefore, DeductionRestore restore)
        {
            var interrogated = new HashSet<string>();
            if (preInterrogated != null)
                foreach (var id in preInterrogated) if (!string.IsNullOrEmpty(id)) interrogated.Add(id);

            var talkDone = selectTalkDone || firstPick != null || startAtDeduction;   // 두 사람을 앉히는 대화는 처음 한 번만
            var deductionEntered = deductionEnteredBefore;  // 한 번이라도 추론에 들어갔으면 인물 선택에 「추론 진행하기」가 보인다
            var resumeDeduction = false;   // 「돌아가기」로 떠난 추론을 이어 간다
            var pendingRestore = restore;  // 세이브에서 되살린 추론 상태 — 다음에 추론에 들어갈 때 한 번 쓴다
            var next = firstPick;
            var toDeduction = startAtDeduction;

            void FillRound(SaveData d)
            {
                d.interrogated = new List<string>(interrogated);
                d.selectTalkDone = talkDone;
                d.deductionEntered = deductionEntered;
                FillDeduction(d, resumeDeduction, pendingRestore);
            }

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

                            Checkpoint(SavePoint.Interrogation1, fill: FillRound);
                            yield return Transition(GamePhase.SuspectSelect);

                            // 추론에서 돌아왔는데 더 심문할 사람이 없다 — 한마디 하고 「추론 진행하기」만 남긴다.
                            var noneLeft = deductionEntered && FirstLeftToInterrogate(interrogated) == null;

                            suspectSelect.BeginRound(chapter, chapter.FirstRoundSuspects, interrogated, noneLeft ? string.Empty : chapter.SuspectSelectPrompt,
                                skipTalk: talkDone, allowDeduction: deductionEntered || interrogated.Count > 0,
                                opening: noneLeft ? chapter.NoSuspectsLeft : null);
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
                        yield return RunGameOver(GameOverKind.Standard);
                        yield break;
                    }

                    // 아직 심문할 사람이 남았다면 인물 선택으로 — 노션: 세피 심문 마무리 "아직 이야기를 나눠야 될 분이 남아계시니." → 용의자 ‘고양이’ 선택.
                    // 바로 추론으로 가고 싶으면 인물 선택의 「추론 진행하기」.
                    if (FirstLeftToInterrogate(interrogated) != null) continue;
                }
                toDeduction = false;

                // ---- 추론 ----
                Checkpoint(SavePoint.Deduction, fill: FillRound);
                yield return Transition(GamePhase.Deduction);
                if (deduction == null) break;

                DeductionExit? exit = null;
                void OnExit(DeductionExit e) => exit = e;
                deduction.Exited += OnExit;
                // 「돌아가기」는 언제나 눌린다 — 심문할 사람이 없으면 인물 선택에서 그렇다고 말해 준다 (2026-10-11 제작자 지시).
                deduction.Begin(chapter, Session.Clues, canReturn: true, resume: resumeDeduction, restore: resumeDeduction ? null : pendingRestore);
                pendingRestore = null;
                deductionEntered = true;
                while (exit == null) yield return null;
                deduction.Exited -= OnExit;

                if (exit == DeductionExit.Completed) break;

                if (exit == DeductionExit.Returned)
                {
                    resumeDeduction = true;   // 다음에 추론에 들어오면 잉크·가설·실패한 짝을 그대로 이어 간다
                    continue;
                }

                // 잉크가 다 마른 채 가설이 없다 — 자기 비하 게임오버(D-2). 복귀 지점은 단서 수집의 시작 (2026-11-27 제작자 지시).
                yield return RunGameOver(GameOverKind.SelfLoathing);
                yield break;
            }

            yield return RunSecondRoundAndAccusation(null);
        }

        /// <summary>추론 상태를 세이브에 적는다 — 「돌아가기」로 떠나 둔 추론이 있으면 그 잉크 · 실패한 짝을.</summary>
        private void FillDeduction(SaveData d, bool live, DeductionRestore pendingRestore)
        {
            d.deductionInk = -1;
            d.deductionFails = 0;
            d.deductionTried = new List<string>();
            if (live && deduction != null && deduction.Session != null && !deduction.Session.IsOver)
            {
                deduction.Session.Capture(out var ink, out var fails, out var tried);
                d.deductionInk = ink;
                d.deductionFails = fails;
                d.deductionTried = tried;
            }
            else if (pendingRestore != null)
            {
                d.deductionInk = pendingRestore.ink;
                d.deductionFails = pendingRestore.failCount;
                d.deductionTried = pendingRestore.triedPairs != null ? new List<string>(pendingRestore.triedPairs) : new List<string>();
            }
        }

        /// <summary>아직 1차 심문을 하지 않았고, 고를 수 있는 사람. 없으면 null.</summary>
        private SuspectDefinition FirstLeftToInterrogate(ISet<string> interrogated)
        {
            var list = chapter.FirstRoundSuspects;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s != null && s.Selectable && !interrogated.Contains(s.Id) && s.IsUnlockedBy(interrogated)) return s;
            }
            return null;
        }

        /// <summary>2차 심문 → 지목 → 결말.</summary>
        private IEnumerator RunSecondRoundAndAccusation(IEnumerable<string> visitedBefore)
        {
            // ---- 2차 심문 ----
            // 2026-09-23 기획: 상대를 고르고, 저장된 가설 중 하나를 들고 심문한다. 인물당 한 번.
            // 타임 토큰·무반응 판정은 없다(제작자 확정). 다른 가설로 여는 선택지는 이번 심문에 나타나지 않는다.
            var secondRound = chapter.SecondRoundSuspects != null && chapter.SecondRoundSuspects.Count > 0
                ? new List<SuspectDefinition>(chapter.SecondRoundSuspects)
                : new List<SuspectDefinition>(chapter.FirstRoundSuspects);
            var visited = new HashSet<string>();
            if (visitedBefore != null) foreach (var id in visitedBefore) if (!string.IsNullOrEmpty(id)) visited.Add(id);

            while (true)
            {
                var anySelectable = false;
                for (int i = 0; i < secondRound.Count; i++)
                {
                    var s = secondRound[i];
                    if (s != null && s.Selectable && !visited.Contains(s.Id)) { anySelectable = true; break; }
                }
                if (!anySelectable) break;

                Checkpoint(SavePoint.Interrogation2, fill: d => d.visited2 = new List<string>(visited));

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
                    yield return RunGameOver(GameOverKind.Standard);
                    yield break;
                }
            }

            yield return RunAccusationAndEnding();
        }

        /// <summary>범인 지목 → 엔딩.</summary>
        private IEnumerator RunAccusationAndEnding()
        {
            // ---- 범인 지목 ----
            // 가설 → 용의자 → 추론 두 갈래 → 주인공의 추론 · 반응 → (억울함 호소 → 단서로 반박) → 결과.
            Checkpoint(SavePoint.Accusation);
            yield return Transition(GamePhase.Accusation);
            if (!IsEmpty(chapter.BeforeAccusation))
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

                // 반박에서 엉뚱한 단서를 거듭 냈다 — 엉뚱한 근거 게임오버(D-3). 챕터에서 켰을 때만 일어난다.
                if (accusation.WrongEvidenceGameOver)
                {
                    yield return RunGameOver(GameOverKind.WrongEvidence);
                    yield break;
                }
            }
            else
            {
                // 지목 UI 가 없으면 Accuse() 를 부를 때까지 기다린다.
                while (string.IsNullOrEmpty(Session.AccusedSuspectId)) yield return null;
            }

            if (accused != null) Session.Accuse(accused.Id);

            // ---- 엔딩 ----
            // 지목한 추론 선택지에 엔딩이 있으면 그것, 없으면 지목한 용의자의 엔딩, 그것도 없으면 기본 엔딩 (2026-11-27).
            var resultOption = accusation != null ? accusation.ResultOption : null;
            var endingDef = chapter.FindEnding(resultOption, Session.AccusedSuspectId);
            if (endingDef != null)
            {
                yield return PlayEndingFlow(endingDef, 0, 0);
                yield break;
            }

            // ---- 예전 방식 — 엔딩 에셋이 없을 때: 엔딩 분기 컷씬 → 챕터의 엔딩 컷씬(#End) → 결과 표시 ----
            yield return Transition(GamePhase.Ending);
            var endingCuts = new List<Sprite>();
            if (resultOption != null && resultOption.EndingCutscenes != null)
                foreach (var s in resultOption.EndingCutscenes) if (s != null) endingCuts.Add(s);
            if (chapter.EndingCutscene != null) endingCuts.Add(chapter.EndingCutscene);
            if (ending != null && endingCuts.Count > 0)
                yield return ending.PlayCutscenes(endingCuts);

            ending?.Show(resultTitle, accused != null ? accused.DisplayName : Session.AccusedSuspectId, Session.Clues.Count);
            ChapterCompleted?.Invoke(Session.AccusedSuspectId);
        }

        // ================================================================ 엔딩 (2026-11-27)

        /// <summary>
        /// 엔딩 한 편을 재생하고, 엔딩의 종류에 맞게 마무리한다.
        /// 일반 → 제목 카드 → 1챕터 인트로 → 데모 종료 / 이어지는 장면 → 데모 종료 / 게임오버 → 게임오버 화면.
        /// </summary>
        private IEnumerator PlayEndingFlow(EndingDefinition def, int fromStep, int fromLine)
        {
            switch (def.Kind)
            {
                case EndingKind.GameOver:
                    yield return RunGameOver(GameOverKindOf(def), def, fromStep, fromLine);
                    yield break;

                case EndingKind.Interlude:
                    yield return PlayEndingSteps(def, fromStep, fromLine, autosave: true);
                    yield return RunDemoEnd();
                    yield break;

                default:
                    yield return PlayEndingSteps(def, fromStep, fromLine, autosave: true);
                    if (def.ShowTitleCard && ending != null)
                    {
                        if (Phase != GamePhase.Ending) yield return TransitionWith(GamePhase.Ending, () => { ending.ResetEffects(); ending.Open(); });
                        yield return ending.ShowTitleCard(def.Title, def.Code);
                    }
                    ChapterCompleted?.Invoke(Session.AccusedSuspectId);
                    yield return RunChapterOneIntro();
                    yield break;
            }
        }

        private GameOverKind GameOverKindOf(EndingDefinition def)
        {
            foreach (var k in new[] { GameOverKind.Standard, GameOverKind.SelfLoathing, GameOverKind.WrongEvidence })
                if (chapter.FindGameOverEnding(k) == def) return k;
            return GameOverKind.Standard;
        }

        /// <summary>엔딩의 단계를 차례로 — 화면을 깔고, 연출을 걸고, 대사를 재생한다(대사가 없으면 클릭을 기다린다).</summary>
        private IEnumerator PlayEndingSteps(EndingDefinition def, int fromStep, int fromLine, bool autosave)
        {
            var steps = def.Steps;
            if (steps == null) yield break;
            for (int i = Mathf.Max(0, fromStep); i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null) continue;
                var id = $"ending:{def.Id}:{i}";

                yield return EnterEndingStep(def, i, resumed: i == fromStep && Phase == GamePhase.None);
                if (step.Effect != EndingEffect.None && ending != null) yield return ending.PlayEffect(step.Effect);

                if (step.HasTalk)
                {
                    yield return PlayStory(id, step.Talk, i == fromStep ? fromLine : 0, autosave);
                }
                else
                {
                    if (autosave) Checkpoint(SavePoint.Story, id, 0);
                    if (Phase == GamePhase.Ending && ending != null) yield return ending.WaitClick();
                }
            }
        }

        /// <summary>단계의 화면을 깐다. 세이브에서 이어 갈 때(resumed) '지금 화면 그대로'는 앞 단계들의 화면을 되짚어 정한다.</summary>
        private IEnumerator EnterEndingStep(EndingDefinition def, int index, bool resumed)
        {
            var step = def.Steps[index];
            var screen = step.Screen;
            var cut = step.Cut;

            if (resumed && (screen == EndingScreen.Keep || (screen == EndingScreen.Cut && cut == null)))
            {
                for (int j = index - 1; j >= 0; j--)
                {
                    var s = def.Steps[j];
                    if (s == null) continue;
                    if (s.Screen == EndingScreen.Room || s.Screen == EndingScreen.Black) { screen = s.Screen; break; }
                    if (s.Screen == EndingScreen.Cut && s.Cut != null) { screen = EndingScreen.Cut; cut = s.Cut; break; }
                }
                if (screen == EndingScreen.Keep) screen = EndingScreen.Room;
            }

            switch (screen)
            {
                case EndingScreen.Cut:
                    if (Phase != GamePhase.Ending)
                        yield return TransitionWith(GamePhase.Ending, () => { ending?.ResetEffects(); ending?.SetCutInstant(cut); });
                    else if (cut != null && ending != null)
                    {
                        ending.ResetEffects();
                        yield return ending.ShowCut(cut);
                    }
                    break;

                case EndingScreen.Room:
                    if (Phase != GamePhase.PreIncidentTalk)
                    {
                        if (step.HasTalk) dialogue?.Prepare(step.Talk);
                        yield return TransitionWith(GamePhase.PreIncidentTalk, () => ending?.ResetEffects());
                    }
                    break;

                case EndingScreen.Black:
                    if (Phase != GamePhase.Ending) yield return TransitionWith(GamePhase.Ending, () => ending?.SetBlack());
                    else ending?.SetBlack();
                    break;
            }
        }

        /// <summary>일반 엔딩 뒤 — 1챕터 인트로(대화 장면 → 컷씬) → 데모 종료.</summary>
        private IEnumerator RunChapterOneIntro()
        {
            var intro = chapter.ChapterOneIntro;
            if (intro != null && intro.Steps != null && intro.Steps.Count > 0)
                yield return PlayEndingSteps(intro, 0, 0, autosave: true);
            yield return RunDemoEnd();
        }

        /// <summary>데모 종료 화면 — 컷씬(임시) 위의 어두운 상자, 감사 문장, '타이틀로 가기' · '불러오기'.</summary>
        private IEnumerator RunDemoEnd()
        {
            Checkpoint(SavePoint.DemoEnd);
            if (demoEnd == null)
            {
                ending?.Show(string.Empty, Session.AccusedSuspectId, Session.Clues.Count);
                yield break;
            }
            if (Phase == GamePhase.None) yield return TransitionWith(GamePhase.Ending, () => demoEnd.Show(chapter.DemoEndImage, chapter.DemoEndMessage));
            else demoEnd.Show(chapter.DemoEndImage, chapter.DemoEndMessage);
        }

        // ================================================================ 게임오버

        private IEnumerator RunGameOver(GameOverKind kind) => RunGameOver(kind, chapter.FindGameOverEnding(kind), 0, 0);

        /// <summary>
        /// 게임오버 엔딩(D-1 ~ D-3)을 재생하고 게임오버 화면을 띄운다.
        /// '단서 수집부터 다시'는 단서 수집 시작의 세이브를 불러온다 — 처음부터가 아니라 (2026-11-27 제작자 지시).
        /// 게임오버 엔딩의 대사는 자동 저장하지 않는다 — 자동 저장 칸은 마지막으로 무사했던 자리에 남는다.
        /// </summary>
        private IEnumerator RunGameOver(GameOverKind kind, EndingDefinition def, int fromStep, int fromLine)
        {
            Session.TriggerGameOver(kind);
            if (def != null) yield return PlayEndingSteps(def, fromStep, fromLine, autosave: false);

            var retry = false;
            yield return ShowGameOver(allowRetry: !string.IsNullOrEmpty(_retryJson), retryText: "단서 수집부터 다시",
                onRetry: () => retry = true, message: def != null ? def.GameOverMessage : null);
            if (!retry) yield break;

            SaveData back = null;
            try { back = JsonUtility.FromJson<SaveData>(_retryJson); } catch { back = null; }
            if (back != null) SaveSystem.LoadAndPlay(back);
        }

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
            _storyActive = false;

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
                ? RunFirstRoundAndDeduction(first, false, null, false, false, null)
                : RunFirstRoundAndDeduction(null, true, first != null ? new[] { first.Id } : null, true, false, null));
        }

        /// <summary>디버그 · 검증 — 지목을 건너뛰고 엔딩을 바로 본다 (엔딩 id, 지목한 용의자 id).</summary>
        public void DebugPlayEnding(string endingId, string accusedId = null)
        {
            var def = chapter != null ? chapter.FindEndingById(endingId) : null;
            if (def == null) { Debug.LogWarning($"[It is You] 엔딩 '{endingId}' 을 찾지 못했습니다."); return; }
            if (_chapterRoutine != null) StopCoroutine(_chapterRoutine);
            StopAllCoroutines();
            dialogue?.Stop();
            _storyActive = false;
            if (!string.IsNullOrEmpty(accusedId)) Session.Accuse(accusedId);
            _chapterRoutine = StartCoroutine(PlayEndingFlow(def, 0, 0));
        }

        /// <summary>
        /// 게임오버 화면을 띄우고 플레이어의 선택을 기다린다. 재시도가 허용되면 그 버튼을 눌렀을 때 돌아온다.
        /// 타이틀로 가는 버튼 · 불러오기는 씬을 갈아 끼우므로 이 코루틴은 그냥 죽는다.
        /// </summary>
        private IEnumerator ShowGameOver(bool allowRetry, string retryText, Action onRetry = null, string message = null)
        {
            yield return TransitionWith(GamePhase.GameOver, () => ending?.Close());
            if (gameOverView == null) yield break;

            var chosen = false;
            void OnRetry() => chosen = true;
            gameOverView.RetryRequested += OnRetry;
            gameOverView.Show(Session.GameOver, allowRetry, retryText, message);

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

        // ================================================================ 세이브 지점

        /// <summary>
        /// 지금 자리를 적는다 — 자동 저장 칸에 쓰고, 일시 정지의 '저장하기'가 이 자리를 쓴다.
        /// 단서 수집의 시작은 게임오버의 '단서 수집부터 다시'를 위해 따로 기억해 둔다.
        /// </summary>
        private void Checkpoint(SavePoint point, string step = null, int line = 0, Action<SaveData> fill = null)
        {
            if (Session == null || chapter == null) return;
            var d = new SaveData { chapterId = chapter.Id, point = point, storyStep = step, storyLine = line };
            Session.CaptureInto(d);
            fill?.Invoke(d);
            d.label = Describe(d);
            if (point == SavePoint.ClueCollection)
            {
                d.retryJson = null;
                _retryJson = JsonUtility.ToJson(d);
            }
            else d.retryJson = _retryJson;
            _current = d;
            SaveSystem.Write(SaveSystem.AutoSlot, d);
        }

        /// <summary>세이브 칸에 보일 설명.</summary>
        private string Describe(SaveData d)
        {
            var where = d.point switch
            {
                SavePoint.ClueCollection => "단서 수집 시작",
                SavePoint.Interrogation1 => d.interrogated != null && d.interrogated.Count > 0
                    ? $"1차 심문 — 인물 선택 ({d.interrogated.Count}명 심문함)" : "1차 심문 — 인물 선택",
                SavePoint.Deduction => "추론 시작",
                SavePoint.Interrogation2 => "2차 심문 — 인물 선택",
                SavePoint.Accusation => "범인 지목 시작",
                SavePoint.DemoEnd => "데모 종료",
                _ => $"{StoryLabel(d.storyStep)} · {d.storyLine + 1}번째 대사"
            };
            var name = chapter != null ? chapter.DisplayName : string.Empty;
            return string.IsNullOrEmpty(name) ? where : $"{name}  ·  {where}";
        }

        private string StoryLabel(string step)
        {
            if (string.IsNullOrEmpty(step)) return "스토리";
            if (step.StartsWith("ending:"))
            {
                var parts = step.Split(':');
                var def = parts.Length > 1 ? chapter.FindEndingById(parts[1]) : null;
                if (def != null && def == chapter.ChapterOneIntro) return "1챕터 인트로";
                return def != null && !string.IsNullOrEmpty(def.Title) ? "엔딩 — " + def.Title : "엔딩";
            }
            if (step.StartsWith("flashback") && int.TryParse(step.Substring(9), out var n)) return $"회상 E#{n + 1}";
            return step switch
            {
                "prelude" => "기차역 대화",
                "preIncident" => "객실 — 사건 이전",
                "cutscene" => "일러스트 컷씬",
                "afterCutscene" => "객실 — 컷씬 뒤",
                "afterFlashback" => "객실 — 회상 뒤",
                "intro" => "사건 진행 #1",
                _ => "스토리"
            };
        }

        // ================================================================ 화면 전환

        /// <summary>화면을 덮은 동안 before 를 실행하고 페이즈를 갈아 끼운다. 같은 페이즈로 다시 들어갈 때(회상 여러 컷)도 덮었다 걷는다.</summary>
        private IEnumerator TransitionWith(GamePhase next, Action before)
        {
            if (fader == null)
            {
                before?.Invoke();
                SetPhase(next);
                yield break;
            }
            yield return fader.Transition(() => { before?.Invoke(); SetPhase(next); });
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
            // 추론도 시네마틱 — 서류철 위에 아래 그라데이션 + 이름표로 얹힌다(2026-10-11 스케치). 엔딩 컷 위도 시네마틱(2026-11-27).
            var cinematic = phase == GamePhase.Prelude || phase == GamePhase.PreIncidentTalk || phase == GamePhase.Cutscene || phase == GamePhase.Intro
                         || phase == GamePhase.Deduction || phase == GamePhase.Ending;
            dialogue?.SetCinematic(cinematic);

            PhaseChanged?.Invoke(phase);
        }

        /// <summary>게임오버가 확정됐다 — 화면은 게임오버 엔딩을 다 본 뒤에 바뀐다(RunGameOver).</summary>
        private void OnGameOver(GameOverKind kind)
        {
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
            if (dialogue != null) dialogue.LineStarted -= OnStoryLine;
            if (Session != null)
            {
                Session.GameOverTriggered -= OnGameOver;
                Session.Clues.ClueAdded -= OnClueAdded;
                Session.Clues.ClueRemoved -= OnClueRemoved;
            }
            if (SaveSystem.CurrentProvider != null && ReferenceEquals(SaveSystem.CurrentProvider.Target, this))
                SaveSystem.CurrentProvider = null;
        }
    }
}
