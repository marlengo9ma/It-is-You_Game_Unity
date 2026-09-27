namespace ItIsYou.Data
{
    /// <summary>
    /// 대사 줄·선택지 회차에 걸 수 있는 연출 이름.
    ///
    /// 시나리오의 괄호 지시문 —— (동공이 떨리는 모습), (째려보며), (부들거리며) —— 중에서
    /// 몸이 실제로 움직이는 것만 이름을 붙였다. 표정만 바뀌는 것은 <c>ExpressionId</c> 가 맡는다.
    ///
    /// 지금은 <c>PlaceholderCharacterView</c> 가 코드로 흉내내고, Live2D 가 붙으면
    /// 같은 이름의 모션으로 갈아끼운다. 그래서 문자열이다 —— 열거형으로 두면
    /// 아티스트가 모션 하나 늘릴 때마다 코드를 고쳐야 한다.
    /// 여기 없는 이름을 써도 오류가 아니라 조용히 무시된다.
    /// </summary>
    public static class StageAnimation
    {
        /// <summary>동공이 떨리는 모습. 잔떨림.</summary>
        public const string Tremble = "tremble";

        /// <summary>시선을 피한다. 고개가 살짝 옆으로.</summary>
        public const string LookAway = "lookaway";

        /// <summary>고양이 쪽을 힐끔 본다. 옆을 봤다가 돌아온다.</summary>
        public const string GlanceCat = "glance_cat";

        /// <summary>작은 한숨. 어깨가 내려갔다 올라온다.</summary>
        public const string Sigh = "sigh";

        /// <summary>놀란다. 짧고 크게 튄다.</summary>
        public const string Startle = "startle";

        /// <summary>째려본다. 앞으로 조금 기울인 채 멈춘다.</summary>
        public const string Glare = "glare";

        /// <summary>화가 나 부들거린다. 빠른 진동.</summary>
        public const string Fume = "fume";

        /// <summary>어깨가 움츠러든다.</summary>
        public const string Shrink = "shrink";

        /// <summary>얼굴이 하얗게 질린다.</summary>
        public const string Pale = "pale";

        /// <summary>고개를 숙인다.</summary>
        public const string Bow = "bow";

        /// <summary>눈을 감고 생각한다. 아주 느린 끄덕임.</summary>
        public const string Ponder = "ponder";
    }
}
