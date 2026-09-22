using System;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;

namespace ItIsYou.Deduction
{
    /// <summary>가설 도출 시도 한 번의 결과.</summary>
    public enum HypothesisOutcome
    {
        /// <summary>두 단서가 이어졌다. 가설이 저장됐다. (PhaseEnded 면 이 시도로 잉크가 다 말라 페이즈가 끝났다)</summary>
        Success = 0,
        /// <summary>이어졌지만 가설 칸이 가득 찼다. 기존 가설 하나를 지워야 저장된다.</summary>
        SuccessNeedsReplace = 1,
        /// <summary>이미 만든 가설이다. 잉크를 쓰지 않는다.</summary>
        Duplicate = 2,
        /// <summary>관계없는 두 단서. 잉크 −1.</summary>
        Fail = 3,
        /// <summary>실패했고 잉크가 다 말랐는데 가설이 하나도 없다 — 게임오버.</summary>
        Fatal = 4
    }

    public readonly struct HypothesisResult
    {
        public readonly HypothesisOutcome Outcome;
        /// <summary>성공이면 맞은 조합식.</summary>
        public readonly HypothesisRecipe Recipe;
        /// <summary>몇 번째 실패인가 (1부터). 실패가 아니면 0.</summary>
        public readonly int FailRound;
        /// <summary>이 시도로 페이즈가 끝났다 (잉크 소진 + 가설 ≥ 1).</summary>
        public readonly bool PhaseEnded;

        public HypothesisResult(HypothesisOutcome outcome, HypothesisRecipe recipe, int failRound, bool phaseEnded)
        {
            Outcome = outcome;
            Recipe = recipe;
            FailRound = failRound;
            PhaseEnded = phaseEnded;
        }
    }

    /// <summary>
    /// 단서 조합(가설) 페이즈의 규칙. MonoBehaviour 가 아니라 순수 C# — 화면 없이 검증할 수 있다.
    ///
    /// 2026-09-23 기획:
    /// 물품 단서 하나 + 정황 단서 하나 → 이어지면 〈가설〉, 아니면 실패하고 〈잉크〉 −1.
    /// 가설은 최대 N개(튜토리얼 2, 기본 3) 저장. 가득 찬 채 새 가설이 생기면 하나를 지우고 저장.
    /// 잉크(기본 4)가 다 마르고 가설이 하나도 없으면 게임오버.
    /// 정한 것(기획서에 없는 부분): 성공은 잉크를 쓰지 않는다. 잉크가 0이 됐는데 가설이 하나라도 있으면
    /// 페이즈가 저절로 끝난다. 가설이 하나라도 있으면 "추리를 마친다" 로 먼저 끝낼 수 있다.
    /// </summary>
    public class DeductionSession
    {
        private readonly ChapterDefinition _chapter;
        private readonly ClueInventory _inventory;
        private readonly HashSet<string> _triedPairs = new HashSet<string>();

        private HypothesisRecipe _pendingReplace;

        public int Ink { get; private set; }
        public int InkCapacity { get; }
        public int MaxHypotheses { get; }
        public bool IsOver { get; private set; }
        public bool Succeeded { get; private set; }
        public int FailCount { get; private set; }

        /// <summary>잉크가 바뀌었다. (남은 수)</summary>
        public event Action<int> InkChanged;

        /// <summary>가설 목록이 바뀌었다.</summary>
        public event Action HypothesesChanged;

        public DeductionSession(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            InkCapacity = chapter.DeductionInk;
            Ink = InkCapacity;
            MaxHypotheses = chapter.MaxHypotheses;
        }

        /// <summary>지금 저장돼 있는 가설(추론 단서)들. 목록 순서.</summary>
        public List<ClueDefinition> Hypotheses
        {
            get
            {
                var list = new List<ClueDefinition>();
                foreach (var c in _inventory.OfKind(ClueKind.Deduced)) list.Add(c);
                return list;
            }
        }

        public int HypothesisCount
        {
            get { var n = 0; foreach (var _ in _inventory.OfKind(ClueKind.Deduced)) n++; return n; }
        }

        public bool CanEnd => HypothesisCount >= 1;
        public bool HasPendingReplace => _pendingReplace != null;
        public HypothesisRecipe PendingReplace => _pendingReplace;

