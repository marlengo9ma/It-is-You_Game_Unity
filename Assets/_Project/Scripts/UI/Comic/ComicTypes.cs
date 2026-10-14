using System.Collections;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>컷이 페이지에 들어오는 방식.</summary>
    public enum CutMotion
    {
        [InspectorName("위에서 내려옴")] FromTop = 0,
        [InspectorName("아래에서 올라옴")] FromBottom = 1,
        [InspectorName("왼쪽에서 들어옴")] FromLeft = 2,
        [InspectorName("오른쪽에서 들어옴")] FromRight = 3,
        [InspectorName("제자리에서 나타남 (페이드)")] Fade = 4,
        [InspectorName("톡 튀어나옴 (말풍선)")] Pop = 5,
        [InspectorName("바로 나타남 (애니메이션 없음)")] Instant = 6
    }

    /// <summary>움직임의 감속 곡선.</summary>
    public enum CutEase
    {
        [InspectorName("일정한 속도")] Linear = 0,
        [InspectorName("부드럽게 멈춤")] EaseOut = 1,
        [InspectorName("빠르게 들어와 천천히 멈춤")] EaseOutStrong = 2,
        [InspectorName("살짝 넘쳤다 돌아옴")] Back = 3
    }

    public static class ComicEase
    {
        public static float Evaluate(CutEase ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case CutEase.Linear: return t;
                case CutEase.EaseOut: return 1f - (1f - t) * (1f - t);
                case CutEase.EaseOutStrong: { var u = 1f - t; return 1f - u * u * u; }
                case CutEase.Back:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    var u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                default: return t;
            }
        }
    }

    /// <summary>
    /// 페이지를 재생하는 쪽(PrologueController)이 페이지에 건네는 창구.
    /// 클릭 · 건너뛰기 · 진행 안내 · 서류 팝업을 페이지가 직접 들고 있지 않게 한다.
    /// </summary>
    public interface IComicDirector
    {
        /// <summary>지난번 호출 이후 화면이 눌렸으면 true 를 돌려주고 기록을 지운다.</summary>
        bool ConsumeClick();

        /// <summary>쌓여 있는 클릭을 버린다. 기다리기 직전에 부른다.</summary>
        void ClearClicks();

        /// <summary>건너뛰기를 눌렀다.</summary>
        bool Skipping { get; }

        /// <summary>일시 정지 중이면 시간이 흐르지 않는다.</summary>
        bool Paused { get; }

        /// <summary>「화면을 누르면 다음」 안내.</summary>
        void SetAdvanceHint(bool visible);

        /// <summary>그림(없으면 안내 글)을 띄우고 닫힐 때까지 기다린다.</summary>
        IEnumerator ShowPopup(Sprite image, string placeholder);
    }
}
