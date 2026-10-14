using ItIsYou.Core;
using ItIsYou.Data;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 인트로 카툰 데이터. 컷 그림 · 자리 · 애니메이션은 IntroComic 프리팹에 있어서(메뉴 9 · 9-c)
    /// 여기서는 카툰이 끝난 뒤 넘어갈 씬만 적는다.
    /// </summary>
    public static class PrologueDataBuilder
    {
        private const string Root = "Assets/_Project/Data";

        [MenuItem("It is You/1-b. 인트로 카툰 데이터 생성", priority = 3)]
        public static PrologueDefinition Build()
        {
            var prologue = Ed.CreateOrLoad<PrologueDefinition>(Root, "Prologue_Intro");

            prologue.Set("id", "PROLOGUE_INTRO")
                    .Set("displayName", "인트로 카툰")
                    .Set("nextSceneName", SceneRoutes.TutorialChapter);

            Ed.Save(prologue);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 인트로 카툰 데이터 생성 완료 — 다음 씬: {SceneRoutes.TutorialChapter} (컷은 IntroComic 프리팹)");
            return prologue;
        }
    }
}
