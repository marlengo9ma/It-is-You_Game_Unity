using System;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;
using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>
    /// 한 판을 관통하는 상태. 단서 보관함, 신뢰도, 플레이 성향, 수집한 컷씬을 들고 있다.
    ///
    /// 성향 집계가 여기 있는 이유는 기획서의
    /// "챕터 중간마다의 플레이어의 성향 표시와 타 유저와의 선택지 차이,
    ///  수집한 컷씬을 표시" 때문이다. 엔딩 크레딧에서 쓰인다.
    /// </summary>
    public class GameSession
    {
        private readonly Dictionary<ChoiceTone, int> _toneCounts = new Dictionary<ChoiceTone, int>();
        private readonly HashSet<string> _seenCutsceneIds = new HashSet<string>();
        private readonly HashSet<string> _craftedRecipeIds = new HashSet<string>();

        public ClueInventory Clues { get; } = new ClueInventory();

        /// <summary>누적 신뢰도. 0 미만으로 내려가면 최종 판정에서 불리해진다.</summary>
        public int Trust { get; private set; }

        public GameOverKind GameOver { get; private set; } = GameOverKind.None;

        /// <summary>플레이어가 지목한 용의자 id. 아직 지목 전이면 null.</summary>
        public string AccusedSuspectId { get; private set; }

        public event Action<int> TrustChanged;
        public event Action<GameOverKind> GameOverTriggered;

        public bool IsGameOver => GameOver != GameOverKind.None;

        // --------------------------------------------------------------- 신뢰도

        public void AddTrust(int delta)
        {
            if (delta == 0) return;
            Trust += delta;
            TrustChanged?.Invoke(Trust);
        }

        // --------------------------------------------------------------- 성향

        public void RecordTone(ChoiceTone tone)
        {
            if (tone != ChoiceTone.Friendly && tone != ChoiceTone.Aggressive) return;
            _toneCounts.TryGetValue(tone, out var n);
            _toneCounts[tone] = n + 1;
        }

        public int ToneCount(ChoiceTone tone) => _toneCounts.TryGetValue(tone, out var n) ? n : 0;

        /// <summary>
        /// -1(완전 강압) ~ +1(완전 친근) 사이의 성향 값.
        /// 챕터 사이에 "당신의 수사 성향" 을 보여줄 때 쓴다.
        /// </summary>
        public float ToneBias
        {
            get
            {
                var friendly = ToneCount(ChoiceTone.Friendly);
                var aggressive = ToneCount(ChoiceTone.Aggressive);
                var total = friendly + aggressive;
                if (total == 0) return 0f;
                return (friendly - aggressive) / (float)total;
            }
        }

        // --------------------------------------------------------------- 컷씬

        /// <summary>컷씬을 봤다고 기록한다. 처음 본 것이면 true.</summary>
        public bool MarkCutsceneSeen(string cutsceneId)
            => !string.IsNullOrEmpty(cutsceneId) && _seenCutsceneIds.Add(cutsceneId);

        public bool HasSeenCutscene(string cutsceneId) => _seenCutsceneIds.Contains(cutsceneId);

        public IReadOnlyCollection<string> SeenCutscenes => _seenCutsceneIds;

        // --------------------------------------------------------------- 조합

        public bool MarkRecipeCrafted(string recipeId)
            => !string.IsNullOrEmpty(recipeId) && _craftedRecipeIds.Add(recipeId);

        public bool HasCrafted(string recipeId) => _craftedRecipeIds.Contains(recipeId);

        // --------------------------------------------------------------- 지목

        public void Accuse(string suspectId) => AccusedSuspectId = suspectId;

        // ------------------------------------------------------------ 게임오버

        public void TriggerGameOver(GameOverKind kind)
        {
            if (kind == GameOverKind.None || IsGameOver) return;
            GameOver = kind;
            GameOverTriggered?.Invoke(kind);
        }

        /// <summary>
        /// 조합 단계에서 재료가 모자라 더 진행할 수 없는가.
        /// 기획서의 "단서 조합 페이즈에서의 단서 부족으로 인한 추합 불가 판정으로
        /// 게임오버(자기 비하 엔딩)" 판정.
        /// </summary>
        public bool CheckDeductionDeadlock(ChapterDefinition chapter)
        {
            if (chapter == null) return false;
            var recipes = chapter.Recipes;
            if (recipes == null) return false;

            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe == null || !recipe.IsRequiredForAccusation) continue;
                if (!recipe.CanCraftWith(Clues.OwnedIds)) return true;
            }
            return false;
        }

        public void Reset()
        {
            Clues.Clear();
            _toneCounts.Clear();
            _seenCutsceneIds.Clear();
            _craftedRecipeIds.Clear();
            Trust = 0;
            GameOver = GameOverKind.None;
            AccusedSuspectId = null;
        }
    }
}
