using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 단서 수집 페이즈의 클릭 대상 하나.
    ///
    /// 기획서 기준으로 일반/특수의 차이는 "수집 애니메이션의 유무"가 아니라
    /// 수집 UI 안에서의 표기 차이다. 애니메이션으로 구분하면 플레이어가
    /// 수집 여부를 확신할 수 없다는 피드백이 반영된 결과라, 이 클래스는
    /// 연출 정보와 규칙 정보를 분리해 둔다.
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
        [Tooltip("클릭했을 때 출력할 주인공/조수의 설명 대사.")]
        [SerializeField] private List<DialogueLine> onInspect = new List<DialogueLine>();

        [Tooltip("이미 조사한 뒤 다시 클릭했을 때의 대사. 비어 있으면 위 대사를 반복한다.")]
        [SerializeField] private List<DialogueLine> onReinspect = new List<DialogueLine>();

        [Header("수집")]
        [Tooltip("특수 오브젝트일 때 획득하는 단서. 일반 오브젝트면 비워 둔다.")]
        [SerializeField] private ClueDefinition grantedClue;

        [Tooltip("클릭 시 확대해서 보여줄 단서 이미지. 없으면 단서 아이콘을 쓴다.")]
        [SerializeField] private Sprite closeUpImage;

        public string Id => id;
        public InteractableKind Kind => kind;
        public string HoverLabel => hoverLabel;
        public IReadOnlyList<DialogueLine> OnInspect => onInspect;
        public IReadOnlyList<DialogueLine> OnReinspect =>
            (onReinspect != null && onReinspect.Count > 0) ? onReinspect : onInspect;
        public ClueDefinition GrantedClue => grantedClue;
        public Sprite CloseUpImage => closeUpImage != null ? closeUpImage
            : (grantedClue != null ? grantedClue.Icon : null);

        public bool IsSpecial => kind == InteractableKind.Special;
        public bool GrantsClue => grantedClue != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;

            // 특수 오브젝트인데 단서가 없거나, 일반 오브젝트인데 단서가 붙어 있으면
            // 수집률 계산이 어긋나므로 에디터에서 바로 알 수 있게 경고한다.
            if (kind == InteractableKind.Special && grantedClue == null)
                Debug.LogWarning($"[{name}] 특수 오브젝트인데 획득 단서가 비어 있습니다.", this);
            if (kind == InteractableKind.Ordinary && grantedClue != null)
                Debug.LogWarning($"[{name}] 일반 오브젝트에 단서가 연결돼 있습니다. 종류를 확인하세요.", this);
        }
#endif
    }
}
