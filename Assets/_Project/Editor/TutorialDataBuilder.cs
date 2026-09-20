using System.Collections.Generic;
using ItIsYou.Data;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 튜토리얼 챕터 〈누가 그랬지?〉 의 데이터를 만든다.
    /// 대사는 노션 시나리오 문서의 만년필 사건 부분을 그대로 옮긴 것이다.
    ///
    /// TSV 임포터가 완성되면 이 역할은 임포터가 가져간다.
    /// 그때까지 챕터를 실제로 굴려 보기 위한 데이터다.
    /// </summary>
    public static class TutorialDataBuilder
    {
        private const string Root = "Assets/_Project/Data";
        private const string ClueFolder = Root + "/Clues";
        private const string ChoiceFolder = Root + "/Choices";
        private const string SuspectFolder = Root + "/Suspects";
        private const string ObjectFolder = Root + "/Objects";

        [MenuItem("It is You/1. 튜토리얼 데이터 생성", priority = 1)]
        public static ChapterDefinition Build()
        {
            Ed.EnsureFolder(ClueFolder);
            Ed.EnsureFolder(ChoiceFolder);
            Ed.EnsureFolder(SuspectFolder);
            Ed.EnsureFolder(ObjectFolder);

            // ============================================================ 단서
            // 기획서 〈시스템 기획 및 화면 전환〉 의 튜토리얼 오브젝트 툴팁을 그대로 옮겼다.
            // <hl> 로 감싼 구간이 "심문에서 제시할 수 있다"는 강조로 표시된다.

            var ribbon = Clue("CLUE_RIBBON", ClueKind.Object,
                unread: "리본 끈",
                read: "얼룩지고 헤진 고급 리본",
                keyClue: true,
                tooltip:
                "당신이 선물 용도로 구매했던 휴대용 타자기에 약간(?)의 정성과 성의의 표시를 위해 " +
                "별도로 구매했던 붉은색의 고급 리본이다. 물론 포장 리본을 묶을 줄 몰랐던 당신은 " +
                "여러 번의 시도 끝에 금새 포기했지만.\n\n" +
                "기차역 테이블에 두고 온 줄 알았으나, <hl>조수가 타자기를 가져오면서 같이 챙긴 듯 하다.</hl>\n\n" +
                "지금은 일부분이 액체에 의해 얼룩졌으며, <hl>드문드문 헤진 곳과 고양이 털이 박혀있는 것</hl>이 보인다.");

            var coffee = Clue("CLUE_COFFEE", ClueKind.Object,
                unread: "커피잔",
                read: "엎질러졌던 커피잔",
                keyClue: false,
                tooltip:
                "당신이 기차에 올라, 새로이 시켰던 커피의 말로이다.\n\n" +
                "다행히 잔에 금이 가거나 깨져 보이지는 않지만 그 안에 있던 내용물의 행방은 " +
                "객실내 테이블 위의 각종 신문, 서류들과, 주변 바닥에서 확인이 가능했다.\n\n" +
                "<hl>아마도 누군가가 급하게 움직이면서 실수를 했거나, 자유분방하고 빠르게 움직이는 " +
                "무언가에 의해 쏟아졌을 것이다.</hl>");

            var glimpse = Clue("CLUE_GLIMPSE", ClueKind.Object,
                unread: "???",
                read: "고양이 아래에 깔린 무언가",
                keyClue: true,
                tooltip:
                "어디서 많이 본 듯한 형태의 일부분. <hl>지금은 고양이의 아래에 깔려있다.</hl>\n\n" +
                "아마 당신이 찾고있는 물건의 일부분 일지도 모른다.");

            // 심문에서만 얻을 수 있는 단서 두 가지.
            var sawIt = Clue("TES_SAW_IT", ClueKind.Testimony,
                unread: "조수의 말",
                read: "\"아까 봤던 것 같긴 한데요\"",
                keyClue: false,
                tooltip:
                "만년필의 행방을 묻자 조수가 흘린 말.\n\n" +
                "<hl>본 적이 있다는 것은 인정했으나, 어디서 봤는지는 끝내 답하지 않았다.</hl> " +
                "모른다는 반응과는 분명히 다르다.");

            var catFlinch = Clue("REA_CAT_FLINCH", ClueKind.Reaction,
                unread: "???",
                read: "고양이 이야기에 굳는 표정",
                keyClue: true,
                tooltip:
                "리본의 상태를 이야기하며 '고양이'라는 단어를 꺼냈을 때, " +
                "<hl>조수의 표정이 부자연스럽게 굳고 시선이 흔들렸다.</hl>\n\n" +
                "리본이 더러워진 것 자체는 숨길 일이 아니다. <hl>다른 이유가 있는 듯한 반응이다.</hl>");

            // ==================================================== 수집 오브젝트
            // 기획서: "튜토리얼 챕터에서는 3개의 일반 오브젝트, 3개의 특수 오브젝트, 총 6개."
            //
            // 겉모습으로는 둘을 구분할 수 없어야 한다. 마우스 오버 반응도 동일하다.
            // 구분은 수집 후 단서 목록 안의 표기로만 드러난다.

            var objRibbon = Obj("OBJ_RIBBON", InteractableKind.Special, "구겨진 끈", ribbon,
                Line(SpeakerId.Inner, "…기차역 테이블에 두고 온 줄 알았던 리본이군."),
                Line(SpeakerId.Assistant, "아, 그거 제가 타자기랑 같이 챙겨왔어요! 잘했죠?",
                    ExpressionId.Smile, "(뿌듯한 표정)"),
                Line(SpeakerId.Inner, "…원래 이런 상태는 아니었는데."));

            var objCoffee = Obj("OBJ_COFFEE", InteractableKind.Special, "쓰러진 잔", coffee,
                Line(SpeakerId.Inner, "방금 시킨 커피다. 잔은 멀쩡한데 내용물이 전부 밖에 있군."),
                Line(SpeakerId.Assistant, "그, 그건… 제가 치우려던 참이었어요.",
                    ExpressionId.Nervous, "(시선을 피하며)"));

            var objGlimpse = Obj("OBJ_GLIMPSE", InteractableKind.Special, "고양이 아래", glimpse,
                Line(SpeakerId.Inner, "저 고양이, 아까부터 저 자세 그대로군."),
                Line(SpeakerId.Cat, "…웨옹.", ExpressionId.Sly, "(꼬리를 살랑이며 자세를 고쳐 앉는다)"),
                Line(SpeakerId.Inner, "발 아래로 뭔가 비죽 나와 있다."));

            var objPapers = Obj("OBJ_PAPERS", InteractableKind.Ordinary, "흐트러진 서류", null,
                Line(SpeakerId.Inner, "사건 개요 서류와 오늘자 신문. 커피 자국이 동그랗게 번져 있다."),
                Line(SpeakerId.Assistant, "죄송해요, 정리하다가 순서가 섞여버려서…", ExpressionId.Sad));

            var objTypewriter = Obj("OBJ_TYPEWRITER", InteractableKind.Ordinary, "휴대용 타자기", null,
                Line(SpeakerId.Inner, "선물로 준 휴대용 타자기다. 벌써 자기 물건처럼 쓰고 있군."),
                Line(SpeakerId.Assistant, "이거 정말 좋아요! 속기할 때 손목이 하나도 안 아파요.",
                    ExpressionId.Smile));

            var objCat = Obj("OBJ_CAT", InteractableKind.Ordinary, "검은 고양이", null,
                Line(SpeakerId.Inner, "기차 안을 제집처럼 돌아다니던 녀석이다."),
                Line(SpeakerId.Cat, "먁.", ExpressionId.Neutral, "(늘어지게 하품)"),
                Line(SpeakerId.Assistant, "귀엽죠? 아까부터 계속 저기 앉아 있어요.", ExpressionId.Smile));

            // ========================================================== 선택지

            // A — 친근. 경계를 낮추며 행방을 묻는다.
            var choiceFriendly = Choice("CH_SEPI_FRIENDLY", ChoiceTone.Friendly,
                label: "내가 매우 아끼는 만년필이라. 그러니 행방을 꼭 알려주었음 합니다.",
                passes: new List<DialogueSequence>
                {
                    Seq("1회차 — 봤다는 것까지는 인정",
                        Line(SpeakerId.Protagonist, "내가 매우 아끼는 만년필이라. 그러니 행방을 꼭 알려주었음 합니다."),
                        Line(SpeakerId.Assistant,
                            "사, 사실은… <clue=TES_SAW_IT>아까 봤던 것 같긴 한데요…</clue>",
                            ExpressionId.Nervous, "(고양이와 당신을 번갈아보며 고민하는 모습)"),
                        Line(SpeakerId.Protagonist, "오, 다행이네요. 그래서 어디서 봤을까요?"),
                        Line(SpeakerId.Assistant, "음…역시 잘 모르겠어요. 죄송해요, 수사관님…",
                            ExpressionId.Sad,
                            "(고민하는 듯 눈을 감고 생각하는 듯 하다가, 작은 한숨 후, 미안하다는 듯 옅은 미소를 지으며)"),
                        Line(SpeakerId.Inner,
                            "거짓말을 하는 것 같군… 이유는 모르겠지만서도 일단 만년필을 모르는 반응은 아니야.")),

                    Seq("2회차 — 눈치를 본다",
                        Line(SpeakerId.Protagonist, "정말 아무것도 기억나지 않습니까?"),
                        Line(SpeakerId.Assistant, "…혹시 무슨 문제라도 있을까요…?",
                            ExpressionId.Nervous, "(계속 눈치를 보며)"))
                });

            // B — 강압. 절도를 의심하며 압박한다.
            var choiceAggressive = Choice("CH_SEPI_AGGRESSIVE", ChoiceTone.Aggressive,
                label: "아무리 그래도 그렇지 상사의 물건을 훔치다니, 초면부터 적잖이 얕보였나보군요?",
                passes: new List<DialogueSequence>
                {
                    Seq("1회차 — 당황, 그러나 거짓은 아님",
                        Line(SpeakerId.Protagonist,
                            "아무리 그래도 그렇지 상사의 물건을 훔치다니, 초면부터 적잖이 얕보였나보군요?"),
                        Line(SpeakerId.Assistant, "엥? 그, 그게 아니라요, 수사관님…",
                            ExpressionId.Flustered,
                            "(당신의 적대적인 언행에 당황한 듯, 눈을 못 마주치며 동공이 떨리는 모습)"),
                        Line(SpeakerId.Protagonist,
                            "그러면 뭐죠? 제가 모르는 새에 본부에서는 보조 수사관 이수 과목에 " +
                            "'현장에서의 짐 관리' 같은 과목이라도 포함되었었나 보죠?"),
                        Line(SpeakerId.Assistant,
                            "아니에요, 저는 훔치거나 하지 않았어요! 제가 당황한 건 그런 이유가 아니라, 그저…",
                            ExpressionId.Sad,
                            "(최대한 감정을 가다듬고는 억울한 표정 / 고양이쪽을 힐끔 힐끔 바라보며 고민하는 모습)"),
                        Line(SpeakerId.Inner, "거짓말을 하는 것 같지는 않은데… 뭔가 숨기는 것 같아보이는군.")),

                    Seq("2회차 — 정돈된 반박", -1,
                        Line(SpeakerId.Assistant,
                            "죄송합니다, 수사관님. 하지만 저는 감히 남의 물건을 가지고 탐내거나 하지 않아요.",
                            ExpressionId.Neutral, "(작은 한 숨을 쉬고는 감정이 정돈된 말투로)"),
                        Line(SpeakerId.Assistant,
                            "제가 오해할 만한 행동을 했다고는 하지만 그렇게 생각하지 않아 주셨음 해요.",
                            ExpressionId.Sad, "(또렷하게 주인공을 바라보며)"))
                });

            // C — 단서 제시. 리본을 들고 묻는다. 여기서 리액션이 나온다.
            var choiceRibbon = Choice("CH_SEPI_RIBBON", ChoiceTone.Evidence,
                label: "이 리본은 분명 기차역에 두고 왔던 거 같은데…",
                labelWhenInformed: "이 리본, 고양이 털이 박혀 있군요. 어떻게 된 겁니까?",
                labelSwapClueIds: Ed.L("REA_CAT_FLINCH"),
                requiredClueIds: Ed.L("CLUE_RIBBON"),
                passes: new List<DialogueSequence>
                {
                    Seq("1회차 — 고양이 단어에서 굳는다",
                        Line(SpeakerId.Protagonist,
                            "이 리본은 분명 기차역에 두고 왔던 거 같은데 타자기랑 같이 챙겨왔었나보군요?"),
                        Line(SpeakerId.Assistant, "앗, 네! 수사관님께서 구매하셨던 것 아닌가요?",
                            ExpressionId.Smile, "(갑작스러운 주제 전환에 조금 당황한 듯 했지만)"),
                        Line(SpeakerId.Assistant,
                            "헉 혹시 저희 짐이 아니였나요?! 이미 기차는 출발해 버렸는데 어떻게하죠, 수사관님?",
                            ExpressionId.Surprised, "(미처 생각 못한 부분이 생각난 듯, 살짝 놀라며)"),
                        Line(SpeakerId.Protagonist, "…제가 구매한 게 맞으니 그건 걱정하지 않으셔도 괜찮습니다."),
                        Line(SpeakerId.Assistant,
                            "그렇다면 다행이네요! 그건 그렇고 이렇게 멋진 리본을 까먹고 두고가시다니, " +
                            "제가 안 가져왔음 어쩔뻔 했어요?",
                            ExpressionId.Smile, "(안심한 듯 하다가, 장난스러운 표정으로)"),
                        Line(SpeakerId.Protagonist,
                            "…고양이 털도 붙어 있고 커피 자국도 그렇고. 멋진 리본이라기엔 이제는 조금 아쉬워졌군요."),
                        // ↓ 리액션 수집 지점. 이 줄이 출력되는 동안 조수를 클릭하면 단서가 된다.
                        Reacting(
                            Line(SpeakerId.Assistant, "그, 그러게요. 하, 하핫…",
                                ExpressionId.Flustered, "(결국 시선을 피하며 얼버무린다)"),
                            clueId: "REA_CAT_FLINCH", openDelay: 0.35f, window: 2.6f),
                        Line(SpeakerId.Inner,
                            "고양이 때문에 오염된 것은 확실해보이는군. 그런데… 마치 다른 이유라도 있는 듯한 반응인걸.")),

                    Seq("2회차 — 더 캐물어도 말을 돌린다",
                        Line(SpeakerId.Protagonist, "리본 이야기를 더 해볼까요."),
                        Line(SpeakerId.Assistant,
                            "그, 그것보다 만년필을 먼저 찾아야 하지 않을까요? 제가 더 찾아볼게요!",
                            ExpressionId.Nervous, "(화제를 돌리려는 듯 부산스럽게)"))
                });

            // ========================================================== 용의자

            var sepi = Ed.CreateOrLoad<SuspectDefinition>(SuspectFolder, "Suspect_Sepi");
            sepi.Set("id", "SEPI")
                .Set("displayName", "세피")
                .Set("speaker", SpeakerId.Assistant)
                .Set("profile",
                    "임시 배치된 보조 수사관. 서류상 '순간 기억 능력'과 '속기사' 자격이 기재되어 있으며, " +
                    "그 중 순간 기억 능력에는 공인 도장이 찍혀 있다.\n시간 약속에 대한 개념이 부족함.")
                .Set("timeTokens", 6)
                .Set("choices", Ed.L(choiceFriendly, choiceAggressive, choiceRibbon))
                .Set("aggressionWarning", Seq("강압 경고",
                    Line(SpeakerId.Assistant, "수사관님… 저, 정말 아니에요…",
                        ExpressionId.Fearful, "(목소리가 작아지며 어깨가 움츠러든다)")))
                .Set("refusalByFear", Seq("강압 누적 — 실신",
                    Line(SpeakerId.Assistant, "…죄, 죄송합니다…",
                        ExpressionId.Fearful, "(말을 잇지 못하고 얼굴이 하얗게 질린다)"),
                    Line(SpeakerId.Narration, "조수는 더 이상 당신의 질문에 답하지 못했다.")))
                .Set("toneAlternationWarning", Seq("성향 교차 1회 — 의문",
                    Line(SpeakerId.Assistant, "…어, 저기, 수사관님? 괜찮으신 거 맞죠…?",
                        ExpressionId.Flustered, "(갑자기 달라진 태도에 당황해 눈을 깜빡인다)")))
                .Set("toneAlternationGameOver", Seq("성향 교차 2회 — 게임오버",
                    Line(SpeakerId.Assistant, "…수사관님, 지금 저를 놀리시는 건가요?",
                        ExpressionId.Angry, "(당혹을 넘어선 표정으로 당신을 바라본다)"),
                    Line(SpeakerId.Narration, "신뢰를 잃었다. 이 상태로는 어떤 진술도 받아낼 수 없다.")))
                .Set("objectSpamWarning", Seq("오브젝트 반복 경고",
                    Line(SpeakerId.Assistant, "저… 저한테 물어보실 건 없으신가요?",
                        ExpressionId.Nervous, "(딴 곳만 보는 당신을 물끄러미)")))
                .Set("refusalByAnger", Seq("오브젝트 반복 — 분노",
                    Line(SpeakerId.Assistant, "…계속 딴 데만 보실 거면, 저는 이만 가보겠습니다.",
                        ExpressionId.Angry, "(자리에서 일어선다)")))
                .Set("onTokensExhausted", Seq("타임 토큰 소진",
                    Line(SpeakerId.Narration, "기차의 흔들림이 한 박자 느려졌다. 더 붙잡고 있을 시간은 없다."),
                    Line(SpeakerId.Assistant, "…저, 이제 정리 좀 도와드려도 될까요?", ExpressionId.Nervous)))
                .Set("onIdle", Seq("무반응",
                    Line(SpeakerId.Assistant, "…수사관님?", ExpressionId.Nervous,
                        "(침묵이 길어지자 눈치를 본다)")));

            // ============================================================ 챕터

            var chapter = Ed.CreateOrLoad<ChapterDefinition>(Root, "Chapter_Tutorial");
            chapter.Set("id", "CH_TUTORIAL")
                   .Set("displayName", "튜토리얼 — 누가 그랬지?")
                   .Set("isTutorial", true)
                   .Set("preIncidentTalk", TutorialScript.PreIncidentTalk())
                   .Set("cutsceneName", "일러스트 컷씬#1 — 주인공 독백")
                   .Set("cutscene", TutorialScript.Cutscene())
                   .Set("afterCutsceneTalk", TutorialScript.AfterCutsceneTalk())
                   .Set("intro", TutorialScript.Incident())
                   .Set("sceneObjects", Ed.L(objRibbon, objCoffee, objGlimpse,
                                             objPapers, objTypewriter, objCat))
                   .Set("gatePass", Seq("수집 100%",
                        Line(SpeakerId.Inner, "음, 이 정도면 충분하겠군.")))
                   .Set("gateWarn", Seq("수집 70% 이상",
                        Line(SpeakerId.Inner, "필요한 건 찾은 것 같긴 한데… 조금만 더 살펴볼까?")))
                   .Set("gateBlocked", Seq("수집 부족",
                        Line(SpeakerId.Inner, "뭔가 부족해. 조금 더 꼼꼼하게 봐야겠어.")))
                   .Set("firstRoundSuspects", Ed.L(sepi))
                   .Set("accusationTargets", Ed.L(sepi));

            // ============================================================ 색인

            var db = Ed.CreateOrLoad<GameDatabase>(Root, "GameDatabase");
            db.EditorSetAll(
                Ed.L(chapter),
                Ed.L(ribbon, coffee, glimpse, sawIt, catFlinch),
                Ed.L(choiceFriendly, choiceAggressive, choiceRibbon),
                Ed.L(objRibbon, objCoffee, objGlimpse, objPapers, objTypewriter, objCat),
                Ed.L(sepi));

            Ed.Save(ribbon, coffee, glimpse, sawIt, catFlinch,
                    objRibbon, objCoffee, objGlimpse, objPapers, objTypewriter, objCat,
                    choiceFriendly, choiceAggressive, choiceRibbon,
                    sepi, chapter, db);
            AssetDatabase.Refresh();

            Debug.Log("[It is You] 튜토리얼 데이터 생성 완료 — " +
                      "단서 5, 오브젝트 6(일반 3/특수 3), 선택지 3, 용의자 1, 챕터 1");
            return chapter;
        }

        // ============================================================== 헬퍼

        private static ClueDefinition Clue(string id, ClueKind kind, string unread, string read,
            string tooltip, bool keyClue)
        {
            var clue = Ed.CreateOrLoad<ClueDefinition>(ClueFolder, "Clue_" + id);
            clue.Set("id", id)
                .Set("kind", kind)
                .Set("unreadName", unread)
                .Set("readName", read)
                .Set("tooltip", tooltip)
                .Set("isKeyClue", keyClue)
                // 증언·리액션은 화면에 놓인 오브젝트가 아니므로 수집률 계산에서 뺀다.
                .Set("countsTowardCompletion", kind == ClueKind.Object);
            return clue;
        }

        private static InteractableDefinition Obj(string id, InteractableKind kind, string hoverLabel,
            ClueDefinition grantedClue, params DialogueLine[] onInspect)
        {
            var obj = Ed.CreateOrLoad<InteractableDefinition>(ObjectFolder, "Obj_" + id);
            obj.Set("id", id)
               .Set("kind", kind)
               .Set("hoverLabel", hoverLabel)
               .Set("grantedClue", grantedClue)
               .Set("onInspect", new List<DialogueLine>(onInspect))
               .Set("onReinspect", new List<DialogueLine>
               {
                   Line(SpeakerId.Inner, "더 볼 것은 없어 보인다.")
               });
            return obj;
        }

        private static ChoiceDefinition Choice(string id, ChoiceTone tone, string label,
            List<DialogueSequence> passes,
            string labelWhenInformed = null,
            List<string> labelSwapClueIds = null,
            List<string> requiredClueIds = null,
            bool availableFromStart = true)
        {
            var choice = Ed.CreateOrLoad<ChoiceDefinition>(ChoiceFolder, "Choice_" + id);
            choice.Set("id", id)
                  .Set("tone", tone)
                  .Set("label", label)
                  .Set("labelWhenInformed", labelWhenInformed ?? string.Empty)
                  .Set("labelSwapClueIds", labelSwapClueIds ?? new List<string>())
                  .Set("requiredClueIds", requiredClueIds ?? new List<string>())
                  .Set("availableFromStart", availableFromStart)
                  .Set("passes", passes)
                  .Set("tokenCost", 1);
            return choice;
        }

        private static DialogueLine Line(SpeakerId speaker, string text,
            ExpressionId expression = ExpressionId.Neutral, string direction = null)
        {
            var line = new DialogueLine(speaker, text);
            line.Set("expression", expression);
            line.Set("stageDirection", direction ?? string.Empty);
            return line;
        }

        /// <summary>이 줄이 출력되는 동안 캐릭터를 클릭하면 단서가 되는 리액션을 붙인다.</summary>
        private static DialogueLine Reacting(DialogueLine line, string clueId, float openDelay, float window)
        {
            var cue = new ReactionCue();
            cue.Set("clueId", clueId)
               .Set("openDelay", openDelay)
               .Set("window", window)
               .Set("expression", ExpressionId.Flustered)
               .Set("blurredFromRecallDepth", 2);
            line.Set("reaction", cue);
            return line;
        }

        private static DialogueSequence Seq(string note, params DialogueLine[] lines)
            => Seq(note, 0, lines);

        private static DialogueSequence Seq(string note, int trustDelta, params DialogueLine[] lines)
        {
            var seq = new DialogueSequence();
            seq.Set("note", note)
               .Set("lines", new List<DialogueLine>(lines))
               .Set("trustDelta", trustDelta);
            return seq;
        }
    }
}
