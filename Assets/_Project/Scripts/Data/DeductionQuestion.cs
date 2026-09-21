using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 단서 조합 페이즈의 문항 하나 — '조수 문답' 방식.
    ///
    /// 조수가 묻고, 플레이어는 수집한 단서 아이콘 중 하나(또는 정해진 개수)로 답한다.
    /// 맞으면 <see cref="resultClue"/> 가 '추론' 단서로 목록에 들어가고, 2차 심문의 단서 제시가 그 카드에 열린다.
    /// 틀리면 회차별 반응 대사와 함께 잉크가 한 방울 마른다.
    ///
    /// 개선안 §II-A. 후보는 아이콘이고 텍스트는 조수의 질문 한 줄뿐 — 읽는 추리가 아니라 고르는 추리.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Deduction Question", fileName = "Deduction_")]
    public class DeductionQuestion : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string id;

        [Header("질문")]
        [Tooltip("조수가 던지는 질문. 대사창에 나온 뒤 후보가 열린다.")]
        [SerializeField] private DialogueSequence prompt = new DialogueSequence();

        [Header("후보와 정답")]
        [Tooltip("좌측에 아이콘으로 나열될 단서 id. 플레이어가 실제로 가진 것만 보인다 — 놓친 단서는 후보에도 없다.")]
        [SerializeField] private List<string> candidateClueIds = new List<string>();

        [Tooltip("정답 단서 id. 두 개 이상이면 이 순서대로 골라야 한다.")]
        [SerializeField] private List<string> answerClueIds = new List<string>();

        [Header("결과")]
        [Tooltip("맞혔을 때 목록에 추가되는 '추론' 단서.")]
        [SerializeField] private ClueDefinition resultClue;

        [Tooltip("맞혔을 때의 대사. 추론 카드가 목록에 들어간 직후 나온다.")]
        [SerializeField] private DialogueSequence onCorrect = new DialogueSequence();

        [Tooltip("틀렸을 때의 대사. 1·2·3회차 순서. 다 쓰면 마지막 것을 반복한다.")]
        [SerializeField] private List<DialogueSequence> wrongReactions = new List<DialogueSequence>();

        [Tooltip("가이드 모드에서 오답 뒤 조수가 후보 하나를 지워 줄 때의 대사.")]
        [SerializeField] private DialogueSequence guideHint = new DialogueSequence();

        [Header("규칙")]
        [Tooltip("켜면 이 추론 없이는 챕터를 끝낼 수 없다. (잉크가 다 마르면 자기 비하 게임오버)")]
        [SerializeField] private bool requiredForAccusation = true;

        public string Id => id;
        public DialogueSequence Prompt => prompt;
        public IReadOnlyList<string> CandidateClueIds => candidateClueIds;
        public IReadOnlyList<string> AnswerClueIds => answerClueIds;
        public ClueDefinition ResultClue => resultClue;
        public DialogueSequence OnCorrect => onCorrect;
        public DialogueSequence GuideHint => guideHint;
        public bool RequiredForAccusation => requiredForAccusation;

        public int AnswerCount => answerClueIds != null ? answerClueIds.Count : 0;

        public bool IsAnswer(string clueId)
            => !string.IsNullOrEmpty(clueId) && answerClueIds != null && answerClueIds.Contains(clueId);

        /// <summary>고른 순서가 정답 순서와 정확히 같은가.</summary>
        public bool IsCorrect(IReadOnlyList<string> picked)
        {
            if (picked == null || answerClueIds == null || picked.Count != answerClueIds.Count) return false;
            for (int i = 0; i < picked.Count; i++)
            {
                if (picked[i] != answerClueIds[i]) return false;
            }
            return true;
        }

        /// <summary><paramref name="round"/> 회차(1부터)의 오답 반응.</summary>
        public DialogueSequence GetWrongReaction(int round)
        {
            if (wrongReactions == null || wrongReactions.Count == 0) return null;
            var index = Mathf.Clamp(round - 1, 0, wrongReactions.Count - 1);
            return wrongReactions[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
