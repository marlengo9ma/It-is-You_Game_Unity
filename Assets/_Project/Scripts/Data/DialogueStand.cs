using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 대화 화면에 서 있는 인물(스탠드 일러스트 / Live2D) 구성.
    /// 대사 묶음(<see cref="DialogueSequence"/>)마다 정하고, 필요하면 대사 한 줄(<see cref="DialogueLine"/>)에서 바꾼다 —
    /// 줄에서 바꾸면 그 묶음이 끝날 때까지 이어진다. 같은 대화 화면을 1인 · 2인 · 아무도 없는 장면으로 나눠 쓴다(2026-10-24).
    /// </summary>
    public enum DialogueStand
    {
        [InspectorName("화면 기본값")] Default = 0,
        [InspectorName("아무도 없음")] None = 1,
        [InspectorName("세피만")] Sepi = 2,
        [InspectorName("고양이만")] Cat = 3,
        [InspectorName("세피 + 고양이")] SepiAndCat = 4,
    }

    public static class DialogueStandExtensions
    {
        public static bool HasSepi(this DialogueStand s) => s == DialogueStand.Sepi || s == DialogueStand.SepiAndCat;
        public static bool HasCat(this DialogueStand s) => s == DialogueStand.Cat || s == DialogueStand.SepiAndCat;
    }
}
