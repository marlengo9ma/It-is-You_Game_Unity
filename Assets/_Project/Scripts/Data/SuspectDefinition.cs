using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 심문 대상 하나. 튜토리얼의 조수와 검은 고양이도 여기에 해당한다.
    /// 용의자마다 타임 토큰이 따로 할당되고, 페널티 반응 대사도 각자 다르다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Suspect", fileName = "Suspect_")]
    public class SuspectDefinition : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private SpeakerId speaker = SpeakerId.SuspectA;

        [Header("인물 정보")]
        [Tooltip("가방 화면에서 보이는 간략한 인적 사항.")]
        [TextArea(2, 6)]
        [SerializeField] private string profile;

        [SerializeField] private Sprite portrait;

        [Header("심문")]
        [Tooltip("이 용의자에게 할당된 타임 토큰 수.")]
        [SerializeField] private int timeTokens = 8;

        [Tooltip("이 용의자에게 쓸 수 있는 메인 선택지 전체.")]
        [SerializeField] private List<ChoiceDefinition> choices = new List<ChoiceDefinition>();

        [Tooltip("심문 화면 안에서 클릭할 수 있는 심문 오브젝트.")]
        [SerializeField] private List<InteractableDefinition> interrogationObjects = new List<InteractableDefinition>();

        [Header("반응 대사")]
        [Tooltip("강압 선택지 누적 경고 시점에 나오는 대사.")]
        [SerializeField] private DialogueSequence aggressionWarning = new DialogueSequence();

        [Tooltip("강압 누적으로 공포/실신해 심문을 거부할 때의 대사.")]
        [SerializeField] private DialogueSequence refusalByFear = new DialogueSequence();

        [Tooltip("심문 오브젝트만 반복 선택했을 때의 경고 대사.")]
        [SerializeField] private DialogueSequence objectSpamWarning = new DialogueSequence();

        [Tooltip("심문 오브젝트 반복으로 분노해 심문을 거부할 때의 대사.")]
        [SerializeField] private DialogueSequence refusalByAnger = new DialogueSequence();

        [Tooltip("친근/강압 첫 교차 선택 시의 의문·당황 반응.")]
        [SerializeField] private DialogueSequence toneAlternationWarning = new DialogueSequence();

        [Tooltip("두 번째 교차 선택으로 게임오버될 때의 분노·당혹 반응.")]
        [SerializeField] private DialogueSequence toneAlternationGameOver = new DialogueSequence();

        [Tooltip("타임 토큰을 모두 소진해 심문이 자동 종료될 때의 대사.")]
        [SerializeField] private DialogueSequence onTokensExhausted = new DialogueSequence();

        [Tooltip("30초 무반응 판정이 났을 때의 대사.")]
        [SerializeField] private DialogueSequence onIdle = new DialogueSequence();

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public SpeakerId Speaker => speaker;
        public string Profile => profile;
        public Sprite Portrait => portrait;
        public int TimeTokens => Mathf.Max(1, timeTokens);
        public IReadOnlyList<ChoiceDefinition> Choices => choices;
        public IReadOnlyList<InteractableDefinition> InterrogationObjects => interrogationObjects;

        public DialogueSequence AggressionWarning => aggressionWarning;
        public DialogueSequence RefusalByFear => refusalByFear;
        public DialogueSequence ObjectSpamWarning => objectSpamWarning;
        public DialogueSequence RefusalByAnger => refusalByAnger;
        public DialogueSequence ToneAlternationWarning => toneAlternationWarning;
        public DialogueSequence ToneAlternationGameOver => toneAlternationGameOver;
        public DialogueSequence OnTokensExhausted => onTokensExhausted;
        public DialogueSequence OnIdle => onIdle;

        public ChoiceDefinition FindChoice(string choiceId)
        {
            if (choices == null || string.IsNullOrEmpty(choiceId)) return null;
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i] != null && choices[i].Id == choiceId) return choices[i];
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
