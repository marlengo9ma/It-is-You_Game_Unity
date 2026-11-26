using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 빌드 용량 줄이기 — 메뉴 [It is You / 11. 빌드 용량 줄이기 (그림 압축 정리)].
    ///
    /// 그림이 빌드에서 큰 이유는 두 가지다.
    /// 1. 가로 · 세로가 4의 배수가 아닌 그림은 압축(DXT)이 안 되어 원본 그대로(RGBA32 / RGB24) 들어간다 — 4~6배 크다.
    ///    → 그림 오른쪽 · 아래 가장자리에 몇 줄을 덧붙여(가장자리 색을 그대로 늘림) 4의 배수로 맞춘다. 눈에는 차이가 없다.
    ///      최대 크기보다 커서 줄여 들어가는 그림은 '줄인 뒤의 크기'가 4의 배수가 되도록 맞춘다.
    ///      9-slice 테두리 값은 덧붙인 만큼 옮겨 모양이 그대로다.
    /// 2. 압축된 그림도 Crunch 압축을 켜면 빌드에서 3~5배 더 작아진다(품질 <see cref="CrunchQuality"/>).
    ///
    /// 세피 Live2D 텍스처(sepi_model)와 커서는 건드리지 않는다. 여러 번 돌려도 된다 — 이미 맞춘 그림은 그냥 지나간다.
    /// 새 그림을 넣은 뒤 빌드하기 전에 한 번 돌리면 된다.
    /// </summary>
    public static class BuildSizeTool
    {
        private const string ArtRoot = "Assets/_Project/Art";

        /// <summary>Crunch 품질(0~100). 낮을수록 작고 거칠다.</summary>
        public const int CrunchQuality = 75;

        [MenuItem("It is You/11. 빌드 용량 줄이기 (그림 압축 정리)", priority = 42)]
        public static void Run()
        {
            var padded = new List<string>();
            var skipped = new List<string>();
            var crunched = 0;

            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    if (EditorUtility.DisplayCancelableProgressBar("빌드 용량 줄이기", path, (float)i / paths.Count)) break;
                    if (Skip(path)) continue;
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null || ti.textureType != TextureImporterType.Sprite) continue;

                    var changed = false;
                    if (PadToMultipleOf4(path, ti, out var why)) { padded.Add(path); changed = true; }
                    else if (why != null) skipped.Add(path + " — " + why);

                    if (!ti.crunchedCompression || ti.compressionQuality != CrunchQuality
                        || ti.textureCompression == TextureImporterCompression.Uncompressed)
                    {
                        if (ti.textureCompression == TextureImporterCompression.Uncompressed)
                            ti.textureCompression = TextureImporterCompression.Compressed;
                        ti.crunchedCompression = true;
                        ti.compressionQuality = CrunchQuality;
                        crunched++;
                        changed = true;
                    }
                    if (changed) ti.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[It is You] 빌드 용량 줄이기 — 4의 배수로 맞춘 그림 {padded.Count}장, Crunch 를 켠 그림 {crunched}장" +
                      (skipped.Count > 0 ? "\n건너뜀:\n" + string.Join("\n", skipped) : ""));
        }

        private static bool Skip(string path)
            => path.Contains("/sepi_model/") || path.StartsWith(ArtRoot + "/Cursors/");

        /// <summary>
        /// 들어갈 크기(최대 크기로 줄인 뒤)가 4의 배수가 아니면 PNG 의 오른쪽 · 아래에 가장자리 색을 덧붙인다.
        /// 덧붙였으면 true. why 는 손댈 수 없을 때의 이유.
        /// </summary>
        private static bool PadToMultipleOf4(string path, TextureImporter ti, out string why)
        {
            why = null;
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return false;
            ti.GetSourceTextureWidthAndHeight(out var w, out var h);
            if (w <= 0 || h <= 0) return false;

            var nw = Target(w, w, h, ti.maxTextureSize);
            var nh = Target(h, w, h, ti.maxTextureSize);
            if (nw == w && nh == h) return false;
            if (nw < 0 || nh < 0) { why = "맞출 크기를 찾지 못함"; return false; }

            var full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(File.ReadAllBytes(full))) { why = "PNG 를 읽지 못함"; return false; }
            if (src.width != w || src.height != h) { why = "크기가 달라 건너뜀"; Object.DestroyImmediate(src); return false; }

            var hasAlpha = src.format == TextureFormat.RGBA32 || src.format == TextureFormat.ARGB32;
            var dst = new Texture2D(nw, nh, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            var s = src.GetPixels32();
            var d = new Color32[nw * nh];
            var padBottom = nh - h;   // 유니티 좌표는 아래가 0 — 덧붙인 줄은 그림 아래쪽
            for (int y = 0; y < nh; y++)
            {
                var sy = Mathf.Clamp(y - padBottom, 0, h - 1);
                for (int x = 0; x < nw; x++)
                    d[y * nw + x] = s[sy * w + Mathf.Min(x, w - 1)];
            }
            dst.SetPixels32(d);
            dst.Apply(false);
            File.WriteAllBytes(full, dst.EncodeToPNG());
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);

            // 9-slice 테두리(왼 · 아래 · 오른 · 위)와 직접 정한 피벗을 덧붙인 만큼 옮긴다.
            var b = ti.spriteBorder;
            if (b != Vector4.zero) ti.spriteBorder = new Vector4(b.x, b.y + padBottom, b.z + (nw - w), b.w);
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            if (settings.spriteAlignment == (int)SpriteAlignment.Custom)
            {
                var p = ti.spritePivot;
                ti.spritePivot = new Vector2(p.x * w / nw, (p.y * h + padBottom) / nh);
            }
            return true;
        }

        /// <summary>
        /// 한 변(len)의 새 길이. 최대 크기보다 크면 유니티가 긴 변을 maxSize 로 줄이므로
        /// 줄인 길이가 4의 배수(반올림 · 내림 모두)가 되는 가장 가까운 원본 길이를 찾는다. 없으면 -1.
        /// </summary>
        private static int Target(int len, int w, int h, int maxSize)
        {
            var longest = Mathf.Max(w, h);
            if (longest <= maxSize) return (len + 3) / 4 * 4;
            var scale = (double)maxSize / longest;
            for (int n = len; n < len + 64; n++)
            {
                var v = n * scale;
                var f = System.Math.Floor(v);
                if (f % 4 == 0 && v - f < 0.5) return n;
            }
            return -1;
        }
    }
}
