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

        [Header("사건 이전")]
        [Tooltip("[게임플레이 배경_캐릭터 씬] — 기차에 오른 뒤의 평범한 대화.")]
        [SerializeField] private DialogueSequence preIncidentTalk = new DialogueSequence();

        [Tooltip("[일러스트 컷씬#1] 의 컷 이름. 그림이 없는 동안 화면에 표시된다.")]
        [SerializeField] private string cutsceneName = "일러스트 컷씬";

        [Tooltip("컷씬의 그림. 비어 있으면 더미 판이 표시된다.")]
        [SerializeField] private Sprite cutsceneImage;

        [Tooltip("[일러스트 컷씬#1] — 주인공 독백.")]
        [SerializeField] private DialogueSequence cutscene = new DialogueSequence();

        [Tooltip("컷씬에서 돌아온 뒤의 대화. 다시 [게임플레이 배경_캐릭터 씬].")]
        [SerializeField] private DialogueSequence afterCutsceneTalk = new DialogueSequence();

        [Header("인트로 (사건 발생)")]
        [Tooltip("<화면 전환> 사건 진행 #1 부터의 대사.")]
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

        [Header("용의자 선택")]
        [Tooltip("단서 수집이 끝나고, 두 사람을 자리에 앉히기까지의 대사.")]
        [SerializeField] private DialogueSequence beforeSuspectSelect = new DialogueSequence();

        [Tooltip("용의자 선택 화면에서 먼저 오가는 대화.")]
        [SerializeField] private DialogueSequence suspectSelectTalk = new DialogueSequence();

        [Tooltip("대화가 끝난 뒤 선택을 재촉하는 한 줄.")]
        [SerializeField] private string suspectSelectPrompt = "누구부터 이야기를 들어볼까.";

        [Header("심문")]
        [Tooltip("1차 심문에서 상대할 대상. 튜토리얼에서는 조수와 고양이.")]
        [SerializeField] private List<SuspectDefinition> firstRoundSuspects = new List<SuspectDefinition>();

        [Tooltip("2차 심문에서 상대할 대상. 비어 있으면 1차와 동일하게 쓴다.")]
        [SerializeField] private List<SuspectDefinition> secondRoundSuspects = new List<SuspectDefinition>();

        [Tooltip("심문에 막 들어갈 때 화면 가운데 잠깐 뜨는 제목. {name} 이 용의자 이름으로 바뀐다. " +
                 "기획: <용의자 ‘세피’ 심문 시작> *텍스트 이미지 애니메이션 및 소리 연출")]
        [SerializeField] private string interrogationTitleFormat = "용의자 ‘{name}’ 심문 시작";

        [Header("튜토리얼 팝업")]
        [Tooltip("숫자마다 페이지가 나뉜 설명 팝업. 트리거마다 한 챕터에 한 번만 뜬다.")]
        [SerializeField] private List<TutorialPopupDefinition> tutorialPopups = new List<TutorialPopupDefinition>();

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

        [Tooltip("무반응으로 판정하기까지의 실제 시간(초). 기획 기준 15 (2026-09-22, 이전 30)")]
        [SerializeField] private float idleSeconds = 15f;

        [Tooltip("기억 짐작에서 조수의 도움을 쓸 때 드는 추가 타임 토큰.")]
        [SerializeField] private int assistRecallTokenCost = 1;

        [Tooltip("선택지 대사가 전부 끝난 뒤, 무반응 타이머가 다시 돌기까지의 유예 시간(초).")]
        [SerializeField] private float idleGraceAfterDialogueSeconds = 5f;

        [Header("화면 표기")]
        [Tooltip("화면 좌상단의 시계. 예: 10:24")]
        [SerializeField] private string sceneClock = "10:24";
        [Tooltip("시계 옆의 날짜 표기. 예: AM · 19--. 3. 19. Tue")]
        [SerializeField] private string sceneDate = "AM · 19--. 3. 19. Tue";
        [Tooltip("장소. 예: 대륙 횡단 열차")]
        [SerializeField] private string sceneLocation = "대륙 횡단 열차";

        [Header("단서 조합 — 가설")]
        [Tooltip("가설 조합식. 물품 단서 + 정황 단서 → 추론. 여기 없는 짝은 전부 실패다.")]
        [SerializeField] private List<HypothesisRecipe> hypotheses = new List<HypothesisRecipe>();

        [Tooltip("시도 자원 — 잉크. 실패마다 하나 마른다. 기획 2026-09-23: 기본 4")]
        [SerializeField] private int deductionInk = 4;

        [Tooltip("저장할 수 있는 가설의 최대 수. 기획: 3 (튜토리얼 2)")]
        [SerializeField] private int maxHypotheses = 3;

        [Tooltip("실패 회차별 반응. 1회, 2회, 3회 이상… 부족하면 마지막 것을 되풀이한다.")]
        [SerializeField] private List<DialogueSequence> deductionFailures = new List<DialogueSequence>();

        [Tooltip("같은 가설을 또 만들려 했을 때.")]
        [SerializeField] private DialogueSequence deductionDuplicate = new DialogueSequence();

        [Tooltip("가설이 가득 찬 채 새 가설이 생겼을 때 — 지울 것을 고르라는 말.")]
        [SerializeField] private DialogueSequence deductionReplacePrompt = new DialogueSequence();

        [Tooltip("잉크가 다 마르고 가설이 하나라도 있어 페이즈가 저절로 끝날 때.")]
        [SerializeField] private DialogueSequence deductionInkOut = new DialogueSequence();

        [Header("2차 심문")]
        [Tooltip("2차 심문에서 전체 용의자가 나눠 쓰는 타임 토큰. 기획: 6")]
        [SerializeField] private int secondRoundSharedTokens = 6;

        [Tooltip("2차 심문에서 누구를 심문할지 고르라는 한 줄.")]
        [SerializeField] private string secondRoundSelectPrompt = "가설을 들고 누구에게 갈까.";

        [Tooltip("2차 심문의 토큰이 다 떨어졌을 때, 지목으로 넘어가기 전의 대사.")]
        [SerializeField] private DialogueSequence beforeAccusation = new DialogueSequence();

        [Tooltip("범인 지목 화면의 한 줄.")]
        [SerializeField] private string accusationPrompt = "누가 가져갔다고 보십니까?";

        [Tooltip("조합 페이즈에 들어갈 때의 대사.")]
        [SerializeField] private DialogueSequence deductionIntro = new DialogueSequence();

        [Tooltip("필요한 추론을 다 만들었을 때의 대사. 이 뒤에 2차 심문으로 넘어간다.")]
        [SerializeField] private DialogueSequence deductionOutro = new DialogueSequence();

        [Tooltip("잉크가 다 마른 채 또 틀렸을 때 — 자기 비하 게임오버 직전의 독백.")]
        [SerializeField] private DialogueSequence selfLoathingEnding = new DialogueSequence();

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

        public string SceneClock => sceneClock;
        public string SceneDate => sceneDate;
        public string SceneLocation => sceneLocation;

        public IReadOnlyList<HypothesisRecipe> Hypotheses => hypotheses;
        public int DeductionInk => Mathf.Max(1, deductionInk);
        public int MaxHypotheses => Mathf.Max(1, maxHypotheses);
        public IReadOnlyList<DialogueSequence> DeductionFailures => deductionFailures;
        public DialogueSequence DeductionDuplicate => deductionDuplicate;
        public DialogueSequence DeductionReplacePrompt => deductionReplacePrompt;
        public DialogueSequence DeductionInkOut => deductionInkOut;
        public DialogueSequence DeductionIntro => deductionIntro;
        public DialogueSequence DeductionOutro => deductionOutro;
        public DialogueSequence SelfLoathingEnding => selfLoathingEnding;

        public int SecondRoundSharedTokens => Mathf.Max(1, secondRoundSharedTokens);
        public string SecondRoundSelectPrompt => secondRoundSelectPrompt;
        public DialogueSequence BeforeAccusation => beforeAccusation;
        public string AccusationPrompt => accusationPrompt;

        /// <summary>실패 회차(1부터)에 맞는 반응. 목록이 짧으면 마지막 것을 쓴다.</summary>
        public DialogueSequence GetDeductionFailure(int round)
        {
            if (deductionFailures == null || deductionFailures.Count == 0) return null;
            var i = Mathf.Clamp(round - 1, 0, deductionFailures.Count - 1);
            return deductionFailures[i];
        }

        /// <summary>두 단서에 맞는 조합식. 없으면 null.</summary>
        public HypothesisRecipe FindHypothesis(string a, string b)
        {
            if (hypotheses == null) return null;
            for (int i = 0; i < hypotheses.Count; i++)
            {
                var h = hypotheses[i];
                if (h != null && h.Matches(a, b)) return h;
            }
            return null;
        }

        public DialogueSequence PreIncidentTalk => preIncidentTalk;
        public string CutsceneName => cutsceneName;
        public Sprite CutsceneImage => cutsceneImage;
        public DialogueSequence Cutscene => cutscene;
        public DialogueSequence AfterCutsceneTalk => afterCutsceneTalk;
        public DialogueSequence Intro => intro;
        public IReadOnlyList<InteractableDefinition> SceneObjects => sceneObjects;
        public float PassThreshold => passThreshold;
        public float WarnThreshold => warnThreshold;
        public DialogueSequence GatePass => gatePass;
        public DialogueSequence GateWarn => gateWarn;
        public DialogueSequence GateBlocked => gateBlocked;

        public DialogueSequence BeforeSuspectSelect => beforeSuspectSelect;
        public DialogueSequence SuspectSelectTalk => suspectSelectTalk;
        public string SuspectSelectPrompt => suspectSelectPrompt;

        public IReadOnlyList<SuspectDefinition> FirstRoundSuspects => firstRoundSuspects;
        /// <summary>
        /// 2차 심문 대상. 비워 두면 2차 심문이 없는 챕터라는 뜻이다.
        /// 1차와 같은 인물을 다시 만나려면 명시적으로 다시 넣는다 — 암묵적 반복은 사고를 부른다.
        /// </summary>
        public IReadOnlyList<SuspectDefinition> SecondRoundSuspects => secondRoundSuspects;

        public string InterrogationTitleFormat => interrogationTitleFormat;
        public IReadOnlyList<TutorialPopupDefinition> TutorialPopups => tutorialPopups;

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

        /// <summary>심문 진입 제목. 형식이 비어 있으면 띄우지 않는다.</summary>
        public string BuildInterrogationTitle(SuspectDefinition suspect)
        {
            if (string.IsNullOrEmpty(interrogationTitleFormat) || suspect == null) return string.Empty;
            return interrogationTitleFormat.Replace("{name}", suspect.DisplayName);
        }

        /// <summary>해당 트리거의 튜토리얼 팝업. 튜토리얼 챕터가 아니거나 없으면 null.</summary>
        public TutorialPopupDefinition FindTutorialPopup(TutorialPopupTrigger trigger)
        {
            if (!isTutorial || tutorialPopups == null) return null;
            for (int i = 0; i < tutorialPopups.Count; i++)
            {
                var p = tutorialPopups[i];
                if (p != null && p.Trigger == trigger && !p.IsEmpty) return p;
            }
            return null;
        }

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
