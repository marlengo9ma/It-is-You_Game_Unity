using System;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;

namespace ItIsYou.Interrogation
{
    /// <summary>선택지 목록 한 줄에 필요한 표시 정보.</summary>
    public readonly struct ChoiceView
    {
        public readonly ChoiceDefinition Definition;
        public readonly string Label;
        public readonly bool HasUnseenContent;
        public readonly int TimesUsed;
        public readonly bool Affordable;

        public ChoiceView(ChoiceDefinition definition, string label, bool hasUnseenContent, int timesUsed, bool affordable)
        {
            Definition = definition;
            Label = label;
            HasUnseenContent = hasUnseenContent;
            TimesUsed = timesUsed;
            Affordable = affordable;
        }
    }

    /// <summary>한 번의 입력에 대해 무엇을 재생하고 무엇이 끝났는지.</summary>
    public readonly struct InterrogationStep
    {
        /// <summary>재생할 본 대사. 없을 수 있다.</summary>
        public readonly DialogueSequence Main;

        /// <summary>본 대사 뒤에 이어 붙일 반응 대사(경고·거부 등). 없을 수 있다.</summary>
        public readonly DialogueSequence Follow;

        public readonly PressureVerdict Verdict;
        public readonly bool Ends;
        public readonly InterrogationEndReason EndReason;

        /// <summary>튜토리얼이라 페널티가 유예됐다. UI 는 설명 팝업을 띄워야 한다.</summary>
        public readonly bool TutorialLesson;

        public InterrogationStep(DialogueSequence main, DialogueSequence follow, PressureVerdict verdict,
            bool ends, InterrogationEndReason endReason, bool tutorialLesson)
        {
            Main = main;
            Follow = follow;
            Verdict = verdict;
            Ends = ends;
            EndReason = endReason;
            TutorialLesson = tutorialLesson;
        }

        public static InterrogationStep Nothing =>
            new InterrogationStep(null, null, PressureVerdict.None, false, InterrogationEndReason.PlayerEnded, false);
    }

    /// <summary>
    /// 용의자 한 명에 대한 심문 진행 규칙. MonoBehaviour 가 아니라 순수 C# 이므로
    /// 화면 없이도 규칙을 그대로 검증할 수 있다.
    ///
    /// 화면 연출과 입력은 <c>InterrogationController</c> 가 맡고,
    /// 이 클래스는 "무엇이 일어나야 하는가"만 답한다.
    /// </summary>
    public class InterrogationSession
    {
        private readonly ChapterDefinition _chapter;
        private readonly SuspectDefinition _suspect;
        private readonly ClueInventory _inventory;

        private readonly Dictionary<string, int> _choiceUseCount = new Dictionary<string, int>();
        private readonly HashSet<string> _unlockedChoiceIds = new HashSet<string>();
        private readonly HashSet<string> _consumedChoiceIds = new HashSet<string>();
        private readonly HashSet<string> _inspectedObjectIds = new HashSet<string>();

        public TimeTokenPool Tokens { get; }
        public PressureTracker Pressure { get; }
        public IdleTimer Idle { get; }
        public MemoryRecall Recall { get; }
        public InterrogationLog Log { get; }

        public ChapterDefinition Chapter => _chapter;
        public SuspectDefinition Suspect => _suspect;
        public bool IsOver { get; private set; }
        public InterrogationEndReason EndReason { get; private set; }

        /// <summary>신뢰도 누적 변화량. 최종 지목 판정에서 쓴다.</summary>
        public int TrustDelta { get; private set; }

        /// <summary>심문이 끝났다. (사유)</summary>
        public event Action<InterrogationEndReason> Ended;

        public InterrogationSession(ChapterDefinition chapter, SuspectDefinition suspect, ClueInventory inventory)
        {
            _chapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            _suspect = suspect ?? throw new ArgumentNullException(nameof(suspect));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            Tokens = new TimeTokenPool(suspect.TimeTokens);
            Pressure = new PressureTracker(chapter);
            Idle = new IdleTimer(chapter.IdleSeconds);
            Recall = new MemoryRecall();
            Log = new InterrogationLog();

            Tokens.Exhausted += OnTokensExhausted;
        }

        public void Begin()
        {
            IsOver = false;
            Idle.Start();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (IsOver) return;
            Idle.Tick(unscaledDeltaTime);
        }

        // ---------------------------------------------------------------- 선택지

