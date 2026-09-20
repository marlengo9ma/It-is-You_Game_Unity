using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 한글 TMP 폰트 에셋을 만든다.
    ///
    /// TMP 기본 폰트(LiberationSans)에는 한글 글리프가 없어서, 그대로 두면
    /// 모든 대사가 빈 네모로 보인다. 그래서 씬을 만들기 전에 폰트부터 깐다.
    ///
    /// 아틀라스는 Dynamic 으로 만든다. 한글은 글자 수가 많아 Static 으로 구우면
    /// 아틀라스가 감당이 안 되고, 시나리오에 새 글자가 추가될 때마다 다시 구워야 한다.
    /// Dynamic 은 필요한 글자만 런타임에 채운다.
    /// </summary>
    public static class FontSetup
    {
        private const string FontFolder = "Assets/_Project/Fonts";
        private const string SourceName = "ItIsYou_KR";

        /// <summary>
        /// 개발용 임시 폰트. Windows 기본 '맑은 고딕'을 복사해 쓴다.
        ///
        /// 배포 빌드에는 쓸 수 없다 — 맑은 고딕은 재배포 라이선스가 없다.
        /// 출시 전에 OFL 계열(노토 산스 KR, 프리텐다드 등)로 교체해야 한다.
        /// 교체는 이 폴더의 ttf 만 갈아 끼우고 이 메뉴를 다시 실행하면 된다.
        /// </summary>
        private static readonly string[] CandidateSystemFonts =
        {
            @"C:\Windows\Fonts\malgun.ttf",
            @"C:\Windows\Fonts\NanumGothic.ttf",
            @"C:\Windows\Fonts\gulim.ttc",
            @"C:\Windows\Fonts\batang.ttc"
        };

        [MenuItem("It is You/0. 한글 폰트 준비", priority = 0)]
        public static TMP_FontAsset Ensure()
        {
            Ed.EnsureFolder(FontFolder);

            var assetPath = $"{FontFolder}/{SourceName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = EnsureSourceFont();
            if (font == null)
            {
                Debug.LogError(
                    "[It is You] 한글 폰트를 찾지 못했습니다. " +
                    $"{FontFolder} 에 한글 ttf 를 직접 넣고 다시 실행하세요.");
                return null;
            }

            // 16px 샘플링 + 4px 패딩이면 본문 크기에서 또렷하고, 아틀라스도 가볍다.
            var tmpFont = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 32,
                atlasPadding: 5,
                renderMode: UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            tmpFont.name = SourceName + " SDF";
            AssetDatabase.CreateAsset(tmpFont, assetPath);

            // 아틀라스 텍스처와 머티리얼을 폰트 에셋의 하위 에셋으로 넣는다.
            if (tmpFont.atlasTextures != null)
            {
                foreach (var tex in tmpFont.atlasTextures)
                {
                    if (tex != null && !AssetDatabase.Contains(tex))
                        AssetDatabase.AddObjectToAsset(tex, tmpFont);
                }
            }
            if (tmpFont.material != null && !AssetDatabase.Contains(tmpFont.material))
            {
                tmpFont.material.name = tmpFont.name + " Material";
                AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
            }

            EditorUtility.SetDirty(tmpFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 한글 TMP 폰트 생성 — {assetPath} (Dynamic 아틀라스)");
            return tmpFont;
        }

        /// <summary>프로젝트 안에 ttf 가 없으면 시스템 폰트를 복사해 온다.</summary>
        private static Font EnsureSourceFont()
        {
            // 이미 넣어 둔 폰트가 있으면 그걸 최우선으로 쓴다.
            var guids = AssetDatabase.FindAssets("t:Font", new[] { FontFolder });
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guids[0]));

            foreach (var systemPath in CandidateSystemFonts)
            {
                if (!File.Exists(systemPath)) continue;

                var fileName = Path.GetFileName(systemPath);
                var target = $"{FontFolder}/{fileName}";
                File.Copy(systemPath, target, overwrite: true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);

                Debug.LogWarning(
                    $"[It is You] 개발용으로 시스템 폰트를 복사했습니다 — {fileName}\n" +
                    "배포 빌드에는 쓸 수 없습니다. 출시 전 OFL 계열(노토 산스 KR, 프리텐다드 등)로 교체하세요.");

                return AssetDatabase.LoadAssetAtPath<Font>(target);
            }

            return null;
        }
    }
}
