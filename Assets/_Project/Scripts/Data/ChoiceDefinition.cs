using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 심문 메인 선택지 하나.
    ///
    /// 기획서의 세 가지 성질을 담는다.
    /// <list type="number">
    /// <item>단서 보유에 따라 선택지가 추가되거나 문구가 바뀐다.</item>
    /// <item>같은 선택지를 중복 선택할 수 있고, 경우에 따라 다른 내용이 이어진다.</item>
    /// <item>남은 내용이 있는 선택지에는 별도 아이콘이 표시된다.</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Choice", fileName = "Choice_")]
    public class ChoiceDefinition : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string id;
        [SerializeField] private ChoiceTone tone = ChoiceTone.Friendly;

        [Header("표기")]
        [Tooltip("선택지 목록에 뜨는 기본 문구.")]
        [TextArea(1, 3)]
        [SerializeField] private string label;

        [Tooltip("아래 '문구 교체 조건' 단서를 모두 가졌을 때 대신 쓰는 문구. 비어 있으면 교체하지 않는다.")]
        [TextArea(1, 3)]
        [SerializeField] private string labelWhenInformed;

        [Tooltip("문구 교체 조건이 되는 단서 id 들.")]
        [SerializeField] private List<string> labelSwapClueIds = new List<string>();

        [Header("등장 조건")]
        [Tooltip("켜면 심문 시작부터 목록에 있다. 끄면 아래 조건을 만족해야 나타난다.")]
        [SerializeField] private bool availableFromStart = true;

        [Tooltip("이 단서들을 모두 가져야 선택지가 나타난다.")]
        [SerializeField] private List<string> requiredClueIds = new List<string>();

        [Tooltip("이 수집품들을 목록에서 읽고 강조 구간을 눌러 '단서 오브젝트'로 만들어 두어야 선택지가 나타난다.")]
        [SerializeField] private List<string> requiredPromotedClueIds = new List<string>();

        [Tooltip("이 선택지 id 들을 먼저 선택해야 나타난다.")]
        [SerializeField] private List<string> requiredChoiceIds = new List<string>();

        [Tooltip("남은 타임 토큰이 이 값 이하일 때만 나타난다. 0이면 조건 없음. (시간의 흐름 트리거)")]
        [SerializeField] private int appearsWhenTokensAtOrBelow = 0;

        [Header("잠금 표시")]
        [Tooltip("켜면 등장 조건(단서)을 못 채웠을 때 숨기지 않고 잠긴 채로 보여 준다. 조합의 결과가 여기서 보이게 하는 장치.")]
        [SerializeField] private bool showLockedPlaceholder = false;

        [Tooltip("잠긴 채로 보일 때의 문구.")]
        [SerializeField] private string lockedLabel = "???";

        [Header("진행")]
        [Tooltip("첫 선택부터 순서대로 재생되는 대사 묶음. 끝에 도달한 뒤에는 마지막 묶음이 반복된다.")]
        [SerializeField] private List<DialogueSequence> passes = new List<DialogueSequence>();

        [Tooltip("이 선택지를 고른 뒤 처음 오는 무반응 판정에서, 회차 대사 대신 나오는 대사. 시나리오의 <시간 경과> 구간.")]
        [SerializeField] private DialogueSequence idleFollowUp = new DialogueSequence();

        [Tooltip("이 선택지가 소비하는 타임 토큰. 보통 1.")]
        [SerializeField] private int tokenCost = 1;

        [Tooltip("한 번 고르면 목록에서 사라진다.")]
        [SerializeField] private bool consumeOnUse = false;

        [Tooltip("선택 후 새로 열리는 선택지 id 들.")]
        [SerializeField] private List<string> unlocksChoiceIds = new List<string>();

        public string Id => id;
        public ChoiceTone Tone => tone;
        public int TokenCost => Mathf.Max(0, tokenCost);
        public bool ConsumeOnUse => consumeOnUse;
        public bool AvailableFromStart => availableFromStart;
        public IReadOnlyList<string> RequiredClueIds => requiredClueIds;
        public IReadOnlyList<string> RequiredPromotedClueIds => requiredPromotedClueIds;
        public IReadOnlyList<string> RequiredChoiceIds => requiredChoiceIds;
        public int AppearsWhenTokensAtOrBelow => appearsWhenTokensAtOrBelow;
        public IReadOnlyList<string> UnlocksChoiceIds => unlocksChoiceIds;
        public bool ShowLockedPlaceholder => showLockedPlaceholder;
        public string LockedLabel => string.IsNullOrEmpty(lockedLabel) ? "???" : lockedLabel;
        public IReadOnlyList<DialogueSequence> Passes => passes;
        public DialogueSequence IdleFollowUp => idleFollowUp;
        public bool HasIdleFollowUp => idleFollowUp != null && !idleFollowUp.IsEmpty;

        /// <summary>교차 선택 게임오버 판정에 포함되는 성향인가. 친근/강압만 해당한다.</summary>
        public bool CountsForToneAlternation => tone == ChoiceTone.Friendly || tone == ChoiceTone.Aggressive;

        /// <summary>보유 단서에 따라 달라지는 표시 문구.</summary>
        public string ResolveLabel(ICollection<string> ownedClueIds)
        {
            if (string.IsNullOrEmpty(labelWhenInformed) || labelSwapClueIds == null || labelSwapClueIds.Count == 0)
                return label;

            if (ownedClueIds == null) return label;

            for (int i = 0; i < labelSwapClueIds.Count; i++)
            {
                if (!ownedClueIds.Contains(labelSwapClueIds[i])) return label;
            }
            return labelWhenInformed;
        }

        /// <summary><paramref name="timesUsed"/> 번 선택된 뒤 재생할 대사 묶음.</summary>
        public DialogueSequence GetPass(int timesUsed)
        {
            if (passes == null || passes.Count == 0) return null;
            var index = Mathf.Clamp(timesUsed, 0, passes.Count - 1);
            return passes[index];
        }

        /// <summary>
        /// 아직 못 본 내용이 남아 있는가. 기획서의 "이미 선택했었던 메인 선택지 위로
        /// 별도 아이콘이 표시된다" 조건.
        /// </summary>
        public bool HasUnseenPass(int timesUsed) => passes != null && timesUsed < passes.Count;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
