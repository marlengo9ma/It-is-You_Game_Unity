using System.Collections.Generic;
using ItIsYou.Data;

namespace ItIsYou.Clues
{
    /// <summary>
    /// 단서 수집 페이즈를 끝낼 수 있는지 판정한다.
    ///
    /// 기획서 기준:
    /// <list type="bullet">
    /// <item>100% 수집 — "음, 이 정도면 충분하겠군." → 진행</item>
    /// <item>70% 이상 — "필요한 건 찾은 것 같긴 한데..조금만 더 살펴볼까?" → 경고 후 진행 여부 선택</item>
    /// <item>50% 미만 혹은 주요 단서 미수집 — "뭔가 부족해." → 진행 불가</item>
    /// </list>
    ///
    /// 주요 단서 누락은 비율과 무관하게 무조건 진행 불가다. 비율 조건보다 먼저 따진다.
    /// </summary>
    public readonly struct CollectionGate
    {
        public readonly CollectionGateResult Result;
        public readonly int Collected;
        public readonly int Total;
        public readonly IReadOnlyList<ClueDefinition> MissingKeyClues;

        public float Ratio => Total <= 0 ? 1f : (float)Collected / Total;
        public bool CanProceed => Result != CollectionGateResult.Blocked;
        public bool NeedsConfirm => Result == CollectionGateResult.WarnAndConfirm;

        private CollectionGate(CollectionGateResult result, int collected, int total,
            IReadOnlyList<ClueDefinition> missingKeyClues)
        {
            Result = result;
            Collected = collected;
            Total = total;
            MissingKeyClues = missingKeyClues;
        }

        public static CollectionGate Evaluate(ChapterDefinition chapter, ClueInventory inventory)
        {
            if (chapter == null || inventory == null)
                return new CollectionGate(CollectionGateResult.Blocked, 0, 0, System.Array.Empty<ClueDefinition>());

            var total = 0;
            var collected = 0;
            List<ClueDefinition> missingKeys = null;

            foreach (var clue in chapter.EnumerateCountedClues())
            {
                total++;
                if (inventory.Has(clue.Id))
                {
                    collected++;
                }
                else if (clue.IsKeyClue)
                {
                    missingKeys ??= new List<ClueDefinition>();
                    missingKeys.Add(clue);
                }
            }

            IReadOnlyList<ClueDefinition> missing =
                (IReadOnlyList<ClueDefinition>)missingKeys ?? System.Array.Empty<ClueDefinition>();

            // 주요 단서가 하나라도 빠졌으면 비율과 무관하게 막는다.
            if (missing.Count > 0)
                return new CollectionGate(CollectionGateResult.Blocked, collected, total, missing);

            var ratio = total <= 0 ? 1f : (float)collected / total;

            if (ratio >= chapter.PassThreshold)
                return new CollectionGate(CollectionGateResult.Pass, collected, total, missing);

            if (ratio >= chapter.WarnThreshold)
                return new CollectionGate(CollectionGateResult.WarnAndConfirm, collected, total, missing);

            return new CollectionGate(CollectionGateResult.Blocked, collected, total, missing);
        }

        /// <summary>판정에 맞는 대사 묶음.</summary>
        public DialogueSequence PickDialogue(ChapterDefinition chapter)
        {
            if (chapter == null) return null;
            return Result switch
            {
                CollectionGateResult.Pass => chapter.GatePass,
                CollectionGateResult.WarnAndConfirm => chapter.GateWarn,
                _ => chapter.GateBlocked
            };
        }
    }
}
