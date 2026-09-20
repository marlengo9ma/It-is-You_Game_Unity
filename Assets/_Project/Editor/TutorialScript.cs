using System.Collections.Generic;
using ItIsYou.Data;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 튜토리얼 챕터의 시나리오 스크립트. 노션 〈시나리오〉 문서의 원문을 그대로 옮긴 것이다.
    ///
    /// 문서의 구획이 그대로 메서드 하나가 된다:
    ///   [게임플레이 배경_캐릭터 씬] → [일러스트 컷씬#1] → [게임플레이 배경_캐릭터 씬]
    ///   → &lt;화면 전환&gt; 사건 진행 #1
    ///
    /// 문장은 손대지 않는다. 표현을 다듬고 싶어지는 자리가 있더라도,
    /// 그건 이 파일이 아니라 노션 쪽에서 고쳐야 한 벌만 존재하게 된다.
    /// TSV 임포터가 완성되면 이 파일의 역할은 임포터가 가져간다.
    /// </summary>
    public static class TutorialScript
    {
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

        /// <summary>단서 수집이 끝나고, 두 사람을 자리에 앉히기까지.</summary>
        public static DialogueSequence BeforeSuspectSelect() => Seq("두 용의자를 자리에 앉히다",
            N("당신은 대부분 정돈이 되었으나, 어딘가 어색하게 주변을 정돈하고 있던 조수를 나즈막히 불러 " +
              "멈추게 하고 고양이가 엎드려있는 테이블 자리에 앉아 소녀를 부른다."),
            P("보조 수사관? 잠시 이리로 와 앉아보도록."),
            A(".. 네? 저, 저요?", ExpressionId.Surprised),
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
        /// 용의자 선택 화면에서 먼저 오가는 대화.
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
            Line(SpeakerId.Cat, "", ExpressionId.Sly, "(고개 피함 / 대충 꼬리 살랑)"));

        /// <summary>세피 심문에 막 들어갔을 때의 첫 마디.</summary>
        public static DialogueSequence SepiOpening() => Seq("세피 심문 시작",
            A("하하, 만년필이라…주변에 떨어지지 않았을까요?", ExpressionId.Smile));

        /// <summary>
        /// 무반응 판정 회차별 반응.
        ///
        /// 회차가 올라갈수록 화제를 돌리고, 끝에는 당신 쪽을 걱정하기 시작한다.
        /// 같은 줄이 반복되면 침묵이 쌓이고 있다는 것이 전달되지 않는다.
        /// </summary>
        public static System.Collections.Generic.List<DialogueSequence> SepiIdleRounds()
            => new System.Collections.Generic.List<DialogueSequence>
            {
                Seq("무반응 1회차",
                    A("제,제가 신경써서 조금 더 찾아볼테니까요!", ExpressionId.Nervous,
                      "(당신의 기분을 살핀다)")),
                Seq("무반응 2회차",
                    A("…아니면 새로 사시는 건 어때요? 아까 보니까 기차내 매점에서 다양한 잡화도 팔던 걸요?",
                      ExpressionId.Smile)),
                Seq("무반응 3회차",
                    A("…저기, 수사관님? / 괜찮으신거죠…?", ExpressionId.Nervous)),
            };

        // ============================================================== 헬퍼
        // 화자별로 짧은 이름을 둔 이유는, 원문을 옮겨 적을 때
        // 문장보다 코드가 길어지지 않게 하기 위해서다.

        private static DialogueLine N(string text) => Line(SpeakerId.Narration, text);
        private static DialogueLine P(string text) => Line(SpeakerId.Protagonist, text);
        private static DialogueLine I(string text) => Line(SpeakerId.Inner, text);
        private static DialogueLine C(string text) => Line(SpeakerId.Cat, text);

        private static DialogueLine A(string text, ExpressionId expression = ExpressionId.Neutral,
            string direction = null)
            => Line(SpeakerId.Assistant, text, expression, direction);

        private static DialogueLine Line(SpeakerId speaker, string text,
            ExpressionId expression = ExpressionId.Neutral, string direction = null)
        {
            var line = new DialogueLine(speaker, text);
            line.Set("expression", expression);
            line.Set("stageDirection", direction ?? string.Empty);
            return line;
        }

        private static DialogueSequence Seq(string note, params DialogueLine[] lines)
        {
            var seq = new DialogueSequence();
            seq.Set("note", note)
               .Set("lines", new List<DialogueLine>(lines));
            return seq;
        }
    }
}
