using System.Collections.Generic;
using ItIsYou.Core;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 씬 셋을 통째로 다시 만들고 빌드 세팅에 등록한다.
    ///
    /// 씬 하나만 고쳐 놓고 다른 하나를 잊으면, 화면은 멀쩡한데 전환에서 끊긴다.
    /// 그래서 재생성은 항상 셋을 함께 한다.
    /// </summary>
    public static class GameBuildSetup
    {
        private const string SceneFolder = "Assets/_Project/Scenes";

        [MenuItem("It is You/5. 전체 다시 만들기 (데이터 + 씬 3개)", priority = 20)]
        public static void RebuildAll()
        {
            // 챕터 씬 빌더가 마지막에 열린 씬으로 남도록 순서를 잡는다.
            ArtSetChangeNotice.Rebuilding = true;
            try
            {
                TitleSceneBuilder.Build();
                PrologueSceneBuilder.Build();
                ChapterSceneBuilder.Build();
            }
            finally
            {
                ArtSetChangeNotice.Rebuilding = false;
            }

            RegisterScenes();

            Debug.Log("[It is You] 전체 재생성 완료 — Title → Prologue → Tutorial_Chapter");
        }

        /// <summary>아트 소스를 끼우는 창구. 인스펙터에 스프라이트를 끌어다 놓고 5번을 다시 돌리면 씬에 반영된다.</summary>
        [MenuItem("It is You/7. 아트 세트 열기 (소스 교체)", priority = 40)]
        public static void OpenArtSet()
        {
            Ed.EnsureFolder("Assets/_Project/Art");
            var art = Ed.CreateOrLoad<ItIsYou.Data.ArtSet>("Assets/_Project/Art", "ArtSet");
            Ed.Save(art);
            Selection.activeObject = art;
            EditorGUIUtility.PingObject(art);
        }

        [MenuItem("It is You/6. 빌드 세팅에 씬 등록", priority = 21)]
        public static void RegisterScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            var missing = new List<string>();

            foreach (var name in SceneRoutes.All)
            {
                var path = $"{SceneFolder}/{name}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    missing.Add(name);
                    continue;
                }
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();

            if (missing.Count > 0)
                Debug.LogWarning($"[It is You] 아직 없는 씬은 건너뛰었습니다: {string.Join(", ", missing)}");

            Debug.Log($"[It is You] 빌드 세팅 등록 완료 — {scenes.Count}개 " +
                      $"(시작 씬: {(scenes.Count > 0 ? scenes[0].path : "없음")})");
        }
    }
}
