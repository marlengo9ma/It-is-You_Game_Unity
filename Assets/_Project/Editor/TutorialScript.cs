using System.Collections.Generic;
using ItIsYou.Data;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 튜토리얼 챕터의 시나리오 스크립트. 노션 〈시나리오〉 〈시스템 기획 및 화면 전환〉 문서의 원문을 그대로 옮긴 것이다.
    ///
    /// 문서의 구획이 그대로 메서드 하나가 된다:
    ///   [게임플레이 배경_캐릭터 씬] → [일러스트 컷씬#1] → [게임플레이 배경_캐릭터 씬]
    ///   → &lt;화면 전환&gt; 사건 진행 #1 → 단서 수집 → 용의자 선택 → 세피 심문
    ///
    /// 문장은 손대지 않는다. 표현을 다듬고 싶어지는 자리가 있더라도,
    /// 그건 이 파일이 아니라 노션 쪽에서 고쳐야 한 벌만 존재하게 된다.
    /// TSV 임포터가 완성되면 이 파일의 역할은 임포터가 가져간다.
    /// </summary>
    public static class TutorialScript
    {
        // ================================================================ 사건 이전

        /// <summary>[게임플레이 배경_캐릭터 씬] — 기차에 오른 뒤, 사건 발생 전.</summary>
        public static DialogueSequence PreIncidentTalk() => Seq("사건 이전 — 객실",
            N("기차에 올라타고 약간의 시간이 흐른 뒤, 당신은 다소 후회스러운 얼굴을 하고 있다."),
            N("소녀는 당신의 선물이 마음에 들었는지 휴대용 타자기를 계속 두들기며 조잘 조잘 떠들고 있다. " +
              "당신의 행동으로 인해 긴장이 풀려 거리감이 가까워졌거나, 본래 이런 성격이거나, 둘 중 하나임은 분명하다."),
            A("…그래서 본부 기록에는 수사관님이 완전 무섭게 묘사되어 있더라고요! " +
              "선배님들이 남긴 기록을 보면, 수사할 때 사람이 180도씩 180번 바뀐다면서요? " +
              "악령이 씌었다는 소문도 있고, 심문할 때는 상냥하다가 갑자기 소리를 질러서 " +
              "용의자가 기절한 적도 한두 번이 아니라면서요! ",
              ExpressionId.Smile),
            P("…"),
            N("당신은 소녀의 조잘거림에 질려가면서도 전 파트너가 복귀하면 쓴소리를 좀 해야겠다고 마음 먹는다."),
            A("그런데 이게 무슨 일! 무뚝뚝한 듯 보이지만 사실은 이렇게 센스넘치고 귀여운 선물을 " +
              "준비해주시는 마음 따듯한 수사관님이셨을 줄은 몰랐지 뭐예요?",
              ExpressionId.Smile),
            P("…그만. 잠시 일 좀 보고 오겠습니다."),
            A("..큼큼, 네~", ExpressionId.Nervous),
            N("당신은 간지러운 분위기를 이기지 못 하고 급하게 자리에서 일어났다. " +
              "그런 분위기를 느꼈는지, 소녀도 조금은 진정하려는 것처럼 자신의 옷매무새를 다듬으며 발랄하게 대답한다."));

        /// <summary>[일러스트 컷씬#1] — 화장실 칸, 주인공 독백과 세계관 설명.</summary>
        public static DialogueSequence Cutscene() => Seq("일러스트 컷씬#1 — 주인공 독백",
            I("…"),
            N("당신은 기차 내의 화장실 칸으로 향하고, 이내 세면대의 거울 앞에 서서 한숨을 내쉰다. " +
              "당신은 사실 소녀가 가진 능력에 대해 약간의 의구심을 품고 있다. " +
              "지나치게 어린 나이에 현장에 발령된 것으로도 모자라, 소녀가 가진 이력 역시 과하게 부풀려졌다고 느꼈기 때문이다."),
            N("특히 특기란에 적혀있던 ‘순간 기억 능력’ 에 공인까지 붙어있는 경우는 당신으로서도 처음 보는 것이다."),
            I("무언가 높은 사람의 뒷배라도 있는 건가?"),
            N("충분히 있을 만한 이야기이다."),
            N("하지만 서류상이라면 모를까, 실제 사건 현장에서는 허위 기재는 엄연한 징계대상이자, " +
              "확실히 하지 않으면 도리어 위험요소가 될 수 있기에 이는 절대로 있어서는 안 되는 일이다."),
            P("…이름이 뭐라고 했지? 세라?"),
            N("당장은 서류가 없으니 확인 할 방도는 없지만 왠지 입에 잘 안 붙은 이름이였다."),
            P("뭐, 정 안 되면 현장에서 인력 교체 요청을 하면 되니."),
            N("‘그 정도야 뭐, 한 번쯤은 괜찮겠지.’ 생각하며 당신은 옷매무새를 다듬은 뒤 자리로 돌아갈 채비를 마쳤다."),
            N("물론 이런 여유는 당연하다. 당신은 이 시대에 가장 각광 받는 직업을 가진 사람이니까."),
            N("‘표정 심리 수사관‘."),
            N("현재 당신이 가진 직업이자, 최근 새롭게 만들어진 수사 기법인 ‘표정 행동 해부학’ 을 " +
              "주력으로 각종 사건에 대한 수사를 진행하는 특수 직업이다."),
            N("최근 급증하는 신종 범죄에 맞서기 위해 다양한 실험적 기법을 시도하는 수사팀이 꾸려졌고, " +
              "그중 '과학 수사'는 대중에게 '이단'으로 취급받았다. " +
              "지문이나 혈흔 같은 보이지 않는 증거는 사람들에게 마녀사냥처럼 불합리하게 느껴졌다."),
            N("반면, '표정 행동 해부학'은 달랐다. 타인의 미세한 표정과 행동을 통계학적인 수치와 근거로 " +
              "분석하여 자백을 끌어내는 방식은 직관적이고 설득력 있었다. " +
              "특히, 앞서 과학 수사팀이 잘못 지목했던 범인을 표정 수사로 바로잡았던 사건은 " +
              "이 새로운 수사 기법에 대한 절대적인 신뢰를 구축했다."),
            N("그렇게 당신처럼 타인의 표정과 행동을 읽어내는 데 천부적인 재능을 가진 이들은 " +
              "엄중하고 까다로운 선발 과정을 거쳐 '표정 행동 수사관'으로 임명되었다."),
            N("젊은 나이에 높은 검거율과 뛰어난 능력을 인정받은 당신은, " +
              "그렇게 언제나 처럼 또 다시 새로운 사건을 맡게 되었다."));

        /// <summary>[게임플레이 배경_캐릭터 씬] — 복도를 지나 자리로 돌아가는 길.</summary>
        public static DialogueSequence AfterCutsceneTalk() => Seq("사건 이전 — 복도",
            P("…그러니까 조심 좀 하라니까."),
            N("당신은 복도를 통해 객실 방향으로 걸어가며 앞선 사건까지 함께 했었던, " +
              "말주변은 없지만 일에는 열정이 넘쳤던 전 보조 수사관을 생각했다."),
            N("그는 바로 이전 사건에서 심문 도중에 도주하려던 용의자를 추적하는 도중, 마차에 치여 부상을 입게 되었다."),
            N("본부에서는 그런 그를 대신하여, 아까의 소녀를 당신의 보조 수사관으로 배치해 주었고, " +
              "함께 이 기차에 오르게 된 것이다."),
            P("아무렴, 조용히만 갔으면 좋겠군요."),
            N("약간의 바램을 담은 혼잣말은 안타깝게도 좌석에 도착과 함께 거부 당해버렸지만 말이다."));

        // ================================================================ 사건 진행

        /// <summary>&lt;화면 전환&gt; 사건 진행 #1 — 사라진 만년필과 두 명?의 용의자.</summary>
        public static DialogueSequence Incident() => Seq("사건 진행 #1 — 사라진 만년필",
            P("…그래서 이게 무슨 일이죠?"),
            N("당신은 다소 엉망이 된 테이블과 주변 바닥, 그리고 어질러진 주변을 정돈하며 " +
              "일부로 당신의 눈을 피하는 듯한 소녀와, 왜 인지는 모르겠지만 테이블 위에서 " +
              "만족스러운 표정으로 식빵을 굽고 있는 검은 고양이 한 마리를 마주했다."),
            A("하하… 어서 오세요, 수사관님. 금방 정리할게요. 조금만 기다려주세요!", ExpressionId.Nervous),
            C("웨옹."),
            N("아무래도 이 난장판이 된 상황은 저 고양이의 등장과 연관이 있겠거니 싶어보였다."),
            N("당신은 버릇처럼 미간을 문지르며, 소녀를 도와 자리를 정돈하기 시작했다."),
            I("입 밖으로도 꺼냈으면 안 되는 거였나."),
            N("당신은 방금 전에 혼잣말로 빌었던 바램을 생각하며, 이전에 동료들과 이야기 나누었던 " +
              "‘징크스’나 ‘플래그’와 같은 심리적 현상에 대한 이야기를 떠올렸다."),
            N("그러는 와중, 아까 서류 뭉치와 함께 테이블에 올려놓고 갔던, -고급 만년필-이 보이지 않음을 알아차린다."),
            N("기차에 탑승하기 전, 잠깐 사용했던 그 만년필은 수사관 임명 당시에 선물 받은 물건으로, " +
              "클래식하지만 수려한 외형과 필기감 및 코너링이 뛰어나고…(생략)"),
            N("아무튼 고급진 외형 만큼이나 좋은 퀄리티의 만년필이기에 눈에 띌 수 밖에 없는 물건이다."),
            N("꽤나 애용하던 만년필이기도 한 만큼, 소녀에게 행방을 물어보려 뒤쪽을 바라보는 순간,"),
            N("당신은 수사관으로서의 ‘감’을 느꼈다."));

        // ================================================================ 단서 수집

        /// <summary>수집률 판정 대사. "수집한 단서와 수집 요소들의 누락된 갯수에 따라서 대사가 일부 바뀜."</summary>
        public static DialogueSequence GatePass() => Seq("수집 100%", I("음, 이 정도면 충분하겠군."));
        public static DialogueSequence GateWarn() => Seq("수집 70% 이상", I("필요한 건 찾은 것 같긴 한데..조금만 더 살펴볼까?"));
        public static DialogueSequence GateBlocked() => Seq("수집 부족", I("뭔가 부족해. 조금 더 꼼꼼하게 봐야겠어."));

        /// <summary>
        /// 〈시스템 기획 및 화면 전환〉의 튜토리얼 오브젝트 툴팁 6종. 원문 그대로.
        /// &lt;hl&gt; 로 감싼 구간은 열람창에서 눌러 '단서 오브젝트'로 승격하는 자리 — 노션에는 강조 표시가 없어
        /// 여기서 정한 것이므로, 노션에서 강조를 정하면 이 구간만 옮긴다.
        /// </summary>
        public static class Tooltips
        {
            public const string Ribbon =
                "당신이 선물용도로 구매했던 휴대용 타자기에 약간(?)의 정성과 성의의 표시를 위해 별도로 구매했던 붉은색의 고급 리본이다.\n" +
                "물론 포장 리본을 묶을 줄 몰랐던 당신은 여러 번의 시도 끝에 금새 포기했지만. \n" +
                "기차역 테이블에 두고 온 줄 알았으나, 조수가 타자기를 가져오면서 같이 챙긴 듯 하다.\n\n" +
                "지금은 일부분이 액체에 의해 얼룩졌으며, <hl>드문드문 헤진 곳과 고양이 털이 박혀있는 것</hl>이 보인다. " +
                "아무래도 처음 구매했었던 상태와는 컨디션이 많이 달라져 있다.";

            public const string Coffee =
                "당신이 기차에 올라, 새로이 시켰던 커피의 말로이다.\n" +
                "다행히 잔에 금이 가거나 깨져 보이지는 않지만 그 안에 있던 내용물의 행방은 " +
                "객실내 테이블 위의 각종 신문, 서류들과, 주변 바닥에서 확인이 가능했다.\n\n" +
                "아마도 누군가가 급하게 움직이면서 실수를 했거나, <hl>자유분방하고 빠르게 움직이는 무언가에 의해 쏟아졌을 것이다.</hl>";

            public const string Glimpse =
                "어디서 많이 본 듯한 형태의 일부분. 지금은 고양이의 아래에 깔려있다.\n" +
                "<hl>아마 당신이 찾고있는 물건의 일부분 일지도 모른다.</hl>";

            public const string Newspaper =
                "기차역에서 구매했던 오늘자 신문이다. \n" +
                "펼쳐진 신문의 페이지의 한 쪽에는 최근 대두되는 신종 범죄 사건에 관련된 이야기를 다루고 있다.\n\n" +
                "최첨단 기술이라고 자부하던 과학 수사, 죄 없는 이에게 사형을?\n\n" +
                "최근 급진적인 산업의 발달로 인해서 우리들은 귀족 평민 할 것 없이 일상의 새로움을 느끼고 있음은 아무도 부정하지 못할 것이다. " +
                "말 대신 돌을 넣어서 철길 위를 달리는 무쇠로 된 마차와 불안했던 어두운 골목에 불빛을 켜준 가로등 등, " +
                "우리들은 일상생활에서 점점 편리와 편안을 보다 쉽게 느낄 수 있게 되었다. \n" +
                "하지만 동시에 이런 기술의 발전을 이용한 새로운 방식의 범죄들도 생겨나기 시작하며, 귀족은 귀족대로, 일반 시민도 시민대로 이런 신종범죄에 노출되어 버렸다. \n" +
                "그래도 우리의 대단한 경찰분들은 시민의 불안함을 해소하고자, 범죄 수사관련해서도 여러 새로운 방식을 연구하였고 " +
                "이에 따라 다양한 실험적인 수사팀이 꾸려져 있는 것에 대해서는 이제는 모두가 알 것이라 생각한다. \n" +
                "허나 혈흔 검사, 지문 감식 등, 평민은 물론 귀족에게 조차 생소한 ‘과학’이라는 분야가 발달하면서 새롭게 생겨난 과학수사 방식은, 기존의 수사방법과는 차이가 컸다.\n" +
                "이 혈액 검사나 지문 감식 등의 그들이 말하고 있는 ‘과학적 수사 방식‘들은 우리들에겐 그저 몰아가기 식의 마녀 사냥과 같다. " +
                "기득권층의 명목을 내새우기 위해 그럴 듯하게 보이는 내용만을 가지고 처벌을 하는 것은 불평등하고 뭔가 잘못되었다.\n" +
                "심지어 콧대 높으신 귀족들도 그렇게 생각할 정도면 더욱이 잘못된 것이 아닐까.\n" +
                "피는 같거나 비슷한 사람도 있다면서 손가락 끝에는 수 만명의 사람마다 각 고유의 다른 모양이 있다고 하는 것도 믿기 어려운데, " +
                "얼마 전에는 이 ‘지문 검식’ 수사 방법을 사용하던 수사팀이 엄한 사람을 범인으로 지목해서 사형시키뻔 하기도 했다!\n\n" +
                "이렇게 수사 과정에서 오류도 있는 방법은 그들만의 좋은 핑계거리라고 밖에 생각이 들지 않는다. " +
                "눈에 잘 보이지도 않는 걸 증거로 삼아 범인으로 이야기하는 것도 웃긴데, 모르는 사람 입장에서는 그럴듯 한 말을 이유로 지어내서 " +
                "엄한 사람을 범인으로 몰아가는 것처럼밖에 안 느껴지는 건 과연 우리의 탓일까?\n" +
                "그에 비해서 우리들의 영웅, ’표정 행동 수사팀’은 달랐다! \n" +
                "직관적인 검시 및 법의학 경험과 구색, 선조에서 부터 내려온 경험들과 과거의 통계 수치를 활용한 심문과 변호는 " +
                "무고한 자의 결백을 밝히거나 지독한 범죄자에게서 자백을 이끌어내기도 효율적이다!\n" +
                "심지어 위의 지문 감식 수사 방식을 가지고 무고한 이를 범인 지목을 했던 사건에서도 ‘표정 행동 수사과’에서 제대로 된 범인에게서 자수를 받아 해결하지 않았는가!\n" +
                "나는 이 표정 행동 수사 방식이야말로 오히려 그들이 말했던 ‘과학’이라는 것에 더욱 가까운게 아닐까 필자는 생각한다.\n" +
                "(이하 생략)";

            public const string Typewriter =
                "당신이 새롭게 배치된 보조 수사관에게 선물한 유명한 브랜드의 휴대용 타자기이다.\n" +
                "고급진 전용 케이스 가방도 있으며, 휴대하기 편하게 가볍게 개량된 나름 중상급의 타자기로 " +
                "초면의 인물에게 주는 선물로는 조금 과한 감이 있었나 싶은 생각이 든다.\n" +
                "그새에 사용해보았는지 고통 주제를 찾을 수 없는 여러 잡다한 글들이 적혀있다.";

            public const string RoomInfo =
                "당신과 새로운 조수가 머물 객실에 대한 정보이다.\n" +
                "장기 운행용 기차의 객실 번호로, 1번부터 4번까지는 고급 객실이고, 5번부터 16번까지는 일반 객실로, " +
                "객실마다 공간 여유와 서비스의 차이가 존재한다고 한다.\n" +
                "4번 객실과 5번 객실 사이에는 누구나 이용 가능한 매점 및 칵테일 바 등, 장기 운행에 필요한 생필품들의 조달과 유흥을 위한 오락 및  편의 시설이 존재한다. " +
                "당신은 수사 본부를 통해 해당 기차의 3번 객실 티켓을 지원 받았다. ";
        }

        /// <summary>오늘자 신문을 살펴본 뒤의 대사. 툴팁의 &gt;&gt; 줄.</summary>
        public static List<DialogueLine> NewspaperInspect() => new List<DialogueLine>
        {
            I("좋게 봐주는 건 고맙지만 오히려 이런 조명 때문에 관련 시험들이 깐깐해져서 신입들이 잘 안 들어온단 말이죠."),
            I("어서 빨리 인원 충당이 되어야지 이런 장기 출장도 없어질텐데…"),
            N("당신은 작은 한숨을 쉬며, 신문을 덮어 해당 기사를 외면했다."),
        };

        /// <summary>객실 정보를 살펴본 뒤의 대사. 툴팁의 &gt;&gt; 줄.</summary>
        public static List<DialogueLine> RoomInfoInspect() => new List<DialogueLine>
        {
            I("하마터면 못 탈뻔 했지만 말이죠."),
            N("당신은 옆에서 꼼지락 거리며 정돈을 하고 있는 조수를 잠시 바라봤다가 시선을 거둔다."),
        };

        // ---------------------------------------------- [임시] 수집 시 대사 (원문 없음)
        // 노션 원문에는 이 네 오브젝트의 >> 줄이 없다. 아래는 이전 버전에서 임의로 지어 넣은 대사로,
        // 제작자 확인 후 '그대로 남겨 두기'로 결정된 것. 원문이 생기면 교체한다.

        /// <summary>[임시] 리본 끈을 처음 살펴볼 때.</summary>
        public static List<DialogueLine> RibbonInspect() => new List<DialogueLine>
        {
            I("…기차역 테이블에 두고 온 줄 알았던 리본이군."),
            A("아, 그거 제가 타자기랑 같이 챙겨왔어요! 잘했죠?", ExpressionId.Smile, "(뿌듯한 표정)"),
            I("…원래 이런 상태는 아니었는데."),
        };

        /// <summary>[임시] 쏟아진 커피를 처음 살펴볼 때.</summary>
        public static List<DialogueLine> CoffeeInspect() => new List<DialogueLine>
        {
            I("방금 시킨 커피다. 잔은 멀쩡한데 내용물이 전부 밖에 있군."),
            A("그, 그건… 제가 치우려던 참이었어요.", ExpressionId.Nervous, "(시선을 피하며)"),
        };

        /// <summary>[임시] 고양이 아래의 무언가를 처음 살펴볼 때.</summary>
        public static List<DialogueLine> GlimpseInspect() => new List<DialogueLine>
        {
            I("저 고양이, 아까부터 저 자세 그대로군."),
            Line(SpeakerId.Cat, "…웨옹.", ExpressionId.Sly, "(꼬리를 살랑이며 자세를 고쳐 앉는다)"),
            I("발 아래로 뭔가 비죽 나와 있다."),
        };

        /// <summary>[임시] 휴대용 타자기를 처음 살펴볼 때.</summary>
        public static List<DialogueLine> TypewriterInspect() => new List<DialogueLine>
        {
            I("선물로 준 휴대용 타자기다. 벌써 자기 물건처럼 쓰고 있군."),
            A("이거 정말 좋아요! 속기할 때 손목이 하나도 안 아파요.", ExpressionId.Smile),
        };

        // ================================================================ 용의자 선택

        /// <summary>단서 수집이 끝나고, 두 사람을 자리에 앉히기까지.</summary>
        public static DialogueSequence BeforeSuspectSelect() => Seq("두 용의자를 자리에 앉히다",
            N("당신은 대부분 정돈이 되었으나, 어딘가 어색하게 주변을 정돈하고 있던 조수를 나즈막히 불러 " +
              "멈추게 하고 고양이가 엎드려있는 테이블 자리에 앉아 소녀를 부른다."),
            P("보조 수사관? 잠시 이리로 와 앉아보도록."),
            A("..네? 저, 저요?", ExpressionId.Surprised),
            N("그런 분위기 탓일까? 늘어지게 하품만 하던 검은 고양이가 스윽하고 당신의 눈치를 보더니, " +
              "마치 어딘가로 도망가려는 듯 자세를 일으키려 한다."),
            N("당신은 그런 고양이에게 조차, 마치 ‘너도 가만히 있어’라는 듯이 날카롭게 째려보았고"),
            N("그런 당신의 카리스마에 무서움을 느꼈는지 당신의 눈치를 보고는 애초부터 움직일 생각이 " +
              "없었다는 듯이 다시 자세를 고쳐 앉았다."),
            P("…"),
            Line(SpeakerId.Cat, "…..먁."),
            P("그래, 너도 포함이야"),
            N("그렇게 두 용의자(?)를 자리에 앉힌 뒤, 당신은 진지한 눈으로 재차 주변과 두 사람을 " +
              "흝어보며 말을 꺼냈다."));

        /// <summary>
        /// step.1 — 용의자 선택 화면에서 먼저 오가는 대화.
        ///
        /// 원문의 &lt;움찔하는 두 캐릭터&gt; 같은 연출 지시는 지문 칸으로 옮겼다.
        /// 본문에 꺾쇠를 그대로 두면 TMP 가 서식 태그로 읽어 통째로 지워 버린다.
        /// </summary>
        public static DialogueSequence SuspectSelectTalk() => Seq("두 용의자 앞에서",
            Line(SpeakerId.Protagonist, "그래서 누구죠? 제 만년필을 가져간 녀석이",
                ExpressionId.Neutral, "(움찔하는 두 캐릭터)"),
            A("네? 만,만년필이요? 저는 당연히 어지른 거를 혼내실 줄 알았어요.", ExpressionId.Flustered),
            Line(SpeakerId.Cat, "…뭬웅."),
            P("뭐, 어지럽인건 이 고양이가 들어온게 원인일 거라고 생각했는데, 제가 틀린건가요?"),
            A("그으…건 그렇죠", ExpressionId.Nervous, "(눈을 피하며 땀이 삐질)"),
            Line(SpeakerId.Cat, "", ExpressionId.Sly, "(고개 피함/대충 꼬리 살랑)"));

        // ================================================================ 세피 심문
        // 2026-10-11 노션 최신본. 같은 선택지를 다시 골라도 주인공이 하는 말이 매번 다르고,
        // 친근↔강압을 갈아타는 순간(교차) 그 회차가 통째로 다른 내용이 된다.
        //   · 1번째 교차 — "B선택지 이후, 선택 시" / "A선택지 이후 선택 시"
        //   · 2번째 교차 — "A선택지 > B선택지 이후, A선택지 재 선택 시" 등 → 신뢰도 하락으로 심문 강제 종료
        // B선택지의 2·3회 연속은 초회 뒤의 <시간 경과_a> 가 실제로 나왔는지에 따라 또 갈린다.
        // 그 구조는 ChoicePass 한 칸이 담는다 — 목록 문구 · 대사 · <시간 경과> · 연출 · 조건이 함께 들어 있다.

        /// <summary>&lt;용의자 ‘세피’ 심문 시작&gt; — 첫 마디. 이 뒤에 튜토리얼 팝업 1.</summary>
        public static DialogueSequence SepiOpening() => Seq("세피 심문 시작",
            A("하하, 만년필이라…주변에 떨어지지 않았을까요?", ExpressionId.Smile));

        /// <summary>
        /// &lt;시간 경과&gt; 회차별 반응. 고른 선택지에 그 회차용 &lt;시간 경과&gt; 가 없을 때 나온다.
        /// 3회차가 끝나면 [신뢰도 하락 및 게임 오버에 대한 경고와 설명 튜토리얼 팝업]
        /// (미출현 시 추후 선택지로 인한 설명에서 재출현 — 한 번만 뜬다).
        /// </summary>
        public static List<DialogueSequence> SepiIdleRounds() => new List<DialogueSequence>
        {
            Seq("시간 경과 1회차",
                A("제,제가 신경써서 조금 더 찾아볼테니까요!", ExpressionId.Nervous, "(당신의 기분을 살핀다)")),
            Seq("시간 경과 2회차",
                A("…아니면 새로 사시는 건 어때요? 아까 보니까 기차내 매점에서 다양한 잡화도 팔던 걸요?",
                  ExpressionId.Smile)),
            Seq("시간 경과 3회차",
                A("…저기, 수사관님? 괜찮으신거죠…?", ExpressionId.Nervous)),
        };

        // ------------------------------------------------------------ A선택지
        // 감성적으로 경계를 낮추며 진행. 회차가 올라갈수록 주인공이 집요해지고, 조수가 질려 간다.

        public const string ChoiceALabel =
            "내가 매우 아끼는 만년필이라. 그러니 행방을 꼭 알려주었음 합니다.";

        private const string ChoiceALabel2 =
            "정말로 주변에 없었나요? 워낙 화려한 디자인이라 못 봤을 것 같지는 않아서요.";
        private const string ChoiceALabel3 =
            "…진짜로 모르겠나요? 제 기억상으로는 테이블 위의 서류철 위에 두고 간거로 기억나서요.";
        private const string ChoiceALabel4 = "정말, 진짜로요?";
        private const string ChoiceALabel5 = "정말 정말 진짜 진짜로요??";
        private const string ChoiceALabel6 = "정말, ㅈ…";

        /// <summary>1번째 교차 — B선택지 이후에 A를 고르면 이 말로 바뀐다.</summary>
        private const string ChoiceALabelAfterB1 = "후우, 미안합니다. 내가 매우 아끼는 만년필이라 조금 흥분을 했군요.";
        private const string ChoiceALabelAfterB2 = "혹 만년필의 행방에 대해서 아는 게 있다면 꼭 알려주었음 합니다.";

        /// <summary>2번째 교차 — A &gt; B 이후 A를 다시 고르면 이 말로 바뀐다. 신뢰를 잃는다.</summary>
        private const string ChoiceALabelSecondCross = "아, 여러모로 이해합니다. 혹시 제가 가진 다른 물건과 교환하지 않겠습니까?";

        /// <summary>A선택지의 칸 목록 — 보통 회차 6 + 교차 칸 2.</summary>
        public static List<ChoicePass> ChoiceARounds() => new List<ChoicePass>
        {
            Round(ChoiceALabel, ChoiceAPass(), ChoiceAIdle()),
            Round(ChoiceALabel2, ChoiceAPass2()),
            Round(ChoiceALabel3, ChoiceAPass3(), ChoiceAIdle3()),
            Round(ChoiceALabel4, ChoiceAPass4()),
            Round(ChoiceALabel5, ChoiceAPass5()),
            Round(ChoiceALabel6, ChoiceAPass6()),
            // "B선택지 이후, 선택 시" — 이후 위와 동일(<시간 경과> 도 초회의 것).
            Alternation(1, ChoiceALabelAfterB1 + " " + ChoiceALabelAfterB2, ChoiceAAfterB(), ChoiceAIdle()),
            // "A선택지 > B선택지 이후, A선택지 재 선택 시" — [신뢰도 하락으로 인한 ‘세피‘ 심문 강제 종료]
            Alternation(2, ChoiceALabelSecondCross, ChoiceASecondCross(), breaksTrust: true),
        };

        /// <summary>A 초회 — 봤다는 것까지는 인정한다. 밑줄 구간이 증언 단서가 된다.</summary>
        public static DialogueSequence ChoiceAPass() => Seq("A 초회 — 봤다는 것까지는 인정",
            P(ChoiceALabel),
            A("사,사실은… <clue=TES_SAW_IT>아까 봤던 것 같긴 한데요…</clue>",
              ExpressionId.Nervous, "(고양이와 당신을 번갈아보며 고민하는 모습)", StageAnimation.GlanceCat),
            P("오, 다행이네요. 그래서 어디서 봤을까요?"),
            A("음…역시 잘 모르겠어요. 죄송해요, 수사관님…", ExpressionId.Sad,
              "(고민하는 듯 눈을 감고 생각하는 듯 하다가, 작은 한숨 후, 미안하다는 듯 옅은 미소를 지으며)",
              StageAnimation.Ponder),
            I("거짓말을 하는 것 같군…이유는 모르겠지만서도 일단 만년필을 모르는 반응은 아니야."));

        /// <summary>A 초회 뒤의 &lt;시간 경과&gt;.</summary>
        public static DialogueSequence ChoiceAIdle() => Seq("A 초회 — 시간 경과",
            A("…혹시 무슨 문제라도 있을까요…?", ExpressionId.Nervous, "(계속 눈치를 보며)"));

        public static DialogueSequence ChoiceAPass2() => Seq("A 2회 — 알긴 알지만 모른다",
            P(ChoiceALabel2),
            A("저도 수사관님께서 무슨 만년필을 이야기하시는 건지는 알 것 같아요. 하지만… 역시 지금 어디에 있는 지에 대해서는 잘 모르겠어요…",
              ExpressionId.Nervous, "(살짝 눈을 피하며)", StageAnimation.LookAway),
            P("흐음…그렇군요, 우선 알겠습니다."),
            A("자리 비우실 때에 가져가셨거나 하신 건 아니시죠? 있다가 복도 쪽도 한 번 찾아볼게요!",
              ExpressionId.Smile));

        public static DialogueSequence ChoiceAPass3() => Seq("A 3회 — 분명히 봤었다",
            P(ChoiceALabel3),
            A("계속 이야기하지만… 저도 아까는 분명히 봤었어요. 그래도 지금은 모르겠는걸요.",
              ExpressionId.Nervous, "(약간 고개는 피하는 듯, 하지만 한숨을 쉬며)", StageAnimation.Sigh),
            P("하아…그렇군요. 미안합니다."),
            A("아,아니에요! 그 만큼 아끼시는 걸테니…", ExpressionId.Surprised,
              "(사과에 놀라며) (미안한 마음인 듯)", StageAnimation.Startle));

        /// <summary>A 3회 뒤의 &lt;시간 경과&gt; — 처음으로 고양이를 입에 올린다.</summary>
        public static DialogueSequence ChoiceAIdle3() => Seq("A 3회 — 시간 경과",
            A("…호,혹시라도 저 고양이가 가져갔다거나…?", ExpressionId.Nervous,
              "(계속 눈치를 보다가)", StageAnimation.GlanceCat));

        public static DialogueSequence ChoiceAPass4() => Seq("A 4회 — 저도 속상해요",
            P(ChoiceALabel4),
            A("…수사관님, 아쉬우신 마음은 이해하지만…계속 이러시면 저도 속상해요…", ExpressionId.Sad,
              "(살짝 질린 표정으로)"));

        public static DialogueSequence ChoiceAPass5() => Seq("A 5회 — 무언의 째려봄",
            P(ChoiceALabel5),
            A("…", ExpressionId.Angry, "(한심하다는 듯 째려보며)", StageAnimation.Glare));

        public static DialogueSequence ChoiceAPass6() => Seq("A 6회 — 조수가 화를 낸다",
            P(ChoiceALabel6),
            A("…저기요, 수사관님~~?", ExpressionId.Angry, "(화가 나는 듯 부들거리며)", StageAnimation.Fume),
            Line(SpeakerId.Protagonist, "아, 알겠습니다.", ExpressionId.Neutral, "(땀 삐질, 펼쳐지는 손)"));

        /// <summary>1번째 교차 — B선택지 이후에 고른 A. 태도가 바뀐 것을 조수가 먼저 알아챈다.
        /// 이 대사가 끝나면 [교차 선택으로 인한 신뢰도 하락 및 게임 오버에 대한 경고와 설명 튜토리얼 팝업].</summary>
        public static DialogueSequence ChoiceAAfterB() => Seq("A (B 이후, 1번째 교차) — 거짓과 진실을 섞는다",
            P(ChoiceALabelAfterB1),
            P(ChoiceALabelAfterB2),
            A("…처음 자리에 앉으셨을 때는… <clue=TES_SAW_IT>분명히 저도 봤던 것 같긴 한데요.</clue>",
              ExpressionId.Flustered, "(바뀐 태도에 조금 당황하고 긴장한 듯)", StageAnimation.Tremble),
            P("오, 다행이네요. 그래서 어디서 봤을까요?"),
            A("음…역시 그 이후로는 잘 모르겠어요. 도움이 못 되어서 죄송해요, 수사관님.", ExpressionId.Sad,
              "(고민하는 듯 눈을 감고 생각하는 듯 하다가, 고양이 쪽을 힐끗 바라보며)", StageAnimation.GlanceCat),
            I("거짓말과 진실을 섞어서 이야기하는 것 같군…이유는 모르겠지만서도 일단 만년필의 행방 자체를 모르는 반응은 아니야."));

        /// <summary>2번째 교차 — A &gt; B 이후 다시 A. 장난으로 받아들인 조수가 실망한다. 심문 강제 종료.</summary>
        public static DialogueSequence ChoiceASecondCross() => Seq("A (A>B 이후, 2번째 교차) — 신뢰를 잃는다",
            P(ChoiceALabelSecondCross),
            A("네? 그, 어…? 뭘 이해하신다는 말씀이실까요…?", ExpressionId.Flustered,
              "(당신의 행동이 이상하다는 듯)", StageAnimation.Startle),
            A("그리고 무엇을 교환하신다는 이야기죠?", ExpressionId.Flustered),
            P("음? 당신이 가진 만년필과 제가 가진 다른 물건들을 이야기하는 거죠?"),
            A("…진심이세요, 수사관님?", ExpressionId.Angry, "(당신의 이야기에 화가난 듯)", StageAnimation.Glare),
            A("아무리 제가 어려보여도 엄연한 정식 수사관이에요.", ExpressionId.Angry),
            A("상황상, 제가 의심될만 하니까 심문하시고 그런 것까지는 이해를 해요.", ExpressionId.Angry),
            A("하지만 그것과 별개로 저를 그렇게 얕잡고 장난스럽게 대하시는 건", ExpressionId.Angry),
            A("동료로서 너무하신 거 아닌가요?", ExpressionId.Angry),
            P("아뇨, 그러려던 의도는 없었습니다만…"),
            A("분명 말했지만 저는 그 만년필을 가지고 있지도 않고, 행방도 몰라요.", ExpressionId.Sad,
              "(화나고 속상한 듯한 표정으로)"),
            A("제가 그렇게 의심스러우시면 본부에 이야기해서 보조인력을 바꾸시는건 어떤가요?", ExpressionId.Sad),
            A("…수사관님을 존경했던 제 자신에게 실망스러워지네요.", ExpressionId.Sad, null, StageAnimation.LookAway),
            I("잘못 생각했나…"),
            I("…신뢰를 완전히 잃어버린 모양이군. 더 이상 심문을 이어가긴 어렵겠어."));

        // ------------------------------------------------------------ B선택지
        // 강압적으로 공격하듯 진행. 2·3회는 '연속' — 반대로 갈아타는 순간 교차가 되므로, 실제로도 늘 연속이다.
        // 2·3회 연속은 초회 뒤의 <시간 경과_a> 가 나왔는지에 따라 -A(미출현) / -B(출현) 로 갈린다.
        // 4회 연속이면 신뢰를 잃고 심문이 강제로 끝난다.

        /// <summary>B 초회·1번째 교차 뒤의 &lt;시간 경과_a&gt; 이름표.</summary>
        private const string IdleKeyBTimeA = "SEPI_B_TIME_A";

        public const string ChoiceBLabel =
            "아무리 그래도 그렇지 상사의 물건을 훔치다니, 초면부터 적잖이 얕보였나보군요?";

        private const string ChoiceBPressLine =
            "그러면 뭐죠? 제가 모르는 새에 본부에서는 보조 수사관 이수 과목에 ‘현장에서의 짐 관리’같은 과목이라도 포함되었었나 보죠?";

        private const string ChoiceBLabel2A1 = "저는 그리 좋은 사람은 아닙니다?";
        private const string ChoiceBLabel2A2 = "어서 빨리 행방을 이야기하는 것이 보조 수사관에게는 좋을 겁니다.";
        private const string ChoiceBLabel2B1 = "하지만 그렇더라도 저는 참을성이 그리 좋은 사람은 아닙니다.";
        private const string ChoiceBLabel2B2 = "억울한 부분은 이해하지만 저라면 오히려 솔직하게 이야기해서 어서 빨리 오해를 풀텐데요?";
        private const string ChoiceBLabel3A1 = "지금 저랑 장난하자는 겁니까?";
        private const string ChoiceBLabel3B1 = "앞뒤가 안 맞지 않습니까…";
        private const string ChoiceBLabel3Common = "하다못해 숨기는 것에 대해서 말을 못하는 이유가 뭐죠?";
        private const string ChoiceBLabel4 = "결국 당신이 가져간 범인이라는 거 아닙니까!";

        /// <summary>1번째 교차 — A선택지 이후에 B를 고르면 이 말로 바뀐다.</summary>
        private const string ChoiceBLabelAfterA = "아무래도 제가 적잖이 얕보였나 봅니다? 당당하게 거짓말을 내뱉고.";

        /// <summary>2번째 교차 — B &gt; A 이후 B를 다시 고르면 이 말로 바뀐다. 신뢰를 잃는다.</summary>
        private const string ChoiceBLabelSecondCross = "이래도 제가 좋은 사람으로 보입니까? 어서 빨리 실토하는 게 빠를겁니다?";

        /// <summary>B선택지의 칸 목록 — 보통 회차 4 + 회차 변형 2 + 교차 칸 2.</summary>
        public static List<ChoicePass> ChoiceBRounds() => new List<ChoicePass>
        {
            Round(ChoiceBLabel, ChoiceBPass(), ChoiceBIdleA(), IdleKeyBTimeA),
            Round(ChoiceBLabel2A1 + " " + ChoiceBLabel2A2, ChoiceBPass2A(), ChoiceBIdle2()),
            Round(ChoiceBLabel3A1 + " " + ChoiceBLabel3Common, ChoiceBPass3A(), ChoiceBIdle3()),
            Round(ChoiceBLabel4, ChoiceBPass4(), breaksTrust: true),
            // <시간 경과_a> 가 나왔다면 2·3회 연속의 말이 바뀐다.
            Variant(1, ChoiceBLabel2B1 + " " + ChoiceBLabel2B2, ChoiceBPass2B(), ChoiceBIdle2(), requiresSeenIdleKey: IdleKeyBTimeA),
            Variant(2, ChoiceBLabel3B1 + " " + ChoiceBLabel3Common, ChoiceBPass3B(), ChoiceBIdle3(), requiresSeenIdleKey: IdleKeyBTimeA),
            // "A선택지 이후 선택 시" — 대사 뒤 경고 팝업, 이어서 <시간 경과_a>.
            Alternation(1, ChoiceBLabelAfterA, ChoiceBAfterA(), ChoiceBIdleA(), IdleKeyBTimeA),
            // "B선택지 > A선택지 이후, B 선택지 재 선택 시" — [신뢰도 하락으로 인한 ‘세피‘ 심문 강제 종료]
            Alternation(2, ChoiceBLabelSecondCross, ChoiceBSecondCross(), breaksTrust: true),
        };

        public static DialogueSequence ChoiceBPass() => Seq("B 초회 — 당황, 그러나 거짓은 아님",
            P(ChoiceBLabel),
            A("엥? 그, 그게 아니라요, 수사관님…", ExpressionId.Flustered,
              "(당신의 적대적인 언행에 당황한 듯, 눈을 못 마주치며 동공이 떨리는 모습)", StageAnimation.Tremble),
            P(ChoiceBPressLine),
            A("아니에요, 저는 훔치거나 하지 않았어요! 제가 당황한 건 그런 이유가 아니라, 그저…", ExpressionId.Sad,
              "(당신의 공격적인 언행에 다소 당혹스럽지만 최대한 감정을 가다듬고는 억울한 표정을 지으며) " +
              "(고양이쪽을 힐끔 힐끔 바라보며 고민하는 모습)", StageAnimation.GlanceCat),
            I("거짓말을 하는 것 같지는 않은데… 뭔가 숨기는 것 같아 보이는군."));

        /// <summary>&lt;시간 경과_a&gt; — B 초회와 1번째 교차 B 뒤. 끝나면 [신뢰도 하락 및 게임 오버에 대한 경고와 설명].</summary>
        public static DialogueSequence ChoiceBIdleA() => Seq("B — 시간 경과_a",
            A("죄송합니다,수사관님. 하지만 저는 감히 남의 물건을 가지고 탐내거나 하지 않아요.",
              ExpressionId.Neutral, "(작은 한 숨을 쉬고는 감정이 정돈된 말투로)", StageAnimation.Sigh),
            A("제가 오해할 만한 행동을 했다고는 하지만 그렇게 생각하지 않아 주셨음 해요.",
              ExpressionId.Sad, "(또렷하게 주인공을 바라보며)"));

        /// <summary>B 2회 연속 -A — &lt;시간 경과_a&gt; 미출현.</summary>
        public static DialogueSequence ChoiceBPass2A() => Seq("B 2회 연속 -A (시간 경과_a 미출현)",
            P(ChoiceBLabel2A1),
            P(ChoiceBLabel2A2),
            A("그, 수사관님…죄송하지만 저도 만년필의 행방에 관해서는 아는 게 없어서요...", ExpressionId.Nervous,
              "(당신이 조금 낯설어진 듯 눈치를 보며)", StageAnimation.LookAway),
            P("하아… 그러면 무엇하러 제게 거짓말을 한거죠?"),
            A("그건…말할 수 없어요…", ExpressionId.Nervous,
              "(눈을 피하고 고양이쪽을 힐끗 바라보며)", StageAnimation.GlanceCat));

        /// <summary>B 2회 연속 -B — &lt;시간 경과_a&gt; 출현.</summary>
        public static DialogueSequence ChoiceBPass2B() => Seq("B 2회 연속 -B (시간 경과_a 출현)",
            P(ChoiceBLabel2B1),
            P(ChoiceBLabel2B2),
            A("그, 수사관님…죄송하지만 저도 만년필의 행방에 관해서는 아는 게 없어서요...", ExpressionId.Nervous,
              "(당신이 조금 낯설어진 듯 눈치를 보며)", StageAnimation.LookAway),
            P("하아… 그러면 무엇하러 제게 거짓말을 한거죠?"),
            A("그건…말할 수 없어요…", ExpressionId.Nervous,
              "(눈을 피하고 고양이쪽을 힐끗 바라보며)", StageAnimation.GlanceCat));

        /// <summary>B 2회 연속 뒤의 &lt;시간 경과&gt;.</summary>
        public static DialogueSequence ChoiceBIdle2() => Seq("B 2회 연속 — 시간 경과",
            A("…더 하시 말씀이라도 있으신가요…?", ExpressionId.Nervous, "(눈치를 보는 듯)"));

        /// <summary>B 3회 연속 -A — &lt;시간 경과_a&gt; 미출현.</summary>
        public static DialogueSequence ChoiceBPass3A() => Seq("B 3회 연속 -A (시간 경과_a 미출현)",
            P(ChoiceBLabel3A1),
            P(ChoiceBLabel3Common),
            A("정말이에요!… 어떤 것 때문에 제가 의심받는건지는 모르겠어요.", ExpressionId.Sad, "(억울한 듯 호소하며)"),
            A("제가 만년필을 가져갈 이유도, 만약에라도 행방을 알고 있다면…", ExpressionId.Sad),
            A("굳이 숨길 이유도 없는걸요.", ExpressionId.Sad),
            Line(SpeakerId.Protagonist, "그래도 예상이 가는 이유가 있기 때문에 숨기는 거 아닙니까!",
                ExpressionId.Neutral, "(책상을 내려치며)"),
            A("저는 그저 고양이랑 놀아준 것뿐이라고요!", ExpressionId.Flustered,
              "(모르겠다는 듯 질끈 눈을 감으며)", StageAnimation.Shrink),
            A("…아마 그 때 어디론가 튕겨나간 거 아닐까 싶긴하네요…", ExpressionId.Sad, "(입삐죽하며)"));

        /// <summary>B 3회 연속 -B — &lt;시간 경과_a&gt; 출현.</summary>
        public static DialogueSequence ChoiceBPass3B() => Seq("B 3회 연속 -B (시간 경과_a 출현)",
            P(ChoiceBLabel3B1),
            P(ChoiceBLabel3Common),
            A("…정말 말해야 될까요?", ExpressionId.Sad, "(억울하고 서운한 듯 이야기하며)"),
            A("제가 만년필을 가져갈 이유도, 만약에라도 행방을 알고 있다면…", ExpressionId.Sad),
            A("굳이 숨길 이유도 없는걸요.", ExpressionId.Sad),
            Line(SpeakerId.Protagonist, "그래도 예상이 가는 이유가 있기 때문에 숨기는 거 아닙니까!",
                ExpressionId.Neutral, "(책상을 내려치며)"),
            A("저는 그저 고양이랑 놀아준 것뿐이라고요!", ExpressionId.Flustered,
              "(모르겠다는 듯 질끈 눈을 감으며)", StageAnimation.Shrink),
            A("…아마 그 때 어디론가 튕겨나간 거 아닐까 싶긴하네요…", ExpressionId.Sad, "(입삐죽하며)"));

        /// <summary>B 3회 연속 뒤의 &lt;시간 경과&gt;.</summary>
        public static DialogueSequence ChoiceBIdle3() => Seq("B 3회 연속 — 시간 경과",
            A("…이제 된거죠?", ExpressionId.Nervous, "(눈치를 보는 듯)"));

        /// <summary>B 4회 연속 — 조수가 정색한다. 심문 강제 종료.</summary>
        public static DialogueSequence ChoiceBPass4() => Seq("B 4회 연속 — 신뢰를 잃는다",
            P(ChoiceBLabel4),
            A("네? 제가 가져간게 아니라고 몇 번을 말씀드려요!", ExpressionId.Angry,
              "(억울하고 화가 난 듯 소리치며)", StageAnimation.Fume),
            A("놀리시는 것도 아니고 정말 너무하시네요, 수사관님.", ExpressionId.Angry),
            A("제가 아무리 의심된다고 해도 그렇지, 큰소리 내신다고 모든 일이 해결되나요?", ExpressionId.Angry),
            P("…"),
            A("하아… 분명 제가 어리다고 무시하시는 거겠죠.", ExpressionId.Angry, "(정색하며)", StageAnimation.Sigh),
            A("저도 수사관님과 똑같은 수사관입니다.", ExpressionId.Angry),
            A("여러 까다로운 테스트를 통해 수사관으로서 임명되었어요.", ExpressionId.Angry),
            A("그리고 아직 현장 경험이 적기 때문에 보조일 뿐,", ExpressionId.Angry),
            A("저도 엄연히 본부에서 정식으로 발령받은 보조 수사관입니다.", ExpressionId.Angry),
            A("놀리기 쉬운 어린애도, 수사관님의 심심풀이용 장난감도 아니라고요.", ExpressionId.Angry, null, StageAnimation.Glare),
            I("…신뢰를 완전히 잃어버린 모양이군. 더 이상 심문을 이어가긴 어렵겠어."));

        /// <summary>1번째 교차 — A선택지 이후에 고른 B. 대사가 끝나면 경고 팝업, 이어서 &lt;시간 경과_a&gt;.</summary>
        public static DialogueSequence ChoiceBAfterA() => Seq("B (A 이후, 1번째 교차) — 갑자기 바뀐 태도",
            P(ChoiceBLabelAfterA),
            A("네,네? 제가요? 그, 그게 아니라요, 수사관님…", ExpressionId.Flustered,
              "(당신의 갑작스럽게 바뀐 태도와 적대적인 언행에 당황한 듯, 눈을 못 마주치며 동공이 떨리는 모습)",
              StageAnimation.Tremble),
            Line(SpeakerId.Protagonist, "표정 행동 수사관 앞에서 거짓말을 내뱉으면서까지, 고급 만년필이 탐난 건가요?",
                ExpressionId.Neutral, "(책상을 내려치며)"),
            P("그게 아니라면 뭐죠?"),
            P("제가 모르는 새에 본부에서는 보조 수사관 이수 과목에 ‘현장에서의 짐 관리’같은 과목이라도 포함되었었나 보죠?"),
            A("아니에요, 저는 훔치거나 하지 않았어요! 제가 당황한 건 그런 이유가 아니라, 그저…", ExpressionId.Sad,
              "(당신의 공격적인 언행에 다소 당혹스럽지만 최대한 감정을 가다듬고는 억울한 표정을 지으며) " +
              "(눈을 꾹 감으며 생각하는 모습)", StageAnimation.Ponder),
            I("거짓말을 하는 것 같지는 않은데… 뭔가 숨기는 것 같아 보이는군."));

        /// <summary>2번째 교차 — B &gt; A 이후 다시 B. 장난으로 받아들였다가 실망한다. 심문 강제 종료.</summary>
        public static DialogueSequence ChoiceBSecondCross() => Seq("B (B>A 이후, 2번째 교차) — 신뢰를 잃는다",
            P(ChoiceBLabelSecondCross),
            A("그… 수사관님? 장난치시는 거라면 저는 마저 정리하러 갈게요.", ExpressionId.Neutral,
              "(당신의 행동에 의문을 가지고 안심한 듯, 한 숨을 쉬며)", StageAnimation.Sigh),
            P("…네?"),
            A("설마 진심으로 제게 캐물어보셨던 거예요?", ExpressionId.Angry,
              "(당신의 반응에 실망한 듯, 찡그리며)", StageAnimation.Glare),
            A("평소에도 그런 식으로 심문하시는 건 아니시죠?", ExpressionId.Angry),
            I("…신뢰를 완전히 잃어버린 모양이군. 더 이상 심문을 이어가긴 어렵겠어."));

        // ------------------------------------------------------------ C선택지

        /// <summary>C선택지 - 단서에 대해서(리본 끈).</summary>
        public const string ChoiceCLabel = "이 리본은 분명 기차역에 두고 왔던 거 같은데 타자기랑 같이 챙겨왔었나 보군요?";
        private const string ChoiceCLabel2 = "고양이가 물고 다니기라도 했나 보군요? 이리 얼룩지고 털들이 붙은걸 보니.";
        private const string ChoiceCLabel3 = "고양이가 원래 리본을 이렇게 좋아했나요?";
        private const string ChoiceCLabel4 = "리본…나름 고급 리본이였는데 말이죠.";
        private const string ChoiceCLabel5 = "리본…";
        private const string ChoiceCLabel6 = "내 리본…";

        /// <summary>리액션 수집 지점은 데이터 빌더가 <c>Reacting</c> 으로 감싼다 — 6번째 줄.</summary>
        public static DialogueSequence ChoiceCPass() => Seq("C 초회 — 고양이 단어에서 굳는다",
            P(ChoiceCLabel),
            A("앗, 네! 수사관님께서 구매하셨던 것 아닌가요?", ExpressionId.Smile,
              "(갑작스러운 주제 전환에 조금 당황한 듯 했지만)"),
            A("헉 혹시 저희 짐이 아니였나요?! 누군가의 소중한 리본이었다던 건… 이미 기차는 출발해 버렸는데 어떻게 하죠, 수사관님?",
              ExpressionId.Surprised, "(미처 생각 못한 부분이 생각난 듯, 살짝 놀라며)", StageAnimation.Startle),
            P("…제가 구매한 게 맞으니 그건 걱정하지 않으셔도 괜찮습니다."),
            A("그렇다면 다행이네요! 그건 그렇고 이렇게 멋진 리본을 까먹고 두고가시다니, 제가 안 가져왔으면 어쩔 뻔 했어요?",
              ExpressionId.Smile, "(당신의 말에 안심한 듯 하다가, 장난스러운 표정으로)"),
            P("…고양이 털도 붙어 있고 커피 자국도 그렇고. 멋진 리본이라기엔 이제는 조금 아쉬워졌군요."),
            A("그,그러게요. 하,하핫…", ExpressionId.Flustered,
              "(당신의 고양이라는 단어에 대한 반응으로 다소 비이상적인 당혹감을 보여주며) (결국 시선을 피하며 얼머무린다)",
              StageAnimation.LookAway),
            I("고양이 때문에 오염된 것은 확실해 보이는군. 그런데… 마치 다른 이유라도 있는 듯한 반응인걸."));

        public static DialogueSequence ChoiceCPass2() => Seq("C 2회",
            P(ChoiceCLabel2),
            A("그,그랬던 것 같아요. 아마도 붙잡고, 물고, 신나서 여기저기 다녔던 것 같은데요?", ExpressionId.Nervous,
              "(시선을 피하지만 끄덕이며)", StageAnimation.LookAway));

        public static DialogueSequence ChoiceCPass3() => Seq("C 3회",
            P(ChoiceCLabel3),
            A("어…그게 그러게 중요할까요?", ExpressionId.Thinking, "(고개를 갸웃기울이며)"));

        public static DialogueSequence ChoiceCPass4() => Seq("C 4회",
            P(ChoiceCLabel4),
            A("아…네…", ExpressionId.Thinking, "(엉뚱하다는 표정으로)"));

        public static DialogueSequence ChoiceCPass5() => Seq("C 5회",
            P(ChoiceCLabel5),
            A("…조금 있다가 매점에 리본을 파는지 한 번 보고 올게요.", ExpressionId.Sad, "(한숨을 쉬며)", StageAnimation.Sigh));

        public static DialogueSequence ChoiceCPass6() => Seq("C 6회",
            P(ChoiceCLabel6),
            A("…도착해서 제 사비로 하나 사올게요. 기운내세요.", ExpressionId.Sad, "(한숨을 쉬며)", StageAnimation.Sigh));

        public static List<ChoicePass> ChoiceCRounds() => new List<ChoicePass>
        {
            Round(ChoiceCLabel, ChoiceCPass()),
            Round(ChoiceCLabel2, ChoiceCPass2()),
            Round(ChoiceCLabel3, ChoiceCPass3()),
            Round(ChoiceCLabel4, ChoiceCPass4()),
            Round(ChoiceCLabel5, ChoiceCPass5()),
            Round(ChoiceCLabel6, ChoiceCPass6()),
        };

        // ------------------------------------------------------------ 심문 오브젝트
        // 심문 화면의 인물·배경 요소. 한 번 고르면 다시 고를 수 없다(노션: "1회 선택 시, 재선택이 불가능하도록 변경").

        /// <summary>심문 오브젝트 — 고양이 엉덩이.</summary>
        public static List<DialogueLine> InterrogationObjectCat() => new List<DialogueLine>
        {
            P("이 아이는 본인이 데려온 겁니까?"),
            A("앗,아뇨! 에헤헤... 귀엽죠? 기차내에서 돌아다니고 있었어요.", ExpressionId.Smile,
              "(고양이쪽을 보고 베시시 웃으며)", StageAnimation.GlanceCat),
            A("…계속 데리고 있는 건 안 되겠죠?", ExpressionId.Nervous),
            P("뭐, 상황봐서 생각해보죠"),
            A("앗,네! 고려해주셔서 감사드려요!!", ExpressionId.Smile, "(살짝 놀라 기쁘게 웃으며)", StageAnimation.Startle),
        };

        /// <summary>심문 오브젝트 — 보조 수사관 프로필 서류.</summary>
        public static List<DialogueLine> InterrogationObjectProfile() => new List<DialogueLine>
        {
            P("혹시 여기있는 본인 서류를 읽어봤습니까?"),
            A("네,조금 궁금해서요. 멋대로 읽어봐서 죄송해요.", ExpressionId.Smile,
              "(베시시 미소를 짓고 멋슥하게 끄덕이며)", StageAnimation.Bow),
            P("…감상은 어떤지 궁금하군요?"),
            A("대부분 알고 있는 내용이였는데요… 지각은 다시 한 번 죄송합니다!", ExpressionId.Flustered,
              "(>.<;; 사과)", StageAnimation.Bow),
            P("뭐, 덕분에 잔소리하는 수고는 덜었군요."),
        };

        // ------------------------------------------------------------ 마무리

        /// <summary>
        /// [타임 토큰을 전부 소모하거나, 심문 종료 버튼을 통해 정상적인 심문 종료 시] — 다음 상대(고양이)로 넘어가는 다리.
        /// </summary>
        public static DialogueSequence SepiEnding() => Seq("세피 심문 마무리",
            P("일단 알았습니다. 그리고 잠시만 기다려보시죠. 아직 이야기를 나눠야 될 분이 남아계시니."),
            A("이야기요…? 설마 이 고양이요?!", ExpressionId.Surprised, "(개그 컷씬?)"));

        /// <summary>[신뢰도 하락으로 인한 ‘세피‘ 심문 강제 종료 시].</summary>
        public static DialogueSequence SepiTrustBroken() => Seq("세피 심문 강제 종료 — 신뢰도 하락",
            P("아무래도 이야기를 더 나누기에는 무리가 있는 듯 하군요."),
            P("뭐, 일단 알았습니다. 그래도 잠시만 기다려보시죠."),
            P("아직 이야기를 나눠야 될 분이 남아계시니."),
            A("아,네… 설마 이 고양이요…?", ExpressionId.Sad));

        // ================================================================ [임시] 단서 조합 · 2차 심문
        // 노션에 단서 조합의 대사와 2차 심문 스크립트는 없다. 아래는 2026-09-23 기획(가설 조합 · 잉크 · 2차 심문 공용 토큰 ·
        // 지목 → 엔딩)을 시험하기 위해 지어 넣은 자리 채우기 텍스트. 원문이 생기면 통째로 교체한다.

        /// <summary>[임시] 조합 페이즈 진입.</summary>
        public static DialogueSequence DeductionIntro() => Seq("[임시] 조합 진입",
            N("당신은 수첩을 펼쳤다. 적어 둘 수 있는 잉크는 얼마 남지 않았다."),
            A("수사관님, 모은 것들을 한번 이어 보죠. 물건 하나, 그리고 그 물건에 얽힌 말이나 표정 하나요.", ExpressionId.Smile),
            I("..두 개를 잇는다. 이어지면 가설이고, 안 이어지면 잉크만 마른다."));

        /// <summary>[임시] 실패 회차별 반응. 1회, 2회, 3회 이상.</summary>
        public static List<DialogueSequence> DeductionFailures() => new List<DialogueSequence>
        {
            Seq("[임시] 실패 1회차",
                A("음… 그 둘은 좀 멀지 않나요?", ExpressionId.Flustered),
                N("당신은 방금 그은 선을 지웠다. 잉크가 한 방울 말랐다.")),
            Seq("[임시] 실패 2회차",
                A("수사관님… 잠깐만요. 조금만 더 천천히 보실래요?", ExpressionId.Nervous, "(수첩을 곁눈질한다)"),
                N("잉크가 또 한 방울 말랐다.")),
            Seq("[임시] 실패 3회차",
                A("…괜찮으세요? 오늘 좀 이상하세요, 수사관님.", ExpressionId.Sad),
                N("손끝이 떨렸다. 남은 잉크가 얼마 없다.")),
        };

        /// <summary>[임시] 같은 가설을 또 세우려 했을 때.</summary>
        public static DialogueSequence DeductionDuplicate() => Seq("[임시] 중복 가설",
            I("..이건 이미 적어 둔 가설이다."));

        /// <summary>[임시] 가설 칸이 가득 찬 채 새 가설이 생겼을 때.</summary>
        public static DialogueSequence DeductionReplacePrompt() => Seq("[임시] 가설 자리 없음",
            I("..수첩에 자리가 없다. 하나를 지워야 한다."),
            A("어떤 걸 지우실 거예요? 지운 건 되돌릴 수 없어요.", ExpressionId.Nervous));

        /// <summary>[임시] 잉크가 다 마르고 가설이 하나라도 있어 저절로 끝날 때.</summary>
        public static DialogueSequence DeductionInkOut() => Seq("[임시] 잉크 소진",
            N("잉크가 다 말랐다. 더는 그을 수 없다."),
            A("그래도 이 정도면… 다시 물어볼 수 있을 것 같아요.", ExpressionId.Nervous));

        public static DialogueSequence DeductionOutro() => Seq("[임시] 조합 마무리",
            A("정리됐네요! 그럼… 다시 이야기해 볼까요?", ExpressionId.Smile),
            I("..이제 물어볼 것이 생겼습니다."));

        /// <summary>[임시] 잉크가 다 마른 채 가설이 하나도 없을 때 — 자기 비하 게임오버 직전.</summary>
        public static DialogueSequence SelfLoathingEnding() => Seq("[임시] 자기 비하",
            N("잉크가 말랐다. 수첩에는 지운 자국만 남았다."),
            I("..수사관이라는 사람이, 제 방에서 잃어버린 물건 하나 찾아내지 못하다니."),
            N("당신은 창밖으로 시선을 돌렸다. 기차는 여전히 달리고 있었다."));

        // ---- [임시] 가설 조합식 세 개. 튜토리얼은 가설을 2개까지만 저장하므로, 셋째를 만들면 하나를 지워야 한다. ----

        /// <summary>[임시] 가설 1 — 언듯 보이는 무언가 + "본 적은 있다" → 만년필은 고양이 아래에.</summary>
        public static DialogueSequence HypPenUnderCat() => Seq("[임시] 가설 1 성공",
            I("..'본 적은 있다'. 그리고 고양이 아래에 비죽 나온 것."),
            I("..처음부터 저기 있었다. 저 녀석이 깔고 앉아 있었을 뿐."),
            A("…아.", ExpressionId.Surprised, "(고양이 쪽을 보지 않으려 애쓴다)"));

        /// <summary>[임시] 가설 2 — 리본 + 고양이 이야기에 굳는 표정 → 세피는 고양이 일을 숨기고 있다.</summary>
        public static DialogueSequence HypSepiHidesCat() => Seq("[임시] 가설 2 성공",
            I("..리본에 박힌 털, 그리고 '고양이'라는 말에 굳은 얼굴."),
            I("..리본이 더러워진 건 숨길 일이 아니다. 숨기는 건 고양이 쪽이다."),
            A("…리본은, 그냥 좀 더러워진 거예요.", ExpressionId.Nervous));

        /// <summary>[임시] 가설 3 — 커피잔 + "본 적은 있다" → 커피가 쏟아진 그때 자리가 바뀌었다.</summary>
        public static DialogueSequence HypSpillMoment() => Seq("[임시] 가설 3 성공",
            I("..커피가 쏟아진 뒤로 만년필을 못 봤다. 그 순간에 자리가 바뀌었다."),
            A("그, 그때는 저도 정신이 없어서…", ExpressionId.Flustered));

        // ---- [임시] 2차 심문 ----

        /// <summary>[임시] 2차 심문 첫 마디.</summary>
        public static DialogueSequence SepiSecondOpening() => Seq("[임시] 2차 심문 진입",
            A("…또 물어보실 게 있으세요?", ExpressionId.Nervous, "(자리에 다시 앉으며)"));

        public const string Choice2ALabel = "조금 전 이야기, 한 번만 더 들려주시겠습니까?";
        public static DialogueSequence Choice2APass() => Seq("[임시] 2차 A 친근",
            A("아까 말씀드린 그대로예요. 정말로요.", ExpressionId.Nervous),
            A("…만년필은, 아마 어디 굴러 들어갔겠죠.", ExpressionId.Nervous, "(시선을 피한다)"));

        public const string Choice2BLabel = "숨기는 게 있죠.";
        public static DialogueSequence Choice2BPass() => Seq("[임시] 2차 B 강압",
            A("…없어요. 없다니까요.", ExpressionId.Fearful, "(어깨가 움츠러든다)"));

        public const string Choice2CLabel = "만년필이 어디 있는지, 알 것 같습니다.";
        /// <summary>[임시] 가설 1 로 열리는 단서 제시. 이 묶음이 끝나면 세피의 심문은 마무리된다(자수).</summary>
        public static DialogueSequence Choice2CPass()
        {
            var seq = Seq("[임시] 2차 C 단서 제시",
                I("..고양이 아래를 보시죠."),
                A("…아. …그, 그러니까, 그건요…", ExpressionId.Flustered, "(시선이 고양이와 당신 사이를 오간다)"),
                N("조수는 한동안 입을 열지 못했다."),
                A("…죄송해요. 제가 말씀드릴게요.", ExpressionId.Sad));
            seq.Set("endsInterrogation", true);
            return seq;
        }

        public const string Choice2DLabel = "고양이 이야기를 할 때마다 표정이 굳더군요.";
        /// <summary>[임시] 가설 2 로 열리는 단서 제시. 심문은 이어진다.</summary>
        public static DialogueSequence Choice2DPass() => Seq("[임시] 2차 D 단서 제시",
            A("…그 아이는 제 고양이예요. 몰래 태웠어요.", ExpressionId.Sad),
            A("만년필이랑은 상관없어요… 아마도요.", ExpressionId.Nervous, "(말끝이 흐려진다)"));

        /// <summary>[임시] 2차 심문의 토큰이 다 떨어졌을 때, 지목으로 넘어가기 전.</summary>
        public static DialogueSequence BeforeAccusation() => Seq("[임시] 지목 전",
            N("물어볼 만큼 물었다."),
            I("..이제 답을 내야 한다."));

        // ---- [임시] 지목 — 가설 × 용의자 쌍마다 추론 두 갈래 ----
        // 갈래 A 는 그 가설로 그 인물을 지목하는 방향, B 는 물러서는 방향(또는 되돌아가기).
        // 세피를 지목하는 A 에는 억울함 호소가 붙고, 단서로 반박해야 한다. 고양이는 말이 없으니 호소가 없다.

        public sealed class Acc
        {
            public string Label; public DialogueSequence Speech, Plea, RefuteWrong, RefuteSuccess, Result;
            public List<string> RefuteIds = new List<string>(); public string ResultTitle; public bool Returns;
        }

        private static Acc Opt(string label, DialogueSequence speech, string resultTitle = null, DialogueSequence result = null,
            DialogueSequence plea = null, List<string> refuteIds = null, DialogueSequence refuteWrong = null,
            DialogueSequence refuteSuccess = null, bool returns = false)
            => new Acc { Label = label, Speech = speech, ResultTitle = resultTitle ?? string.Empty, Result = result,
                         Plea = plea, RefuteIds = refuteIds ?? new List<string>(), RefuteWrong = refuteWrong,
                         RefuteSuccess = refuteSuccess, Returns = returns };

        private static Acc Undecided(string inner) => Opt("…아직 확신이 없다.", Seq("[임시] 되돌아가기", I(inner)), returns: true);

        /// <summary>가설 1 (만년필은 고양이 아래에) × 세피</summary>
        public static List<Acc> AccPenSepi() => new List<Acc>
        {
            Opt("세피가 고양이 아래에 숨긴 것이다.",
                Seq("[임시] 추론 H1×세피 A",
                    P("세피 씨. 만년필은 처음부터 고양이 아래에 있었습니다. 당신이 그리로 밀어 넣었죠."),
                    A("제, 제가요…?", ExpressionId.Flustered),
                    C("냐옹.")),
                "세피의 자백",
                Seq("[임시] 결과 H1×세피 A",
                    N("조수는 만년필을 꺼내 당신에게 건넸다."),
                    I("..사건은 끝났다. 고양이 문제는 다른 이야기지만.")),
                plea: Seq("[임시] 호소 H1×세피 A",
                    A("아니에요! 그건… 고양이가 제멋대로 앉은 거예요. 저는 만년필 근처에도 안 갔어요.", ExpressionId.Nervous)),
                refuteIds: new List<string> { "TES_SAW_IT", "REA_CAT_FLINCH" },
                refuteWrong: Seq("[임시] 반박 실패 H1×세피 A",
                    A("…그게 무슨 상관이죠?", ExpressionId.Nervous),
                    I("..아니다. 이걸로는 안 된다.")),
                refuteSuccess: Seq("[임시] 반박 성공 H1×세피 A",
                    P("'본 적은 있다'고 했죠. 어디서 봤는지는 끝내 말하지 않았고."),
                    A("…네. 제가 밀어 넣었어요. 고양이를 들킬까 봐.", ExpressionId.Sad))),
            Opt("고양이가 앉았을 뿐, 세피는 몰랐다.",
                Seq("[임시] 추론 H1×세피 B",
                    P("만년필은 고양이 아래에 있었습니다. 세피 씨는 그저 눈치채지 못했던 거고요."),
                    A("…네, 네. 그렇죠.", ExpressionId.Nervous)),
                "만년필 회수 — 남은 의문",
                Seq("[임시] 결과 H1×세피 B",
                    N("당신은 만년필을 꺼내 들었다. 조수는 시선을 피했다."),
                    I("..뭔가 더 있었던 것 같지만, 만년필은 돌아왔다."))),
        };

        /// <summary>가설 1 × 고양이</summary>
        public static List<Acc> AccPenCat() => new List<Acc>
        {
            Opt("고양이가 범인이다. 깔고 앉은 채 시치미를 뗐다.",
                Seq("[임시] 추론 H1×고양이 A",
                    I("..범인은 저 고양이다."),
                    P("만년필을 깔고 앉아 있던 건 저 녀석이니까."),
                    A("수사관님… 진심이세요?", ExpressionId.Flustered),
                    C("냐앙.")),
                "고양이 지목 — 미심쩍은 결말",
                Seq("[임시] 결과 H1×고양이 A",
                    N("고양이는 대답하지 않았다. 조수는 한숨을 쉬었다."),
                    I("..만년필은 돌아왔다. 어쩐지 개운하지 않다."))),
            Undecided("..고양이가 스스로 만년필을 옮겼을 리 없다. 다시 생각하자."),
        };

        /// <summary>가설 2 (세피는 고양이 일을 숨기고 있다) × 세피</summary>
        public static List<Acc> AccHidesSepi() => new List<Acc>
        {
            Opt("고양이를 숨기려던 소동 속에서 만년필을 치웠다.",
                Seq("[임시] 추론 H2×세피 A",
                    P("세피 씨, 당신은 고양이를 숨기고 있었습니다. 커피를 엎지른 것도, 리본이 더러워진 것도 그 소동이었고."),
                    P("그 와중에 만년필이 고양이 아래로 들어갔죠. 당신은 알면서 모른 척했고요."),
                    A("…", ExpressionId.Fearful)),
                "세피의 자백 — 고양이와 만년필",
                Seq("[임시] 결과 H2×세피 A",
                    N("조수는 고양이를 안아 올렸다. 그 아래에서 만년필이 굴러 나왔다."),
                    I("..사건은 끝났다.")),
                plea: Seq("[임시] 호소 H2×세피 A",
                    A("고양이는… 맞아요. 하지만 만년필은 정말 몰라요. 리본이 더러워진 것도 그냥 고양이가 문 거예요.", ExpressionId.Nervous)),
                refuteIds: new List<string> { "CLUE_GLIMPSE", "TES_SAW_IT" },
                refuteWrong: Seq("[임시] 반박 실패 H2×세피 A",
                    A("…그건 고양이랑 상관없는 물건이잖아요.", ExpressionId.Nervous),
                    I("..맞는 말이다. 다른 걸 대야 한다.")),
                refuteSuccess: Seq("[임시] 반박 성공 H2×세피 A",
                    P("고양이 아래에 비죽 나온 것. 당신은 그걸 '본 적은 있다'고 했습니다."),
                    A("…들켰네요.", ExpressionId.Sad))),
            Opt("고양이를 숨긴 것과 만년필은 별개다.",
                Seq("[임시] 추론 H2×세피 B",
                    P("고양이를 숨긴 건 알겠습니다. 하지만 만년필은 다른 이야기죠."),
                    A("…네. 정말 다른 이야기예요.", ExpressionId.Nervous)),
                "미해결 — 고양이만 밝혀지다",
                Seq("[임시] 결과 H2×세피 B",
                    N("만년필은 끝내 나오지 않았다. 기차는 다음 역에 닿았다."),
                    I("..고양이 이야기만 듣고 끝난 셈이다."))),
        };

        /// <summary>가설 2 × 고양이</summary>
        public static List<Acc> AccHidesCat() => new List<Acc>
        {
            Opt("고양이가 몰래 탄 것이 모든 소동의 시작이다.",
                Seq("[임시] 추론 H2×고양이 A",
                    I("..고양이가 몰래 탔다. 소동은 거기서 시작됐다."),
                    P("그러니 만년필도 저 녀석 탓입니다."),
                    A("그, 그렇게 되나요…?", ExpressionId.Flustered)),
                "고양이 지목 — 절반의 진실",
                Seq("[임시] 결과 H2×고양이 A",
                    N("고양이는 하품을 했다. 만년필은 고양이 아래에서 나왔다."),
                    I("..결론은 맞았는데, 이유가 틀린 기분이다."))),
            Undecided("..고양이를 숨긴 건 세피다. 고양이가 아니라."),
        };

        /// <summary>가설 3 (커피가 쏟아진 그때) × 세피</summary>
        public static List<Acc> AccSpillSepi() => new List<Acc>
        {
            Opt("커피를 엎지른 순간, 세피가 만년필을 치웠다.",
                Seq("[임시] 추론 H3×세피 A",
                    P("커피가 쏟아진 그때, 만년필이 자리를 옮겼습니다. 그 자리에 있던 건 당신뿐이었죠."),
                    A("저, 저는 그때 닦느라…", ExpressionId.Flustered)),
                "세피의 자백 — 커피와 고양이",
                Seq("[임시] 결과 H3×세피 A",
                    N("조수는 고양이 아래에서 만년필을 꺼냈다."),
                    I("..사건은 끝났다.")),
                plea: Seq("[임시] 호소 H3×세피 A",
                    A("커피는 정말 실수였어요. 만년필은… 닦다가 어디 굴러갔나 봐요. 제가 치운 게 아니에요.", ExpressionId.Nervous)),
                refuteIds: new List<string> { "CLUE_GLIMPSE", "REA_CAT_FLINCH" },
                refuteWrong: Seq("[임시] 반박 실패 H3×세피 A",
                    A("…그건 그때 일이랑 상관없어요.", ExpressionId.Nervous),
                    I("..그렇다. 다른 걸 대야 한다.")),
                refuteSuccess: Seq("[임시] 반박 성공 H3×세피 A",
                    P("굴러간 곳이 하필 고양이 아래군요. 고양이 이야기만 나오면 얼굴이 굳는, 그 고양이 말입니다."),
                    A("…네. 제가 그랬어요.", ExpressionId.Sad))),
            Opt("커피는 실수였고, 만년필은 그때 굴러떨어졌다.",
                Seq("[임시] 추론 H3×세피 B",
                    P("커피는 실수였고, 만년필은 그때 굴러떨어진 겁니다. 아무도 가져가지 않았죠."),
                    A("…네, 그럴 거예요.", ExpressionId.Nervous)),
                "만년필 회수 — 이유는 묻지 않았다",
                Seq("[임시] 결과 H3×세피 B",
                    N("당신은 바닥을 살폈다. 만년필은 고양이 아래에서 나왔다. 조수는 아무 말도 하지 않았다."),
                    I("..잃어버린 물건은 찾았다. 잃어버린 이유는 못 찾았다."))),
        };

        /// <summary>가설 3 × 고양이</summary>
        public static List<Acc> AccSpillCat() => new List<Acc>
        {
            Opt("커피 소동에 놀란 고양이가 만년필을 깔고 앉았다.",
                Seq("[임시] 추론 H3×고양이 A",
                    I("..커피가 쏟아지고, 놀란 고양이가 만년필 위에 앉았다."),
                    P("범인은 고양이입니다. 실수였지만."),
                    A("…아.", ExpressionId.Surprised)),
                "고양이 지목 — 사고였다",
                Seq("[임시] 결과 H3×고양이 A",
                    N("고양이는 그 자리에서 꿈쩍도 하지 않았다."),
                    I("..틀리진 않았다. 다만 고양이가 왜 여기 있는지는 묻지 않았다."))),
            Undecided("..고양이가 놀란 건 맞다. 그런데 누가 고양이를 여기 데려왔지."),
        };

        /// <summary>맞는 쌍이 없을 때의 기본 갈래.</summary>
        public static List<Acc> AccDefault() => new List<Acc>
        {
            Opt("그래도 이 사람이다.",
                Seq("[임시] 추론 기본 A", I("..근거는 없다. 하지만 이 사람이다.")),
                "미해결 — 근거 없는 지목",
                Seq("[임시] 결과 기본 A",
                    N("아무도 수긍하지 않았다. 기차는 다음 역에 닿았다."))),
            Undecided("..이 가설로는 이 사람을 가리킬 수 없다."),
        };

        // ================================================================ 튜토리얼 팝업

        /// <summary>튜토리얼 팝업 1~3 과 페널티 설명. 본문은 노션 원문을 페이지로 나눈 것.</summary>
        public static List<TutorialPopupDefinition> TutorialPopups() => new List<TutorialPopupDefinition>
        {
            Popup("POPUP_1", "1차 심문 페이즈 — 튜토리얼 1", TutorialPopupTrigger.AfterSuspectOpening,
                "기본적으로 주인공과 용의자의 대화로 진행되며, 각 용의자마다 제한된 양의 [타임 토큰]이 주어집니다.\n\n" +
                ">>[타임 토큰]은 기본적으로 플레이어의 행동에 따라 소모되며, 이에 따라서 심문/단서 조합 페이즈가 진행됩니다.",
                "[심문 페이즈]에 진입 시, 화면 좌측에 나열된 <메인 선택지>를 좌클릭을 통해 선택하여 심문을 진행하거나, " +
                "[심문 페이즈] 화면내에서 특수 오브젝트 요소인, <심문 오브젝트>를 좌클릭하여 메인 선택지와는 별개의 대화를 진행할 수 있습니다.",
                "<메인 선택지>에는 기본적으로 ’경계도를 낮추며 자연스럽게 대화를 이끌어내는 <친근 선택지>‘와 " +
                "’추궁하거나 강압적으로 압박하며 심문하는 <강압 선택지>‘가 생성됩니다. \n\n" +
                ">>앞서 [단서 수집 페이즈]에서 수집한 특정 단서의 유무에 따라서도 선택지가 추가되거나, 기본 메인 선택지의 텍스트가 변경될 수 있습니다.",
                ">>특정한 대화 혹은 심문의 진행 및 시간의 흐름(타임 토큰의 잔여량) 등, 특정 트리거에 따라서 화면 좌측에 선택지에 별도의 선택지가 추가로 생성되기도 합니다. \n\n" +
                ">> 기본적으로 주어지는 선택지와, 특정 트리거 및 진행에 따라 추가/변경되는 선택지를 포함하여, 용의자마다 최소 2가지에서 최대 5가지의 선택지가 생성/변경될 수 있습니다."),

            Popup("POPUP_2", "1차 심문 페이즈 — 튜토리얼 2", TutorialPopupTrigger.BeforeFirstCollectableLine,
                "메인 선택지 선택 및 심문 오브젝트 선택 시, 해당 선택지 혹은 오브젝트에 대한 반응이 용의자에게서 나타나며, 주인공과 용의자 간의 심문 및 대화가 진행됩니다.\n\n" +
                ">> 대화 중에 밑줄이 그어져있는 특정 단어나 문장 표현을 클릭하면 [증언 및 정황 증거]로써 수집이 가능해지며, 우측의 단서 목록에 추가됩니다.",
                "주인공의 대사 및 행동에 따라, 용의자 캐릭터에게서는 놀라거나 동공이 떨리는 등, 일반적이지 않은 반응인 <리액션>이 나타나기도 합니다.\n\n" +
                ">>특정 <리액션>이 나타났을 때에 대사가 진행될 동안 용의자 캐릭터 위로 마우스 오버시, 커서 이미지가 일시적으로 바뀌게 되며, " +
                "해당 타이밍에 좌클릭 시, 해당 행동을 [증언 및 정황 단서]로써 수집이 됩니다.",
                "메인 선택지는 같은 선택지더라도 중복하여 여러 번 선택이 가능합니다. 경우에 따라, 중복되는 선택지를 선택하더라도 다른 내용의 심문을 이어갈 수 있습니다. \n\n" +
                ">>이 조건이 충족된 경우에는 이미 선택했었던 메인 선택지 위로, 별도로 아이콘이 표시 됩니다."),

            Popup("POPUP_3", "1차 심문 페이즈 — 튜토리얼 3", TutorialPopupTrigger.OnRecallAvailable,
                "특정 선택지나 질문에 대한 반응 및 대화를 돌아볼 때에는 [기억 짐작]을 진행합니다.\n\n" +
                ">>[기억 짐작]통해 앞서 용의자와 나누었던 대화 내용이나 용의자의 리액션을 흐릿해진 화면으로 다시 볼 수 있습니다. ",
                "기억 짐작은 심문 페이즈 중에 여러 번 진행 할 수 있으나, 횟수에 따라 더욱 불분명한 내용과 인영, 왜곡된 내용들로 보여집니다.\n\n" +
                ">>기억 짐작 중에도 동일한 상호 작용을 통해서  ‘증언 및 정황 단서’를 수집할 수 있으나, 일부 흐릿해진 표정 리액션이나 왜곡된 텍스트에서는 수집이 불가능해집니다."),

            Popup("POPUP_PENALTY", "타임 토큰과 페널티", TutorialPopupTrigger.OnFirstPenalty,
                "[타임 토큰]은 게임내에서의 시간을 토큰으로 시각화 한 요소이며, 각 페이즈에 할당된 타임 토큰이 전부 소진되면 해당 페이즈가 <강제 종료>된다.\n\n" +
                "타임 토큰은 플레이어의 다음과 같은 행동으로 소모된다.\n" +
                "· 선택지 선택&재선택 시 (심문 페이즈)\n" +
                "· 심문 오브젝트 선택 시 (심문 페이즈)\n" +
                "· 30초 간의 미행동으로 인한 무반응 판정 시 (심문 페이즈)\n" +
                "· 회상 기능 내에서의 ‘조수의 도움(약어 미정)‘ 사용 시 (심문 페이즈)\n" +
                "· 스토리 진행으로 인한 강제 대화 진행 시 (심문 페이즈)",
                "다음과 같은 행동으로 타임 토큰의 잔여량과 별개로 전량 소진된다.\n\n" +
                "과도한 <강압 선택지>의 재선택 혹은 사건과는 동 떨어진 질문들의 반복으로 인한 용의자의 심문 거부.\n" +
                "3번 이상의 <강압 선택지> 선택 시 경고 팝업, 4번 이상 선택 시에는 잔여 타임 토큰의 갯수와 상관 없이, 용의자가 공포 및 실신으로 인한 심문 거부 및 불가 판정이 나타난다.\n" +
                "심문 페이즈의 시작 이후에 메인 선택지가 아닌, <심문 오브젝트>의 선택만 3번 이상 반복 될 시, 용의자의 의문 혹은 불쾌에 대한 반응과 함께 경고 팝업이 나타나며, " +
                "4번 이상 선택 시 잔여 타임 토큰의 갯수와 상관 없이, 용의자의 분노에 대한 반응과 함께 심문 거부 및 불가 판정이 나타난다.",
                "비정상적인 선택지 선택의 양상으로 인한 게임오버 (통상 게임오버 엔딩)\n\n" +
                "비정상적인 선택지 선택 양상의 기준은 2번 이상의 <친근 선택지>와 <강압 선택지>를 교차 선택 시 이루어진다.\n" +
                "1번 째 교차 선택 시, 용의자의 의문 및 당황에 대한 반응이 나타난다. \n" +
                "2번 째 교차 선택 시, 용의자의 분노 혹은 당혹에 대한 반응과 함께 게임오버 된다.",
                "무반응 판정은 실제 현실에서의 시간 흐름과 상호 작용되어 판정이 이루어진다.\n\n" +
                "무반응에 대한 시간 타이머 판정은 다음과 같은 행동으로 일시 정지 및 초기화 할 수 있다.\n" +
                "· 선택지 선택&재선택 후 대화 진행 - 타이머 초기화\n" +
                "· 심문 오브젝트 선택 후 대화 진행 및 텍스트 출력 - 타이머 초기화 \n" +
                "· 게임 일시 정지 - 타이머 일시 정지\n" +
                "· 단서 툴팁 팝업 시 - 텍스트 출력 중 및 출력 이후 1~20초 간 타이머 일시 정지\n\n" +
                ">> 최종적으로 수동으로 페이즈를 종료시키거나, 해당 용의자 캐릭터 마다 할당된 타임 토큰을 전부 소진하게 될 시, 해당 용의자 캐릭터의 페이즈가 자동으로 마무리된다. \n" +
                ">> 타임 토큰의 소진으로 인해 특정 단서나 주요 증언 등을 못 듣게 될 수 있으며, 이 경우에는 최종적으로 게임 오버로 이어지게 된다."),
            // ---- [임시] 단서 조합 팝업. 노션에 없어 2026-09-23 기획 규칙을 내 문장으로 적었다. ----
            Popup("POPUP_4", "단서 조합 페이즈 — 튜토리얼 4", TutorialPopupTrigger.BeforeDeduction,
                "[추론 페이즈]에서는 왼쪽 서류의 [Evidence(물품 증거)] 하나와 오른쪽 서류의 [Leads(정황 증거)] 하나를 골라 잇습니다. " +
                "두 단서가 이어지면 〈가설〉이 되어 오른쪽 끝의 [가설] 탭에 꽂힙니다.\n\n" +
                ">>양쪽에서 하나씩 누르면 붉은 선이 이어집니다. 선 위의 [가설 추론]을 누르세요. 항목을 우클릭하면 전문을 읽을 수 있습니다.",
                "[가설 추론]을 누를 때마다 위쪽의 [잉크]가 한 방울 마릅니다 — 성공해도, 실패해도. 잉크는 4번의 시도입니다. " +
                "관계없는 두 단서를 이으면 실패하고 가설을 얻지 못합니다. 잉크가 다 마른 채 가설이 하나도 없으면 [게임 오버]가 됩니다.\n\n" +
                ">>가설은 최대 2개(튜토리얼)까지 저장됩니다. 가득 찬 채 새 가설이 생기면 기존 가설 탭 하나를 골라 지우고 저장합니다. " +
                "가설이 하나라도 있으면 [돌아가기]로 추론을 마칠 수 있습니다."),
            Popup("POPUP_4B", "잉크와 실패", TutorialPopupTrigger.OnDeductionFirstMiss,
                "이미 이어 본 짝은 [가설 추론]이 눌리지 않습니다. 남은 잉크만큼만 더 이어 볼 수 있습니다.\n\n" +
                ">>2차 심문에서는 인물마다 가설 하나를 들고 들어가고, 지목에서는 가설과 용의자를 골라 추론을 세웁니다. 어떤 가설을 남길지가 곧 어떤 답을 낼 수 있을지입니다."),
        };

        // ============================================================== 헬퍼
        // 화자별로 짧은 이름을 둔 이유는, 원문을 옮겨 적을 때
        // 문장보다 코드가 길어지지 않게 하기 위해서다.

        private static DialogueLine N(string text) => Line(SpeakerId.Narration, text);
        private static DialogueLine P(string text) => Line(SpeakerId.Protagonist, text);
        private static DialogueLine I(string text) => Line(SpeakerId.Inner, text);
        private static DialogueLine C(string text) => Line(SpeakerId.Cat, text);

        private static DialogueLine A(string text, ExpressionId expression = ExpressionId.Neutral,
            string direction = null, string animation = null)
            => Line(SpeakerId.Assistant, text, expression, direction, animation);

        public static DialogueLine Line(SpeakerId speaker, string text,
            ExpressionId expression = ExpressionId.Neutral, string direction = null, string animation = null)
        {
            var line = new DialogueLine(speaker, text);
            line.Set("expression", expression);
            line.Set("stageDirection", direction ?? string.Empty);
            line.Set("animationId", animation ?? string.Empty);
            return line;
        }

        /// <summary>회차가 하나뿐인 선택지. 목록 문구는 선택지 기본값을 그대로 쓴다.</summary>
        public static List<ChoicePass> Single(DialogueSequence sequence, string label = null)
            => new List<ChoicePass> { Round(label, sequence) };

        /// <summary>
        /// 순서대로 쌓이는 보통 회차 한 칸. 목록에 뜰 문구와 대사, 그리고 그 회차에만 붙는
        /// &lt;시간 경과&gt; 를 함께 넣는다. 시간 경과를 비우면 용의자의 일반 무반응 회차 대사가 나온다.
        /// <paramref name="idleKey"/> 는 그 &lt;시간 경과&gt; 가 실제로 나왔는지를 다른 칸이 조건으로 볼 때의 이름표.
        /// </summary>
        private static ChoicePass Round(string label, DialogueSequence sequence,
            DialogueSequence idleFollowUp = null, string idleKey = null, bool breaksTrust = false, string animation = null)
        {
            var pass = new ChoicePass();
            pass.Set("note", sequence != null ? sequence.Note : string.Empty)
                .Set("labelOverride", label ?? string.Empty)
                .Set("sequence", sequence ?? new DialogueSequence())
                .Set("idleFollowUp", idleFollowUp ?? new DialogueSequence())
                .Set("idleFollowUpKey", idleKey ?? string.Empty)
                .Set("breaksTrust", breaksTrust)
                .Set("animationId", animation ?? string.Empty);
            return pass;
        }

        /// <summary>
        /// 회차 변형 한 칸. <paramref name="round"/> 회차(0 = 초회)를 대신하되, 조건이 맞을 때만 쓰인다.
        /// 예: B선택지 2회 연속의 -B 는 "&lt;시간 경과_a&gt; 가 나왔을 때".
        /// </summary>
        private static ChoicePass Variant(int round, string label, DialogueSequence sequence,
            DialogueSequence idleFollowUp = null, string idleKey = null,
            string requiresSeenIdleKey = null, string afterChoiceId = null, bool breaksTrust = false)
        {
            var pass = Round(label, sequence, idleFollowUp, idleKey, breaksTrust);
            pass.Set("variantOfRound", round);
            if (!string.IsNullOrEmpty(requiresSeenIdleKey))
                pass.Set("requiresSeenIdleKeys", new List<string> { requiresSeenIdleKey });
            if (!string.IsNullOrEmpty(afterChoiceId))
                pass.Set("requiresPriorChoiceIds", new List<string> { afterChoiceId });
            return pass;
        }

        /// <summary>
        /// 교차 칸 한 칸. 이번 선택이 <paramref name="n"/> 번째 친근↔강압 교차일 때 쓰인다 — 회차 순서보다 먼저.
        /// 1번째 = "B선택지 이후, 선택 시", 2번째 = "A선택지 &gt; B선택지 이후, A선택지 재 선택 시".
        /// </summary>
        private static ChoicePass Alternation(int n, string label, DialogueSequence sequence,
            DialogueSequence idleFollowUp = null, string idleKey = null, bool breaksTrust = false)
        {
            var pass = Round(label, sequence, idleFollowUp, idleKey, breaksTrust);
            pass.Set("onAlternation", n);
            return pass;
        }

        public static DialogueSequence Seq(string note, params DialogueLine[] lines)
        {
            var seq = new DialogueSequence();
            seq.Set("note", note)
               .Set("lines", new List<DialogueLine>(lines));
            return seq;
        }

        private static TutorialPopupDefinition Popup(string id, string title, TutorialPopupTrigger trigger,
            params string[] pages)
        {
            var popup = new TutorialPopupDefinition();
            popup.Set("id", id)
                 .Set("title", title)
                 .Set("trigger", trigger)
                 .Set("pages", new List<string>(pages));
            return popup;
        }
    }
}
