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

        [Header("타이틀 화면")]
        [Tooltip("타이틀 화면 배경 1920×1080.")]
        public Sprite titleBackground;
        public Placement titleBackgroundPlace = Placement.Stretch;

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
    }
}
