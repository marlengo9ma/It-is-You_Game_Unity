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
    /// 인트로 카툰 씬을 조립한다.
    ///
    /// 대사창이 없다. 카툰 파트는 텍스트를 읽는 구간이 아니라 그림이 넘어가는 구간이라,
    /// 화면에는 컷 한 장과 "누르면 다음" 안내만 둔다.
    ///
    /// 컷 안의 서류만 예외다. 그 자리에는 클릭 가능한 더미 오브젝트를 놓고,
    /// 그림이 들어오면 스프라이트만 끼우면 되도록 위치를 컷 데이터가 정하게 했다.
    /// </summary>
    public static class PrologueSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Prologue.unity";

        [MenuItem("It is You/4. 인트로 카툰 씬 생성", priority = 11)]
        public static void Build()
        {
            K.Prepare();
            Ed.EnsureFolder(SceneFolder);

            var prologue = PrologueDataBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = K.Boot(Color.black);

            var bg = K.Img("Background", root, Color.black);
            K.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            // ============================================================ 컷
            var cut = K.UI("Cut", root);
            var cutRt = cut.GetComponent<RectTransform>();
            K.Stretch(cutRt);
            var cutGroup = cut.AddComponent<CanvasGroup>();

            var cutImage = K.Img("Image", cutRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            K.Stretch(cutImage.rectTransform);
            cutImage.raycastTarget = false;

            // 그림이 없는 동안에는 컷 번호와 이름을 띄워, 어느 장면인지 알 수 있게 한다.
            var cutLabel = K.Text("Label", cutRt, "", T.Display,
                new Color32(0x5A, 0x61, 0x69, 0xFF), TextAlignmentOptions.Center);
            cutLabel.textWrappingMode = TextWrappingModes.Normal;
            cutLabel.lineSpacing = 22f;
            K.Anchor(cutLabel.rectTransform, K.V(0.12f, 0.3f), K.V(0.88f, 0.7f), Vector2.zero, Vector2.zero);

            // 컷 전체가 하나의 버튼이다. 클릭 = 다음 컷.
            var advanceMade = K.Button("AdvanceArea", cutRt, "", T.Caption);
            K.Stretch(advanceMade.rect);
            var advanceImg = advanceMade.rect.GetComponent<Image>();
            advanceImg.color = new Color(0f, 0f, 0f, 0f);   // 보이지 않지만 눌린다
            advanceImg.sprite = null;
            advanceMade.label.gameObject.SetActive(false);
            var advColors = advanceMade.button.colors;
            advColors.highlightedColor = Color.white;
            advColors.pressedColor = Color.white;
            advanceMade.button.colors = advColors;

            // 컷 안의 더미 오브젝트. 자리와 크기는 컷 데이터가 정한다.
            var propMade = K.Button("Prop", cutRt, "", T.Caption);
            var propRt = propMade.rect;
            var propImg = propRt.GetComponent<Image>();
            // 둥근 버튼 스프라이트를 쓰면 그림 위에 버튼이 떠 있는 것처럼 보인다.
            // 여기 놓이는 것은 버튼이 아니라 "컷 안의 물건"이라서 각진 판으로 둔다.
            propImg.sprite = null;
            propImg.color = new Color32(0x39, 0x3F, 0x47, 0xFF);
            var propColors = propMade.button.colors;
            propColors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            propColors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            propMade.button.colors = propColors;
            propMade.label.gameObject.SetActive(true);
            propMade.label.alignment = TextAlignmentOptions.Center;
            propMade.label.fontSize = T.Choice;

            var propOutline = propRt.gameObject.AddComponent<Outline>();
            propOutline.effectColor = P.Brass;
            propOutline.effectDistance = new Vector2(2f, -2f);

            var propHint = K.Text("Hint", propRt, "클릭", T.Caption, P.Brass, TextAlignmentOptions.Bottom);
            K.Anchor(propHint.rectTransform, K.V(0, 0), K.V(1, 0), K.V(0, L.Unit), K.V(0, L.Unit * 4));

            // ======================================================== 진행 안내
            var hint = K.UI("AdvanceHint", root);
            var hintRt = hint.GetComponent<RectTransform>();
            K.Anchor(hintRt, K.V(1, 0), K.V(1, 0),
                K.V(-L.Gutter - 320f, L.Gutter), K.V(-L.Gutter, L.Gutter + 40f));

            var hintLabel = K.Text("Label", hintRt, "화면을 누르면 다음 컷  ▼", T.Caption,
                new Color32(0x8A, 0x90, 0x98, 0xFF), TextAlignmentOptions.Right);
            K.Stretch(hintLabel.rectTransform);

            // ---------------------------------------------------- 건너뛰기
            var skip = K.Button("Skip", root, "건너뛰기  ▶▶", T.Caption);
            K.Anchor(skip.rect, K.V(1, 1), K.V(1, 1),
                K.V(-L.Gutter - 184f, -L.Gutter - 56f), K.V(-L.Gutter, -L.Gutter));

            // ========================================================== 팝업
            // 스킵 버튼보다 뒤에 만들어 위로 오게 한다. 팝업이 떠 있는 동안에는
            // 서류를 덮는 것이 아무것도 없어야 한다.
            var popupRoot = K.UI("Popup", root);
            var popupRootRt = popupRoot.GetComponent<RectTransform>();
            K.Stretch(popupRootRt);

            var scrim = K.Img("Scrim", popupRootRt, new Color(0f, 0f, 0f, 0.72f));
            K.Stretch(scrim.rectTransform);

            var popupPanel = K.Img("Panel", popupRootRt, P.Panel);
            K.Anchor(popupPanel.rectTransform, K.V(0.5f, 0.5f), K.V(0.5f, 0.5f),
                K.V(-384f, -304f), K.V(384f, 304f));   // 768 x 608
            // 서류 그림은 패널의 배경과 별개의 오브젝트여야 한다.
            // 같은 Image 를 쓰면 그림이 없을 때 패널까지 같이 사라진다.
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

            // ====================================================== 컨트롤러
            var systems = new GameObject("Systems");
            var settingsMenu = SettingsUiBuilder.BuildSettings(root, systems);
            SettingsUiBuilder.BuildPause(root, systems, settingsMenu);
            var controller = systems.AddComponent<PrologueController>();
            controller.Set("prologue", prologue)
                      .Set("cutImage", cutImage)
                      .Set("cutLabel", cutLabel)
                      .Set("cutGroup", cutGroup)
                      .Set("crossFadeSeconds", 0.28f)
                      .Set("advanceArea", advanceMade.button)
                      .Set("advanceHint", hint)
                      .Set("propRoot", propRt)
                      .Set("propButton", propMade.button)
                      .Set("propImage", propImg)
                      .Set("propLabel", propMade.label)
                      .Set("popupRoot", popupRoot)
                      .Set("popupImage", popupArt)
                      .Set("popupPlaceholder", popupPlaceholder)
                      .Set("popupCloseButton", popupClose.button)
                      .Set("skipButton", skip.button);

            propRt.gameObject.SetActive(false);
            popupRoot.SetActive(false);
            hint.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 인트로 카툰 씬 생성 완료 — {ScenePath}\n" +
                      $"컷 {prologue.Cuts.Count}장 · 끝나면 {prologue.NextSceneName} 로 넘어감");
        }
    }
}
