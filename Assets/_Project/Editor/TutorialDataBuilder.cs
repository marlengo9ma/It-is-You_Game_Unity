using System.Collections.Generic;
using ItIsYou.Data;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 튜토리얼 챕터 〈누가 그랬지?〉 의 데이터를 만든다.
    /// 대사와 툴팁은 전부 <see cref="TutorialScript"/> 에 있는 노션 원문이다.
    ///
    /// 원문이 없는 자리(페널티 반응, 토큰 소진 등)는 노션에 문장이 생길 때까지의 임시 대사이며,
    /// 아래에 [임시] 로 표시해 두었다.
    ///
    /// TSV 임포터가 완성되면 이 역할은 임포터가 가져간다.
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

            // 이전 구성의 오브젝트는 지운다. 남겨 두면 빈 수집품 경고가 계속 뜬다.
            DeleteIfExists(ObjectFolder + "/Obj_OBJ_PAPERS.asset");
            DeleteIfExists(ObjectFolder + "/Obj_OBJ_CAT.asset");

            // ============================================================ 수집품
            // 〈시스템 기획 및 화면 전환〉: 튜토리얼은 일반 오브젝트 3 + 특수(단서) 오브젝트 3 = 6.
            // 여섯 개 모두 우측 목록에 들어가고 수집률에 포함된다. 특수 쪽만 툴팁에 강조 구간이 있어
            // 열람창에서 '단서 오브젝트'로 승격할 수 있다.

            var ribbon = Clue("CLUE_RIBBON", ClueKind.Object,
                unread: "리본 끈", read: "포장용 고급 리본", keyClue: true,
                tooltip: TutorialScript.Tooltips.Ribbon);

            var coffee = Clue("CLUE_COFFEE", ClueKind.Object,
                unread: "커피잔", read: "엎질러졌던 커피잔", keyClue: false,
                tooltip: TutorialScript.Tooltips.Coffee);

            var glimpse = Clue("CLUE_GLIMPSE", ClueKind.Object,
                unread: "???", read: "언듯 보이는 무언가", keyClue: true,
                tooltip: TutorialScript.Tooltips.Glimpse);

            var newspaper = Clue("CLUE_NEWSPAPER", ClueKind.Object,
                unread: "신문", read: "오늘자 신문", keyClue: false,
                tooltip: TutorialScript.Tooltips.Newspaper);

            var typewriter = Clue("CLUE_TYPEWRITER", ClueKind.Object,
                unread: "타자기", read: "휴대용 타자기", keyClue: false,
                tooltip: TutorialScript.Tooltips.Typewriter);

            var roomInfo = Clue("CLUE_ROOMINFO", ClueKind.Object,
                unread: "객실 안내", read: "객실 정보", keyClue: false,
                tooltip: TutorialScript.Tooltips.RoomInfo);

            // 심문에서만 얻을 수 있는 단서 두 가지. 툴팁은 [임시] — 노션에 항목이 없다.
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
            // 겉모습으로는 둘을 구분할 수 없어야 한다. 마우스 오버 반응도 동일하다.
            // 클릭 시 화면 중앙에 이미지 팝업 + 설명 대사(툴팁의 >> 줄, 있는 것만) + 목록 수집.

            var objRibbon = Obj("OBJ_RIBBON", InteractableKind.Special, "구겨진 끈", ribbon);
            var objCoffee = Obj("OBJ_COFFEE", InteractableKind.Special, "쓰러진 잔", coffee);
            var objGlimpse = Obj("OBJ_GLIMPSE", InteractableKind.Special, "고양이 아래", glimpse);
            var objNewspaper = Obj("OBJ_NEWSPAPER", InteractableKind.Ordinary, "펼쳐진 신문", newspaper,
                TutorialScript.NewspaperInspect().ToArray());
            var objTypewriter = Obj("OBJ_TYPEWRITER", InteractableKind.Ordinary, "휴대용 타자기", typewriter);
            var objRoomInfo = Obj("OBJ_ROOMINFO", InteractableKind.Ordinary, "객실 안내", roomInfo,
                TutorialScript.RoomInfoInspect().ToArray());

            // ========================================================== 선택지

            // A — 친근. 경계를 낮추며 행방을 묻는다.
            var choiceFriendly = Choice("CH_SEPI_FRIENDLY", ChoiceTone.Friendly,
                label: TutorialScript.ChoiceALabel,
                passes: new List<DialogueSequence> { TutorialScript.ChoiceAPass() },
                idleFollowUp: TutorialScript.ChoiceAIdle());

            // B — 강압. 절도를 의심하며 압박한다. 시간 경과 뒤에 페널티 설명 팝업.
            var choiceAggressive = Choice("CH_SEPI_AGGRESSIVE", ChoiceTone.Aggressive,
                label: TutorialScript.ChoiceBLabel,
                passes: new List<DialogueSequence> { TutorialScript.ChoiceBPass() },
                idleFollowUp: TutorialScript.ChoiceBIdle());

            // C — 단서 제시. 리본을 열람창에서 '단서 오브젝트'로 정리해 두어야 나타난다.
            var cPass = TutorialScript.ChoiceCPass();
            Reacting(cPass.Lines[6], clueId: "REA_CAT_FLINCH", openDelay: 0.35f, window: 2.6f);

            var choiceRibbon = Choice("CH_SEPI_RIBBON", ChoiceTone.Evidence,
                label: TutorialScript.ChoiceCLabel,
                passes: new List<DialogueSequence> { cPass },
                requiredClueIds: Ed.L("CLUE_RIBBON"),
                requiredPromotedClueIds: Ed.L("CLUE_RIBBON"));

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
                // ---- 아래 반응 대사는 [임시]. 노션에 "용의자의 의문 및 당황에 대한 반응" 같은 지시만 있고 문장은 없다. ----
                .Set("aggressionWarning", Seq("[임시] 강압 경고",
                    Line(SpeakerId.Assistant, "수사관님… 저, 정말 아니에요…",
                        ExpressionId.Fearful, "(목소리가 작아지며 어깨가 움츠러든다)")))
                .Set("refusalByFear", Seq("[임시] 강압 누적 — 실신",
                    Line(SpeakerId.Assistant, "…죄, 죄송합니다…",
                        ExpressionId.Fearful, "(말을 잇지 못하고 얼굴이 하얗게 질린다)"),
                    Line(SpeakerId.Narration, "조수는 더 이상 당신의 질문에 답하지 못했다.")))
                .Set("toneAlternationWarning", Seq("[임시] 성향 교차 1회 — 의문",
                    Line(SpeakerId.Assistant, "…어, 저기, 수사관님? 괜찮으신 거 맞죠…?",
                        ExpressionId.Flustered, "(갑자기 달라진 태도에 당황해 눈을 깜빡인다)")))
                .Set("toneAlternationGameOver", Seq("[임시] 성향 교차 2회 — 게임오버",
                    Line(SpeakerId.Assistant, "…수사관님, 지금 저를 놀리시는 건가요?",
                        ExpressionId.Angry, "(당혹을 넘어선 표정으로 당신을 바라본다)"),
                    Line(SpeakerId.Narration, "신뢰를 잃었다. 이 상태로는 어떤 진술도 받아낼 수 없다.")))
                .Set("objectSpamWarning", Seq("[임시] 오브젝트 반복 경고",
                    Line(SpeakerId.Assistant, "저… 저한테 물어보실 건 없으신가요?",
                        ExpressionId.Nervous, "(딴 곳만 보는 당신을 물끄러미)")))
                .Set("refusalByAnger", Seq("[임시] 오브젝트 반복 — 분노",
                    Line(SpeakerId.Assistant, "…계속 딴 데만 보실 거면, 저는 이만 가보겠습니다.",
                        ExpressionId.Angry, "(자리에서 일어선다)")))
                .Set("onTokensExhausted", Seq("[임시] 타임 토큰 소진",
                    Line(SpeakerId.Narration, "기차의 흔들림이 한 박자 느려졌다. 더 붙잡고 있을 시간은 없다."),
                    Line(SpeakerId.Assistant, "…저, 이제 정리 좀 도와드려도 될까요?", ExpressionId.Nervous)))
                // ---- 여기부터는 원문 ----
                .Set("onEnterInterrogation", TutorialScript.SepiOpening())
                .Set("onEndInterrogation", TutorialScript.SepiEnding())
                .Set("idleRounds", TutorialScript.SepiIdleRounds())
                .Set("selectable", true)
                .Set("onIdle", Seq("[임시] 무반응",
                    Line(SpeakerId.Assistant, "…수사관님?", ExpressionId.Nervous,
                        "(침묵이 길어지자 눈치를 본다)")));

            // ---- 검은 고양이 ----
            // 기획: 튜토리얼에서는 가이드가 조수를 먼저 고르게 유도하므로,
            // 지금은 고를 수 없게 잠가 둔다. 심문 대사는 시나리오가 확정되면 채운다.
            // (선택지 제목만 있음 — A 쓰다듬는다 / B 부자연스러운 앞발을 치워본다 / C 단서에 대해서(발바닥 모양의 커피자국))
            var cat = Ed.CreateOrLoad<SuspectDefinition>(SuspectFolder, "Suspect_Cat");
            cat.Set("id", "CAT")
               .Set("displayName", "검은 고양이")
               .Set("speaker", SpeakerId.Cat)
               .Set("profile", "기차 안을 제집처럼 돌아다니던 녀석. 사건 당시 테이블 위에 있었다.")
               .Set("timeTokens", 4)
               .Set("selectable", false)
               .Set("lockedReason", "…먼저 사람부터 상대하지.");

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
                                             objNewspaper, objTypewriter, objRoomInfo))
                   .Set("gatePass", TutorialScript.GatePass())
                   .Set("gateWarn", TutorialScript.GateWarn())
                   .Set("gateBlocked", TutorialScript.GateBlocked())
                   .Set("beforeSuspectSelect", TutorialScript.BeforeSuspectSelect())
                   .Set("suspectSelectTalk", TutorialScript.SuspectSelectTalk())
                   .Set("suspectSelectPrompt", "누구부터 이야기를 들어볼까.")
                   .Set("firstRoundSuspects", Ed.L(sepi, cat))
                   .Set("interrogationTitleFormat", "용의자 ‘{name}’ 심문 시작")
                   .Set("tutorialPopups", TutorialScript.TutorialPopups())
                   .Set("accusationTargets", Ed.L(sepi, cat));

            // ============================================================ 색인

            var db = Ed.CreateOrLoad<GameDatabase>(Root, "GameDatabase");
            db.EditorSetAll(
                Ed.L(chapter),
                Ed.L(ribbon, coffee, glimpse, newspaper, typewriter, roomInfo, sawIt, catFlinch),
                Ed.L(choiceFriendly, choiceAggressive, choiceRibbon),
                Ed.L(objRibbon, objCoffee, objGlimpse, objNewspaper, objTypewriter, objRoomInfo),
                Ed.L(sepi, cat));

            Ed.Save(ribbon, coffee, glimpse, newspaper, typewriter, roomInfo, sawIt, catFlinch,
                    objRibbon, objCoffee, objGlimpse, objNewspaper, objTypewriter, objRoomInfo,
                    choiceFriendly, choiceAggressive, choiceRibbon,
                    sepi, cat, chapter, db);
            AssetDatabase.Refresh();

            Debug.Log("[It is You] 튜토리얼 데이터 생성 완료 — " +
                      "수집품 6(단서 3/일반 3) + 증언·리액션 2, 선택지 3, 용의자 2, 튜토리얼 팝업 4, 챕터 1");
            return chapter;
        }

        // ============================================================== 헬퍼

        private static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
        }

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
               // 재조사 대사는 원문에 없다. 비워 두면 이미지 팝업만 다시 뜬다.
               .Set("onReinspect", new List<DialogueLine>());
            return obj;
        }

        private static ChoiceDefinition Choice(string id, ChoiceTone tone, string label,
            List<DialogueSequence> passes,
            DialogueSequence idleFollowUp = null,
            string labelWhenInformed = null,
            List<string> labelSwapClueIds = null,
            List<string> requiredClueIds = null,
            List<string> requiredPromotedClueIds = null,
            bool availableFromStart = true)
        {
            var choice = Ed.CreateOrLoad<ChoiceDefinition>(ChoiceFolder, "Choice_" + id);
            choice.Set("id", id)
                  .Set("tone", tone)
                  .Set("label", label)
                  .Set("labelWhenInformed", labelWhenInformed ?? string.Empty)
                  .Set("labelSwapClueIds", labelSwapClueIds ?? new List<string>())
                  .Set("requiredClueIds", requiredClueIds ?? new List<string>())
                  .Set("requiredPromotedClueIds", requiredPromotedClueIds ?? new List<string>())
                  .Set("availableFromStart", availableFromStart)
                  .Set("passes", passes)
                  .Set("idleFollowUp", idleFollowUp ?? new DialogueSequence())
                  .Set("tokenCost", 1);
            return choice;
        }

        private static DialogueLine Line(SpeakerId speaker, string text,
            ExpressionId expression = ExpressionId.Neutral, string direction = null)
            => TutorialScript.Line(speaker, text, expression, direction);

        /// <summary>이 줄이 출력되는 동안 캐릭터를 클릭하면 단서가 되는 리액션을 붙인다.</summary>
        private static void Reacting(DialogueLine line, string clueId, float openDelay, float window)
        {
            var cue = new ReactionCue();
            cue.Set("clueId", clueId)
               .Set("openDelay", openDelay)
               .Set("window", window)
               .Set("expression", ExpressionId.Flustered)
               .Set("blurredFromRecallDepth", 2);
            line.Set("reaction", cue);
        }

        private static DialogueSequence Seq(string note, params DialogueLine[] lines)
            => TutorialScript.Seq(note, lines);
    }
}
