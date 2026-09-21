using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 단서 수집 페이즈의 클릭 대상 하나.
    ///
    /// 기획서 〈시스템 기획 및 화면 전환〉:
    /// "증거 수집 페이즈에서는 단순 상호 작용만을 목적으로 하는 '일반 오브젝트'와
    ///  단서로 활용 가능한 '단서 오브젝트'로 구분된다.
    ///  상호 작용이 가능한 오브젝트 마우스 좌클릭 시 &gt;&gt; 화면 중앙에 단서의 이미지 팝업과
    ///  &lt;주인공&gt; 혹은 &lt;조수&gt;의 설명 대사 출력 및 우측 ui내에 수집"
    ///
    /// 즉 일반 오브젝트도 우측 목록에 '수집 오브젝트'로 들어간다. 일반/특수의 차이는
    /// 수집 연출이 아니라 목록 안에서의 표기(툴팁의 강조 구간, 승격 후 금색 테두리)로만 드러난다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Interactable", fileName = "Obj_")]
    public class InteractableDefinition : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string id;
        [SerializeField] private InteractableKind kind = InteractableKind.Ordinary;

        [Header("표기")]
        [Tooltip("마우스 오버 시 표시할 짧은 이름.")]
        [SerializeField] private string hoverLabel;

        [Header("상호작용")]
        [Tooltip("클릭했을 때 출력할 주인공/조수의 설명 대사. 비어 있으면 이미지 팝업만 뜬다.")]
        [SerializeField] private List<DialogueLine> onInspect = new List<DialogueLine>();

        [Tooltip("이미 조사한 뒤 다시 클릭했을 때의 대사. 비어 있으면 이미지 팝업만 다시 뜬다.")]
        [SerializeField] private List<DialogueLine> onReinspect = new List<DialogueLine>();

        [Header("수집")]
        [Tooltip("클릭하면 우측 목록에 들어가는 수집품. 일반 오브젝트도 갖는다.")]
        [SerializeField] private ClueDefinition grantedClue;

        [Tooltip("클릭 시 화면 중앙에 띄울 이미지. 없으면 단서의 클로즈업/아이콘을 쓴다.")]
        [SerializeField] private Sprite closeUpImage;

        public string Id => id;
        public InteractableKind Kind => kind;
        public string HoverLabel => hoverLabel;
        public IReadOnlyList<DialogueLine> OnInspect => onInspect;

        /// <summary>재조사 대사. 비어 있으면 아무 대사도 없다 — 원문에 없는 문장을 지어 넣지 않는다.</summary>
        public IReadOnlyList<DialogueLine> OnReinspect => onReinspect;

        public ClueDefinition GrantedClue => grantedClue;
        public Sprite CloseUpImage => closeUpImage != null ? closeUpImage
            : (grantedClue != null ? grantedClue.CloseUpImage : null);

        public bool IsSpecial => kind == InteractableKind.Special;
        public bool GrantsClue => grantedClue != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;

            if (grantedClue == null)
                Debug.LogWarning($"[{name}] 수집품이 비어 있습니다. 클릭해도 목록에 들어가지 않습니다.", this);
            else if (kind == InteractableKind.Special && !grantedClue.HasUsableHighlight)
                Debug.LogWarning($"[{name}] 단서 오브젝트인데 툴팁에 <hl> 강조 구간이 없어 승격할 수 없습니다.", this);
        }
#endif
    }
}
