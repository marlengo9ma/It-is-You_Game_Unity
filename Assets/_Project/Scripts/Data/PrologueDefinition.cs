using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 인트로 카툰의 컷 한 장.
    ///
    /// 인트로는 대사창으로 읽는 구간이 아니라 일러스트가 넘어가는 구간이다.
    /// 그래서 여기에는 대사가 없다. 클릭하면 다음 컷으로 넘어갈 뿐이다.
    /// </summary>
    [Serializable]
    public class PrologueCut
    {
        [Tooltip("컷 이름. 개발용 라벨이며, 그림이 없는 동안에만 화면에 표시된다.")]
        [SerializeField] private string cutName;

        [Tooltip("이 컷의 그림. 비어 있으면 더미 판과 컷 이름이 대신 표시된다.")]
        [SerializeField] private Sprite image;

        // Unity 는 [Serializable] 클래스 필드를 null 로 저장하지 않는다. 비워 둬도
        // 기본 인스턴스가 하나 생겨 되살아나기 때문에, "있다/없다"는 플래그로 따로 적어 둔다.
        [Tooltip("이 컷에 클릭해야 넘어갈 수 있는 오브젝트가 있는가.")]
        [SerializeField] private bool hasInteraction;

        [Tooltip("이 컷 안에서 클릭해야 넘어갈 수 있는 오브젝트.")]
        [SerializeField] private PrologueProp interaction;

        public string CutName => cutName;
        public Sprite Image => image;
        public PrologueProp Interaction => interaction;

        public bool HasInteraction => hasInteraction && interaction != null && interaction.IsValid;
    }

    /// <summary>
    /// 컷 안에 놓인 클릭 가능한 물건.
    ///
    /// 기획서 인트로 흐름의 "전달받은 서류 내용 팝업(상호작용)" 이 이것이다.
    /// 선택지 버튼이 아니라 그림 위의 서류를 직접 집는 동작이라서,
    /// 컷 안의 영역으로 잡고 그 자리에 더미 오브젝트를 놓는다.
    /// </summary>
    [Serializable]
    public class PrologueProp
    {
        [Tooltip("개발용 이름. 그림이 없는 동안 더미 오브젝트 위에 표시된다.")]
        [SerializeField] private string label = "서류";

        [Tooltip("컷 안에서 차지하는 영역. 0~1 비율이며 좌하단이 (0,0) 이다.")]
        [SerializeField] private Rect area = new Rect(0.38f, 0.22f, 0.24f, 0.22f);

        [Tooltip("클릭하면 뜨는 팝업 그림. 비어 있으면 더미 판이 대신 표시된다.")]
        [SerializeField] private Sprite popupImage;

        [Tooltip("팝업 그림이 없는 동안 더미 판에 표시할 내용. 아트가 들어오면 지워도 된다.")]
        [TextArea(3, 12)]
        [SerializeField] private string popupPlaceholder;

        public string Label => string.IsNullOrEmpty(label) ? "물건" : label;
        public Rect Area => area;
        public Sprite PopupImage => popupImage;
        public string PopupPlaceholder => popupPlaceholder;

        /// <summary>그림이든 자리든, 띄울 것이 있으면 유효하다.</summary>
        public bool IsValid => area.width > 0f && area.height > 0f;
    }

    /// <summary>
    /// 게임 시작부터 기차 탑승까지의 인트로 카툰.
    ///
    /// 챕터와 분리한 이유는 페이즈 규칙이 하나도 적용되지 않기 때문이다.
    /// 여기에는 타임 토큰도, 수집률도, 페널티도 없다. 그림과 클릭만 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Prologue", fileName = "Prologue_")]
    public class PrologueDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [SerializeField] private List<PrologueCut> cuts = new List<PrologueCut>();

        [Tooltip("카툰이 끝나면 넘어갈 씬 이름. 비어 있으면 자동 전환하지 않는다.")]
        [SerializeField] private string nextSceneName;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public IReadOnlyList<PrologueCut> Cuts => cuts;
        public string NextSceneName => nextSceneName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
