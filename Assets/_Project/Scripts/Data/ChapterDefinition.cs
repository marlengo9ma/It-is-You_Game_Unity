using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 챕터 하나의 구성과 규칙 수치. 기획서의 임계값들이 코드가 아니라
    /// 여기에 모여 있어서, 밸런스 조정 때 스크립트를 건드리지 않아도 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Chapter", fileName = "Chapter_")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Tooltip("켜면 튜토리얼 규칙이 적용된다. 신뢰도 페널티가 붙어도 심문이 끝나지 않고, " +
                 "대신 관련 설명 팝업이 뜬다.")]
        [SerializeField] private bool isTutorial;

        [Header("인트로")]
        [SerializeField] private DialogueSequence intro = new DialogueSequence();

        [Header("단서 수집 페이즈")]
        [SerializeField] private List<InteractableDefinition> sceneObjects = new List<InteractableDefinition>();

        [Tooltip("이 비율 이상 모으면 경고 없이 진행한다. 기획 기준 1.0")]
        [Range(0f, 1f)]
        [SerializeField] private float passThreshold = 1.0f;

        [Tooltip("이 비율 이상이면 경고 팝업 후 플레이어가 진행 여부를 고른다. 기획 기준 0.7")]
        [Range(0f, 1f)]
        [SerializeField] private float warnThreshold = 0.7f;

        [Tooltip("수집률 100% 일 때의 대사.")]
        [SerializeField] private DialogueSequence gatePass = new DialogueSequence();

        [Tooltip("경고 구간일 때의 대사.")]
        [SerializeField] private DialogueSequence gateWarn = new DialogueSequence();

        [Tooltip("진행 불가일 때의 대사.")]
        [SerializeField] private DialogueSequence gateBlocked = new DialogueSequence();

        [Header("심문")]
        [Tooltip("1차 심문에서 상대할 대상. 튜토리얼에서는 조수와 고양이.")]
        [SerializeField] private List<SuspectDefinition> firstRoundSuspects = new List<SuspectDefinition>();

        [Tooltip("2차 심문에서 상대할 대상. 비어 있으면 1차와 동일하게 쓴다.")]
        [SerializeField] private List<SuspectDefinition> secondRoundSuspects = new List<SuspectDefinition>();

        [Header("페널티 수치")]
        [Tooltip("강압 선택지를 몇 번 누르면 경고가 뜨는지. 기획 기준 3")]
        [SerializeField] private int aggressionWarnAt = 3;

        [Tooltip("강압 선택지를 몇 번 누르면 심문 거부인지. 기획 기준 4")]
        [SerializeField] private int aggressionRefuseAt = 4;

        [Tooltip("심문 오브젝트만 몇 번 연속 고르면 경고가 뜨는지. 기획 기준 3")]
        [SerializeField] private int objectSpamWarnAt = 3;

        [Tooltip("심문 오브젝트만 몇 번 연속 고르면 심문 거부인지. 기획 기준 4")]
        [SerializeField] private int objectSpamRefuseAt = 4;

        [Tooltip("친근/강압 교차를 몇 번 하면 경고인지. 기획 기준 1")]
        [SerializeField] private int toneAlternationWarnAt = 1;

        [Tooltip("친근/강압 교차를 몇 번 하면 게임오버인지. 기획 기준 2")]
        [SerializeField] private int toneAlternationGameOverAt = 2;

        [Tooltip("무반응으로 판정하기까지의 실제 시간(초). 기획 기준 30")]
        [SerializeField] private float idleSeconds = 30f;

        [Tooltip("기억 짐작에서 조수의 도움을 쓸 때 드는 추가 타임 토큰.")]
        [SerializeField] private int assistRecallTokenCost = 1;

        [Tooltip("선택지 대사가 전부 끝난 뒤, 무반응 타이머가 다시 돌기까지의 유예 시간(초).")]
        [SerializeField] private float idleGraceAfterDialogueSeconds = 5f;

        [Header("조합 / 지목")]
        [SerializeField] private List<CombinationRecipe> recipes = new List<CombinationRecipe>();

        [Tooltip("범인 지목 대상. 보통 firstRoundSuspects 와 같다.")]
        [SerializeField] private List<SuspectDefinition> accusationTargets = new List<SuspectDefinition>();

        [Tooltip("기획상 '정답'에 해당하는 용의자 id. 비워 두면 정답 판정을 하지 않는다.")]
        [SerializeField] private string cannonicalCulpritId;

        // ---- 접근자 ----
        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public bool IsTutorial => isTutorial;

        public DialogueSequence Intro => intro;
        public IReadOnlyList<InteractableDefinition> SceneObjects => sceneObjects;
        public float PassThreshold => passThreshold;
        public float WarnThreshold => warnThreshold;
        public DialogueSequence GatePass => gatePass;
        public DialogueSequence GateWarn => gateWarn;
        public DialogueSequence GateBlocked => gateBlocked;

        public IReadOnlyList<SuspectDefinition> FirstRoundSuspects => firstRoundSuspects;
        /// <summary>
        /// 2차 심문 대상. 비워 두면 2차 심문이 없는 챕터라는 뜻이다.
        /// 1차와 같은 인물을 다시 만나려면 명시적으로 다시 넣는다 — 암묵적 반복은 사고를 부른다.
        /// </summary>
        public IReadOnlyList<SuspectDefinition> SecondRoundSuspects => secondRoundSuspects;

        public int AggressionWarnAt => aggressionWarnAt;
        public int AggressionRefuseAt => aggressionRefuseAt;
        public int ObjectSpamWarnAt => objectSpamWarnAt;
        public int ObjectSpamRefuseAt => objectSpamRefuseAt;
        public int ToneAlternationWarnAt => toneAlternationWarnAt;
        public int ToneAlternationGameOverAt => toneAlternationGameOverAt;
        public float IdleSeconds => idleSeconds;
        public int AssistRecallTokenCost => Mathf.Max(0, assistRecallTokenCost);
        public float IdleGraceAfterDialogueSeconds => Mathf.Max(0f, idleGraceAfterDialogueSeconds);

        public IReadOnlyList<CombinationRecipe> Recipes => recipes;
        public IReadOnlyList<SuspectDefinition> AccusationTargets =>
            (accusationTargets != null && accusationTargets.Count > 0) ? accusationTargets : firstRoundSuspects;
        public string CannonicalCulpritId => cannonicalCulpritId;

        /// <summary>수집률 계산에 포함되는 단서 전체.</summary>
        public IEnumerable<ClueDefinition> EnumerateCountedClues()
        {
            if (sceneObjects == null) yield break;
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var obj = sceneObjects[i];
                if (obj == null || !obj.GrantsClue) continue;
                var clue = obj.GrantedClue;
                if (clue.CountsTowardCompletion) yield return clue;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
            if (warnThreshold > passThreshold) warnThreshold = passThreshold;
        }
#endif
    }
}
