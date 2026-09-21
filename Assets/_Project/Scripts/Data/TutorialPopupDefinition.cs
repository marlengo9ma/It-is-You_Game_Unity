using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>튜토리얼 팝업이 언제 뜨는가.</summary>
    public enum TutorialPopupTrigger
    {
        /// <summary>코드에서 직접 띄운다.</summary>
        Manual = 0,

        /// <summary>용의자의 심문 진입 대사가 끝난 직후. 기획: "[대사 이후, 튜토리얼 팝업_1 창 실행]"</summary>
        AfterSuspectOpening = 1,

        /// <summary>밑줄 구간이나 리액션이 들어 있는 대사가 처음 나오기 직전.</summary>
        BeforeFirstCollectableLine = 2,

        /// <summary>첫 메인 선택지의 대사가 끝난 뒤. (기억 짐작 설명 자리)</summary>
        AfterFirstChoice = 3,

        /// <summary>
        /// 페널티에 대한 첫 설명. 강압 선택지 뒤의 시간 경과 대사가 끝났을 때,
        /// 무반응 회차 대사가 동났을 때, 또는 어떤 페널티 판정이든 처음 났을 때 — 먼저 오는 쪽.
        /// 기획: "[신뢰도 하락 및 게임 오버에 대한 경고와 설명 튜토리얼 팝업]
        ///        > 해당 이벤트 미출현 시, 추후 선택지로 인한 설명에서 재출현"
        /// </summary>
        OnFirstPenalty = 4,

        /// <summary>기억 짐작을 처음 쓸 수 있게 됐을 때. (기억 짐작 UI 가 생기면 연결)</summary>
        OnRecallAvailable = 5,
    }

    /// <summary>
    /// 튜토리얼 팝업 한 장. 기획서: "각각 숫자마다 페이지가 구분되어 있는 팝업창이 팝업됨.
    /// 타이밍은 시나리오 텍스트 중간에 기재."
    ///
    /// 본문은 노션 원문을 페이지 단위로 그대로 담는다. 같은 트리거의 팝업은 한 챕터에 한 번만 뜬다.
    /// </summary>
    [Serializable]
    public class TutorialPopupDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private TutorialPopupTrigger trigger = TutorialPopupTrigger.Manual;

        [Tooltip("페이지 단위 본문. 한 항목이 한 페이지.")]
        [TextArea(3, 12)]
        [SerializeField] private List<string> pages = new List<string>();

        public string Id => id;
        public string Title => title;
        public TutorialPopupTrigger Trigger => trigger;
        public IReadOnlyList<string> Pages => pages;
        public bool IsEmpty => pages == null || pages.Count == 0;
    }
}
