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
    /// 도입부 씬을 조립한다.
    ///
    /// 챕터 씬과 대사창의 위치·크기·글자 크기를 정확히 같게 맞췄다.
    /// 인트로에서 챕터로 넘어갈 때 대사창이 조금이라도 움직이면
    /// 장면이 이어지지 않고 끊긴 것처럼 보이기 때문이다.
    /// </summary>
    public static class PrologueSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Prologue.unity";
        
        [MenuItem("It is You/4. 인트로 씬 생성", priority = 11)]
        public static void Build()
        {
            K.Prepare();
            Ed.EnsureFolder(SceneFolder);

            var prologue = PrologueDataBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = K.Boot(P.Bg);

            // ---------------------------------------------------------- 배경
            var backdrop = K.Img("Backdrop", root, new Color32(0x1E, 0x22, 0x27, 0xFF));
            K.Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = false;

            // 아트가 없는 동안에는 장면 이름을 띄워, 어디를 보고 있는지 알 수 있게 한다.
            var backdropLabel = K.Text("BackdropLabel", root, "", T.Display,
                new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Center);
            backdropLabel.characterSpacing = 8f;
            K.Anchor(backdropLabel.rectTransform, K.V(0, 0.5f), K.V(1, 0.5f),
                K.V(L.Gutter, -L.Unit * 6), K.V(-L.Gutter, L.Unit * 6));

            // ------------------------------------------------------ 대사창
            // 챕터 씬과 같은 수치를 쓴다. 씬이 바뀌어도 창이 제자리에 있어야 한다.
            var dialoguePanel = K.Img("DialoguePanel", root, P.PanelSunk);
            var dlgRt = dialoguePanel.rectTransform;
            K.Anchor(dlgRt, K.V(0, 0), K.V(1, 0),
                K.V(L.Gutter, L.Gutter), K.V(-L.Gutter, L.DialogueTop));
            var dlgGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();

            var speaker = K.Text("Speaker", dlgRt, "", T.Choice, P.Brass, TextAlignmentOptions.Left);
            K.Anchor(speaker.rectTransform, K.V(0, 1), K.V(0.5f, 1),
                K.V(L.Unit * 4, -L.Unit * 7), K.V(0, -L.Unit * 3));

            var direction = K.Text("StageDirection", dlgRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            direction.fontStyle = FontStyles.Italic;
            K.Anchor(direction.rectTransform, K.V(0, 1), K.V(1, 1),
                K.V(L.Unit * 4, -L.Unit * 11), K.V(-L.Unit * 4, -L.Unit * 7));

            var body = K.Text("Body", dlgRt, "", T.Body, P.Ink, TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 16f;
            body.raycastTarget = true;
            K.Anchor(body.rectTransform, K.V(0, 0), K.V(1, 1),
                K.V(L.Unit * 4, L.Unit * 6), K.V(-L.Unit * 4, -L.Unit * 12));

            var advance = K.Text("AdvanceIndicator", dlgRt, "▼", T.Caption, P.Brass, TextAlignmentOptions.Right);
            K.Anchor(advance.rectTransform, K.V(1, 0), K.V(1, 0),
                K.V(-L.Unit * 8, L.Unit * 2), K.V(-L.Unit * 3, L.Unit * 5));

            var presenter = dialoguePanel.gameObject.AddComponent<DialoguePresenter>();
            presenter.Set("root", dlgGroup)
                     .Set("speakerLabel", speaker)
                     .Set("bodyLabel", body)
                     .Set("stageDirectionLabel", direction)
                     .Set("advanceIndicator", advance.gameObject)
                     .Set("uiCamera", (Camera)null);

            // --------------------------------------------------- 상호작용 버튼
            // 대사창 바로 위 가운데. 대사를 읽다가 시선을 크게 옮기지 않아도 되는 자리다.
            var interactionRoot = K.UI("Interaction", root);
            var interRt = interactionRoot.GetComponent<RectTransform>();
            K.Anchor(interRt, K.V(0.5f, 0), K.V(0.5f, 0),
                K.V(-240f, L.BandBottom), K.V(240f, L.BandBottom + 80f));

            var interactionMade = K.Button("Button", interRt, "들여다본다", T.Choice);
            K.Stretch(interactionMade.rect);
            var interactionOutline = interactionMade.rect.gameObject.AddComponent<Outline>();
            interactionOutline.effectColor = P.Brass;
            interactionOutline.effectDistance = new Vector2(2f, -2f);

            // ------------------------------------------------------ 서류 팝업
            var documentRoot = K.UI("Document", root);
            var docRootRt = documentRoot.GetComponent<RectTransform>();
            K.Stretch(docRootRt);

            var scrim = K.Img("Scrim", docRootRt, new Color(0f, 0f, 0f, 0.62f));
            K.Stretch(scrim.rectTransform);

            var docPanel = K.Img("Panel", docRootRt, P.Panel);
            K.Anchor(docPanel.rectTransform, K.V(0.5f, 0.5f), K.V(0.5f, 0.5f),
                K.V(-384f, -304f), K.V(384f, 304f));   // 768 x 608
            // 둥근 모서리 스프라이트를 이 크기로 늘리면 모서리 반경까지 같이 커져
            // 종이가 아니라 커다란 버튼처럼 보인다. 서류는 각진 판으로 둔다.

            var docTitle = K.Text("Title", docPanel.rectTransform, "", T.Display, P.Brass,
                TextAlignmentOptions.TopLeft);
            K.Anchor(docTitle.rectTransform, K.V(0, 1), K.V(1, 1),
                K.V(L.Pad * 2, -L.Unit * 12), K.V(-L.Pad * 2, -L.Unit * 5));

            var docRule = K.Img("Rule", docPanel.rectTransform, P.Rule);
            K.Anchor(docRule.rectTransform, K.V(0, 1), K.V(1, 1),
                K.V(L.Pad * 2, -L.Unit * 14), K.V(-L.Pad * 2, -L.Unit * 14 + 2f));
            docRule.raycastTarget = false;

            var docBody = K.Text("Body", docPanel.rectTransform, "", T.Body, P.Ink,
                TextAlignmentOptions.TopLeft);
            docBody.textWrappingMode = TextWrappingModes.Normal;
            docBody.lineSpacing = 14f;
            K.Anchor(docBody.rectTransform, K.V(0, 0), K.V(1, 1),
                K.V(L.Pad * 2, L.Unit * 12), K.V(-L.Pad * 2, -L.Unit * 17));

            var docClose = K.Button("Close", docPanel.rectTransform, "닫기", T.Choice);
            K.Anchor(docClose.rect, K.V(0.5f, 0), K.V(0.5f, 0),
                K.V(-96f, L.Unit * 3), K.V(96f, L.Unit * 3 + 56f));

            // ---------------------------------------------------- 건너뛰기
            var skip = K.Button("Skip", root, "건너뛰기  ▶▶", T.Caption);
            K.Anchor(skip.rect, K.V(1, 1), K.V(1, 1),
                K.V(-L.Gutter - 184f, -L.Gutter - 56f), K.V(-L.Gutter, -L.Gutter));

            // ---------------------------------------------------- 컨트롤러
            var systems = new GameObject("Systems");
            var controller = systems.AddComponent<PrologueController>();
            controller.Set("prologue", prologue)
                      .Set("dialogue", presenter)
                      .Set("backdropImage", backdrop)
                      .Set("backdropLabel", backdropLabel)
                      .Set("interactionRoot", interactionRoot)
                      .Set("interactionButton", interactionMade.button)
                      .Set("interactionLabel", interactionMade.label)
                      .Set("documentRoot", documentRoot)
                      .Set("documentTitle", docTitle)
                      .Set("documentBody", docBody)
                      .Set("documentCloseButton", docClose.button)
                      .Set("skipButton", skip.button);

            interactionRoot.SetActive(false);
            documentRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 인트로 씬 생성 완료 — {ScenePath}\n" +
                      $"장면 {prologue.Beats.Count}개 · 끝나면 {prologue.NextSceneName} 로 넘어감");
        }
    }
}
