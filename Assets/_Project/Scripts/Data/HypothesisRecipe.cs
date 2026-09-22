using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 〈가설〉 하나의 조합식. 단서 수집에서 얻은 물품 단서 하나 + 심문에서 얻은 정황 단서 하나가
    /// 이어지면 결과 단서(추론, Kind = Deduced)가 생긴다.
    ///
    /// 2026-09-23 기획: "단서(단서 수집 페이즈에서 얻은)와 정황 단서(심문 페이즈에서 얻은)를 플레이어가
    /// 하나씩 선택하여 조합하는 〈가설〉 도출 시도". 관계없는 두 단서면 실패하고 잉크를 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Hypothesis Recipe", fileName = "Hyp_")]
    public class HypothesisRecipe : ScriptableObject
    {
        [SerializeField] private string id;

        [Tooltip("가설의 이름. 우측 〈가설〉 칸과 성공 연출에 쓴다.")]
        [SerializeField] private string title;

        [Tooltip("물품 단서(수집 페이즈) id.")]
        [SerializeField] private string objectClueId;

        [Tooltip("정황 단서(심문의 증언·리액션) id.")]
        [SerializeField] private string leadClueId;

        [Tooltip("성공하면 얻는 추론 단서. Kind 는 Deduced.")]
        [SerializeField] private ClueDefinition result;

        [Tooltip("성공했을 때 주인공의 말.")]
        [SerializeField] private DialogueSequence onSuccess = new DialogueSequence();

        public string Id => id;
        public string Title => string.IsNullOrEmpty(title) ? (result != null ? result.ReadName : id) : title;
        public string ObjectClueId => objectClueId;
        public string LeadClueId => leadClueId;
        public ClueDefinition Result => result;
        public DialogueSequence OnSuccess => onSuccess;

        /// <summary>두 단서가 이 조합식과 맞는가. 순서는 상관없다.</summary>
        public bool Matches(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return (a == objectClueId && b == leadClueId) || (a == leadClueId && b == objectClueId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
