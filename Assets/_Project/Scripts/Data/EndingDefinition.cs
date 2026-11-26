using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>엔딩의 종류. 끝난 뒤에 무엇이 이어지는지가 갈린다.</summary>
    public enum EndingKind
    {
        /// <summary>범인 지목으로 닿는 엔딩. 끝나면 엔딩 제목 → 1챕터 인트로 → 데모 종료 화면.</summary>
        [InspectorName("일반 엔딩")] Normal = 0,
        /// <summary>게임오버 엔딩. 끝나면 게임오버 화면(단서 수집부터 다시 / 불러오기 / 타이틀로).</summary>
        [InspectorName("게임오버")] GameOver = 1,
        /// <summary>엔딩 사이에 끼는 장면 — 1챕터 인트로처럼, 끝나도 제목이나 게임오버가 뜨지 않는다.</summary>
        [InspectorName("이어지는 장면 (1챕터 인트로 등)")] Interlude = 2
    }

    /// <summary>엔딩 한 단계가 시작될 때 화면에 무엇을 깔지.</summary>
    public enum EndingScreen
    {
        [InspectorName("지금 화면 그대로")] Keep = 0,
        [InspectorName("컷 그림")] Cut = 1,
        [InspectorName("객실 (대화 화면 + 스탠드)")] Room = 2,
        [InspectorName("까만 화면")] Black = 3
    }

    /// <summary>단계가 시작될 때 한 번 거는 연출.</summary>
    public enum EndingEffect
    {
        [InspectorName("없음")] None = 0,
        [InspectorName("화면 흔들림")] Shake = 1,
        [InspectorName("하얗게 번쩍임")] Flash = 2,
        [InspectorName("천천히 어두워짐")] Darken = 3,
        [InspectorName("어두워진 것 걷기")] Brighten = 4
    }

    /// <summary>
    /// 엔딩의 한 단계 — 화면(컷 / 객실 / 까만 화면) + 연출 + 대사 묶음.
    /// 노션 대본의 [컷 EA#2] 한 번이 한 단계가 된다. 대사가 없으면 화면만 띄우고 클릭을 기다린다.
    /// </summary>
    [Serializable]
    public class EndingStep
    {
        [Tooltip("에디터에서 알아보기 위한 메모. 게임에는 쓰이지 않는다.")]
        [SerializeField] private string note;

        [Tooltip("이 단계가 시작될 때 깔 화면.")]
        [SerializeField] private EndingScreen screen = EndingScreen.Keep;

        [Tooltip("화면이 '컷 그림'일 때 보여 줄 그림. 비우면 앞 단계의 컷을 그대로 둔다.")]
        [SerializeField] private Sprite cut;

        [Tooltip("단계가 시작될 때 한 번 거는 연출.")]
        [SerializeField] private EndingEffect effect = EndingEffect.None;

        [Tooltip("이 단계의 대사. 줄마다 '일러스트' 칸에 그림을 넣으면 팝업(예: Evt_#2)으로 뜬다. 비우면 화면만 보이고 클릭을 기다린다.")]
        [SerializeField] private DialogueSequence talk = new DialogueSequence();

        public string Note => note;
        public EndingScreen Screen => screen;
        public Sprite Cut => cut;
        public EndingEffect Effect => effect;
        public DialogueSequence Talk => talk;
        public bool HasTalk => talk != null && !talk.IsEmpty;
    }

    /// <summary>
    /// 엔딩 한 편 (2026-11-27). 지목 결과 · 게임오버마다 하나씩 두고, 컷 · 연출 · 대사를 단계별로 바꿔 끼운다.
    ///
    /// 어느 엔딩이 나오는가:
    /// 1) 지목의 추론 선택지(AccusationOption)에 엔딩이 지정돼 있으면 그것,
    /// 2) 아니면 챕터의 엔딩 목록에서 '지목한 용의자'가 같은 것,
    /// 3) 그것도 없으면 챕터의 기본 엔딩.
    /// 게임오버는 챕터의 게임오버 엔딩 칸(신뢰 상실 · 자기 비하 · 엉뚱한 근거)에서 고른다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Ending", fileName = "End_")]
    public class EndingDefinition : ScriptableObject
    {
        [SerializeField] private string id;

        [Tooltip("엔딩 제목. 엔딩이 끝난 뒤 제목 카드에, 세이브 칸 설명에 쓰인다. 예: 부주의 판결")]
        [SerializeField] private string title;

        [Tooltip("엔딩 기호. 제목 카드 위에 작게 쓰인다. 예: ENDING A-1")]
        [SerializeField] private string code;

        [SerializeField] private EndingKind kind = EndingKind.Normal;

        [Tooltip("이 용의자를 지목하면 이 엔딩이 나온다 (SEPI / CAT). 비우면 자동으로 이어지지 않는다 — 지목 선택지에 직접 지정할 때.")]
        [SerializeField] private string suspectId;

        [Tooltip("차례로 재생되는 단계들.")]
        [SerializeField] private List<EndingStep> steps = new List<EndingStep>();

        [Tooltip("일반 엔딩이 끝난 뒤 제목 카드를 띄운다.")]
        [SerializeField] private bool showTitleCard = true;

        [Tooltip("게임오버일 때 게임오버 화면에 뜨는 한 줄. 예: 보조 수사관의 신뢰를 잃었습니다.")]
        [TextArea(1, 3)]
        [SerializeField] private string gameOverMessage;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Title => title;
        public string Code => code;
        public EndingKind Kind => kind;
        public string SuspectId => suspectId;
        public IReadOnlyList<EndingStep> Steps => steps;
        public bool ShowTitleCard => showTitleCard;
        public string GameOverMessage => gameOverMessage;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
