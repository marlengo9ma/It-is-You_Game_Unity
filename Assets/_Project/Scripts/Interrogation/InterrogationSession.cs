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
        /// <summary>등장 조건을 못 채워 잠긴 채로 보이는 줄. 누를 수 없다.</summary>
        public readonly bool Locked;

        public ChoiceView(ChoiceDefinition definition, string label, bool hasUnseenContent, int timesUsed, bool affordable, bool locked = false)
        {
            Definition = definition;
            Label = label;
            HasUnseenContent = hasUnseenContent;
            TimesUsed = timesUsed;
            Affordable = affordable;
            Locked = locked;
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

        /// <summary>
        /// 페널티에 대한 첫 설명을 띄울 자리. 강압 선택지 뒤의 시간 경과 대사가 끝났을 때,
        /// 무반응 회차 대사가 동났을 때, 어떤 페널티 판정이든 처음 났을 때.
        /// 실제로 띄울지(한 번만)는 컨트롤러가 정한다.
        /// </summary>
        public readonly bool PenaltyLesson;

        public InterrogationStep(DialogueSequence main, DialogueSequence follow, PressureVerdict verdict,
            bool ends, InterrogationEndReason endReason, bool tutorialLesson, bool penaltyLesson = false)
        {
            Main = main;
            Follow = follow;
            Verdict = verdict;
            Ends = ends;
            EndReason = endReason;
            TutorialLesson = tutorialLesson;
            PenaltyLesson = penaltyLesson;
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

        // 직전에 고른 선택지의 <시간 경과> 대사. 다음 무반응 판정에서 회차 대사 대신 나온다.
        private DialogueSequence _pendingIdleFollowUp;
        private ChoiceTone _pendingIdleFollowUpTone;

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

        /// <summary>지금까지 무반응 판정이 몇 번 났는가(선택지 뒤의 시간 경과는 세지 않는다). 회차마다 반응이 달라진다.</summary>
        public int IdleRounds { get; private set; }

        /// <summary>메인 선택지를 몇 번 골랐는가. 첫 선택 뒤 튜토리얼 팝업의 트리거가 된다.</summary>
        public int ChoicesMade { get; private set; }

        /// <summary>심문이 끝났다. (사유)</summary>
        public event Action<InterrogationEndReason> Ended;

        /// <summary>몇 차 심문인가 (1 또는 2). 2차는 선택지·토큰이 다를 수 있다.</summary>
        public int Round { get; }

        /// <summary>이야기상 이 상대의 심문이 마무리됐다(자수 등 EndsInterrogation 선택지). 2차 심문에서 다시 고를 수 없다.</summary>
        public bool Concluded { get; private set; }

        /// <summary>토큰을 바깥에서 받았는가 (2차 심문의 공용 타임 토큰).</summary>
        public bool SharesTokens { get; }

        /// <summary>타임 토큰·무반응 판정이 없는 회차 (2차 심문). 토큰 주머니는 형식상만 있다.</summary>
        public bool Untimed { get; }

        /// <summary>2차 심문에서 들고 들어간 가설(추론 단서 id). 다른 가설로 여는 선택지는 이번 심문에 나타나지 않는다.</summary>
        public string ActiveHypothesisId { get; }

        public InterrogationSession(ChapterDefinition chapter, SuspectDefinition suspect, ClueInventory inventory, int round = 1,
            TimeTokenPool sharedTokens = null, bool untimed = false, string activeHypothesisId = null)
        {
            Untimed = untimed;
            ActiveHypothesisId = activeHypothesisId;
            _chapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            _suspect = suspect ?? throw new ArgumentNullException(nameof(suspect));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Round = Math.Max(1, round);

            SharesTokens = sharedTokens != null;
            Tokens = sharedTokens ?? new TimeTokenPool(untimed ? 999 : suspect.TokensForRound(Round));
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
            if (IsOver || Untimed) return;
            Idle.Tick(unscaledDeltaTime);
        }

        // ---------------------------------------------------------------- 선택지

        /// <summary>지금 목록에 띄울 선택지들.</summary>
        public List<ChoiceView> BuildChoiceList()
        {
            var result = new List<ChoiceView>();
            var choices = _suspect.ChoicesForRound(Round);
            if (choices == null) return result;

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice == null) continue;
                if (_consumedChoiceIds.Contains(choice.Id)) continue;
                if (OpensWithOtherHypothesis(choice)) continue;
                if (!IsChoiceAvailable(choice))
                {
                    // 단서 제시 선택지는 추론 카드가 없으면 ??? 로 잠긴 채 보인다 — 조합의 결과가 여기서 보인다.
                    if (choice.ShowLockedPlaceholder)
                        result.Add(new ChoiceView(choice, choice.LockedLabel, false, 0, false, locked: true));
                    continue;
                }

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

        /// <summary>가설을 하나 들고 들어온 심문에서, 다른 가설로 여는 선택지는 아예 보이지 않는다.</summary>
        private bool OpensWithOtherHypothesis(ChoiceDefinition choice)
        {
            if (string.IsNullOrEmpty(ActiveHypothesisId)) return false;
            var req = choice.RequiredClueIds;
            if (req == null) return false;
            for (int i = 0; i < req.Count; i++)
            {
                if (req[i] != ActiveHypothesisId && _chapter.IsHypothesisResult(req[i])) return true;
            }
            return false;
        }

        private bool IsChoiceAvailable(ChoiceDefinition choice)
        {
            if (!_inventory.HasAll(choice.RequiredClueIds)) return false;

            // 단서 제시 선택지는 수집품을 '단서 오브젝트'로 승격해 두어야 열린다.
            // 기획: "이를 누를 시, 해당 오브젝트는 단순 '수집 오브젝트'에서 '단서 오브젝트'로 변경된다."
            if (!_inventory.HasAllPromoted(choice.RequiredPromotedClueIds)) return false;

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
            ChoicesMade++;
            if (choice.ConsumeOnUse) _consumedChoiceIds.Add(choice.Id);

            var unlocks = choice.UnlocksChoiceIds;
            if (unlocks != null)
            {
                for (int i = 0; i < unlocks.Count; i++) _unlockedChoiceIds.Add(unlocks[i]);
            }

            // 이 선택지 뒤에 <시간 경과> 대사가 있으면 다음 침묵에서 그 줄이 나온다.
            _pendingIdleFollowUp = choice.HasIdleFollowUp ? choice.IdleFollowUp : null;
            _pendingIdleFollowUpTone = choice.Tone;

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

            _pendingIdleFollowUp = null;

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

        /// <summary>
        /// 플레이어가 직접 심문을 끝낸다. 기획: "최종적으로 수동으로 페이즈를 종료시키거나".
        /// 용의자의 마무리 대사가 있으면 그것이 다음 인물로 넘어가는 다리가 된다.
        /// </summary>
        public InterrogationStep EndManually() => Finish(InterrogationEndReason.PlayerEnded, _suspect.OnEndInterrogation);

        /// <summary>무반응 판정이 났을 때 컨트롤러가 호출한다.</summary>
        public InterrogationStep ApplyIdlePenalty()
        {
            if (IsOver || Untimed) return InterrogationStep.Nothing;

            Tokens.Spend(1, TimeTokenPool.SpendReason.Idle);
            Idle.Restart();

            if (IsOver) return InterrogationStep.Nothing; // 토큰 소진으로 이미 끝났다

            // 직전 선택지에 <시간 경과> 대사가 붙어 있으면 그것이 먼저다.
            // 강압 선택지의 시간 경과 뒤에는 페널티 설명 팝업이 이어진다.
            if (_pendingIdleFollowUp != null)
            {
                var followUp = _pendingIdleFollowUp;
                _pendingIdleFollowUp = null;
                var lesson = _pendingIdleFollowUpTone == ChoiceTone.Aggressive;
                return new InterrogationStep(followUp, null, PressureVerdict.None,
                    false, InterrogationEndReason.PlayerEnded, false, lesson);
            }

            // 회차마다 다른 줄을 쓴다. 같은 말이 반복되면 침묵에 값이 붙지 않는다.
            IdleRounds++;
            var exhausted = _suspect.IdleRoundCount > 0 && IdleRounds >= _suspect.IdleRoundCount;
            return new InterrogationStep(_suspect.GetIdle(IdleRounds), null, PressureVerdict.None,
                false, InterrogationEndReason.PlayerEnded, false, exhausted);
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
            var penaltyLesson = verdict != PressureVerdict.None;

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
                return new InterrogationStep(main, follow, verdict, true, reason, false, penaltyLesson);
            }

            if (forcedEnd && !IsOver)
            {
                Concluded = true;
                MarkOver(InterrogationEndReason.PlayerEnded);
                return new InterrogationStep(main, follow, verdict, true,
                    InterrogationEndReason.PlayerEnded, tutorialLesson, penaltyLesson);
            }

            // 토큰 소진으로 이미 끝났다면 그 사유를 그대로 전한다.
            if (IsOver)
                return new InterrogationStep(main, _suspect.OnTokensExhausted, verdict, true,
                    EndReason, tutorialLesson, penaltyLesson);

            return new InterrogationStep(main, follow, verdict, false,
                InterrogationEndReason.PlayerEnded, tutorialLesson, penaltyLesson);
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
            Tokens.Exhausted -= OnTokensExhausted;   // 공용 토큰이면 다음 심문의 소진 알림이 이 세션으로 오면 안 된다
            EndReason = reason;
            Idle.Stop();
            Idle.ReleaseAll();
            Ended?.Invoke(reason);
        }
    }
}
