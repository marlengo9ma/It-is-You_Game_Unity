using System.Collections.Generic;
using ItIsYou.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using L = ItIsYou.EditorTools.Layout;
using P = ItIsYou.EditorTools.Layout.Palette;
using T = ItIsYou.EditorTools.Layout.Type;
using K = ItIsYou.EditorTools.UiKit;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 스타트 화면을 조립한다.
    ///
    /// 기획서: "레트로 타자기를 2/3 지점에 배치 / 배경의 움직이는 요소".
    /// 아트가 들어오기 전이라 타자기 자리에는 같은 크기의 빈 판을 세워 둔다.
    /// 자리를 미리 잡아 두면, 나중에 그림만 끼워 넣어도 구도가 흔들리지 않는다.
    ///
    /// 움직이는 요소는 증기 층이다. 타이틀 화면이 완전히 멈춰 있으면
    /// 로딩이 걸린 것처럼 보이기 때문에, 아트가 없는 지금도 뭔가는 흘러야 한다.
    /// 다만 증기는 배경이지 요소가 아니므로, 배경색과의 차이를 아주 좁게 둔다.
    /// </summary>
    public static class TitleSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Title.unity";

        /// <summary>플랫폼의 지평. 타자기가 이 선 위에 선다.</summary>
        private const float HorizonY = 0.28f;

        // ---- 메뉴 ----
        private const float MenuWidth = 480f;    // 60u
        private const float MenuButton = 72f;    // 9u
        private const float MenuGap = 16f;       // 2u

        [MenuItem("It is You/3. 타이틀 씬 생성", priority = 10)]
        public static void Build()
        {
            K.Prepare();
            Ed.EnsureFolder(SceneFolder);

            // 둥근 사각형(UISprite)은 어떤 크기로 늘려도 '버튼'처럼 보인다.
            // 증기에는 원형 스프라이트를 쓴다.
            var puff = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = K.Boot(P.Bg);

            var bg = K.Img("Background", root, P.Bg);
            K.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            // 아트 세트 (Assets/_Project/Art/ArtSet.asset) — 배경과 타자기 그림이 있으면 끼운다.
            var art = AssetDatabase.LoadAssetAtPath<ItIsYou.Data.ArtSet>("Assets/_Project/Art/ArtSet.asset");
            if (art != null && art.titleBackground != null)
            {
                bg.sprite = art.titleBackground;
                bg.color = Color.white;
                bg.preserveAspect = art.titleBackgroundPlace.preserveAspect;
                Place(bg.rectTransform, art.titleBackgroundPlace);
            }

            // ---------------------------------------------------- 움직이는 배경
            var backdrop = K.UI("Backdrop", root);
            var backdropRt = backdrop.GetComponent<RectTransform>();
            K.Stretch(backdropRt);

            var drifting = new List<RectTransform>
            {
                DriftLayer("Steam_Far",  backdropRt, puff, 0.44f, 340f, new Color32(0x20, 0x25, 0x2B, 0x26)),
                DriftLayer("Steam_Mid",  backdropRt, puff, 0.32f, 240f, new Color32(0x23, 0x28, 0x2F, 0x2E)),
                DriftLayer("Steam_Near", backdropRt, puff, 0.14f, 180f, new Color32(0x17, 0x1A, 0x1F, 0x59)),
            };

            // 지평선. 메뉴를 가로지르지 않도록 오른쪽 절반에만 긋는다.
            var horizon = K.Img("Horizon", backdropRt, P.Rule);
            K.Anchor(horizon.rectTransform, K.V(0.42f, HorizonY), K.V(1, HorizonY), K.V(0, 0f), K.V(0, 2f));
            horizon.raycastTarget = false;

            // ------------------------------------------------- 타자기 (2/3 지점)
            // 가로 2/3 지점을 중심으로 지평선 위에 세운다.
            // 실제 아트가 오면 이 판만 교체하면 된다.
            var typewriter = K.Img("Typewriter_Placeholder", root, new Color32(0x2A, 0x2F, 0x36, 0xFF));
            K.Anchor(typewriter.rectTransform,
                K.V(0.666f, HorizonY), K.V(0.666f, HorizonY),
                K.V(-304f, 0f), K.V(304f, 384f));   // 608 x 384 (76u x 48u)
            typewriter.raycastTarget = false;

            var typewriterTag = K.Text("Tag", typewriter.rectTransform,
                "레트로 타자기\n(아트 대기)", T.Caption, new Color32(0x55, 0x5B, 0x63, 0xFF),
                TextAlignmentOptions.Center);
            K.Stretch(typewriterTag.rectTransform);
            if (art != null && art.titleTypewriter != null)
            {
                typewriter.sprite = art.titleTypewriter;
                typewriter.color = Color.white;
                typewriter.preserveAspect = art.titleTypewriterPlace.preserveAspect;
                Place(typewriter.rectTransform, art.titleTypewriterPlace);
                typewriterTag.gameObject.SetActive(false);
            }

            // -------------------------------------------------------- 제목
            var titleBlock = K.UI("Title", root);
            var titleRt = titleBlock.GetComponent<RectTransform>();
            K.Anchor(titleRt, K.V(0, 1), K.V(0, 1),
                K.V(L.Gutter * 2, -L.Unit * 54), K.V(L.Gutter * 2 + 720f, -L.Unit * 24));

            var mainTitle = K.Text("Main", titleRt, "It is You", 96f, P.Ink, TextAlignmentOptions.TopLeft);
            mainTitle.characterSpacing = 6f;
            K.Anchor(mainTitle.rectTransform, K.V(0, 1), K.V(1, 1), K.V(0, -120f), K.V(0, 0f));

            var subTitle = K.Text("Sub", titleRt, "바 로   너", 40f, P.Brass, TextAlignmentOptions.TopLeft);
            subTitle.characterSpacing = 18f;
            K.Anchor(subTitle.rectTransform, K.V(0, 1), K.V(1, 1), K.V(0, -184f), K.V(0, -128f));

            // 깜빡이는 커서. 타자기 앞에 앉은 화면이라는 신호다.
            var caret = K.Text("Caret", titleRt, "▌", 40f, P.Brass, TextAlignmentOptions.TopLeft);
            K.Anchor(caret.rectTransform, K.V(0, 1), K.V(0, 1), K.V(352f, -184f), K.V(392f, -128f));

            // -------------------------------------------------------- 메뉴
            var menu = K.UI("Menu", root);
            var menuRt = menu.GetComponent<RectTransform>();
            var menuHeight = MenuButton * 4 + MenuGap * 3;   // 336
            K.Anchor(menuRt, K.V(0, 0), K.V(0, 0),
                K.V(L.Gutter * 2, L.Unit * 26), K.V(L.Gutter * 2 + MenuWidth, L.Unit * 26 + menuHeight));

            var vlg = menu.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = MenuGap;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            var newGame = MenuItem(menuRt, "NewGame", "새 게임");
            var continueGame = MenuItem(menuRt, "Continue", "이어하기");
            var settings = MenuItem(menuRt, "Settings", "설정");
            var quit = MenuItem(menuRt, "Quit", "종료");

            // -------------------------------------------------------- 하단
            var version = K.Text("Version", root, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomRight);
            K.Anchor(version.rectTransform, K.V(0.5f, 0), K.V(1, 0),
                K.V(0, L.Gutter), K.V(-L.Gutter, L.Gutter + 32f));

            var note = K.Text("Note", root, "졸업작품 개발 빌드  ·  내용은 확정본이 아닙니다",
                T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.BottomLeft);
            K.Anchor(note.rectTransform, K.V(0, 0), K.V(0.5f, 0),
                K.V(L.Gutter, L.Gutter), K.V(0, L.Gutter + 32f));

            // -------------------------------------------------------- 컨트롤러
            var systems = new GameObject("Systems");
            var settingsMenu = SettingsUiBuilder.BuildSettings(root, systems);
            var title = systems.AddComponent<TitleController>();
            title.Set("newGameButton", newGame)
                 .Set("continueButton", continueGame)
                 .Set("settingsButton", settings)
                 .Set("settings", settingsMenu)
                 .Set("quitButton", quit)
                 .Set("versionLabel", version)
                 .Set("caret", caret)
                 .Set("driftingLayers", drifting)
                 .Set("baseDriftSeconds", 34f)
                 .Set("caretBlinkSeconds", 0.62f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 타이틀 씬 생성 완료 — {ScenePath}");
        }

        // ============================================================== 헬퍼

        private static Button MenuItem(Transform parent, string name, string label)
        {
            var made = K.Button(name, parent, label, T.Body);
            made.rect.sizeDelta = new Vector2(0f, MenuButton);
            made.label.alignment = TextAlignmentOptions.Left;
            made.label.margin = new Vector4(L.Pad, 0f, 0f, 0f);

            // 왼쪽에 놋쇠 선 한 줄. 선택지 버튼과 같은 어휘를 쓴다.
            var strip = K.Img("Strip", made.rect, P.Brass);
            K.Anchor(strip.rectTransform, K.V(0, 0), K.V(0, 1), Vector2.zero, K.V(L.Unit * 0.5f, 0));
            strip.raycastTarget = false;

            return made.button;
        }

        /// <summary>
        /// 옆으로 흐르는 증기 층 하나.
        ///
        /// 폭은 화면 하나(1920)로 잡고, 그 안에 같은 무늬를 두 벌 넣는다.
        /// 컨트롤러가 폭만큼 왼쪽으로 밀면 두 번째 벌이 정확히 첫 번째 자리에 와서,
        /// 이음매 없이 계속 흐른다.
        /// </summary>
        /// <summary>아트 세트의 자리 값(offset · scale · 좌우 반전)을 적용한다.</summary>
        private static void Place(RectTransform rt, ItIsYou.Data.ArtSet.Placement place)
        {
            if (rt == null) return;
            rt.anchoredPosition += place.offset;
            var scale = place.Scale;
            rt.localScale = new Vector3(place.flipX ? -scale : scale, scale, 1f);
        }

        private static RectTransform DriftLayer(string name, Transform parent, Sprite puff,
            float centerY, float height, Color color)
        {
            var go = K.UI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, centerY);
            rt.anchorMax = new Vector2(0f, centerY);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(L.Reference.x, height);
            rt.anchoredPosition = Vector2.zero;

            // 한 벌 안의 덩어리 배치. 고정값이라 매번 같은 그림이 나온다.
            // 가로로 길고 납작한 타원이라야 뭉게구름이 아니라 흘러가는 증기로 보인다.
            var blobs = new (float x, float w, float h, float y)[]
            {
                (  20f, 520f, 0.86f,  0.06f),
                ( 360f, 300f, 0.52f, -0.20f),
                ( 760f, 620f, 1.00f,  0.02f),
                (1240f, 380f, 0.62f,  0.18f),
                (1560f, 460f, 0.78f, -0.08f),
            };

            for (int tile = 0; tile < 2; tile++)
            {
                var offset = tile * L.Reference.x;
                for (int i = 0; i < blobs.Length; i++)
                {
                    var b = blobs[i];
                    var blob = K.Img($"Puff_{tile}_{i}", rt, color);
                    var brt = blob.rectTransform;
                    brt.anchorMin = new Vector2(0f, 0.5f);
                    brt.anchorMax = new Vector2(0f, 0.5f);
                    brt.pivot = new Vector2(0f, 0.5f);
                    brt.sizeDelta = new Vector2(b.w, height * b.h);
                    brt.anchoredPosition = new Vector2(offset + b.x, height * b.y);
                    blob.sprite = puff;
                    blob.raycastTarget = false;
                }
            }

            return rt;
        }
    }
}
