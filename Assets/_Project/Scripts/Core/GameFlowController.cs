using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Characters;
using ItIsYou.Clues;
using ItIsYou.Data;
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
        [SerializeField] private InterrogationController interrogation;

        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private ClueTrayView clueTray;
        [SerializeField] private CursorController cursor;

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

            StartCoroutine(RunChapter());
        }

        private IEnumerator RunChapter()
        {
            // ---- Intro ----
            SetPhase(GamePhase.Intro);
            if (chapter.Intro != null && !chapter.Intro.IsEmpty)
                yield return dialogue.Play(chapter.Intro);

            // ---- 단서 수집 ----
            SetPhase(GamePhase.ClueCollection);
            var collectionDone = false;
            void OnReady(CollectionGate _) => collectionDone = true;

            investigation.ReadyToAdvance += OnReady;
            investigation.Begin(chapter, Session.Clues);

            while (!collectionDone) yield return null;
            investigation.ReadyToAdvance -= OnReady;

            // ---- 1차 심문 ----
            SetPhase(GamePhase.Interrogation1);
            yield return RunInterrogationRound(chapter.FirstRoundSuspects);
            if (Session.IsGameOver) yield break;

            // ---- 단서 조합 ----
            SetPhase(GamePhase.Deduction);
            if (Session.CheckDeductionDeadlock(chapter))
            {
                // 필요한 조합을 만들 수 없다 — 자기 비하 엔딩.
                Session.TriggerGameOver(GameOverKind.SelfLoathing);
                yield break;
            }

            // ---- 2차 심문 ----
            SetPhase(GamePhase.Interrogation2);
            yield return RunInterrogationRound(chapter.SecondRoundSuspects);
            if (Session.IsGameOver) yield break;

            // ---- 범인 지목 ----
            SetPhase(GamePhase.Accusation);
            // 지목 UI 가 Accuse() 를 부를 때까지 기다린다.
            while (string.IsNullOrEmpty(Session.AccusedSuspectId)) yield return null;

            SetPhase(GamePhase.Ending);
            ChapterCompleted?.Invoke(Session.AccusedSuspectId);
        }

        private IEnumerator RunInterrogationRound(IReadOnlyList<SuspectDefinition> suspects)
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
                interrogation.Begin(chapter, suspect, Session.Clues, view);

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

        private void SetPhase(GamePhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            cursor?.ResetState();
            PhaseChanged?.Invoke(phase);
        }

        private void OnGameOver(GameOverKind kind)
        {
            SetPhase(GamePhase.GameOver);
            GameOver?.Invoke(kind);
        }

        // 툴팁이 떠 있는 동안 무반응 타이머를 멈춘다.
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

        private void OnDestroy()
        {
            if (clueTray != null)
            {
                clueTray.TooltipOpened -= OnTooltipOpened;
                clueTray.TooltipClosed -= OnTooltipClosed;
            }
            if (Session != null) Session.GameOverTriggered -= OnGameOver;
        }
    }
}
