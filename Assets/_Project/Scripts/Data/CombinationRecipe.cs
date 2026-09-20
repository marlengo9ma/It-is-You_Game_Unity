using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 단서 조합 한 건. 기획서: "증거 수집 페이즈에서 수집한 &lt;단서&gt;와
    /// 심문 페이즈에서 수집한 &lt;의심&gt;을 조합하여, 새로운 심문 주제 및 선택지 추가 가능."
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Combination Recipe", fileName = "Combo_")]
    public class CombinationRecipe : ScriptableObject
    {
        [Header("재료")]
        [Tooltip("이 단서들을 모두 가지고 있어야 조합할 수 있다. 순서는 상관없다.")]
        [SerializeField] private List<ClueDefinition> ingredients = new List<ClueDefinition>();

        [Header("결과")]
        [Tooltip("조합으로 새로 얻는 단서.")]
        [SerializeField] private ClueDefinition result;

        [Tooltip("조합 성공 시 열리는 선택지 id 들.")]
        [SerializeField] private List<string> unlocksChoiceIds = new List<string>();

        [Tooltip("조합에 성공했을 때의 연출 대사.")]
        [SerializeField] private DialogueSequence onSuccess = new DialogueSequence();

        [Header("규칙")]
        [Tooltip("켜면 이 조합 없이는 범인 지목 페이즈로 넘어갈 수 없다.")]
        [SerializeField] private bool isRequiredForAccusation;

        [Tooltip("조합해도 재료가 사라지지 않게 할지. 보통 켜 둔다.")]
        [SerializeField] private bool keepIngredients = true;

        public IReadOnlyList<ClueDefinition> Ingredients => ingredients;
        public ClueDefinition Result => result;
        public IReadOnlyList<string> UnlocksChoiceIds => unlocksChoiceIds;
        public DialogueSequence OnSuccess => onSuccess;
        public bool IsRequiredForAccusation => isRequiredForAccusation;
        public bool KeepIngredients => keepIngredients;

        /// <summary>주어진 단서 두 개가 이 조합의 재료와 정확히 일치하는가.</summary>
        public bool Matches(string clueIdA, string clueIdB)
        {
            if (ingredients == null || ingredients.Count != 2) return false;
            var a = ingredients[0] != null ? ingredients[0].Id : null;
            var b = ingredients[1] != null ? ingredients[1].Id : null;
            return (a == clueIdA && b == clueIdB) || (a == clueIdB && b == clueIdA);
        }

        /// <summary>보유 단서만으로 이 조합이 성립하는가.</summary>
        public bool CanCraftWith(ICollection<string> ownedClueIds)
        {
            if (ingredients == null || ingredients.Count == 0 || ownedClueIds == null) return false;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == null) return false;
                if (!ownedClueIds.Contains(ingredients[i].Id)) return false;
            }
            return true;
        }
    }
}
