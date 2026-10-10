using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 회차를 고를 때 필요한 지금의 상황. 세션이 채워서 넘긴다.
    /// </summary>
    public readonly struct ChoicePassContext
    {
        /// <summary>이 선택지를 지금까지 몇 번 골랐는가.</summary>
        public readonly int TimesUsed;

        /// <summary>
        /// 이번 선택이 몇 번째 친근↔강압 교차가 되는가. 교차가 아니면 0.
        /// 시나리오의 "B선택지 이후, 선택 시"(1번째)와 "A선택지 &gt; B선택지 이후, A선택지 재 선택 시"(2번째).
        /// </summary>
        public readonly int Alternation;

        /// <summary>다른 선택지를 몇 번 골랐는가.</summary>
        public readonly Func<string, int> UseCount;

        /// <summary>이 이름표의 &lt;시간 경과&gt; 대사가 실제로 화면에 나왔는가.</summary>
        public readonly Func<string, bool> IdleSeen;

        public ChoicePassContext(int timesUsed, int alternation, Func<string, int> useCount, Func<string, bool> idleSeen)
        {
            TimesUsed = timesUsed;
            Alternation = alternation;
            UseCount = useCount;
            IdleSeen = idleSeen;
        }

        public static ChoicePassContext ForRound(int timesUsed) => new ChoicePassContext(timesUsed, 0, null, null);
    }

    /// <summary>
    /// 선택지를 한 번 고른 분량. 기획서의 "같은 선택지를 중복하여 여러 번 선택이 가능하며,
    /// 경우에 따라 다른 내용의 심문을 이어갈 수 있다"를 한 칸으로 표현한 것이다.
    ///
    /// 회차마다 바뀌는 것이 대사만은 아니다. 그 회차에서만 쓰는 문구, 그 회차 뒤에만 붙는
    /// &lt;시간 경과&gt; 대사, 그 회차에서만 재생하는 연출이 각각 따로 있을 수 있다.
    /// 그래서 한 칸 안에 같이 둔다 — 따로 나열하면 회차가 어긋나는 순간
    /// 어느 대사에 어느 연출이 붙는지 아무도 모르게 된다.
    ///
    /// 칸은 세 종류다. 위에서부터 먼저 맞는 것이 쓰인다.
    /// <list type="number">
    /// <item><b>교차 칸</b>(<see cref="OnAlternation"/> &gt; 0) — 이번 선택이 N번째 친근↔강압 교차일 때.</item>
    /// <item><b>회차 변형</b>(<see cref="VariantOfRound"/> ≥ 0) — 그 회차에서 조건이 맞을 때 보통 칸을 대신한다.</item>
    /// <item><b>보통 회차</b> — 순서대로 쌓인다. 끝에 닿으면 마지막 칸이 반복된다.</item>
    /// </list>
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

        [Tooltip("위 <시간 경과> 대사의 이름표. 이 대사가 실제로 나왔는지를 다른 회차가 조건으로 볼 때 쓴다. " +
                 "시나리오의 <시간 경과_a> 같은 것.")]
        [SerializeField] private string idleFollowUpKey;

        [Tooltip("이 묶음이 시작될 때 캐릭터에 거는 연출 이름. 줄 하나에만 걸고 싶으면 대사 줄 쪽의 연출 칸을 쓴다.")]
        [SerializeField] private string animationId;

        [Tooltip("켜면 이 묶음이 끝나는 순간 신뢰를 잃어 심문이 강제로 끝난다. " +
                 "용의자의 '신뢰도 하락 강제 종료' 마무리 대사가 이어진다. (튜토리얼 챕터에서도 끝난다)")]
        [SerializeField] private bool breaksTrust;

        [Header("교차 칸")]
        [Tooltip("0 이면 해당 없음. 1 이상이면 이번 선택이 그 번째 친근↔강압 교차일 때 이 칸이 쓰인다. 회차 순서보다 먼저 본다.")]
        [SerializeField] private int onAlternation;

        [Header("회차 변형")]
        [Tooltip("-1 이면 순서대로 쌓이는 보통 회차. 0 이상이면 '그 회차의 변형'이며, " +
                 "아래 조건을 만족할 때 같은 회차의 보통 칸을 대신한다.")]
        [SerializeField] private int variantOfRound = -1;

        [Tooltip("이 선택지들을 한 번이라도 먼저 골랐어야 쓰인다.")]
        [SerializeField] private List<string> requiresPriorChoiceIds = new List<string>();

        [Tooltip("이 선택지들을 한 번이라도 골랐다면 쓰이지 않는다.")]
        [SerializeField] private List<string> blockedByChoiceIds = new List<string>();

        [Tooltip("이 이름표의 <시간 경과> 대사가 나왔어야 쓰인다.")]
        [SerializeField] private List<string> requiresSeenIdleKeys = new List<string>();

        [Tooltip("이 이름표의 <시간 경과> 대사가 나왔다면 쓰이지 않는다.")]
        [SerializeField] private List<string> blockedBySeenIdleKeys = new List<string>();

        public string Note => note;
        public string LabelOverride => labelOverride;
        public bool HasLabelOverride => !string.IsNullOrEmpty(labelOverride);
        public DialogueSequence Sequence => sequence;
        public DialogueSequence IdleFollowUp => idleFollowUp;
        public bool HasIdleFollowUp => idleFollowUp != null && !idleFollowUp.IsEmpty;
        public string IdleFollowUpKey => idleFollowUpKey;
        public string AnimationId => animationId;
        public bool BreaksTrust => breaksTrust;

        public int OnAlternation => onAlternation;
        public bool IsAlternation => onAlternation > 0;

        /// <summary>순서대로 쌓이는 보통 회차가 아니라, 특정 회차를 대신하는 변형인가.</summary>
        public bool IsVariant => variantOfRound >= 0;
        public int VariantOfRound => variantOfRound;

        /// <summary>순서에 끼는 보통 회차인가.</summary>
        public bool IsRound => !IsVariant && !IsAlternation;

        public IReadOnlyList<string> RequiresPriorChoiceIds => requiresPriorChoiceIds;
        public IReadOnlyList<string> BlockedByChoiceIds => blockedByChoiceIds;

        public ChoicePass() { }

        public ChoicePass(DialogueSequence sequence, string note = null)
        {
            this.sequence = sequence ?? new DialogueSequence();
            this.note = note;
        }

        /// <summary>앞서 무엇을 골랐고 무엇을 보았는지 보고, 이 칸을 지금 쓸 수 있는가.</summary>
        public bool Matches(in ChoicePassContext ctx)
        {
            if (!AllUsed(requiresPriorChoiceIds, ctx.UseCount)) return false;
            if (AnyUsed(blockedByChoiceIds, ctx.UseCount)) return false;
            if (!AllSeen(requiresSeenIdleKeys, ctx.IdleSeen)) return false;
            if (AnySeen(blockedBySeenIdleKeys, ctx.IdleSeen)) return false;
            return true;
        }

        private static bool AllUsed(List<string> ids, Func<string, int> useCount)
        {
            if (ids == null || ids.Count == 0) return true;
            if (useCount == null) return false;
            for (int i = 0; i < ids.Count; i++) if (useCount(ids[i]) <= 0) return false;
            return true;
        }

        private static bool AnyUsed(List<string> ids, Func<string, int> useCount)
        {
            if (ids == null || useCount == null) return false;
            for (int i = 0; i < ids.Count; i++) if (useCount(ids[i]) > 0) return true;
            return false;
        }

        private static bool AllSeen(List<string> keys, Func<string, bool> seen)
        {
            if (keys == null || keys.Count == 0) return true;
            if (seen == null) return false;
            for (int i = 0; i < keys.Count; i++) if (!seen(keys[i])) return false;
            return true;
        }

        private static bool AnySeen(List<string> keys, Func<string, bool> seen)
        {
            if (keys == null || seen == null) return false;
            for (int i = 0; i < keys.Count; i++) if (seen(keys[i])) return true;
            return false;
        }
    }
}
