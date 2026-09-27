using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 대사 한 줄. 기획서의 "대사 중 밑줄이 그어져있는 특정 단어나 문장 표현을 클릭하면
    /// [증언 및 정황 증거]로써 수집" 과 "특정 리액션이 나타났을 때 캐릭터를 클릭하면 수집"
    /// 두 가지 수집 경로를 모두 이 한 줄 안에서 표현한다.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        [SerializeField] private SpeakerId speaker = SpeakerId.Narration;

        [TextArea(2, 6)]
        [SerializeField]
        private string text;

        [Tooltip("이 줄을 말하는 동안 화면의 캐릭터가 지을 표정.")]
        [SerializeField] private ExpressionId expression = ExpressionId.Neutral;

        [Tooltip("괄호 안 지시문. 예: (눈을 피하며 땀이 삐질). 연출 참고용이며 화면에는 별도 스타일로 출력된다.")]
        [SerializeField] private string stageDirection;

        [Tooltip("이 줄이 나올 때 캐릭터에 거는 연출 이름. 표정만 바뀌는 것은 위 '표정' 칸이 맡는다. (StageAnimation 참고)")]
        [SerializeField] private string animationId;

        [Tooltip("이 줄이 출력되는 동안 캐릭터를 클릭해 수집할 수 있는 리액션. 비어 있으면 리액션 없음.")]
        [SerializeField] private ReactionCue reaction = new ReactionCue();

        [Tooltip("이 줄이 출력되면 조건 없이 바로 얻는 단서 id. 보통은 비워둔다.")]
        [SerializeField] private string autoGrantClueId;

        public SpeakerId Speaker => speaker;
        public string Text => text;
        public ExpressionId Expression => expression;
        public string StageDirection => stageDirection;
        public string AnimationId => animationId;
        public ReactionCue Reaction => reaction;
        public string AutoGrantClueId => autoGrantClueId;

        public bool HasReaction => reaction != null && reaction.IsValid;
        public bool HasAutoGrant => !string.IsNullOrEmpty(autoGrantClueId);

        public DialogueLine() { }

        public DialogueLine(SpeakerId speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }

        /// <summary>
        /// 본문에 심어진 <c>&lt;clue=ID&gt;...&lt;/clue&gt;</c> 태그에서 수집 가능한 단서 id 를 모두 뽑는다.
        /// </summary>
        public IEnumerable<string> EnumerateInlineClueIds()
        {
            return DialogueMarkup.ExtractClueIds(text);
        }
    }

    /// <summary>
    /// 대사 중 나타나는 리액션. 기획서: "놀라거나 동공이 떨리는 등, 일반적이지 않은 반응"이
    /// 나타났을 때 캐릭터 위로 마우스를 올리면 커서가 바뀌고, 클릭하면 정황 단서로 수집된다.
    /// </summary>
    [Serializable]
    public class ReactionCue
    {
        [Tooltip("이 리액션을 클릭해 수집했을 때 얻는 단서 id.")]
        [SerializeField] private string clueId;

        [Tooltip("줄이 출력되기 시작하고 몇 초 뒤부터 수집 창이 열리는지.")]
        [SerializeField] private float openDelay = 0f;

        [Tooltip("수집 창이 열려 있는 시간(초). 이 시간이 지나면 놓친 것으로 처리된다.")]
        [SerializeField] private float window = 2.5f;

        [Tooltip("리액션이 나타나는 동안의 표정. 대사 줄의 기본 표정을 덮어쓴다.")]
        [SerializeField] private ExpressionId expression = ExpressionId.Flustered;

        [Tooltip("기억 짐작에서 이 리액션이 몇 번째 회차부터 흐려져 수집 불가가 되는지. 0이면 항상 수집 가능.")]
        [SerializeField] private int blurredFromRecallDepth = 2;

        public string ClueId => clueId;
        public float OpenDelay => openDelay;
        public float Window => window;
        public ExpressionId Expression => expression;
        public int BlurredFromRecallDepth => blurredFromRecallDepth;

        public bool IsValid => !string.IsNullOrEmpty(clueId) && window > 0f;

        /// <summary>기억 짐작 회차 <paramref name="recallDepth"/> 에서 이 리액션을 아직 수집할 수 있는가.</summary>
        public bool IsCollectableAtRecallDepth(int recallDepth)
        {
            if (!IsValid) return false;
            if (recallDepth <= 0) return true;
            if (blurredFromRecallDepth <= 0) return true;
            return recallDepth < blurredFromRecallDepth;
        }
    }
}
