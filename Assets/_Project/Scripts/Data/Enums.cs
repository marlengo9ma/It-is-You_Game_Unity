namespace ItIsYou.Data
{
    /// <summary>게임 전체 페이즈. 기획서의 Phase.Intro ~ Phase.End 에 대응.</summary>
    public enum GamePhase
    {
        None = 0,
        Intro = 1,          // 사건 발발 및 대화
        ClueCollection = 2, // Phase.1 단서 수집
        Interrogation1 = 3, // Phase.2 1차 심문
        Deduction = 4,      // Phase.3 단서 추가 수집 및 조합
        Interrogation2 = 5, // Phase.End 2차 심문
        Accusation = 6,     // 범인 지목
        Ending = 7,
        GameOver = 8,

        // ---- 사건 발생 이전 ----
        // 기획서에서 튜토리얼 챕터는 <화면 전환> 으로 두 번 끊긴다.
        // [게임플레이 배경_캐릭터 씬] → [일러스트 컷씬#1] → 다시 [게임플레이 배경_캐릭터 씬]
        // → <화면 전환> 사건 진행 #1. 그 앞의 두 구간을 따로 둔다.

        /// <summary>사건 발생 전의 평범한 대화. 페이즈 규칙이 적용되지 않는다.</summary>
        PreIncidentTalk = 9,

        /// <summary>일러스트 컷씬. 화면 전체가 컷 하나로 덮인다.</summary>
        Cutscene = 10
    }

    /// <summary>단서의 출처. UI 표기와 조합 규칙이 종류별로 달라진다.</summary>
    public enum ClueKind
    {
        /// <summary>단서 수집 페이즈에서 오브젝트를 클릭해 얻은 물증.</summary>
        Object = 0,
        /// <summary>심문 중 밑줄 표시된 대사를 클릭해 얻은 증언.</summary>
        Testimony = 1,
        /// <summary>리액션 타이밍에 캐릭터를 클릭해 얻은 정황 단서.</summary>
        Reaction = 2,
        /// <summary>단서 조합으로 새로 만들어진 단서.</summary>
        Deduced = 3
    }

    /// <summary>메인 선택지의 성향. 교차 선택 판정과 페널티의 기준이 된다.</summary>
    public enum ChoiceTone
    {
        /// <summary>경계도를 낮추며 자연스럽게 대화를 이끌어내는 선택지.</summary>
        Friendly = 0,
        /// <summary>추궁하거나 강압적으로 압박하는 선택지.</summary>
        Aggressive = 1,
        /// <summary>단서를 제시하는 선택지. 교차 판정에 포함되지 않는다.</summary>
        Evidence = 2,
        /// <summary>트리거로 생성되는 특수 선택지. 교차 판정에 포함되지 않는다.</summary>
        Special = 3
    }

    /// <summary>화면 내 클릭 가능한 오브젝트의 성격.</summary>
    public enum InteractableKind
    {
        /// <summary>상호작용만 가능한 일반 오브젝트.</summary>
        Ordinary = 0,
        /// <summary>단서로 수집 가능한 특수 오브젝트.</summary>
        Special = 1
    }

    /// <summary>대사 화자.</summary>
    public enum SpeakerId
    {
        Narration = 0,
        Protagonist = 1,   // 주인공 (수사관)
        Inner = 2,         // 주인공 속마음 ('...' 표기)
        Assistant = 3,     // 조수 세피
        Cat = 4,           // 검은 고양이
        SuspectA = 5,
        SuspectB = 6,
        SuspectC = 7,
        System = 8         // 튜토리얼 팝업 등
    }

    /// <summary>
    /// 표정/리액션 식별자. Live2D Cubism 이든 Spine 이든 플레이스홀더든
    /// 이 값만 받아서 각자의 방식으로 재생한다.
    /// </summary>
    public enum ExpressionId
    {
        Neutral = 0,
        Smile = 1,
        Nervous = 2,      // 땀, 눈 피함
        Flustered = 3,    // 당황, 동공 떨림
        Angry = 4,
        Sad = 5,
        Surprised = 6,
        Fearful = 7,
        Thinking = 8,
        Sly = 9,
        Exhausted = 10
    }

    /// <summary>심문 페이즈가 끝난 사유.</summary>
    public enum InterrogationEndReason
    {
        /// <summary>플레이어가 직접 페이즈를 종료했다.</summary>
        PlayerEnded = 0,
        /// <summary>할당된 타임 토큰을 모두 소진했다.</summary>
        TokensExhausted = 1,
        /// <summary>과도한 강압으로 용의자가 공포/실신해 심문을 거부했다.</summary>
        RefusedByFear = 2,
        /// <summary>심문 오브젝트만 반복 선택해 용의자가 분노로 심문을 거부했다.</summary>
        RefusedByAnger = 3,
        /// <summary>친근/강압 교차 선택으로 게임오버됐다.</summary>
        GameOverByErraticTone = 4
    }

    /// <summary>수집률 게이트 판정 결과.</summary>
    public enum CollectionGateResult
    {
        /// <summary>충분히 모았다. 그대로 진행.</summary>
        Pass = 0,
        /// <summary>어느 정도 모았다. 경고 팝업 후 진행 여부를 플레이어가 선택.</summary>
        WarnAndConfirm = 1,
        /// <summary>부족하다. 진행 불가.</summary>
        Blocked = 2
    }

    /// <summary>게임오버 종류. 엔딩 연출이 갈린다.</summary>
    public enum GameOverKind
    {
        None = 0,
        /// <summary>통상 게임오버 — 비정상적 심문, 엉뚱한 근거 제시로 신뢰 상실.</summary>
        Standard = 1,
        /// <summary>자기 비하 엔딩 — 단서 부족으로 조합 불가.</summary>
        SelfLoathing = 2
    }
}
