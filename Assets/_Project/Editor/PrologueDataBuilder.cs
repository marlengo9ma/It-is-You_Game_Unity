using System.Collections.Generic;
using ItIsYou.Core;
using ItIsYou.Data;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 인트로 카툰의 컷 구성을 만든다.
    ///
    /// 노션 시나리오 〈인트로(카툰)〉 의 서술을 컷 단위로 끊은 것이다.
    /// 여기에 들어가는 것은 대사가 아니라 "이 컷에 무엇이 그려지는가" 뿐이다.
    /// 카툰 파트는 텍스트를 읽는 구간이 아니라 그림이 넘어가는 구간이라서,
    /// 컷 이름은 그림이 들어오기 전까지만 화면에 표시되는 개발용 라벨이다.
    ///
    /// 컷 그림이 들어오면 Prologue_Intro.asset 의 각 컷에 스프라이트만 끼우면 된다.
    /// </summary>
    public static class PrologueDataBuilder
    {
        private const string Root = "Assets/_Project/Data";

        [MenuItem("It is You/1-b. 인트로 카툰 데이터 생성", priority = 3)]
        public static PrologueDefinition Build()
        {
            var prologue = Ed.CreateOrLoad<PrologueDefinition>(Root, "Prologue_Intro");

            var cuts = new List<PrologueCut>
            {
                Cut("기차역 — 신문과 서류를 읽고 있는 당신"),
                Cut("대합실 — 배웅하는 연인과 가족들, 밝아진 역사"),
                Cut("주변과 분리된 분위기의 당신 / 눈을 반짝이며 다가오는 소녀"),
                Cut("소녀를 마주보며 슬쩍 미소 짓는 당신"),
                Cut("보호자에게 가로막혀 돌아가는 소녀의 뒷모습"),
                Cut("테이블 — 펼쳐진 신문과 서류, 휴대용 타자기, 구겨진 리본 끈"),
                Cut("한 입만 베어문 샌드위치와 커피링 자국이 남은 잔"),

                // 기획서 인트로 흐름의 "전달받은 서류 내용 팝업(상호작용)".
                // 이 컷에서만 진행이 멈춘다. 조수의 이력을 플레이어가 직접 읽어야
                // 본편에서 주인공이 품는 의심에 같은 시점으로 설 수 있기 때문이다.
                Cut("서류를 들여다보는 당신 — \"음…\"",
                    Prop(
                        label: "전달받은 서류",
                        area: new Rect(0.36f, 0.20f, 0.28f, 0.26f),
                        popupPlaceholder:
                        "＜보조 수사관 인적사항 — 서류 컷 이미지 자리＞\n\n" +
                        "· 앳된 소녀의 증명사진\n" +
                        "· 특기사항란에 수기로 적힌 <hl>‘순간 기억 능력’</hl> 과 ‘속기사’ 자격증\n" +
                        "· ‘순간 기억 능력’ 위에 선명하게 찍힌 <hl>공인 도장</hl>\n\n" +
                        "이 팝업은 서류 일러스트로 교체될 자리입니다.")),

                Cut("만년필을 꺼내 서류 구석에 한 줄을 적어 넣는 당신\n― ‘시간 약속에 대한 개념이 부족함’"),
                Cut("저 멀리서 당신을 발견하고 빠른 걸음으로 다가오는 소녀 — 긴장한 얼굴"),
                Cut("숨을 고르며 인사하는 소녀 / 가방에서 서류철을 꺼내 내미는 손"),
                Cut("서류철 속 문서를 꺼내 사건 개요를 훑어보는 당신 / 눈치를 보는 소녀"),
                Cut("\"…출발하죠.\" — 자리를 정리하고 일어서는 당신 / 당황한 소녀"),
                Cut("\"…저 타자기도 챙겨요. 당신껍니다.\" — 멀뚱히 서서 타자기를 바라보는 소녀"),
                Cut("살며시 미소 짓고 타자기를 챙겨 기차에 올라타는 소녀"),
            };

            prologue.Set("id", "PROLOGUE_INTRO")
                    .Set("displayName", "인트로 카툰")
                    .Set("cuts", cuts)
                    .Set("nextSceneName", SceneRoutes.TutorialChapter);

            Ed.Save(prologue);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 인트로 카툰 데이터 생성 완료 — 컷 {cuts.Count}장, " +
                      $"상호작용 1곳(서류) · 다음 씬: {SceneRoutes.TutorialChapter}");
            return prologue;
        }

        // ============================================================== 헬퍼

        private static PrologueCut Cut(string cutName, PrologueProp interaction = null)
        {
            var cut = new PrologueCut();
            cut.Set("cutName", cutName)
               .Set("hasInteraction", interaction != null)
               .Set("interaction", interaction ?? new PrologueProp());
            return cut;
        }

        private static PrologueProp Prop(string label, Rect area, string popupPlaceholder)
        {
            var prop = new PrologueProp();
            prop.Set("label", label)
                .Set("area", area)
                .Set("popupPlaceholder", popupPlaceholder);
            return prop;
        }
    }
}
