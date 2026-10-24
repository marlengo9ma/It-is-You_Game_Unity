using ItIsYou.Core;
using ItIsYou.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using L = ItIsYou.EditorTools.Layout;
using P = ItIsYou.EditorTools.Layout.Palette;
using T = ItIsYou.EditorTools.Layout.Type;
using K = ItIsYou.EditorTools.UiKit;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 스타트 화면을 조립한다 (2026-10-24 개편).
    ///
    /// 화면 전체가 배경 그림 한 장(나중에 애니메이션)이고, 그 위에는 메뉴 버튼만 있다.
    /// · 배경: 아트 세트의 titleBackgroundVideo(영상)가 있으면 영상을, 없으면 titleBackground(그림)를 화면에 꽉 차게 깐다
    ///   (비율이 달라도 빈 띠 없이 — 가장자리만 잘린다). 위아래 위치는 titleBackgroundPlace 의 offset 으로 맞춘다.
    /// · 메뉴: 마우스를 올리면 살짝 떠오르며 밝아진다(<see cref="HoverLift"/>). 자리는 UI 편집기로 옮긴다.
    /// </summary>
    public static class TitleSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Title.unity";

        // ---- 메뉴 ----
        private const float MenuWidth = 400f;    // 50u
        private const float MenuButton = 72f;    // 9u
        private const float MenuGap = 16f;       // 2u

        private static readonly Color ButtonColor = new Color(0.08f, 0.09f, 0.11f, 0.78f);
        private static readonly Color ButtonHighlight = new Color(0.30f, 0.24f, 0.12f, 0.92f);
        private static readonly Color LabelHighlight = new Color32(0xF2, 0xD7, 0x8C, 0xFF);

        [MenuItem("It is You/3. 타이틀 씬 생성", priority = 10)]
        public static void Build()
        {
            UiLayout.RecordBeforeRebuild();
            K.Prepare();
            Ed.EnsureFolder(SceneFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = K.Boot(Color.black);

            var art = AssetDatabase.LoadAssetAtPath<ItIsYou.Data.ArtSet>("Assets/_Project/Art/ArtSet.asset");

            // ---------------------------------------------------- 배경
            // 화면 전체를 덮는 판. 안의 그림(또는 영상)은 비율을 지킨 채 꽉 채운다.
            var bgFrame = K.UI("Background", root);
            var bgFrameRt = (RectTransform)bgFrame.transform;
            K.Stretch(bgFrameRt);
            bgFrame.AddComponent<RectMask2D>();

            var video = art != null ? art.titleBackgroundVideo : null;
            var sprite = art != null ? art.titleBackground : null;
            var place = art != null ? art.titleBackgroundPlace : ItIsYou.Data.ArtSet.Placement.Stretch;

            // Fit = 그림 비율로 화면을 꽉 덮는 판(AspectRatioFitter). 그 안의 그림을 offset · scale 로 옮긴다 —
            // AspectRatioFitter 는 자기 자리를 매번 가운데로 되돌리므로 옮기는 값은 자식에 둔다.
            var fit = K.UI("Fit", bgFrameRt);
            var fitRt = (RectTransform)fit.transform;
            K.Stretch(fitRt);
            var fitter = fit.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            if (video != null)
            {
                fitter.aspectRatio = video.height > 0 ? (float)video.width / video.height : 16f / 9f;
                var raw = K.UI("Video", fitRt).AddComponent<RawImage>();
                raw.raycastTarget = false;
                K.Stretch(raw.rectTransform);
                var player = raw.gameObject.AddComponent<VideoPlayer>();
                player.clip = video;
                player.isLooping = true;
                player.playOnAwake = true;
                player.audioOutputMode = VideoAudioOutputMode.None;
                player.renderMode = VideoRenderMode.RenderTexture;
                raw.gameObject.AddComponent<VideoBackground>();
                Place(raw.rectTransform, place);
            }
            else
            {
                fitter.aspectRatio = sprite != null && sprite.rect.height > 0 ? sprite.rect.width / sprite.rect.height : 16f / 9f;
                var img = K.Img("Image", fitRt, sprite != null ? Color.white : P.Bg);
                img.raycastTarget = false;
                K.Stretch(img.rectTransform);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    Place(img.rectTransform, place);
                }
            }

            // -------------------------------------------------------- 메뉴
            // 버튼마다 자리를 직접 잡는다(자동 정렬 없음) — UI 편집기에서 하나씩 옮길 수 있게.
            var menu = K.UI("Menu", root);
            var menuRt = menu.GetComponent<RectTransform>();
            var menuHeight = MenuButton * 4 + MenuGap * 3;   // 336
            K.Anchor(menuRt, K.V(0, 0), K.V(0, 0),
                K.V(L.Gutter * 2, L.Unit * 16), K.V(L.Gutter * 2 + MenuWidth, L.Unit * 16 + menuHeight));

            var newGame = MenuItem(menuRt, "NewGame", "새 게임", 0);
            var continueGame = MenuItem(menuRt, "Continue", "이어하기", 1);
            var settings = MenuItem(menuRt, "Settings", "설정", 2);
            var quit = MenuItem(menuRt, "Quit", "종료", 3);

            // -------------------------------------------------------- 컨트롤러
            var systems = new GameObject("Systems");
            var settingsMenu = SettingsUiBuilder.BuildSettings(root, systems);
            var title = systems.AddComponent<TitleController>();
            title.Set("newGameButton", newGame)
                 .Set("continueButton", continueGame)
                 .Set("settingsButton", settings)
                 .Set("settings", settingsMenu)
                 .Set("quitButton", quit);

            // UI 편집기에서 바꾼 자리 · 이미지 · 글자 모양을 입히고 저장한다(기준도 함께 적는다).
            UiLayout.SaveBuiltScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 타이틀 씬 생성 완료 — {ScenePath}" +
                      (video != null ? " (배경 영상)" : sprite != null ? $" (배경 {sprite.name})" : " (배경 없음)"));
        }

        // ============================================================== 헬퍼

        /// <summary>
        /// 메뉴 버튼 하나. 눌리는 판(투명) 안에 보이는 판(Visual)이 있고, 마우스를 올리면 Visual 만 떠오른다 —
        /// 눌리는 자리는 그대로라 버튼 가장자리에서 떨리지 않는다.
        /// </summary>
        private static Button MenuItem(RectTransform parent, string name, string label, int index)
        {
            var go = K.UI(name, parent);
            var rt = (RectTransform)go.transform;
            var top = -(MenuButton + MenuGap) * index;
            K.Anchor(rt, K.V(0, 1), K.V(1, 1), K.V(0, top - MenuButton), K.V(0, top));

            var hit = go.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;

            var visual = K.Img("Visual", rt, ButtonColor);
            K.Stretch(visual.rectTransform);
            visual.sprite = K.UiSprite; visual.type = Image.Type.Sliced;
            visual.raycastTarget = false;

            // 왼쪽에 놋쇠 선 한 줄. 선택지 버튼과 같은 어휘를 쓴다.
            var strip = K.Img("Strip", visual.rectTransform, P.Brass);
            K.Anchor(strip.rectTransform, K.V(0, 0), K.V(0, 1), Vector2.zero, K.V(L.Unit * 0.5f, 0));
            strip.raycastTarget = false;

            var text = K.Text("Label", visual.rectTransform, label, T.Body, P.Ink, TextAlignmentOptions.Left);
            K.Stretch(text.rectTransform);
            text.margin = new Vector4(L.Pad, 0f, 0f, 0f);

            go.AddComponent<HoverLift>()
              .Set("button", button)
              .Set("visual", visual.rectTransform)
              .Set("background", visual)
              .Set("label", text)
              .Set("liftPixels", 8f)
              .Set("highlightColor", ButtonHighlight)
              .Set("labelHighlightColor", LabelHighlight);
            return button;
        }

        /// <summary>아트 세트의 자리 값(offset · scale · 좌우 반전)을 적용한다.</summary>
        private static void Place(RectTransform rt, ItIsYou.Data.ArtSet.Placement place)
        {
            if (rt == null) return;
            rt.anchoredPosition += place.offset;
            var scale = place.Scale;
            rt.localScale = new Vector3(place.flipX ? -scale : scale, scale, 1f);
        }
    }
}