        /// <summary>이 짝을 이미 시도해 실패했는가 — 화면에서 흐리게 알려 주기 위해.</summary>
        public bool TriedAndFailed(string a, string b) => _triedPairs.Contains(PairKey(a, b));

        /// <summary>단서 종류가 자리에 맞는가. 왼쪽은 물품, 오른쪽은 정황(증언·리액션).</summary>
        public static bool IsObjectSide(ClueDefinition c) => c != null && c.Kind == ClueKind.Object;
        public static bool IsLeadSide(ClueDefinition c) => c != null && (c.Kind == ClueKind.Testimony || c.Kind == ClueKind.Reaction);

        /// <summary>물품 단서 하나와 정황 단서 하나로 가설 도출을 시도한다.</summary>
        public HypothesisResult TryCombine(ClueDefinition objectClue, ClueDefinition leadClue)
        {
            if (IsOver || _pendingReplace != null || objectClue == null || leadClue == null)
                return new HypothesisResult(HypothesisOutcome.Fail, null, FailCount, false);

            var recipe = _chapter.FindHypothesis(objectClue.Id, leadClue.Id);

            // 이미 세운 가설을 또 이으면 아무 일도 없다 — 시도로 치지 않는다.
            if (recipe != null && recipe.Result != null && _inventory.Has(recipe.Result.Id))
                return new HypothesisResult(HypothesisOutcome.Duplicate, recipe, 0, false);

            // 모든 시도는 잉크를 쓴다 (2026-09-23 기획: "이 과정에서 모든 시도는 <잉크>를 소비").
            Ink = Math.Max(0, Ink - 1);
            InkChanged?.Invoke(Ink);

            if (recipe != null && recipe.Result != null)
            {
                if (HypothesisCount >= MaxHypotheses)
                {
                    _pendingReplace = recipe;
                    return new HypothesisResult(HypothesisOutcome.SuccessNeedsReplace, recipe, 0, false);
                }

                AddHypothesis(recipe.Result);
                if (Ink <= 0) Finish(true);
                return new HypothesisResult(HypothesisOutcome.Success, recipe, 0, Ink <= 0);
            }

            // 실패
            _triedPairs.Add(PairKey(objectClue.Id, leadClue.Id));
            FailCount++;

            if (Ink > 0)
                return new HypothesisResult(HypothesisOutcome.Fail, null, FailCount, false);

            if (HypothesisCount == 0)
            {
                Finish(false);
                return new HypothesisResult(HypothesisOutcome.Fatal, null, FailCount, true);
            }

            Finish(true);
            return new HypothesisResult(HypothesisOutcome.Fail, null, FailCount, true);
        }

        /// <summary>가득 찬 가설 칸에서 하나를 지우고, 기다리던 새 가설을 저장한다.</summary>
        public ClueDefinition ConfirmReplace(string removeClueId)
        {
            if (_pendingReplace == null) return null;
            var removed = _inventory.Get(removeClueId);
            if (removed == null || removed.Kind != ClueKind.Deduced) return null;

            _inventory.Remove(removeClueId);
            var recipe = _pendingReplace;
            _pendingReplace = null;
            AddHypothesis(recipe.Result);
            if (Ink <= 0) Finish(true);
            return recipe.Result;
        }

        /// <summary>새 가설을 버리고 기존 것을 지킨다. 잉크는 이미 쓴 뒤다.</summary>
        public void CancelReplace()
        {
            _pendingReplace = null;
            if (Ink <= 0 && HypothesisCount > 0) Finish(true);
        }

        /// <summary>가설이 하나라도 있으면 먼저 끝낼 수 있다.</summary>
        public bool EndManually()
        {
            if (IsOver || !CanEnd) return false;
            Finish(true);
            return true;
        }

        /// <summary>가설은 플레이어가 직접 세운 것이라 처음부터 세부 명칭으로 보인다.</summary>
        private void AddHypothesis(ClueDefinition result)
        {
            _inventory.Add(result);
            _inventory.MarkRead(result.Id);
            HypothesesChanged?.Invoke();
        }

        private static string PairKey(string a, string b)
            => string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;

        private void Finish(bool success)
        {
            IsOver = true;
            Succeeded = success;
        }
    }
}
