using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 심문 말풍선의 모양 — 대사의 감정에 따라 다르게 뜬다.
    /// '자동'이면 그 줄의 표정(<see cref="ExpressionId"/>)을 보고 고른다.
    /// </summary>
    public enum BubbleStyle
    {
        [InspectorName("자동 (표정을 따라)")] Auto = 0,
        [InspectorName("일반")] Normal = 1,
        [InspectorName("짜증 · 분노")] Angry = 2,
        [InspectorName("불안 · 당황")] Anxious = 3,
        [InspectorName("기쁨")] Joy = 4,
    }

    public static class BubbleStyles
    {
        /// <summary>'자동'을 표정으로 풀어 실제 모양을 돌려준다.</summary>
        public static BubbleStyle Resolve(BubbleStyle style, ExpressionId expression)
        {
            if (style != BubbleStyle.Auto) return style;
            switch (expression)
            {
                case ExpressionId.Angry:
                    return BubbleStyle.Angry;
                case ExpressionId.Nervous:
                case ExpressionId.Flustered:
                case ExpressionId.Fearful:
                case ExpressionId.Surprised:
                    return BubbleStyle.Anxious;
                case ExpressionId.Smile:
                case ExpressionId.Sly:
                    return BubbleStyle.Joy;
                default:
                    return BubbleStyle.Normal;
            }
        }
    }
}
