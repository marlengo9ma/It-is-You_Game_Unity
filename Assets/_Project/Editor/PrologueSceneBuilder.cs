using ItIsYou.Core;
using ItIsYou.UI;
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
    /// 인트로 카툰 씬을 조립한다.
    ///
    /// 화면 가운데에 종이 한 장(1440×1080)이 놓이고, 그 위로 페이지마다 컷이 차례로 들어온다.
    /// 컷 그림 · 자리 · 애니메이션은 IntroComic 프리팹에 있고, 씬에는 프리팹 <b>연결된 인스턴스</b>를 넣는다 —
    /// 그래서 프리팹을 고치면 씬을 다시 만들지 않아도 바로 반영되고, 씬을 다시 만들어도 편집한 값이 지워지지 않는다.
    ///
    /// 대사창은 없다. 카툰이 끝나면 챕터 씬의 기차역 대화로 넘어간다.
    /// </summary>
    public static class PrologueSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Prologue.unity";

        private static readonly Color Backdrop = new Color32(0x12, 0x13, 0x15, 0xFF);

        [MenuItem("It is You/4. 인트로 카툰 씬 생성", priority = 11)]
        public static void Build()
        {
            UiLayout.RecordBeforeRebuild();
            K.Prepare();
            Ed.EnsureFolder(SceneFolder);

            var prologue = PrologueDataBuilder.Build();

            // 프리팹이 아직 없으면 ArtSource/Intro 에서 한 번 만들어 둔다. 있으면 손대지 않는다.
            var comicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IntroComicImporter.PrefabPath);
            if (comicPrefab == null)
            {
                IntroComicImporter.Import(resetLayout: false);
                comicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IntroComicImporter.PrefabPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = K.Boot(Backdrop);

            var bg = K.Img("Background", root, Backdrop);
            K.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            // 화면 전체가 '다음' 버튼이다. 컷 그림보다 뒤에 깔고, 컷 그림은 클릭을 받지 않게 해 두었으므로
            // 어디를 눌러도 이 버튼이 받는다 — 봉투(클릭 영역)만 그 위에서 따로 받는다.
            var advanceMade = K.Button("AdvanceArea", root, "", T.Caption);
            K.Stretch(advanceMade.rect);
            var advanceImg = advanceMade.rect.GetComponent<Image>();
            advanceImg.color = new Color(0f, 0f, 0f, 0f);
            advanceImg.sprite = null;
            advanceMade.label.gameObject.SetActive(false);
            advanceMade.button.transition = Selectable.Transition.None;

            // ============================================================ 카툰
            ComicBookView comic = null;
            if (comicPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(comicPrefab, root);
                inst.name = "IntroComic";
                K.Stretch((RectTransform)inst.transform);
                comic = inst.GetComponent<ComicBookView>();
            }
            else
            {
                Debug.LogWarning("[It is You] IntroComic 프리팹을 만들지 못했습니다 — ArtSource/Intro 폴더를 확인하세요.");
            }

            // ======================================================== 진행 안내
            var hint = K.UI("AdvanceHint", root);
            var hintRt = hint.GetComponent<RectTransform>();
            K.Anchor(hintRt, K.V(1, 0), K.V(1, 0),
                K.V(-L.Gutter - 200f, L.Gutter), K.V(-L.Pad, L.Gutter + 40f));
            var hintLabel = K.Text("Label", hintRt, "다음  ▼", T.Caption,
                new Color32(0x8A, 0x90, 0x98, 0xFF), TextAlignmentOptions.Right);
            K.Stretch(hintLabel.rectTransform);

            // ---------------------------------------------------- 건너뛰기
            var skip = K.Button("Skip", root, "건너뛰기  ▶▶", T.Caption);
            K.Anchor(skip.rect, K.V(1, 1), K.V(1, 1),
                K.V(-L.Pad - 184f, -L.Pad - 56f), K.V(-L.Pad, -L.Pad));

            // ========================================================== 팝업
            // 서류 봉투를 누르면 뜨는 서류. 스킵 버튼보다 뒤에 만들어 위로 오게 한다.
            var popupRoot = K.UI("Popup", root);
            var popupRootRt = popupRoot.GetComponent<RectTransform>();
            K.Stretch(popupRootRt);

            var scrim = K.Img("Scrim", popupRootRt, new Color(0f, 0f, 0f, 0.72f));
            K.Stretch(scrim.rectTransform);

            var popupPanel = K.Img("Panel", popupRootRt, P.Panel);
            K.Anchor(popupPanel.rectTransform, K.V(0.5f, 0.5f), K.V(0.5f, 0.5f),
                K.V(-384f, -340f), K.V(384f, 340f));   // 768 x 680
            // 서류 그림은 패널의 배경과 별개의 오브젝트여야 한다 — 같은 Image 를 쓰면 그림이 없을 때 패널까지 사라진다.
            var popupArt = K.Img("Art", popupPanel.rectTransform, Color.white);
            K.Anchor(popupArt.rectTransform, K.V(0, 0), K.V(1, 1),
                K.V(L.Pad, L.Unit * 11), K.V(-L.Pad, -L.Pad));
            popupArt.preserveAspect = true;
            popupArt.raycastTarget = false;
            popupArt.enabled = false;

            var popupPlaceholder = K.Text("Placeholder", popupPanel.rectTransform, "", T.Body, P.Ink,
                TextAlignmentOptions.TopLeft);
            popupPlaceholder.textWrappingMode = TextWrappingModes.Normal;
            popupPlaceholder.lineSpacing = 14f;
            K.Anchor(popupPlaceholder.rectTransform, K.V(0, 0), K.V(1, 1),
                K.V(L.Pad * 2, L.Unit * 12), K.V(-L.Pad * 2, -L.Unit * 6));

            var popupClose = K.Button("Close", popupPanel.rectTransform, "닫기", T.Choice);
            K.Anchor(popupClose.rect, K.V(0.5f, 0), K.V(0.5f, 0),
                K.V(-96f, L.Unit * 3), K.V(96f, L.Unit * 3 + 56f));

            // ====================================================== 끝 막
            var endFade = K.Img("EndFade", root, Color.black);
            K.Stretch(endFade.rectTransform);
            endFade.raycastTarget = true;
            var endGroup = endFade.gameObject.AddComponent<CanvasGroup>();
            endGroup.alpha = 0f;
            endGroup.blocksRaycasts = false;

            // ====================================================== 컨트롤러
            var systems = new GameObject("Systems");
            var settingsMenu = SettingsUiBuilder.BuildSettings(root, systems);
            SettingsUiBuilder.BuildPause(root, systems, settingsMenu);
            var controller = systems.AddComponent<PrologueController>();
            controller.Set("prologue", prologue)
                      .Set("comic", comic)
                      .Set("advanceArea", advanceMade.button)
                      .Set("advanceHint", hint)
                      .Set("popupRoot", popupRoot)
                      .Set("popupImage", popupArt)
                      .Set("popupPlaceholder", popupPlaceholder)
                      .Set("popupCloseButton", popupClose.button)
                      .Set("skipButton", skip.button)
                      .Set("endFade", endGroup)
                      .Set("endFadeSeconds", 1.0f);

            popupRoot.SetActive(false);
            hint.SetActive(false);

            // UI 편집기에서 바꾼 자리 · 이미지 · 글자 모양을 입히고 저장한다(기준도 함께 적는다).
            UiLayout.SaveBuiltScene(scene, ScenePath);
            AssetDatabase.Refresh();

            var pages = comic != null ? comic.Pages.Count : 0;
            Debug.Log($"[It is You] 인트로 카툰 씬 생성 완료 — {ScenePath}\n" +
                      $"페이지 {pages}장 (IntroComic 프리팹) · 끝나면 {prologue.NextSceneName} 로 넘어감");
        }
    }
}
