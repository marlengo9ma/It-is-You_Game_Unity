using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 선택지를 한 번 고른 분량. 기획서의 "같은 선택지를 중복하여 여러 번 선택이 가능하며,
    /// 경우에 따라 다른 내용의 심문을 이어갈 수 있다"를 한 칸으로 표현한 것이다.
    ///
    /// 회차마다 바뀌는 것이 대사만은 아니다. 그 회차에서만 쓰는 문구, 그 회차 뒤에만 붙는
    /// &lt;시간 경과&gt; 대사, 그 회차에서만 재생하는 연출이 각각 따로 있을 수 있다.
    /// 그래서 한 칸 안에 넷을 같이 둔다 — 셋을 따로 나열하면 회차가 어긋나는 순간
    /// 어느 대사에 어느 연출이 붙는지 아무도 모르게 된다.
    ///
    /// 앞서 무엇을 골랐느냐에 따라 같은 회차가 통째로 다른 내용이 되기도 한다.
    /// 시나리오의 "❕B선택지 이후, 선택 시" 가 그것이고, 그 자리가 <see cref="VariantOfRound"/> 다.
    /// </summary>
    [Serializable]
    public class ChoicePass
    {
        [Tooltip("에디터에서 알아보기 위한 메모. 게임에는 쓰이지 않는다.")]
        [SerializeField] private string note;

        [Tooltip("이 회차에서 목록에 뜨는 문구. 비우면 선택지의 기본 문구를 쓴다. " +
                 "회차마다 주인공이 다른 말을 하는 스크립트에서는, 고른 문장과 실제로 하는 말이 어긋나지 않도록 여기에 그 줄을 적는다.")]
        [TextArea(1, 3)]
        [SerializeField] private string labelOverride;

        [SerializeField] private DialogueSequence sequence = new DialogueSequence();

        [Tooltip("이 회차를 고른 뒤 처음 오는 무반응 판정에서 나오는 <시간 경과> 대사. " +
                 "비어 있으면 선택지의 기본값을, 그것도 없으면 용의자의 일반 무반응 회차 대사를 쓴다.")]
        [SerializeField] private DialogueSequence idleFollowUp = new DialogueSequence();

        [Tooltip("이 묶음이 시작될 때 캐릭터에 거는 연출 이름. 줄 하나에만 걸고 싶으면 대사 줄 쪽의 연출 칸을 쓴다.")]
        [SerializeField] private string animationId;

        [Header("조건부 변형")]
        [Tooltip("-1 이면 순서대로 쌓이는 보통 회차. 0 이상이면 '그 회차의 변형'이며, " +
                 "아래 조건을 만족할 때 같은 회차의 보통 칸을 대신한다.")]
        [SerializeField] private int variantOfRound = -1;

        [Tooltip("이 선택지들을 한 번이라도 먼저 골랐어야 이 변형이 쓰인다.")]
        [SerializeField] private List<string> requiresPriorChoiceIds = new List<string>();

        [Tooltip("이 선택지들을 한 번이라도 골랐다면 이 변형을 쓰지 않는다.")]
        [SerializeField] private List<string> blockedByChoiceIds = new List<string>();

        public string Note => note;
        public string LabelOverride => labelOverride;
        public bool HasLabelOverride => !string.IsNullOrEmpty(labelOverride);
        public DialogueSequence Sequence => sequence;
        public DialogueSequence IdleFollowUp => idleFollowUp;
        public bool HasIdleFollowUp => idleFollowUp != null && !idleFollowUp.IsEmpty;
        public string AnimationId => animationId;

        /// <summary>순서대로 쌓이는 보통 회차가 아니라, 특정 회차를 대신하는 변형인가.</summary>
        public bool IsVariant => variantOfRound >= 0;
        public int VariantOfRound => variantOfRound;

        public IReadOnlyList<string> RequiresPriorChoiceIds => requiresPriorChoiceIds;
        public IReadOnlyList<string> BlockedByChoiceIds => blockedByChoiceIds;

        public ChoicePass() { }

        public ChoicePass(DialogueSequence sequence, string note = null)
        {
            this.sequence = sequence ?? new DialogueSequence();
            this.note = note;
        }

        /// <summary>앞서 무엇을 몇 번 골랐는지 보고, 이 변형을 지금 쓸 수 있는가.</summary>
        public bool Matches(Func<string, int> useCount)
        {
            if (requiresPriorChoiceIds != null && requiresPriorChoiceIds.Count > 0)
            {
                if (useCount == null) return false;
                for (int i = 0; i < requiresPriorChoiceIds.Count; i++)
                    if (useCount(requiresPriorChoiceIds[i]) <= 0) return false;
            }

            if (useCount != null && blockedByChoiceIds != null)
            {
                for (int i = 0; i < blockedByChoiceIds.Count; i++)
                    if (useCount(blockedByChoiceIds[i]) > 0) return false;
            }

            return true;
        }
    }
}
