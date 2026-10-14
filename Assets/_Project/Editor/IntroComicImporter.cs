using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ItIsYou.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using K = ItIsYou.EditorTools.UiKit;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 인트로 컷 가져오기.
    ///
    /// 프로젝트 폴더의 ArtSource/Intro/ 에 놓인 원본 PNG(4000×3000 전체 캔버스, 컷 하나씩 레이어로 뽑은 것)를 읽어
    ///  ① 투명한 테두리를 잘라 내고 절반 크기로 줄여 Assets/_Project/Art/Intro/ 에 스프라이트로 저장하고
    ///  ② IntroComic 프리팹의 같은 이름 컷에 끼운다. 없는 컷은 원본 캔버스의 자리 그대로 새로 만든다.
    ///
    /// 파일 이름 규칙 — C{페이지}_{순번}.png
    /// · C1_1, C1_2 … : 페이지 1 에서 1번째, 2번째로 나오는 컷.
    /// · C2_A : 그 페이지의 완성 모습(참고용). 게임에는 쓰지 않는다.
    /// · C4_B / C4_M / C4_F : 글자로 된 레이어(뒤판·가운데·앞판). 이런 레이어가 있는 페이지는
    ///   숫자 파일(C4_1, C4_2)을 "상태 참고 그림"으로 보고 쓰지 않는다. 레이어는 한꺼번에 나온다.
    ///
    /// 메뉴 9 는 그림만 바꾸고 이미 잡아 둔 자리 · 크기 · 애니메이션은 그대로 둔다.
    /// 메뉴 9-b 는 모든 컷을 원본 캔버스의 자리와 기본 애니메이션으로 되돌린다.
    /// </summary>
    public static class IntroComicImporter
    {
        public const string SourceFolder = "ArtSource/Intro";
        public const string SpriteFolder = "Assets/_Project/Art/Intro";
        public const string PrefabFolder = "Assets/_Project/Prefabs";
        public const string PrefabPath = PrefabFolder + "/IntroComic.prefab";

        /// <summary>원본 캔버스 크기와 화면 위 페이지 크기. 원본 1px = 화면 0.36px.</summary>
        public const float SourceWidth = 4000f, SourceHeight = 3000f;
        public const float PageWidth = 1440f, PageHeight = 1080f;
        private const float SourceToPage = PageWidth / SourceWidth;

        /// <summary>스프라이트는 원본의 1/2 로 저장한다 — 화면에서는 0.36 배로 보이므로 1080p·1440p 에서 충분하다.</summary>
        private const int Downscale = 2;

        private static readonly Color PaperColor = new Color32(0xFB, 0xFA, 0xF7, 0xFF);

        private static readonly Regex NamePattern = new Regex(@"^C(\d+)_([0-9]+|[A-Za-z]+)$", RegexOptions.Compiled);

        // ================================================================= 메뉴

        [MenuItem("It is You/9. 인트로 컷 가져오기 (그림만 교체 · 배치 유지)", priority = 50)]
        public static void ImportKeepLayout() => Import(resetLayout: false);

        [MenuItem("It is You/9-b. 인트로 컷 가져오기 + 배치·애니메이션 초기화", priority = 51)]
        public static void ImportResetMenu()
        {
            if (!EditorUtility.DisplayDialog("인트로 컷 초기화",
                    "IntroComic 프리팹의 모든 컷을 원본 PNG 의 자리와 기본 애니메이션으로 되돌립니다.\n" +
                    "Scene 뷰에서 옮기거나 인스펙터에서 바꾼 값이 지워집니다. 계속할까요?",
                    "초기화", "취소"))
                return;
            Import(resetLayout: true);
        }

        [MenuItem("It is You/9-c. 인트로 프리팹 열기 (컷 배치 · 애니메이션 편집)", priority = 52)]
        public static void OpenPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Import(resetLayout: false);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            if (prefab != null) AssetDatabase.OpenAsset(prefab);
        }

        // ================================================================= 본체

        private sealed class SourceCut
        {
            public string Name;          // C1_2
            public int Page;             // 1
            public string Suffix;        // "2" / "M"
            public string SourcePath;
            public RectInt Bounds;       // 원본 캔버스 위 자리 (위에서부터 y)
            public string SpritePath;
        }

        /// <summary>가져오기. 만들어진(또는 고친) 프리팹 경로를 돌려준다.</summary>
        public static string Import(bool resetLayout)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var sourceDir = Path.Combine(projectRoot, SourceFolder);
            if (!Directory.Exists(sourceDir))
            {
                Debug.LogError($"[It is You] 인트로 원본 폴더가 없습니다: {sourceDir}");
                return null;
            }

            K.Prepare();
            Ed.EnsureFolder(SpriteFolder);
            Ed.EnsureFolder(PrefabFolder);

            // ---- 1. 원본 PNG → 잘라 낸 스프라이트 ----
            var all = new List<SourceCut>();
            var references = new List<string>();
            foreach (var file in Directory.GetFiles(sourceDir, "*.png").OrderBy(f => f, StringComparer.Ordinal))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var m = NamePattern.Match(name);
                if (!m.Success) { Debug.LogWarning($"[It is You] 이름 규칙(Cx_y)에 맞지 않아 건너뜀: {name}"); continue; }
                all.Add(new SourceCut { Name = name.ToUpperInvariant(), Page = int.Parse(m.Groups[1].Value), Suffix = m.Groups[2].Value.ToUpperInvariant(), SourcePath = file });
            }

            var cuts = new List<SourceCut>();
            foreach (var page in all.GroupBy(c => c.Page))
            {
                var hasLayers = page.Any(c => IsLetter(c.Suffix) && c.Suffix != "A");
                foreach (var c in page)
                {
                    var isReference = c.Suffix == "A" || (hasLayers && !IsLetter(c.Suffix));
                    if (isReference) { references.Add(c.Name); continue; }
                    cuts.Add(c);
                }
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < cuts.Count; i++)
                {
                    var c = cuts[i];
                    EditorUtility.DisplayProgressBar("인트로 컷 가져오기", c.Name, (float)i / Math.Max(1, cuts.Count));
                    c.Bounds = CropToSprite(c.SourcePath, $"{SpriteFolder}/{c.Name}.png");
                    c.SpritePath = $"{SpriteFolder}/{c.Name}.png";
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.Refresh();
            foreach (var c in cuts) EnsureSpriteImport(c.SpritePath);

            // ---- 2. 프리팹 ----
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var root = existing != null ? PrefabUtility.LoadPrefabContents(PrefabPath) : CreateRoot();
            var created = 0;
            try
            {
                var book = root.GetComponent<ComicBookView>();
                var paper = book.Paper;

                foreach (var page in cuts.GroupBy(c => c.Page).OrderBy(g => g.Key))
                {
                    var pageGo = FindOrCreatePage(paper, page.Key, out var pageIsNew);
                    var pageCuts = page.ToList();

                    // 앞뒤(Hierarchy) 순서는 새로 만들거나 초기화할 때만 정한다 — 손으로 바꾼 순서를 지키기 위해서.
                    foreach (var c in pageCuts.OrderBy(c => Defaults(c).Depth))
                    {
                        var t = pageGo.transform.Find(c.Name);
                        var isNew = t == null;
                        if (isNew) { t = CreateCut(pageGo.transform, c.Name).transform; created++; }

                        var img = t.GetComponent<Image>();
                        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(c.SpritePath);
                        img.color = Color.white;
                        img.preserveAspect = false;
                        img.raycastTarget = false;

                        if (isNew || resetLayout)
                        {
                            var d = Defaults(c);
                            PlaceFromSource((RectTransform)t, c.Bounds, d.SourceShift);
                            var view = t.GetComponent<ComicCutView>();
                            view.Set("order", d.Order)
                                .Set("motion", d.Motion)
                                .Set("distance", d.Distance)
                                .Set("duration", d.Duration)
                                .Set("delay", d.Delay)
                                .Set("ease", d.Ease)
                                .Set("fade", true)
                                .Set("popStartScale", 0.6f)
                                .Set("waitForClick", false);
                        }
                        if (isNew || resetLayout) t.SetAsLastSibling();
                    }

                    if (page.Key == 4) SetupEnvelope(pageGo, pageCuts, resetLayout || pageIsNew);
                }

                // 페이지 순서: C1 → C5. 편집하기 쉽게 첫 페이지만 켜 둔다.
                var pages = book.Pages.OrderBy(p => PageNumber(p.name)).ToList();
                for (int i = 0; i < pages.Count; i++)
                {
                    pages[i].transform.SetSiblingIndex(i);
                    if (resetLayout || existing == null) pages[i].gameObject.SetActive(i == 0);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (existing != null) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[It is You] 인트로 컷 가져오기 완료 — 컷 {cuts.Count}장 (새로 만든 컷 {created}), " +
                      $"참고 그림 {references.Count}장은 쓰지 않음 ({string.Join(", ", references)}) · " +
                      $"{(resetLayout ? "배치·애니메이션 초기화" : "배치 유지")} · {PrefabPath}");
            return PrefabPath;
        }

        private static bool IsLetter(string s) => s.Length > 0 && char.IsLetter(s[0]);

        private static int PageNumber(string pageName)
        {
            var digits = new string(pageName.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : 999;
        }

        // ================================================================= 기본값

        private struct CutDefaults
        {
            public int Order;
            public int Depth;            // 클수록 앞
            public CutMotion Motion;
            public float Distance, Duration, Delay;
            public CutEase Ease;
            public Vector2Int SourceShift;   // 원본 자리에서 더 옮길 픽셀(원본 기준, 아래가 +)
        }

        /// <summary>
        /// 컷별 처음 값. 예시 영상(C1: 위에서 내려옴 → 내려옴 → 뒤 배경이 아래에서 올라옴)을 기준으로 나머지 페이지도 맞췄다.
        /// 여기 없는 이름은 순번 = 파일 번호, 위에서 내려옴.
        /// </summary>
        private static CutDefaults Defaults(SourceCut c)
        {
            var d = new CutDefaults
            {
                Order = int.TryParse(c.Suffix, out var n) ? n : 1,
                Depth = int.TryParse(c.Suffix, out var n2) ? n2 : 1,
                Motion = CutMotion.FromTop,
                Distance = 90f, Duration = 0.55f, Delay = 0.25f,
                Ease = CutEase.EaseOutStrong
            };

            switch (c.Name)
            {
                // C1 — 기차(위) → 커피잔 든 손(왼쪽, 내려옴) → 테이블 배경(아래에서 올라와 맨 뒤에 깔림)
                case "C1_1": d.Depth = 1; d.Motion = CutMotion.FromTop; d.Delay = 0.3f; break;
                case "C1_2": d.Depth = 2; d.Motion = CutMotion.FromTop; break;
                case "C1_3": d.Depth = 0; d.Motion = CutMotion.FromBottom; d.Distance = 120f; d.Duration = 0.7f; break;

                case "C2_1": d.Motion = CutMotion.FromTop; d.Delay = 0.3f; break;
                case "C2_2": d.Motion = CutMotion.FromLeft; break;
                case "C2_3": d.Motion = CutMotion.FromRight; break;

                // C3 — 위 띠 컷이 먼저, 아래 큰 컷이 그 뒤에 깔린다.
                case "C3_1": d.Depth = 1; d.Motion = CutMotion.FromTop; d.Delay = 0.3f; break;
                case "C3_2": d.Depth = 0; d.Motion = CutMotion.FromBottom; d.Distance = 120f; d.Duration = 0.7f; break;

                // C4 — 봉투 세 겹은 한꺼번에 나타난다. 앞에서부터 F > M > B.
                case "C4_B": d.Order = 1; d.Depth = 0; d.Motion = CutMotion.Fade; d.Distance = 0f; d.Delay = 0.3f; break;
                case "C4_M": d.Order = 1; d.Depth = 1; d.Motion = CutMotion.Fade; d.Distance = 0f; d.Delay = 0.3f;
                             d.SourceShift = new Vector2Int(0, -56); break;   // 꺼낸 뒤 자리를 C4_2 에 맞춘다
                case "C4_F": d.Order = 1; d.Depth = 2; d.Motion = CutMotion.Fade; d.Distance = 0f; d.Delay = 0.3f; break;

                // C5 — 시계 → 흰 띠 → 작은 컷 → 세피 → 말풍선
                case "C5_1": d.Depth = 2; d.Motion = CutMotion.FromLeft; d.Delay = 0.3f; break;
                case "C5_2": d.Depth = 1; d.Motion = CutMotion.FromRight; break;
                case "C5_3": d.Depth = 3; d.Motion = CutMotion.FromBottom; break;
                case "C5_4": d.Depth = 0; d.Motion = CutMotion.FromBottom; d.Distance = 120f; d.Duration = 0.7f; break;
                case "C5_5": d.Depth = 4; d.Motion = CutMotion.Pop; d.Distance = 0f; d.Duration = 0.4f; d.Ease = CutEase.Back; d.Delay = 0.35f; break;
            }
            return d;
        }

        // ================================================================= 봉투 (C4)

        private static void SetupEnvelope(GameObject pageGo, List<SourceCut> pageCuts, bool reset)
        {
            var env = pageGo.GetComponent<ComicEnvelope>();
            var isNew = env == null;
            if (isNew) env = pageGo.AddComponent<ComicEnvelope>();

            var doc = pageGo.transform.Find("C4_M");
            var back = pageCuts.FirstOrDefault(c => c.Name == "C4_B");
            var front = pageCuts.FirstOrDefault(c => c.Name == "C4_F");

            // 눌러야 하는 자리 = 봉투(뒤판 ∪ 앞판).
            var clickT = pageGo.transform.Find("EnvelopeClickArea");
            if (clickT == null)
            {
                var go = K.UI("EnvelopeClickArea", pageGo.transform);
                var img = go.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
                var btn = go.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                clickT = go.transform;
                reset = true;
            }
            clickT.SetAsLastSibling();

            var hintT = pageGo.transform.Find("EnvelopeHint");
            if (hintT == null)
            {
                var go = K.UI("EnvelopeHint", pageGo.transform);
                go.AddComponent<CanvasGroup>();
                var label = K.Text("Label", go.transform, "클릭", 34f, new Color32(0x6B, 0x4E, 0x1E, 0xFF), TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
                K.Stretch(label.rectTransform);
                hintT = go.transform;
                reset = true;
            }
            hintT.SetAsLastSibling();

            if (reset && back != null && front != null)
            {
                var x0 = Math.Min(back.Bounds.xMin, front.Bounds.xMin);
                var y0 = Math.Min(back.Bounds.yMin, front.Bounds.yMin);
                var x1 = Math.Max(back.Bounds.xMax, front.Bounds.xMax);
                var y1 = Math.Max(back.Bounds.yMax, front.Bounds.yMax);
                PlaceFromSource((RectTransform)clickT, new RectInt(x0, y0, x1 - x0, y1 - y0), Vector2Int.zero);

                // '클릭' 안내는 봉투 앞판 가운데쯤.
                var hr = (RectTransform)hintT;
                PlaceFromSource(hr, new RectInt(front.Bounds.x + front.Bounds.width / 2 - 200, front.Bounds.y + front.Bounds.height / 2 - 60, 400, 120), Vector2Int.zero);
            }

            if (isNew || reset)
            {
                // 봉투 안 자리(C4_1)는 꺼낸 뒤 자리(C4_2)에서 원본 기준 1060px 아래 — 원본 그림을 겹쳐 맞춘 값.
                env.Set("insideOffset", new Vector2(0f, -1060f * SourceToPage))
                   .Set("pullSeconds", 0.9f)
                   .Set("pullEase", CutEase.EaseOutStrong)
                   .Set("popupDelay", 0.25f)
                   .Set("popupPlaceholder",
                        "＜보조 수사관 인적사항 — 서류 이미지 자리＞\n\n" +
                        "· 앳된 소녀의 증명사진\n" +
                        "· 특기사항란에 수기로 적힌 <hl>‘순간 기억 능력’</hl> 과 ‘속기사’ 자격증\n" +
                        "· ‘순간 기억 능력’ 위에 선명하게 찍힌 <hl>공인 도장</hl>\n\n" +
                        "이 팝업은 서류 일러스트로 교체될 자리입니다.\n" +
                        "(IntroComic 프리팹 › Page_C4 › Comic Envelope › Popup Image)");
            }

            env.Set("document", doc != null ? doc.GetComponent<ComicCutView>() : null)
               .Set("clickArea", clickT.GetComponent<Button>())
               .Set("hint", hintT.GetComponent<CanvasGroup>());
        }

        // ================================================================= 프리팹 뼈대

        private static GameObject CreateRoot()
        {
            var root = new GameObject("IntroComic", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            K.Stretch(rt);

            var paperGo = K.UI("Paper", root.transform);
            var paper = (RectTransform)paperGo.transform;
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0.5f);
            paper.pivot = new Vector2(0.5f, 0.5f);
            paper.sizeDelta = new Vector2(PageWidth, PageHeight);
            paper.anchoredPosition = Vector2.zero;
            var paperImg = paperGo.AddComponent<Image>();
            paperImg.color = PaperColor;
            paperImg.raycastTarget = false;
            paperGo.AddComponent<RectMask2D>();   // 봉투 속 서류처럼 페이지 밖으로 나간 부분은 잘린다

            root.AddComponent<ComicBookView>().Set("paper", paper);
            return root;
        }

        private static GameObject FindOrCreatePage(RectTransform paper, int number, out bool isNew)
        {
            var name = $"Page_C{number}";
            var t = paper.Find(name);
            isNew = t == null;
            if (!isNew) return t.gameObject;

            var go = K.UI(name, paper);
            K.Stretch((RectTransform)go.transform);
            go.AddComponent<CanvasGroup>();
            go.AddComponent<ComicPageView>();
            return go;
        }

        private static GameObject CreateCut(Transform page, string name)
        {
            var go = K.UI(name, page);
            go.AddComponent<Image>();
            go.AddComponent<CanvasGroup>();
            go.AddComponent<ComicCutView>();
            return go;
        }

        /// <summary>원본 캔버스(4000×3000, 위가 0) 위의 자리를 페이지(1440×1080, 가운데가 0, 위가 +) 자리로 옮긴다.</summary>
        private static void PlaceFromSource(RectTransform rt, RectInt b, Vector2Int shift)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            var cx = (b.x + shift.x + b.width * 0.5f) * SourceToPage - PageWidth * 0.5f;
            var cy = PageHeight * 0.5f - (b.y + shift.y + b.height * 0.5f) * SourceToPage;
            rt.anchoredPosition = new Vector2(cx, cy);
            rt.sizeDelta = new Vector2(b.width * SourceToPage, b.height * SourceToPage);
        }

        // ================================================================= 이미지

        /// <summary>
        /// 원본을 읽어 투명 테두리를 잘라 내고 1/Downscale 로 줄여 저장한다. 원본 캔버스 위의 자리(위가 0)를 돌려준다.
        /// 줄일 때는 알파를 곱한 평균을 써서, 투명한 바깥 색이 선 가장자리로 번지지 않게 한다.
        /// </summary>
        private static RectInt CropToSprite(string sourcePath, string assetPath)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!src.LoadImage(File.ReadAllBytes(sourcePath)))
                    throw new Exception($"PNG 를 읽지 못했습니다: {sourcePath}");

                int w = src.width, h = src.height;
                var px = src.GetPixels32();

                // 알파가 있는 범위 (텍스처는 아래가 0)
                int minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    var row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        if (px[row + x].a <= 2) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
                if (maxX < 0) { minX = 0; minY = 0; maxX = w - 1; maxY = h - 1; }

                int cw = maxX - minX + 1, ch = maxY - minY + 1;
                int ow = (cw + Downscale - 1) / Downscale, oh = (ch + Downscale - 1) / Downscale;
                var outPx = new Color32[ow * oh];
                for (int oy = 0; oy < oh; oy++)
                {
                    for (int ox = 0; ox < ow; ox++)
                    {
                        float r = 0, g = 0, b = 0, a = 0; int n = 0;
                        for (int dy = 0; dy < Downscale; dy++)
                        {
                            var sy = minY + oy * Downscale + dy;
                            if (sy > maxY) continue;
                            for (int dx = 0; dx < Downscale; dx++)
                            {
                                var sx = minX + ox * Downscale + dx;
                                if (sx > maxX) continue;
                                var c = px[sy * w + sx];
                                var ca = c.a / 255f;
                                r += c.r * ca; g += c.g * ca; b += c.b * ca; a += ca; n++;
                            }
                        }
                        if (a > 0f) outPx[oy * ow + ox] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(r / a), 0, 255),
                                                                     (byte)Mathf.Clamp(Mathf.RoundToInt(g / a), 0, 255),
                                                                     (byte)Mathf.Clamp(Mathf.RoundToInt(b / a), 0, 255),
                                                                     (byte)Mathf.Clamp(Mathf.RoundToInt(a / Math.Max(1, n) * 255f), 0, 255));
                    }
                }

                var dst = new Texture2D(ow, oh, TextureFormat.RGBA32, false);
                dst.SetPixels32(outPx);
                dst.Apply();
                File.WriteAllBytes(assetPath, dst.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(dst);

                // 텍스처 좌표(아래가 0) → 원본 캔버스 좌표(위가 0)
                return new RectInt(minX, h - 1 - maxY, cw, ch);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(src);
            }
        }

        private static void EnsureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            var changed = importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize < 2048 || importer.mipmapEnabled;
            if (!changed) return;
            ArtImportPostprocessor.Apply(importer, assetPath);
            importer.SaveAndReimport();
        }
    }
}
