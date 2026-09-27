using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// Assets/_Project/ 아래에 들어오는 그림을 자동으로 스프라이트로 임포트한다.
    /// PNG 를 폴더에 끌어다 놓기만 하면 아트 세트(ArtSet)와 단서·용의자 에셋의
    /// Sprite 칸에 바로 끌어 넣을 수 있게 된다 — Texture Type 을 손으로 바꿀 필요가 없다.
    ///
    /// Art/Cursors/ 안의 그림만 커서(Cursor)로 들어온다. 마우스 커서는 스프라이트가 아니라
    /// Texture2D 라서 타입이 다르다.
    ///
    /// 한 번 임포트된 뒤 손으로 바꾼 설정(피벗 · 9-slice border · Pixels Per Unit 등)은
    /// 건드리지 않는다. .meta 가 아직 없는 첫 임포트에만 손을 댄다.
    /// 이미 들어와 있는 그림을 한꺼번에 바꾸려면 메뉴 [It is You / 8. Art 그림 다시 임포트] 를 쓴다.
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        private const string ProjectRoot = "Assets/_Project/";
        private const string CursorFolder = "Assets/_Project/Art/Cursors/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ProjectRoot)) return;

            var importer = (TextureImporter)assetImporter;

            // 이미 설정이 잡혀 있는 그림은 그대로 둔다 — 손으로 잡아 둔 피벗과 border 를 지우지 않기 위해서.
            if (!importer.importSettingsMissing) return;

            Apply(importer, assetPath);
        }

        /// <summary>임포트 설정 본체. 메뉴에서 부를 때는 이미 임포트된 그림에도 적용된다.</summary>
        public static void Apply(TextureImporter importer, string path)
        {
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            if (path.StartsWith(CursorFolder))
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
        }

        // ------------------------------------------------------------------ 메뉴

        /// <summary>
        /// 이미 프로젝트에 들어와 있는 그림을 전부 스프라이트로 다시 임포트한다.
        /// 이 스크립트를 넣기 전에 넣어 둔 PNG 들을 한 번에 고칠 때 쓴다.
        /// </summary>
        [MenuItem("It is You/8. Art 그림 다시 임포트 (스프라이트로)", priority = 41)]
        public static void ReimportProjectTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" });
            var changed = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    var wasType = importer.textureType;
                    Apply(importer, path);

                    // 타입이 그대로면 다시 임포트하지 않는다 — 큰 폴더에서 시간을 아끼기 위해서.
                    if (wasType == importer.textureType) continue;

                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[It is You] Art 그림 다시 임포트 — 검사 {guids.Length}장, 타입 변경 {changed}장 " +
                      "(Art/Cursors 안은 커서, 나머지는 스프라이트)");
        }
    }
}
