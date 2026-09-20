using System.Collections.Generic;
using ItIsYou.Core;
using ItIsYou.Data;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 게임 시작부터 사건 직전까지의 도입부 데이터를 만든다.
    ///
    /// 노션 시나리오의 〈인트로 카툰〉 흐름을 그대로 장면 단위로 끊었다.
    /// 마지막 두 장면(기차 탑승 ~ 자리를 비우기 전)은 인트로 카툰과
    /// 튜토리얼 챕터의 첫 줄("당신은 잠시 자리를 비웠다 돌아왔다") 사이를
    /// 잇기 위해 덧붙인 연결부다. 노션 원문이 확정되면 이 두 장면만 갈아 끼우면 된다.
    /// </summary>
    public static class PrologueDataBuilder
    {
        private const string Root = "Assets/_Project/Data";

        [MenuItem("It is You/1-b. 인트로 데이터 생성", priority = 3)]
        public static PrologueDefinition Build()
        {
            var prologue = Ed.CreateOrLoad<PrologueDefinition>(Root, "Prologue_Intro");

            var beats = new List<PrologueBeat>
            {
                // ---------------------------------------------------------- 1
                Beat("기차역 — 플랫폼",
                    Seq("도착",
                        Line(SpeakerId.Narration,
                            "증기가 플랫폼을 한 번 쓸고 지나갔다. 출발까지는 아직 시간이 남아 있었다."),
                        Line(SpeakerId.Narration,
                            "본부에서 온 전보에는 사건의 이름 대신 열차 번호와 시각만 적혀 있었다. " +
                            "그리고 한 줄, '보조 수사관이 동행함'."),
                        Line(SpeakerId.Inner, "…보조라. 혼자가 편한데."))),

                // ---------------------------------------------------------- 2
                Beat("기차역 — 대합실 테이블",
                    Seq("테이블",
                        Line(SpeakerId.Narration,
                            "당신은 대합실 구석 테이블에 앉아 기다리기로 했다."),
                        Line(SpeakerId.Narration,
                            "테이블 위에는 아직 포장을 마치지 못한 휴대용 타자기 한 대와, " +
                            "몇 번을 시도하다 결국 포기한 붉은 리본이 늘어져 있다."),
                        Line(SpeakerId.Narration,
                            "그 옆으로는 반쯤 남은 샌드위치와, 잔을 들었다 놓은 자리마다 남은 동그란 커피 자국."),
                        Line(SpeakerId.Inner, "매듭 하나가 사람을 이렇게까지 무력하게 만들 줄이야."))),

                // ---------------------------------------------------------- 3
                // 기획서의 "전달받은 서류 내용 팝업(상호작용)".
                // 조수의 이력을 플레이어가 직접 읽게 해 둬야, 본편에서 주인공이 품는
                // 의심의 근거를 플레이어도 같은 시점에 갖게 된다.
                Beat("기차역 — 전달받은 서류",
                    Seq("서류를 꺼낸다",
                        Line(SpeakerId.Narration,
                            "가방에서 어제 전달받은 서류를 꺼냈다. 동행할 보조 수사관의 인적사항이다."),
                        Line(SpeakerId.Inner, "음…")),
                    Prop(
                        prompt: "서류를 들여다본다",
                        title: "보조 수사관 인적사항",
                        body:
                        "성명 : 세피\n" +
                        "소속 : 본부 직할 · 임시 배치\n" +
                        "동행 사유 : 현장 기록 및 진술 정리\n\n" +
                        "특기사항\n" +
                        "  · <hl>순간 기억 능력</hl> — 공인 도장 있음\n" +
                        "  · 속기사 자격\n\n" +
                        "비고 : (공란)",
                        afterReading: Seq("만년필로 한 줄 적는다",
                            Line(SpeakerId.Inner,
                                "순간 기억 능력에 공인 도장까지. 서류상으로는 나무랄 데가 없군."),
                            Line(SpeakerId.Narration,
                                "출발 시각이 지났다. 당신은 만년필을 꺼내 비고란에 한 줄을 적었다."),
                            Line(SpeakerId.Narration,
                                "― <hl>시간 약속에 대한 개념이 부족함.</hl>"),
                            Line(SpeakerId.Inner, "…첫 줄부터 이런 걸 적게 될 줄은 몰랐는데.")))),

                // ---------------------------------------------------------- 4
                Beat("기차역 — 플랫폼",
                    Seq("조수의 등장",
                        Line(SpeakerId.Narration, "그때 플랫폼 저편에서 누군가 숨차게 달려왔다."),
                        Line(SpeakerId.Assistant, "안녕하세요 조사관님…! 하, 하…",
                            ExpressionId.Flustered, "(무릎을 짚고 숨을 고르며)"),
                        Line(SpeakerId.Assistant, "이번 사건에 대한 개요 파일입니다!",
                            ExpressionId.Smile, "(숨도 고르기 전에 파일부터 내민다)"),
                        Line(SpeakerId.Inner, "…인사보다 서류가 먼저 나오는 건 나쁘지 않군."),
                        Line(SpeakerId.Protagonist, "받죠. …출발하죠."))),

                // ---------------------------------------------------------- 5
                Beat("기차역 — 개찰구",
                    Seq("타자기",
                        Line(SpeakerId.Assistant, "앗, 조사관님. 저 타자기는요?",
                            ExpressionId.Neutral, "(테이블 쪽을 가리키며)"),
                        Line(SpeakerId.Protagonist, "…저 타자기도 챙겨요. 당신 겁니다."),
                        Line(SpeakerId.Assistant, "…네? 저, 정말요?!",
                            ExpressionId.Surprised),
                        Line(SpeakerId.Narration,
                            "조수는 타자기를 두 팔로 끌어안고 종종걸음으로 뒤를 따라왔다."),
                        Line(SpeakerId.Inner, "리본은… 뭐, 두고 가도 상관없겠지."))),

                // ---------------------------------------------------------- 6
                // 여기부터가 튜토리얼 챕터 첫 줄로 이어지는 연결부다.
                Beat("기차 — 객실",
                    Seq("자리에 앉다",
                        Line(SpeakerId.Narration,
                            "객실은 조용했다. 창밖으로 역사가 천천히 밀려나기 시작했다."),
                        Line(SpeakerId.Narration,
                            "당신은 건네받은 개요 파일을 테이블 위에 펼치고, 새로 커피를 한 잔 시켰다."),
                        Line(SpeakerId.Assistant, "정리는 제가 할게요! 순서대로 놓아둘까요?",
                            ExpressionId.Smile),
                        Line(SpeakerId.Protagonist, "…그대로 두세요. 잠깐 다녀오겠습니다."),
                        Line(SpeakerId.Narration,
                            "당신은 만년필을 서류 뭉치 위에 올려두고 자리를 비웠다."),
                        Line(SpeakerId.Inner, "…금방 돌아올 테니까."))),
            };

            prologue.Set("id", "PROLOGUE_INTRO")
                    .Set("displayName", "인트로 — 출발 전")
                    .Set("beats", beats)
                    .Set("nextSceneName", SceneRoutes.TutorialChapter);

            Ed.Save(prologue);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 인트로 데이터 생성 완료 — 장면 {beats.Count}개, " +
                      $"다음 씬: {SceneRoutes.TutorialChapter}");
            return prologue;
        }

        // ============================================================== 헬퍼

        private static PrologueBeat Beat(string backdropName, DialogueSequence lines,
            PrologueProp interaction = null)
        {
            var beat = new PrologueBeat();
            beat.Set("backdropName", backdropName)
                .Set("lines", lines)
                .Set("interaction", interaction);
            return beat;
        }

        private static PrologueProp Prop(string prompt, string title, string body,
            DialogueSequence afterReading)
        {
            var prop = new PrologueProp();
            prop.Set("prompt", prompt)
                .Set("documentTitle", title)
                .Set("documentBody", body)
                .Set("afterReading", afterReading);
            return prop;
        }

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
