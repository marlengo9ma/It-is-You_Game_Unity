using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 지목 페이즈의 〈추론〉 선택지 하나.
    /// 고르면 주인공의 추론 대사와 주변 반응이 나오고, 지목당한 쪽이 억울함을 호소하면(plea)
    /// 주인공이 단서를 골라 반박한다. 맞는 단서를 고를 때까지 반복(2026-09-23 확정).
    /// </summary>
    [Serializable]
    public class AccusationOption
    {
        [Tooltip("선택지에 보이는 문장.")]
        [SerializeField] private string label;

        [Tooltip("고른 뒤 — 주인공의 추론 대사와 주변 인물의 반응.")]
        [SerializeField] private DialogueSequence speech = new DialogueSequence();

        [Tooltip("지목당한 용의자의 억울함 호소·변명. 비어 있으면 반박 없이 결과로 간다.")]
        [SerializeField] private DialogueSequence plea = new DialogueSequence();

        [Tooltip("반박에 통하는 단서 id (물품·정황). 하나라도 맞으면 성공.")]
        [SerializeField] private List<string> refuteClueIds = new List<string>();

        [Tooltip("엉뚱한 단서로 반박했을 때 — 용의자가 되받아친다. 그 뒤 다시 고른다.")]
        [SerializeField] private DialogueSequence refuteWrong = new DialogueSequence();

        [Tooltip("맞는 단서로 반박했을 때.")]
        [SerializeField] private DialogueSequence refuteSuccess = new DialogueSequence();

        [Tooltip("최종 결과의 제목. 엔딩 자리에 뜬다. 예: 세피의 자백")]
        [SerializeField] private string resultTitle;

        [Tooltip("최종 결과 대사. 이 뒤에 엔딩 컷씬 자리.")]
        [SerializeField] private DialogueSequence result = new DialogueSequence();

        [Tooltip("켜면 결과로 가지 않고 가설·용의자 선택으로 되돌아간다 (\"…아직 확신이 없다\").")]
        [SerializeField] private bool returnsToSelection;

        public string Label => label;
        public DialogueSequence Speech => speech;
        public DialogueSequence Plea => plea;
        public bool HasPlea => plea != null && !plea.IsEmpty;
        public IReadOnlyList<string> RefuteClueIds => refuteClueIds;
        public DialogueSequence RefuteWrong => refuteWrong;
        public DialogueSequence RefuteSuccess => refuteSuccess;
        public string ResultTitle => resultTitle;
        public DialogueSequence Result => result;
        public bool ReturnsToSelection => returnsToSelection;

        public bool Refutes(string clueId) => !string.IsNullOrEmpty(clueId) && refuteClueIds != null && refuteClueIds.Contains(clueId);
    }

    /// <summary>
    /// 〈가설〉 + 〈용의자〉 한 쌍에 딸린 추론 선택지 두 개. 챕터가 쌍마다 하나씩 갖는다.
    /// 맞는 쌍이 없으면 챕터의 기본 갈래(defaultAccusationBranch)를 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Accusation Branch", fileName = "Acc_")]
    public class AccusationBranch : ScriptableObject
    {
        [SerializeField] private string id;

        [Tooltip("가설(추론 단서) id. 기본 갈래는 비워 둔다.")]
        [SerializeField] private string hypothesisClueId;

        [Tooltip("용의자 id. 기본 갈래는 비워 둔다.")]
        [SerializeField] private string suspectId;

        [Tooltip("추론 선택지. 기획: 2가지.")]
        [SerializeField] private List<AccusationOption> options = new List<AccusationOption>();

        public string Id => id;
        public string HypothesisClueId => hypothesisClueId;
        public string SuspectId => suspectId;
        public IReadOnlyList<AccusationOption> Options => options;

        public bool Matches(string hypothesisId, string suspect)
            => hypothesisClueId == hypothesisId && suspectId == suspect;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