        /// <summary>지금 목록에 띄울 선택지들.</summary>
        public List<ChoiceView> BuildChoiceList()
        {
            var result = new List<ChoiceView>();
            var choices = _suspect.Choices;
            if (choices == null) return result;

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice == null) continue;
                if (_consumedChoiceIds.Contains(choice.Id)) continue;
                if (!IsChoiceAvailable(choice)) continue;

                var used = GetUseCount(choice.Id);
                result.Add(new ChoiceView(
                    choice,
                    choice.ResolveLabel(_inventory.OwnedIds),
                    choice.HasUnseenPass(used),
                    used,
                    Tokens.CanAfford(choice.TokenCost)));
            }

            return result;
        }

        private bool IsChoiceAvailable(ChoiceDefinition choice)
        {
            if (!_inventory.HasAll(choice.RequiredClueIds)) return false;

            var required = choice.RequiredChoiceIds;
            if (required != null)
            {
                for (int i = 0; i < required.Count; i++)
                {
                    if (GetUseCount(required[i]) <= 0) return false;
                }
            }

            if (choice.AppearsWhenTokensAtOrBelow > 0 && Tokens.Remaining > choice.AppearsWhenTokensAtOrBelow)
                return false;

            if (choice.AvailableFromStart) return true;
            return _unlockedChoiceIds.Contains(choice.Id);
        }

        public int GetUseCount(string choiceId)
            => (!string.IsNullOrEmpty(choiceId) && _choiceUseCount.TryGetValue(choiceId, out var n)) ? n : 0;

        /// <summary>메인 선택지를 고른다.</summary>
        public InterrogationStep SelectChoice(ChoiceDefinition choice)
        {
            if (IsOver || choice == null) return InterrogationStep.Nothing;

            var used = GetUseCount(choice.Id);
            var pass = choice.GetPass(used);

            _choiceUseCount[choice.Id] = used + 1;
            if (choice.ConsumeOnUse) _consumedChoiceIds.Add(choice.Id);

            var unlocks = choice.UnlocksChoiceIds;
            if (unlocks != null)
            {
                for (int i = 0; i < unlocks.Count; i++) _unlockedChoiceIds.Add(unlocks[i]);
            }

            Tokens.Spend(choice.TokenCost, TimeTokenPool.SpendReason.Choice);
            Idle.Restart();

            if (pass != null) TrustDelta += pass.TrustDelta;

            var verdict = Pressure.RegisterChoice(choice.Tone);
            return Resolve(pass, verdict, forcedEnd: pass != null && pass.EndsInterrogation);
        }

        // ---------------------------------------------------------- 심문 오브젝트

        /// <summary>심문 화면 안의 오브젝트를 고른다.</summary>
        public InterrogationStep SelectInterrogationObject(InteractableDefinition target)
        {
            if (IsOver || target == null) return InterrogationStep.Nothing;

            var firstTime = _inspectedObjectIds.Add(target.Id);
            var lines = firstTime ? target.OnInspect : target.OnReinspect;
            var sequence = new DialogueSequence(lines, target.Id);

            Tokens.Spend(1, TimeTokenPool.SpendReason.InterrogationObject);
            Idle.Restart();

            var verdict = Pressure.RegisterInterrogationObject();
            return Resolve(sequence, verdict, forcedEnd: false);
        }

        // ------------------------------------------------------------------ 수집

        /// <summary>대사 중 밑줄 구간을 클릭해 증언을 수집한다.</summary>
        public bool CollectTestimony(ClueDefinition clue)
        {
            if (IsOver || clue == null) return false;
            Idle.Restart();
            return _inventory.Add(clue);
        }

        /// <summary>
        /// 리액션 타이밍에 캐릭터를 클릭해 정황 단서를 수집한다.
        /// 기억 짐작 중이라면 현재 회차에서 아직 읽히는 리액션이어야 한다.
        /// </summary>
        public bool CollectReaction(InterrogationLogEntry entry, ClueDefinition clue)
        {
            if (IsOver || entry == null || clue == null) return false;
            if (entry.ReactionCollected) return false;

            var cue = entry.Line?.Reaction;
            if (cue == null || !cue.IsValid) return false;
            if (!cue.IsCollectableAtRecallDepth(Recall.EffectiveDepth)) return false;

            if (!_inventory.Add(clue)) return false;

            entry.ReactionCollected = true;
            entry.ReactionMissed = false;
            Idle.Restart();
            return true;
        }

        // ------------------------------------------------------------ 기억 짐작

        /// <summary>기억 짐작에 들어간다. 토큰은 들지 않는다 — 조수의 도움에만 든다.</summary>
        public RecallDistortion EnterRecall()
        {
            Idle.Hold();
            Recall.Enter();
            return Recall.CurrentDistortion();
        }

        /// <summary>조수의 도움으로 흐려진 부분을 복원한다. 추가 타임 토큰을 쓴다.</summary>
        public bool UseRecallAssist()
        {
            if (IsOver) return false;
            if (!Tokens.CanAfford(_chapter.AssistRecallTokenCost)) return false;
            if (!Recall.UseAssist()) return false;

            Tokens.Spend(_chapter.AssistRecallTokenCost, TimeTokenPool.SpendReason.AssistedRecall);
            return true;
        }

        public void ExitRecall()
        {
            Recall.Exit();
            Idle.Release();
            Idle.Restart();
        }

        // ------------------------------------------------------------------ 종료

        /// <summary>플레이어가 직접 심문을 끝낸다.</summary>
        public InterrogationStep EndManually() => Finish(InterrogationEndReason.PlayerEnded, null);

        /// <summary>무반응 판정이 났을 때 컨트롤러가 호출한다.</summary>
        public InterrogationStep ApplyIdlePenalty()
        {
            if (IsOver) return InterrogationStep.Nothing;

            Tokens.Spend(1, TimeTokenPool.SpendReason.Idle);
            Idle.Restart();

            if (IsOver) return InterrogationStep.Nothing; // 토큰 소진으로 이미 끝났다
            return new InterrogationStep(_suspect.OnIdle, null, PressureVerdict.None,
                false, InterrogationEndReason.PlayerEnded, false);
        }

        private void OnTokensExhausted()
        {
            if (IsOver) return;
            Finish(InterrogationEndReason.TokensExhausted, _suspect.OnTokensExhausted);
        }

        // ---------------------------------------------------------------- 내부

        private InterrogationStep Resolve(DialogueSequence main, PressureVerdict verdict, bool forcedEnd)
        {
            var follow = PickVerdictDialogue(verdict);
            var terminal = PressureTracker.IsTerminal(verdict);

            // 튜토리얼 챕터는 페널티가 붙어도 심문을 끊지 않는다.
            // 대신 무엇을 잘못했는지 설명 팝업을 띄운다.
            var tutorialLesson = terminal && _chapter.IsTutorial;
            if (tutorialLesson) terminal = false;

            if (terminal)
            {
                var reason = verdict switch
                {
                    PressureVerdict.RefusedByFear => InterrogationEndReason.RefusedByFear,
                    PressureVerdict.RefusedByAnger => InterrogationEndReason.RefusedByAnger,
                    PressureVerdict.GameOverByToneAlternation => InterrogationEndReason.GameOverByErraticTone,
                    _ => InterrogationEndReason.PlayerEnded
                };

                Tokens.DrainAll();
                MarkOver(reason);
                return new InterrogationStep(main, follow, verdict, true, reason, false);
            }

            if (forcedEnd && !IsOver)
            {
                MarkOver(InterrogationEndReason.PlayerEnded);
                return new InterrogationStep(main, follow, verdict, true,
                    InterrogationEndReason.PlayerEnded, tutorialLesson);
            }

            // 토큰 소진으로 이미 끝났다면 그 사유를 그대로 전한다.
            if (IsOver)
                return new InterrogationStep(main, _suspect.OnTokensExhausted, verdict, true,
                    EndReason, tutorialLesson);

            return new InterrogationStep(main, follow, verdict, false,
                InterrogationEndReason.PlayerEnded, tutorialLesson);
        }

        private DialogueSequence PickVerdictDialogue(PressureVerdict verdict) => verdict switch
        {
            PressureVerdict.AggressionWarning => _suspect.AggressionWarning,
            PressureVerdict.RefusedByFear => _suspect.RefusalByFear,
            PressureVerdict.ObjectSpamWarning => _suspect.ObjectSpamWarning,
            PressureVerdict.RefusedByAnger => _suspect.RefusalByAnger,
            PressureVerdict.ToneAlternationWarning => _suspect.ToneAlternationWarning,
            PressureVerdict.GameOverByToneAlternation => _suspect.ToneAlternationGameOver,
            _ => null
        };

        private InterrogationStep Finish(InterrogationEndReason reason, DialogueSequence closing)
        {
            if (IsOver) return InterrogationStep.Nothing;
            MarkOver(reason);
            return new InterrogationStep(closing, null, PressureVerdict.None, true, reason, false);
        }

        private void MarkOver(InterrogationEndReason reason)
        {
            IsOver = true;
            EndReason = reason;
            Idle.Stop();
            Idle.ReleaseAll();
            Ended?.Invoke(reason);
        }
    }
}
