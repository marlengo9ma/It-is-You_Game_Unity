using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 단서 하나. 기획서의 2단계 명칭 규칙과 '수집 오브젝트 → 단서 오브젝트' 승격 규칙을 담는다.
    ///
    /// "수집한 단서들의 UI 아이콘 위에 마우스 오버 시, ??? 혹은 단순한 명칭으로 출력.
    ///  이후 수집품 목록에서 좌클릭해서 읽을 시, 기존 화면이 반투명하게 어두워지며,
    ///  화면 중앙에 단서의 이미지 및 우측에 스크롤 가능한 텍스트 툴팁 창이 팝업된다.
    ///  툴팁 출력 중, 해당 수집품이 단서로 쓸 수 있는 오브젝트인 경우에는 텍스트의 일부가 강조 표시가 된다.
    ///  이를 누를 시, 해당 오브젝트는 단순 '수집 오브젝트'에서 '단서 오브젝트'로 변경된다."
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Clue", fileName = "Clue_")]
    public class ClueDefinition : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("데이터 전체에서 이 단서를 가리키는 고유 id. CSV 와 대사 태그가 이 값을 참조한다.")]
        [SerializeField] private string id;

        [SerializeField] private ClueKind kind = ClueKind.Object;

        [Header("표기")]
        [Tooltip("읽기 전 명칭. 예: ??? / 리본 끈")]
        [SerializeField] private string unreadName = "???";

        [Tooltip("읽은 뒤 명칭. 부사가 포함된 세부 명칭. 예: 얼룩지고 헤진 고급 리본")]
        [SerializeField] private string readName;

        [Tooltip("툴팁 본문. <hl>...</hl> 로 감싼 구간이 '단서로 쓸 수 있음' 강조로 출력되고, 누르면 단서 오브젝트로 승격된다.")]
        [TextArea(3, 10)]
        [SerializeField] private string tooltip;

        [Tooltip("목록 칸의 아이콘. 96 × 96.")]
        [SerializeField] private Sprite icon;

        [Tooltip("열람창 가운데에 크게 띄울 이미지. 비어 있으면 아이콘을 쓴다.")]
        [SerializeField] private Sprite closeUpImage;

        [Header("규칙")]
        [Tooltip("켜면 이 단서 없이는 수집률과 무관하게 다음 페이즈로 넘어갈 수 없다.")]
        [SerializeField] private bool isKeyClue;

        [Tooltip("수집률 계산에 포함할지. 연출용으로만 주는 단서는 꺼 둔다.")]
        [SerializeField] private bool countsTowardCompletion = true;

        public string Id => id;
        public ClueKind Kind => kind;
        public string UnreadName => string.IsNullOrEmpty(unreadName) ? "???" : unreadName;
        public string ReadName => string.IsNullOrEmpty(readName) ? UnreadName : readName;
        public string Tooltip => tooltip;
        public Sprite Icon => icon;
        public Sprite CloseUpImage => closeUpImage != null ? closeUpImage : icon;
        public bool IsKeyClue => isKeyClue;
        public bool CountsTowardCompletion => countsTowardCompletion;

        /// <summary>툴팁에 강조 구간이 있는가 — 즉 '단서 오브젝트'로 승격할 수 있는 수집품인가.</summary>
        public bool HasUsableHighlight => !string.IsNullOrEmpty(tooltip) && tooltip.Contains("<hl>");

        /// <summary>승격할 수 있는가. 증언·리액션·추론은 얻는 순간부터 단서이므로 승격 대상이 아니다.</summary>
        public bool IsPromotable => kind == ClueKind.Object && HasUsableHighlight;

        /// <summary>읽음 여부에 따른 표시 이름.</summary>
        public string DisplayName(bool hasBeenRead) => hasBeenRead ? ReadName : UnreadName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
