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
        private const string DeductionFolder = Root + "/Deduction";
        private const string AccusationFolder = Root + "/Accusation";

        [MenuItem("It is You/1. 튜토리얼 데이터 생성", priority = 1)]
        public static ChapterDefinition Build()
        {
            Ed.EnsureFolder(ClueFolder);
            Ed.EnsureFolder(ChoiceFolder);
            Ed.EnsureFolder(SuspectFolder);
            Ed.EnsureFolder(ObjectFolder);
            Ed.EnsureFolder(DeductionFolder);
            Ed.EnsureFolder(AccusationFolder);

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

            // 정황은 누구에게서 나온 것인지 적어 둔다 — 추론 서류철의 Leads 장이 그 사람의 인적 사항 아래로 묶는다.
            sawIt.Set("sourceSuspectId", "SEPI");
            catFlinch.Set("sourceSuspectId", "SEPI");

            // [임시] 단서 조합의 결과 — '추론' 카드. 노션에 항목이 없다. 개선안: 결과가 목록에 남고 2차 심문의 단서 제시가 여기에 열린다.
            var penUnderCat = Clue("DED_PEN_UNDER_CAT", ClueKind.Deduced,
                unread: "가설",
                read: "만년필은 고양이 아래에",
                keyClue: false,
                tooltip:
                "'언듯 보이는 무언가'와 '본 적은 있다'는 말을 이어 세운 가설.\n\n" +
                "만년필은 사라진 것이 아니라, 처음부터 저 고양이가 깔고 앉아 있었다. " +
                "커피가 쏟아진 것도, 리본에 고양이 털이 박힌 것도 그 무렵의 일이다.\n\n" +
                "조수는 이 사실을 알고 있었던 듯한 얼굴을 했다. 다시 물어볼 차례다.");

            var sepiHidesCat = Clue("DED_SEPI_HIDES_CAT", ClueKind.Deduced,
                unread: "가설",
                read: "세피는 고양이 일을 숨기고 있다",
                keyClue: false,
                tooltip:
                "리본과 굳은 표정을 이어 세운 가설.\n\n" +
                "리본에 고양이 털이 박힌 것은 숨길 일이 아니다. 그런데 '고양이'라는 말에 조수의 얼굴이 굳었다. " +
                "더러워진 리본이 아니라, 고양이가 이 객실에 있다는 사실 자체를 숨기고 있는 듯하다.");

            var spillMoment = Clue("DED_SPILL_MOMENT", ClueKind.Deduced,
                unread: "가설",
                read: "커피가 쏟아진 그때",
                keyClue: false,
                tooltip:
                "커피잔과 '본 적은 있다'는 말을 이어 세운 가설.\n\n" +
                "만년필이 테이블에서 사라진 것은 커피가 쏟아진 무렵이다. 그 소동 속에서 무언가가 자리를 옮겼다.");

            // ==================================================== 수집 오브젝트
            // 겉모습으로는 둘을 구분할 수 없어야 한다. 마우스 오버 반응도 동일하다.
            // 클릭 시 화면 중앙에 이미지 팝업 + 설명 대사(툴팁의 >> 줄, 있는 것만) + 목록 수집.

            // [임시] 아래 네 오브젝트의 수집 대사는 원문에 없고, 이전 버전에서 지어 넣은 것을 유지한다 (TutorialScript 참고).
            var objRibbon = Obj("OBJ_RIBBON", InteractableKind.Special, "구겨진 끈", ribbon,
                TutorialScript.RibbonInspect().ToArray());
            var objCoffee = Obj("OBJ_COFFEE", InteractableKind.Special, "쓰러진 잔", coffee,
                TutorialScript.CoffeeInspect().ToArray());
            var objGlimpse = Obj("OBJ_GLIMPSE", InteractableKind.Special, "고양이 아래", glimpse,
                TutorialScript.GlimpseInspect().ToArray());
            var objNewspaper = Obj("OBJ_NEWSPAPER", InteractableKind.Ordinary, "펼쳐진 신문", newspaper,
                TutorialScript.NewspaperInspect().ToArray());
            var objTypewriter = Obj("OBJ_TYPEWRITER", InteractableKind.Ordinary, "휴대용 타자기", typewriter,
                TutorialScript.TypewriterInspect().ToArray());
            var objRoomInfo = Obj("OBJ_ROOMINFO", InteractableKind.Ordinary, "객실 안내", roomInfo,
                TutorialScript.RoomInfoInspect().ToArray());

            // ====================================================== 심문 오브젝트
            // 심문 화면의 인물·배경 요소. 아무것도 수집하지 않고 대화만 이어진다. 한 번 고르면 끝(2026-10-11 노션).
            var intCat = Obj("OBJ_INT_CAT", InteractableKind.InterrogationObject, "고양이 엉덩이", null,
                TutorialScript.InterrogationObjectCat().ToArray());
            var intProfile = Obj("OBJ_INT_PROFILE", InteractableKind.InterrogationObject, "보조 수사관 프로필 서류", null,
                TutorialScript.InterrogationObjectProfile().ToArray());

            // ========================================================== 선택지

            // A — 친근. 경계를 낮추며 행방을 묻는다. 회차마다 문구와 대사가 다르고,
            // B를 먼저 골랐다면 초회가 통째로 바뀐다 (ChoiceARounds 안의 변형 칸).
            var choiceFriendly = Choice("CH_SEPI_FRIENDLY", ChoiceTone.Friendly,
                label: TutorialScript.ChoiceALabel,
                passes: TutorialScript.ChoiceARounds());

            // B — 강압. 절도를 의심하며 압박한다. 초회의 시간 경과 뒤에 페널티 설명 팝업.
            var choiceAggressive = Choice("CH_SEPI_AGGRESSIVE", ChoiceTone.Aggressive,
                label: TutorialScript.ChoiceBLabel,
                passes: TutorialScript.ChoiceBRounds());

            // C — 단서 제시. 리본을 열람창에서 '단서 오브젝트'로 정리해 두어야 나타난다.
            var cRounds = TutorialScript.ChoiceCRounds();
            Reacting(cRounds[0].Sequence.Lines[6], clueId: "REA_CAT_FLINCH", openDelay: 0.35f, window: 2.6f);

            var choiceRibbon = Choice("CH_SEPI_RIBBON", ChoiceTone.Evidence,
                label: TutorialScript.ChoiceCLabel,
                passes: cRounds,
                requiredClueIds: Ed.L("CLUE_RIBBON"),
                requiredPromotedClueIds: Ed.L("CLUE_RIBBON"));

            // ---- [임시] 2차 심문 선택지. 노션에 2차 스크립트가 없어 짧게 지어 넣었다. 토큰은 1차의 절반. ----
            var choice2Friendly = Choice("CH_SEPI2_FRIENDLY", ChoiceTone.Friendly,
                label: TutorialScript.Choice2ALabel,
                passes: TutorialScript.Single(TutorialScript.Choice2APass()));
            var choice2Aggressive = Choice("CH_SEPI2_AGGRESSIVE", ChoiceTone.Aggressive,
                label: TutorialScript.Choice2BLabel,
                passes: TutorialScript.Single(TutorialScript.Choice2BPass()));
            // 추론 카드가 있어야 열린다. 없으면 ??? 로 잠긴 채 보인다 — 조합의 결과가 여기서 보인다.
            var choice2Deduced = Choice("CH_SEPI2_DEDUCED", ChoiceTone.Evidence,
                label: TutorialScript.Choice2CLabel,
                passes: TutorialScript.Single(TutorialScript.Choice2CPass()),
                requiredClueIds: Ed.L("DED_PEN_UNDER_CAT"));
            choice2Deduced.Set("showLockedPlaceholder", true).Set("lockedLabel", "???");
            // 가설 2 로 열리는 단서 제시 — 심문은 이어진다.
            var choice2Cat = Choice("CH_SEPI2_CAT", ChoiceTone.Evidence,
                label: TutorialScript.Choice2DLabel,
                passes: TutorialScript.Single(TutorialScript.Choice2DPass()),
                requiredClueIds: Ed.L("DED_SEPI_HIDES_CAT"));
            choice2Cat.Set("showLockedPlaceholder", true).Set("lockedLabel", "???");

            // ========================================================== 단서 조합 (가설)
            // [임시] 조합식 3개. 물품 + 정황 → 추론. 튜토리얼은 가설 2개까지 저장 — 셋째를 세우면 하나를 지운다.
            var hypPen = Ed.CreateOrLoad<HypothesisRecipe>(DeductionFolder, "Hyp_PenUnderCat");
            hypPen.Set("id", "HYP_PEN_UNDER_CAT")
                  .Set("title", "만년필은 고양이 아래에")
                  .Set("objectClueId", "CLUE_GLIMPSE")
                  .Set("leadClueId", "TES_SAW_IT")
                  .Set("result", penUnderCat)
                  .Set("onSuccess", TutorialScript.HypPenUnderCat());

            var hypHides = Ed.CreateOrLoad<HypothesisRecipe>(DeductionFolder, "Hyp_SepiHidesCat");
            hypHides.Set("id", "HYP_SEPI_HIDES_CAT")
                    .Set("title", "세피는 고양이 일을 숨기고 있다")
                    .Set("objectClueId", "CLUE_RIBBON")
                    .Set("leadClueId", "REA_CAT_FLINCH")
                    .Set("result", sepiHidesCat)
                    .Set("onSuccess", TutorialScript.HypSepiHidesCat());

            var hypSpill = Ed.CreateOrLoad<HypothesisRecipe>(DeductionFolder, "Hyp_SpillMoment");
            hypSpill.Set("id", "HYP_SPILL_MOMENT")
                    .Set("title", "커피가 쏟아진 그때")
                    .Set("objectClueId", "CLUE_COFFEE")
                    .Set("leadClueId", "TES_SAW_IT")
                    .Set("result", spillMoment)
                    .Set("onSuccess", TutorialScript.HypSpillMoment());

            DeleteIfExists(DeductionFolder + "/Deduction_TUT_Q1.asset");

            // ========================================================== 지목 갈래
            // [임시] 가설 3 × 용의자 2 = 6쌍 + 기본 갈래. 갈래마다 추론 2개.
            AccusationBranch Branch(string fileName, string hypId, string suspectId, List<TutorialScript.Acc> opts)
            {
                var b = Ed.CreateOrLoad<AccusationBranch>(AccusationFolder, fileName);
                var options = new List<AccusationOption>();
                foreach (var o in opts)
                {
                    var opt = new AccusationOption();
                    opt.Set("label", o.Label)
                       .Set("speech", o.Speech ?? new DialogueSequence())
                       .Set("plea", o.Plea ?? new DialogueSequence())
                       .Set("refuteClueIds", o.RefuteIds ?? new List<string>())
                       .Set("refuteWrong", o.RefuteWrong ?? new DialogueSequence())
                       .Set("refuteSuccess", o.RefuteSuccess ?? new DialogueSequence())
                       .Set("resultTitle", o.ResultTitle ?? string.Empty)
                       .Set("result", o.Result ?? new DialogueSequence())
                       .Set("returnsToSelection", o.Returns);
                    options.Add(opt);
                }
                b.Set("id", fileName.ToUpperInvariant()).Set("hypothesisClueId", hypId ?? string.Empty)
                 .Set("suspectId", suspectId ?? string.Empty).Set("options", options);
                return b;
            }

            var accPenSepi = Branch("Acc_Pen_Sepi", "DED_PEN_UNDER_CAT", "SEPI", TutorialScript.AccPenSepi());
            var accPenCat = Branch("Acc_Pen_Cat", "DED_PEN_UNDER_CAT", "CAT", TutorialScript.AccPenCat());
            var accHidesSepi = Branch("Acc_Hides_Sepi", "DED_SEPI_HIDES_CAT", "SEPI", TutorialScript.AccHidesSepi());
            var accHidesCat = Branch("Acc_Hides_Cat", "DED_SEPI_HIDES_CAT", "CAT", TutorialScript.AccHidesCat());
            var accSpillSepi = Branch("Acc_Spill_Sepi", "DED_SPILL_MOMENT", "SEPI", TutorialScript.AccSpillSepi());
            var accSpillCat = Branch("Acc_Spill_Cat", "DED_SPILL_MOMENT", "CAT", TutorialScript.AccSpillCat());
            var accDefault = Branch("Acc_Default", null, null, TutorialScript.AccDefault());

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
                // [임시] 2차 심문 — 선택지 3, 토큰 3 (1차의 절반), 첫 마디 별도
                .Set("secondRoundChoices", Ed.L(choice2Friendly, choice2Aggressive, choice2Deduced, choice2Cat))
                .Set("secondRoundTokens", 3)
                .Set("onEnterSecondInterrogation", TutorialScript.SepiSecondOpening())
                // ---- 아래 반응 대사는 [임시]. 노션에 "용의자의 의문 및 당황에 대한 반응" 같은 지시만 있고 문장은 없다. ----
                // 2026-10-11: 강압 3회째의 [임시] 경고 줄은 뺐다. 노션의 B선택지 3회 연속 대사가 그 자리를 채우고,
                // 규칙상 경고는 '팝업'이다 — 대사 뒤에 한 줄을 더 얹으면 같은 말을 두 번 하게 된다.
                .Set("aggressionWarning", new DialogueSequence())
                .Set("refusalByFear", Seq("[임시] 강압 누적 — 실신",
                    Line(SpeakerId.Assistant, "…죄, 죄송합니다…",
                        ExpressionId.Fearful, "(말을 잇지 못하고 얼굴이 하얗게 질린다)"),
                    Line(SpeakerId.Narration, "조수는 더 이상 당신의 질문에 답하지 못했다.")))
                // 2026-10-11 제작자 지시: 교차 선택의 튜토리얼 팝업은 그 선택지의 대사가 다 나온 뒤에 바로 뜬다.
                // 팝업 앞에 끼던 [임시] '괜찮으신 거 맞죠…?' 줄은 뺐다.
                .Set("toneAlternationWarning", new DialogueSequence())
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
                // 노션: [타임 토큰을 전부 소모하거나, 심문 종료 버튼을 통해 정상적인 심문 종료 시] — 같은 마무리.
                .Set("onTokensExhausted", TutorialScript.SepiEnding())
                .Set("onTrustBroken", TutorialScript.SepiTrustBroken())
                .Set("interrogationObjects", Ed.L(intCat, intProfile))
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
                   // 무반응: 대사 종료 5초 뒤 타이머 시작 → 15초에 판정. 7초부터 초침 연출.
                   .Set("idleSeconds", 15f)
                   .Set("idleGraceAfterDialogueSeconds", 5f)
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
                   .Set("accusationTargets", Ed.L(sepi, cat))
                   // [임시] 단서 조합 — 잉크 4, 가설 최대 2, 조합식 3, 진입/실패/마무리/자기 비하 대사
                   .Set("hypotheses", Ed.L(hypPen, hypHides, hypSpill))
                   .Set("deductionInk", 4)
                   .Set("maxHypotheses", 2)
                   .Set("deductionFailures", TutorialScript.DeductionFailures())
                   .Set("deductionDuplicate", TutorialScript.DeductionDuplicate())
                   .Set("deductionReplacePrompt", TutorialScript.DeductionReplacePrompt())
                .Set("noSuspectsLeft", TutorialScript.NoSuspectsLeft())
                   .Set("deductionInkOut", TutorialScript.DeductionInkOut())
                   .Set("deductionIntro", TutorialScript.DeductionIntro())
                   .Set("deductionOutro", TutorialScript.DeductionOutro())
                   .Set("selfLoathingEnding", TutorialScript.SelfLoathingEnding())
                   // [임시] 2차 심문 — 공용 타임 토큰 6, 지목 전 대사
                   .Set("secondRoundSelectPrompt", "가설을 들고, 누구에게 다시 물어볼까.")
                   .Set("hypothesisPickPrompt", "어떤 가설로 물어볼까.")
                   .Set("beforeAccusation", TutorialScript.BeforeAccusation())
                   // [임시] 지목 — 가설 → 용의자 → 추론 두 갈래 → 호소 → 단서로 반박(맞을 때까지) → 결과
                   .Set("accusationHypothesisPrompt", "어떤 가설로 답을 낼까.")
                   .Set("accusationPrompt", "누가 가져갔다고 보십니까?")
                   .Set("accusationReasoningPrompt", "그렇다면, 어떻게 된 일인가.")
                   .Set("refutePrompt", "우측에서 반박할 단서를 고른다.")
                   .Set("accusationBranches", Ed.L(accPenSepi, accPenCat, accHidesSepi, accHidesCat, accSpillSepi, accSpillCat))
                   .Set("defaultAccusationBranch", accDefault);

            // ============================================================ 색인

            var db = Ed.CreateOrLoad<GameDatabase>(Root, "GameDatabase");
            db.EditorSetAll(
                Ed.L(chapter),
                Ed.L(ribbon, coffee, glimpse, newspaper, typewriter, roomInfo, sawIt, catFlinch, penUnderCat, sepiHidesCat, spillMoment),
                Ed.L(choiceFriendly, choiceAggressive, choiceRibbon, choice2Friendly, choice2Aggressive, choice2Deduced, choice2Cat),
                Ed.L(objRibbon, objCoffee, objGlimpse, objNewspaper, objTypewriter, objRoomInfo, intCat, intProfile),
                Ed.L(sepi, cat));

            Ed.Save(ribbon, coffee, glimpse, newspaper, typewriter, roomInfo, sawIt, catFlinch, penUnderCat, sepiHidesCat, spillMoment,
                    objRibbon, objCoffee, objGlimpse, objNewspaper, objTypewriter, objRoomInfo, intCat, intProfile,
                    choiceFriendly, choiceAggressive, choiceRibbon, choice2Friendly, choice2Aggressive, choice2Deduced, choice2Cat,
                    hypPen, hypHides, hypSpill, accPenSepi, accPenCat, accHidesSepi, accHidesCat, accSpillSepi, accSpillCat, accDefault,
                    sepi, cat, chapter, db);
            AssetDatabase.Refresh();

            Debug.Log("[It is You] 튜토리얼 데이터 생성 완료 — " +
                      "수집품 6(단서 3/일반 3) + 증언·리액션 2 + 가설 3, 선택지 3+4, 조합식 3, 지목 갈래 7, 용의자 2, 튜토리얼 팝업 6, 챕터 1");
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
            List<ChoicePass> passes,
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
