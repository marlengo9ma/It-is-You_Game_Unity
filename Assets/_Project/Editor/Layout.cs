using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 화면 배치의 기준값.
    ///
    /// 모든 수치가 8의 배수다. 패널을 손으로 끌어 옮기다 보면 47, 313 같은 값이 남고
    /// 그게 쌓이면 "왜인지 모르게 어긋나 보이는" 화면이 된다. 숫자를 여기 모아 두면
    /// 배치를 바꿀 때도 격자가 유지된다.
    /// </summary>
    public static class Layout
    {
        public const float Unit = 8f;

        /// <summary>기준 해상도.</summary>
        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        /// <summary>화면 바깥 여백.</summary>
        public const float Gutter = 48f;          // 6u

        /// <summary>패널 사이 간격.</summary>
        public const float Gap = 24f;             // 3u

        /// <summary>패널 안쪽 여백.</summary>
        public const float Pad = 24f;             // 3u

        // ---- 세로 구획 ----
        public const float TopBarHeight = 80f;    // 10u
        public const float DialogueHeight = 200f; // 25u — 수첩 (y 16~240)

        /// <summary>상단 바 아래쪽 y (음수, 화면 위에서부터).</summary>
        public const float TopBarBottom = -(Gutter + TopBarHeight);   // -128

        /// <summary>대사창 위쪽 y (양수, 화면 아래에서부터).</summary>
        public const float DialogueTop = Gutter + DialogueHeight;      // 288

        /// <summary>가운데 띠의 아래/위 경계.</summary>
        public const float BandBottom = DialogueTop + Gap;             // 312
        public const float BandTop = TopBarBottom - Gap;               // -152

        // ---- 가로 구획 ----
        /// <summary>
        /// 우측 목록(단서·정황·가설)과 기능 아이콘의 자리. 화면 끝까지 붙는다. (2026-09-22 스케치)
        /// 2026-09-27: 1/3(640) → 3/10(576). 2/7(548.6)은 8의 배수가 아니라 격자가 깨진다.
        /// 안쪽 여백 48×2 를 빼면 480 이 남아, 128 칸 3열 + 간격 24×2 = 432 가 그대로 들어간다.
        /// </summary>
        public const float SideWidth = 576f;      // 72u = 1920 × 3/10
        public const float ChoiceWidth = 352f;    // 44u — 스케치의 선택지 폭
        public const float ClueWidth = SideWidth;

        public const float ChoiceRight = Gutter + ChoiceWidth;         // 400
        public const float ClueLeft = -SideWidth;                      // -640
        /// <summary>주 영역(좌측 2/3)의 오른쪽 경계. 우측 패널 앞에서 한 칸 띈다.</summary>
        public const float MainRight = ClueLeft - Gap;                 // -664

        /// <summary>심문 화면의 무대 영역 (선택지와 단서 사이).</summary>
        public const float StageLeft = ChoiceRight + Gap;              // 652
        public const float StageRight = ClueLeft - Gap;                // -452

        // ---- 글자 크기 ----
        public static class Type
        {
            public const float Display = 32f;   // 인물 이름, 화면 제목
            public const float Metric = 40f;    // 토큰 숫자
            public const float Body = 24f;      // 대사 본문
            public const float Choice = 20f;    // 선택지
            public const float Label = 16f;     // 구획 제목 (자간 넓게)
            public const float Caption = 14f;   // 보조 설명, 이름표
        }

        // ---- 색 ----
        public static class Palette
        {
            public static readonly Color Bg        = new Color32(0x14, 0x16, 0x1A, 0xFF);
            public static readonly Color Panel     = new Color32(0x1C, 0x20, 0x25, 0xE6);
            public static readonly Color PanelSunk = new Color32(0x11, 0x13, 0x17, 0xF2);
            public static readonly Color Surface   = new Color32(0x26, 0x2B, 0x31, 0xFF);
            public static readonly Color Ink       = new Color32(0xDE, 0xD9, 0xCF, 0xFF);
            public static readonly Color InkSoft   = new Color32(0x9A, 0x96, 0x8D, 0xFF);
            public static readonly Color Brass     = new Color32(0xC4, 0xA7, 0x5C, 0xFF);
            public static readonly Color Rule      = new Color32(0x33, 0x38, 0x3D, 0xFF);
            public static readonly Color Alarm     = new Color32(0xC0, 0x8A, 0x85, 0xFF);
            public static readonly Color Good      = new Color32(0x8F, 0xB0, 0x98, 0xFF);
        }

        /// <summary>가장 가까운 8의 배수로 맞춘다.</summary>
        public static float Snap(float v) => Mathf.Round(v / Unit) * Unit;
    }
}
