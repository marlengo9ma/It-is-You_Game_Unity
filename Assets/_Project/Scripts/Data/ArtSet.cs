using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 아트 소스 교체 창구. 씬은 빌더가 다시 만들기 때문에 씬의 그림을 직접 갈아 끼우면 다음 재생성 때 사라진다.
    /// 대신 여기(Assets/_Project/Art/ArtSet.asset)에 스프라이트를 끼워 두면, 씬을 만들 때마다 빌더가 읽어 간다.
    ///
    /// 비어 있는 칸은 지금처럼 색 판 + "(아트 대기)" 글자로 남는다. 칸을 채우고
    /// 메뉴 It is You › 5. 전체 다시 만들기 를 한 번 돌리면 반영된다.
    ///
    /// 그림마다 바로 아래에 붙은 <see cref="Placement"/> 칸이 위치·크기·좌우 반전이다.
    /// 여기서 잡은 값은 씬을 다시 만들어도 남는다 — 씬에서 직접 옮긴 것은 남지 않는다.
    ///
    /// 단서 아이콘·클로즈업, 용의자 초상, 컷씬 그림, 프롤로그 컷은 여기가 아니라 각 데이터 에셋에 있다
    /// (Clue_*.asset · Obj_*.asset · Suspect_*.asset · Chapter_Tutorial.asset · Prologue_Intro.asset).
    /// 그쪽은 씬을 다시 만들어도 지워지지 않는다.
    ///
    /// 크기는 기준 해상도 1920×1080 기준의 화면 픽셀. 그림은 Assets/_Project/ 아래에 넣으면
    /// 자동으로 Sprite 로 임포트된다(ArtImportPostprocessor).
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Art Set", fileName = "ArtSet")]
    public class ArtSet : ScriptableObject
    {
        [Serializable]
        public struct ExpressionSprite
        {
            public ExpressionId expression;
            public Sprite sprite;
        }

        /// <summary>
        /// 그림 한 장의 자리. 빌더가 그림을 끼운 뒤 이 값을 적용한다.
        ///
        /// offset 은 원래 자리에서 밀어내는 픽셀, scale 은 그림 <b>가운데</b>를 기준으로 한 배율이다.
        /// 바닥에 서 있는 인물을 키우면 발이 같이 내려오므로, scale 을 올린 만큼 offset.y 로 올려 준다.
        ///
        /// preserveAspect 는 배경처럼 화면에 꽉 차는 그림에서 쓴다. 끄면 칸에 맞춰 늘어나고(비율이 다르면 찌그러짐),
        /// 켜면 비율을 지키며 칸 안에 들어간다(가장자리가 빈다). 비율이 다른 배경은
        /// preserveAspect 를 켜고 scale 을 1.1~1.3 쯤 올려 빈 데를 덮는 식으로 맞춘다.
        /// </summary>
        [Serializable]
        public struct Placement
        {
            [Tooltip("원래 자리에서 밀어내는 픽셀 (1920×1080 기준). +x 오른쪽, +y 위.")]
            public Vector2 offset;

            [Tooltip("크기 배율. 1 이 그대로. 그림 가운데를 기준으로 커지고 작아진다.")]
            public float scale;

            [Tooltip("좌우 반전. 인물이 반대쪽을 보게 할 때.")]
            public bool flipX;

            [Tooltip("비율 유지. 끄면 칸에 맞춰 늘어난다. 배경처럼 비율이 다른 그림에서 켠다.")]
            public bool preserveAspect;

            /// <summary>0 이나 음수로 둔 배율은 1 로 본다 — 인스펙터에서 비워 둬도 그림이 사라지지 않게.</summary>
            public float Scale => scale <= 0f ? 1f : scale;

            /// <summary>늘어나는 그림(배경·판)의 기본값.</summary>
            public static Placement Stretch => new Placement { scale = 1f, preserveAspect = false };

            /// <summary>비율을 지키는 그림(인물·소품)의 기본값.</summary>
            public static Placement Fit => new Placement { scale = 1f, preserveAspect = true };
        }

        [Header("배경")]
        [Tooltip("객실 배경 — 화면 전체 1920×1080. 사건 이전 대화 · 인트로 · 컷씬 뒤 배경.")]
        public Sprite roomBackground;
        public Placement roomBackgroundPlace = Placement.Stretch;

        [Tooltip("인트로 대화(기차역) 배경 — 화면 전체 1920×1080. 인트로 카툰 뒤 기차에 오르기 전까지의 대화. 비우면 타이틀 배경(titleBackground)을 쓴다.")]
        public Sprite preludeBackground;
        public Placement preludeBackgroundPlace = Placement.Stretch;

        [Tooltip("심문 · 조합 · 용의자 선택 · 지목 화면의 무대 배경 — 좌측 2/3 영역 1208×984. 비우면 roomBackground 를 쓴다.")]
        public Sprite stageBackground;
        public Placement stageBackgroundPlace = Placement.Stretch;

        [Tooltip("단서 수집 무대 — 객실 테이블 1208×984. 넣으면 핫스팟의 색 판이 투명해지고 그림 위에서 마우스 오버 테두리만 남는다.")]
        public Sprite collectionStage;
        public Placement collectionStagePlace = Placement.Stretch;

        [Tooltip("엔딩 자리 배경 1920×1080. 엔딩 컷씬이 오기 전까지의 판.")]
        public Sprite endingBackground;
        public Placement endingBackgroundPlace = Placement.Stretch;

        [Tooltip("추론 화면의 책상 1920×1080 — 서류 두 장이 놓이는 나무 책상. 비우면 짙은 나무색 판.")]
        public Sprite deductionDesk;
        public Placement deductionDeskPlace = Placement.Stretch;

        [Tooltip("추론 화면의 서류 한 장(종이 질감). 9-slice 로 늘어난다. 비우면 회갈색 판.")]
        public Sprite dossierPaper;

        [Header("인물 — 세피")]
        [Tooltip("사건 이전 대화 · 인트로 화면의 세피 (서 있는 전신). 약 576×994, 아래 정렬.")]
        public Sprite sepiTalk;
        public Placement sepiTalkPlace = Placement.Fit;

        [Tooltip("심문 · 조합 화면의 세피 (앉아 있는 상반신~전신). 512×688, 아래가 수첩에 살짝 가린다. 비우면 sepiTalk 를 쓴다.")]
        public Sprite sepiInterrogation;
        public Placement sepiStagePlace = Placement.Fit;

        [Tooltip("심문 표정 차분. 표정마다 sepiInterrogation 과 같은 크기의 그림. 없는 표정은 기본 그림으로 남는다. Live2D 가 오면 통째로 대체.")]
        public List<ExpressionSprite> sepiExpressions = new List<ExpressionSprite>();

        [Tooltip("세피 Live2D 모델 프리팹(Sepi 1.prefab). 넣으면 대화 · 심문 화면의 세피가 그림 대신 Live2D 로 나온다. 메뉴 It is You › 10 이 채운다.")]
        public GameObject sepiLive2D;

        [Tooltip("세피 Live2D 의 모션 표 — 기본(idle) 순서와 특수 모션 칸. 메뉴 It is You › 10 이 만든다.")]
        public Live2DMotionSet sepiLive2DMotions;

        [Tooltip("끄면 Live2D 모델이 있어도 그림(sepiTalk · sepiInterrogation)을 쓴다.")]
        public bool useSepiLive2D = true;

        [Header("인물 — 검은 고양이")]
        [Tooltip("사건 이전 대화 화면의 고양이. 약 307×281.")]
        public Sprite catTalk;
        public Placement catTalkPlace = Placement.Fit;

        [Tooltip("단서 수집 무대 위의 고양이 (테이블에 앉아 만년필을 깔고 있는). 약 362×335.")]
        public Sprite catStage;
        public Placement catStagePlace = Placement.Fit;

        [Header("대사창")]
        [Tooltip("주인공의 수첩 판 1024×224. 9-slice 로 늘리려면 Sprite Editor 에서 border 를 잡는다.")]
        public Sprite notebook;

        [Tooltip("상대의 말풍선 판 648×200 (9-slice).")]
        public Sprite speechBubble;

        [Tooltip("말풍선 꼬리 40×40. 비우면 판을 45° 돌린 사각형.")]
        public Sprite bubbleTail;

        [Tooltip("시네마틱 대사창의 붓자국 이름표. 흰색으로 그려 두면 화자 색(놋쇠/녹색/보라)으로 물든다. 약 320×60, 글자 길이에 따라 가로로 늘어난다(9-slice 권장).")]
        public Sprite nameTagBrush;

        [Tooltip("시네마틱 대사창 아래의 어두운 그라데이션 1920×440. 비우면 자동 생성.")]
        public Sprite bottomGradient;

        [Header("아이콘 · 잡동사니")]
        [Tooltip("우측 패널 '기록' 버튼 (돋보기) 72×72.")]
        public Sprite iconRecord;

        [Tooltip("우측 패널 · 대화 씬 우상단 '설정' 버튼 (톱니) 64×64.")]
        public Sprite iconSettings;

        [Tooltip("대화 씬 좌하단 LOG 버튼 (책) 88×64.")]
        public Sprite iconLog;

        [Tooltip("잉크 방울 32×32. 남은 것은 놋쇠색, 마른 것은 어두운 색으로 물든다 — 흰색으로 그린다.")]
        public Sprite inkDrop;

        [Header("UI 스킨 — 버튼 (Art/UI/UI_A/Cut)")]
        [Tooltip("기본 버튼 판 — 보통. 비우면 예전처럼 단색 판. 9-slice.")]
        public Sprite buttonNormal;
        [Tooltip("기본 버튼 판 — 마우스를 올렸을 때.")]
        public Sprite buttonHover;
        [Tooltip("기본 버튼 판 — 눌렀을 때.")]
        public Sprite buttonPressed;
        [Tooltip("기본 버튼 판 — 누를 수 없을 때.")]
        public Sprite buttonDisabled;
        [Tooltip("강조 버튼(와인색: 돌아가기 · 추리를 마친다 · 추론 진행하기 · 교체 취소) — 보통.")]
        public Sprite accentNormal;
        public Sprite accentHover;
        public Sprite accentPressed;
        public Sprite accentDisabled;

        [Header("UI 스킨 — 창 · 부품")]
        [Tooltip("큰 창 판(설정 · 대화 기록 · 튜토리얼 팝업). 9-slice.")]
        public Sprite windowPanel;
        [Tooltip("작은 창 판(확인 팝업 · 일시 정지). 9-slice.")]
        public Sprite smallPanel;
        [Tooltip("창 제목 아래 구분선(양 끝 장식, 가로 9-slice).")]
        public Sprite divider;
        [Tooltip("설정 창 슬라이더 — 홈 / 채움 / 손잡이.")]
        public Sprite sliderTrack;
        public Sprite sliderFill;
        public Sprite sliderKnob;
        [Tooltip("스크롤바 — 홈 / 손잡이 (대화 기록).")]
        public Sprite scrollTrack;
        public Sprite scrollHandle;
        [Tooltip("설정 창의 값 고르기 판(‹ 값 › 가운데 판).")]
        public Sprite optionPlate;

        [Header("UI 스킨 — 아이콘")]
        [Tooltip("닫기(X) 아이콘 — 설정 창 오른쪽 위.")]
        public Sprite iconClose;
        [Tooltip("이전 / 다음 쪽 아이콘 — 추론 화면 쪽 넘김 · 설정 창 값 고르기.")]
        public Sprite iconPrev;
        public Sprite iconNext;
        [Tooltip("대사가 끝났을 때 본문 오른쪽 아래의 ▼ 표시. 흰색 — 대사창마다 색이 입혀진다.")]
        public Sprite advanceArrow;

        [Header("UI 스킨 — 대화 화면 · 상단")]
        [Tooltip("좌상단 시계 · 날짜 뒤 띠(오른쪽으로 사라짐).")]
        public Sprite topBar;
        [Tooltip("시계 글자 왼쪽의 회중시계 그림.")]
        public Sprite clockOrnament;
        [Tooltip("상단 챕터 제목 명판(9-slice).")]
        public Sprite chapterPlate;
        [Tooltip("회상씬(E#1 ~ E#3) 위에 덮는 빛바랜 질감.")]
        public Sprite recallOverlay;

        [Header("UI 스킨 — SKIP · AUTO")]
        [Tooltip("AUTO 꺼짐 — 어두운 알약 판.")]
        public Sprite autoTrackOff;
        [Tooltip("AUTO 켜짐 — 밝은 알약 판.")]
        public Sprite autoTrackOn;
        [Tooltip("AUTO 꺼짐 — 구슬(왼쪽).")]
        public Sprite autoKnobOff;
        [Tooltip("AUTO 켜짐 — 구슬(오른쪽으로 미끄러짐).")]
        public Sprite autoKnobOn;
        [Tooltip("SKIP 아이콘 — 켜졌을 때(밝음).")]
        public Sprite skipIconOn;
        [Tooltip("SKIP 아이콘 — 평소(어두움).")]
        public Sprite skipIconOff;

        [Header("UI 스킨 — 단서 수집")]
        [Tooltip("조사할 수 있는 물건 둘레 표시(흰색 — 상태에 따라 색이 입혀진다, 9-slice).")]
        public Sprite hotspotFrame;
        [Tooltip("단서 칸 아래 이름표 판.")]
        public Sprite namePlate;
        [Tooltip("왼쪽 아래 '살펴본 곳' 진행도 판(왼쪽에 작은 사각 자리).")]
        public Sprite progressPlate;
        [Tooltip("물건을 눌렀을 때 뜨는 클로즈업 창의 액자 틀(가운데 비어 있음).")]
        public Sprite closeupFrame;
        [Tooltip("오른쪽 단서 목록 패널 판.")]
        public Sprite sidePanel;
        [Tooltip("목록 제목 옆 아이콘 — Evidence(봉투) / Leads(눈) / Hypotheses(만년필).")]
        public Sprite listIconEvidence;
        public Sprite listIconLeads;
        public Sprite listIconHypotheses;
        [Tooltip("단서 칸 — 빈 칸 / 채운 칸 / 마우스를 올린 칸.")]
        public Sprite clueSlotEmpty;
        public Sprite clueSlotFilled;
        public Sprite clueSlotHover;
        [Tooltip("단서로 승격된 칸의 테두리(흰색 — 놋쇠색이 입혀진다).")]
        public Sprite clueSlotPromoted;
        [Tooltip("정황(Leads) · 가설 한 줄 판.")]
        public Sprite leadPlate;
        [Tooltip("단서 열람창(왼쪽 그림 구멍 + 오른쪽 글 판). 그림은 구멍 자리에 맞춰 놓인다.")]
        public Sprite readingWindow;

        [Header("UI 스킨 — 심문")]
        [Tooltip("말풍선 — 짜증(분노) / 불안(당황) / 기쁨. 일반은 위 '대사창'의 speechBubble.")]
        public Sprite speechBubbleAngry;
        public Sprite speechBubbleAnxious;
        public Sprite speechBubbleJoy;
        [Tooltip("말풍선 꼬리 — 짜증 / 불안 / 기쁨. 일반은 bubbleTail.")]
        public Sprite bubbleTailAngry;
        public Sprite bubbleTailAnxious;
        public Sprite bubbleTailJoy;
        [Tooltip("선택지 버튼 — 보통 / 올림 / 누름 / 잠김.")]
        public Sprite choiceNormal;
        public Sprite choiceHover;
        public Sprite choicePressed;
        public Sprite choiceLocked;
        [Tooltip("선택지 목록 위 'Choice' 명판.")]
        public Sprite choicePlate;
        [Tooltip("남은 시간 토큰 — 남음 / 씀.")]
        public Sprite tokenFull;
        public Sprite tokenUsed;
        [Tooltip("무반응 게이지 — 홈 / 채움.")]
        public Sprite gaugeTrack;
        public Sprite gaugeFill;
        [Tooltip("'용의자 ○○ 심문 시작' 제목 띠.")]
        public Sprite interrogationBand;

        [Header("UI 스킨 — 타이틀 메뉴")]
        [Tooltip("타이틀 메뉴 버튼 — 보통 / 마우스를 올렸을 때(9-slice).")]
        public Sprite titleMenuNormal;
        public Sprite titleMenuHover;
        [Tooltip("마우스를 올린 메뉴 버튼 왼쪽에 나타나는 펜촉 표식.")]
        public Sprite titleMarker;

        [Header("타이틀 화면")]
        [Tooltip("타이틀 화면 배경. 화면을 꽉 채우도록 깔린다(비율이 달라도 빈 띠 없이 가장자리만 잘림). 위아래 자리는 아래 offset 으로.")]
        public Sprite titleBackground;
        public Placement titleBackgroundPlace = Placement.Stretch;

        [Tooltip("타이틀 애니메이션 배경(영상, .mp4 등). 넣으면 위 그림 대신 영상이 반복 재생된다.")]
        public UnityEngine.Video.VideoClip titleBackgroundVideo;

        [Tooltip("타이틀 로고(투명 배경 PNG). 화면 왼쪽 위 하늘 자리에 놓이고, 약한 빛이 비스듬히 지나간다. 자리 · 크기는 아래 offset · scale 로.")]
        public Sprite titleLogo;
        [Tooltip("로고 자리 — offset 은 기본 자리(왼쪽 위)에서 옮기는 양(1920×1080 기준 px), scale 은 배율.")]
        public Placement titleLogoPlace = Placement.Fit;

        [Tooltip("타이틀 날씨 효과(비 · 눈 · 안개 등)를 담은 프리팹. UI(Image/RawImage 등)로 만들면 배경과 로고 사이에 깔린다. 비우면 없음.")]
        public GameObject titleWeather;

        [Tooltip("타이틀 화면의 레트로 타자기. 약 608×384, 가로 2/3 지점의 지평선 위.")]
        public Sprite titleTypewriter;
        public Placement titleTypewriterPlace = Placement.Fit;

        [Header("커서 (Art/Cursors 폴더에 넣으면 자동으로 Cursor 타입, 32×32)")]
        public Texture2D penCursor;
        public Texture2D eyeCursor;
        public Texture2D eyeCursorBlink;
        public Texture2D reactionCursor;

        [Tooltip("커서 그림의 클릭 지점 (왼쪽 위 기준 픽셀).")]
        public Vector2 penHotspot = Vector2.zero;
        public Vector2 eyeHotspot = new Vector2(16f, 16f);
        public Vector2 reactionHotspot = new Vector2(16f, 16f);

        public Sprite StageOrRoom => stageBackground != null ? stageBackground : roomBackground;
        public Sprite SepiOnStage => sepiInterrogation != null ? sepiInterrogation : sepiTalk;
        public Sprite CatOnTalk => catTalk != null ? catTalk : catStage;
        public Sprite SepiOnTalk => sepiTalk != null ? sepiTalk : sepiInterrogation;
        public bool HasSepiLive2D => useSepiLive2D && sepiLive2D != null;
    }
}
