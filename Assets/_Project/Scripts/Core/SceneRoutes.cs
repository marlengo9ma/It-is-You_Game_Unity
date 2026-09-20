namespace ItIsYou.Core
{
    /// <summary>
    /// 씬 이름을 한곳에 모아 둔다.
    ///
    /// 씬 이름을 코드 여기저기에 문자열로 흩어 두면, 씬 파일 이름을 바꾸는 순간
    /// 컴파일은 통과하는데 실행하면 끊긴다. 빌드 세팅 등록도 이 목록을 기준으로 한다.
    /// </summary>
    public static class SceneRoutes
    {
        public const string Title = "Title";
        public const string Prologue = "Prologue";
        public const string TutorialChapter = "Tutorial_Chapter";

        /// <summary>빌드 세팅에 들어가야 하는 순서. 첫 항목이 시작 씬이다.</summary>
        public static readonly string[] All = { Title, Prologue, TutorialChapter };
    }
}
