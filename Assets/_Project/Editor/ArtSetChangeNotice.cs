using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 아트 세트(ArtSet.asset)에 그림을 넣어도 씬은 메뉴 5 를 다시 돌려야 바뀐다.
    /// 그걸 잊고 빌드하면 "그림을 넣었는데 게임에서 안 바뀐다"가 된다 — 실제로 한 번 그랬다(2026-10-14, 기차역 배경).
    /// 그래서 아트 세트가 저장될 때 한 번 알려 주고, 빌드할 때 씬이 아트 세트보다 오래됐으면 경고를 남긴다.
    /// </summary>
    public class ArtSetChangeNotice : AssetPostprocessor, IPreprocessBuildWithReport
    {
        private const string ArtSetPath = "Assets/_Project/Art/ArtSet.asset";
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Title.unity",
            "Assets/_Project/Scenes/Prologue.unity",
            "Assets/_Project/Scenes/Tutorial_Chapter.unity"
        };

        /// <summary>메뉴 5 가 도는 중 — 빌더가 아트 세트를 저장해도 알리지 않는다.</summary>
        public static bool Rebuilding;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Rebuilding) return;
            foreach (var path in imported)
            {
                if (path != ArtSetPath) continue;
                if (!SceneOlderThanArtSet(out _)) return;
                Debug.LogWarning("[It is You] 아트 세트가 바뀌었습니다 — 메뉴 It is You › 5. 전체 다시 만들기 를 눌러야 씬(과 빌드)에 반영됩니다.");
                return;
            }
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (SceneOlderThanArtSet(out var scene))
                Debug.LogWarning($"[It is You] 아트 세트가 씬보다 나중에 바뀌었습니다({scene}). " +
                                 "이대로 빌드하면 새 그림이 빠집니다 — 메뉴 5 를 돌린 뒤 다시 빌드하세요.");
        }

        private static bool SceneOlderThanArtSet(out string olderScene)
        {
            olderScene = null;
            var root = Path.GetDirectoryName(Application.dataPath);
            var art = Path.Combine(root, ArtSetPath);
            if (!File.Exists(art)) return false;
            var artTime = File.GetLastWriteTimeUtc(art);
            // 메뉴 5 는 세 씬을 차례로 만들고 그 사이에 아트 세트를 한 번 저장한다. 그래서 '가장 최근에 만든 씬'과 비교한다.
            var newest = System.DateTime.MinValue;
            foreach (var s in Scenes)
            {
                var p = Path.Combine(root, s);
                if (!File.Exists(p)) continue;
                var w = File.GetLastWriteTimeUtc(p);
                if (w > newest) { newest = w; olderScene = Path.GetFileNameWithoutExtension(s); }
            }
            return newest != System.DateTime.MinValue && newest < artTime;
        }
    }
}
