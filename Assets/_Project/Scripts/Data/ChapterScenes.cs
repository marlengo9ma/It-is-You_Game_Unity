using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 사건 이전 대화 사이에 끼는 회상 장면 한 컷 — 노션 &lt;회상씬으로 전환 E#1&gt; 같은 자리.
    /// 화면을 덮었다 걷고, 그림(없으면 이름판) 위에서 대사가 흐른다.
    /// </summary>
    [Serializable]
    public class FlashbackScene
    {
        [Tooltip("그림이 없을 때 화면에 표시할 이름. 예: 회상씬 E#1")]
        [SerializeField] private string name;

        [Tooltip("회상 그림. 비어 있으면 이름판이 뜬다.")]
        [SerializeField] private Sprite image;

        [SerializeField] private DialogueSequence sequence = new DialogueSequence();

        public string Name => name;
        public Sprite Image => image;
        public DialogueSequence Sequence => sequence;
        public bool IsEmpty => sequence == null || sequence.IsEmpty;
    }

    /// <summary>수집 튜토리얼 한 단계가 화면에서 밝게 남겨 둘 곳.</summary>
    public enum GuideFocus
    {
        /// <summary>어둡게 덮지 않는다.</summary>
        None = 0,
        /// <summary>화면 전체를 어둡게 — 설명만.</summary>
        Dim = 1,
        /// <summary>수집 화면의 오브젝트 하나 (targetId = 오브젝트 id).</summary>
        SceneObject = 2,
        /// <summary>우측 수집 인벤토리의 칸 하나 (targetId = 단서 id).</summary>
        TraySlot = 3,
        /// <summary>열람창 전체.</summary>
        TooltipWindow = 4,
        /// <summary>열람창 안의 강조된 텍스트.</summary>
        TooltipHighlight = 5,
    }

    /// <summary>수집 튜토리얼 한 단계가 끝나는 조건.</summary>
    public enum GuideAdvance
    {
        /// <summary>안내창의 버튼을 누른다.</summary>
        Button = 0,
        /// <summary>targetId 오브젝트를 눌러 수집을 마친다.</summary>
        ObjectCollected = 1,
        /// <summary>targetId 단서의 열람창을 연다.</summary>
        TooltipOpened = 2,
        /// <summary>targetId 단서를 '단서'로 갱신한다(강조 텍스트를 누른다).</summary>
        Promoted = 3,
    }

    /// <summary>
    /// 노션 〈수집페이즈에 대한 튜토리얼 팝업〉의 한 단계. 문장과 &lt;화면이 어두워지고 …만이 하이라이트 된다&gt; 지시를
    /// 그대로 옮긴 것 — 무엇을 밝게 두고, 무엇을 해야 다음으로 넘어가는지.
    /// </summary>
    [Serializable]
    public class GuideStep
    {
        [SerializeField] private string id;

        [TextArea(2, 8)]
        [SerializeField] private string text;

        [SerializeField] private GuideFocus focus = GuideFocus.Dim;
        [SerializeField] private GuideAdvance advance = GuideAdvance.Button;

        [Tooltip("focus · advance 가 가리키는 오브젝트 id 또는 단서 id.")]
        [SerializeField] private string targetId;

        public string Id => id;
        public string Text => text;
        public GuideFocus Focus => focus;
        public GuideAdvance Advance => advance;
        public string TargetId => targetId;
    }
}
