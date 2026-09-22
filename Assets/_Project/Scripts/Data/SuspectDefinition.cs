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

        [Header("2차 심문")]
        [Tooltip("2차 심문에서 쓸 선택지. 비어 있으면 1차와 같은 목록을 쓴다.")]
        [SerializeField] private List<ChoiceDefinition> secondRoundChoices = new List<ChoiceDefinition>();

        [Tooltip("2차 심문의 타임 토큰. 0이면 1차와 같다. 개선안: 1차의 절반 — 짧고 세게.")]
        [SerializeField] private int secondRoundTokens = 0;

        [Tooltip("2차 심문에 들어갈 때의 첫 마디. 비어 있으면 1차의 것을 쓴다.")]
        [SerializeField] private DialogueSequence onEnterSecondInterrogation = new DialogueSequence();

        [Tooltip("범인으로 지목했을 때 주인공이 '왜 이 사람인가'를 말하는 대사. 엔딩 컷씬 앞에 나온다.")]
        [SerializeField] private DialogueSequence accusationSpeech = new DialogueSequence();

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

        [Tooltip("심문에 막 들어갔을 때 이 인물이 먼저 꺼내는 말. 이 대사가 끝나야 선택지가 열린다.")]
        [SerializeField] private DialogueSequence onEnterInterrogation = new DialogueSequence();

        [Tooltip("플레이어가 '심문 종료'를 눌러 수동으로 끝낼 때의 대사. 다음 인물로 넘어가는 다리.")]
        [SerializeField] private DialogueSequence onEndInterrogation = new DialogueSequence();

        [Tooltip("무반응 판정 1회차 대사. 목록이 비어 있으면 아래 onIdle 을 쓴다.")]
        [SerializeField] private List<DialogueSequence> idleRounds = new List<DialogueSequence>();

        [Tooltip("회차별 대사가 동나면 쓰는 기본 무반응 대사.")]
        [SerializeField] private DialogueSequence onIdle = new DialogueSequence();

        [Header("용의자 선택 화면")]
        [Tooltip("이 인물을 플레이어가 고를 수 있는가. 튜토리얼에서는 고양이를 잠가 둔다.")]
        [SerializeField] private bool selectable = true;

        [Tooltip("고를 수 없을 때 카드에 표시할 사유.")]
        [SerializeField] private string lockedReason = "아직은 아니다.";

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public SpeakerId Speaker => speaker;
        public string Profile => profile;
        public Sprite Portrait => portrait;
        public int TimeTokens => Mathf.Max(1, timeTokens);
        public IReadOnlyList<ChoiceDefinition> Choices => choices;
        public IReadOnlyList<InteractableDefinition> InterrogationObjects => interrogationObjects;

        public IReadOnlyList<ChoiceDefinition> ChoicesForRound(int round)
            => round >= 2 && secondRoundChoices != null && secondRoundChoices.Count > 0 ? secondRoundChoices : choices;

        public int TokensForRound(int round)
            => round >= 2 && secondRoundTokens > 0 ? secondRoundTokens : TimeTokens;

        public DialogueSequence OpeningForRound(int round)
            => round >= 2 && onEnterSecondInterrogation != null && !onEnterSecondInterrogation.IsEmpty
                ? onEnterSecondInterrogation : onEnterInterrogation;

        public DialogueSequence AggressionWarning => aggressionWarning;
        public DialogueSequence RefusalByFear => refusalByFear;
        public DialogueSequence ObjectSpamWarning => objectSpamWarning;
        public DialogueSequence RefusalByAnger => refusalByAnger;
        public DialogueSequence ToneAlternationWarning => toneAlternationWarning;
        public DialogueSequence ToneAlternationGameOver => toneAlternationGameOver;
        public DialogueSequence OnTokensExhausted => onTokensExhausted;
        public DialogueSequence OnEnterInterrogation => onEnterInterrogation;
        public DialogueSequence OnEndInterrogation => onEndInterrogation;
        public DialogueSequence OnIdle => onIdle;
        public bool Selectable => selectable;
        public DialogueSequence AccusationSpeech => accusationSpeech;
        public string LockedReason => lockedReason;

        /// <summary>회차별 무반응 대사가 몇 벌 있는가. 다 쓰고 나면 페널티 설명 팝업의 트리거가 된다.</summary>
        public int IdleRoundCount => idleRounds != null ? idleRounds.Count : 0;

        /// <summary>
        /// 무반응 판정 <paramref name="round"/> 회차(1부터)의 대사.
        ///
        /// 회차마다 반응이 달라야 "기다리는 쪽도 불편해지고 있다"가 읽힌다.
        /// 같은 줄이 계속 반복되면 침묵에 값이 붙지 않는다.
        /// 목록을 다 쓰면 마지막 줄을 계속 쓴다.
        /// </summary>
        public DialogueSequence GetIdle(int round)
        {
            if (idleRounds == null || idleRounds.Count == 0) return onIdle;
            var index = Mathf.Clamp(round - 1, 0, idleRounds.Count - 1);
            var seq = idleRounds[index];
            return (seq != null && !seq.IsEmpty) ? seq : onIdle;
        }

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
