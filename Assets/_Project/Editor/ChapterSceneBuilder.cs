using System.Collections;
using System.Collections.Generic;
using ItIsYou.Characters;
using ItIsYou.Core;
using ItIsYou.Data;
using ItIsYou.Interrogation;
using ItIsYou.Investigation;
using ItIsYou.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using L = ItIsYou.EditorTools.Layout;
using P = ItIsYou.EditorTools.Layout.Palette;
using T = ItIsYou.EditorTools.Layout.Type;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 튜토리얼 챕터 한 편을 통째로 조립한다. 인트로 → 단서 수집 → 심문 → 지목.
    ///
    /// 페이즈마다 씬을 나누지 않고 한 씬 안에서 화면 묶음을 갈아 끼운다.
    /// 단서 목록과 대사창이 페이즈를 가로질러 살아 있어야 하기 때문이다.
    ///
    /// 배치 수치는 전부 <see cref="Layout"/> 에 있다. 여기서는 그 격자에 맞춰 놓기만 한다.
    ///
    /// 주의 하나: 에디터에서 <c>onClick.AddListener</c> 로 붙인 연결은 씬에 저장되지 않는다.
    /// 그래서 버튼은 전부 컴포넌트의 직렬화 필드로 넘기고, 연결은 런타임에 각자 한다.
    /// </summary>
    public static class ChapterSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string ScenePath = SceneFolder + "/Tutorial_Chapter.unity";
        private const string DataRoot = "Assets/_Project/Data";

        private static TMP_FontAsset _font;
        private static Sprite _uiSprite;

        [MenuItem("It is You/2. 튜토리얼 챕터 씬 생성", priority = 2)]
        public static void Build()
        {
            _font = FontSetup.Ensure();
            _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var chapter = TutorialDataBuilder.Build();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataRoot + "/GameDatabase.asset");

            Ed.EnsureFolder(SceneFolder);
            Ed.EnsureFolder(PrefabFolder);

            var choicePrefab = BuildChoiceButtonPrefab();
            var cluePrefab = BuildClueSlotPrefab();
            var plainButtonPrefab = BuildPlainButtonPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------------------------------------------------------- 기본
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = P.Bg;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            // 이 프로젝트는 새 Input System 을 쓴다. 구형 모듈을 붙이면 클릭이 전부 죽는다.
            new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = L.Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.transform as RectTransform;

            var bg = Img("Background", root, P.Bg);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            // =========================================================== 공통 UI

            // ---- 상단 바 ----
            var topBar = UI("TopBar", root);
            var topRt = topBar.GetComponent<RectTransform>();
            Anchor(topRt, V(0, 1), V(1, 1), V(L.Gutter, L.TopBarBottom), V(-L.Gutter, -L.Gutter));

            var titleLabel = Text("ChapterTitle", topRt, "튜토리얼 — 누가 그랬지?", T.Display, P.Ink, TextAlignmentOptions.Left);
            Anchor(titleLabel.rectTransform, V(0, 0.42f), V(0.5f, 1), Vector2.zero, Vector2.zero);

            var phaseLabel = Text("PhaseLabel", topRt, "", T.Caption, P.Brass, TextAlignmentOptions.Left);
            Spaced(phaseLabel);
            Anchor(phaseLabel.rectTransform, V(0, 0), V(0.5f, 0.42f), Vector2.zero, Vector2.zero);

            var tokenCaption = Text("TokenCaption", topRt, "남은 시간", T.Caption, P.InkSoft, TextAlignmentOptions.Right);
            Spaced(tokenCaption);
            Anchor(tokenCaption.rectTransform, V(0.6f, 0.52f), V(1, 1), Vector2.zero, Vector2.zero);

            var tokenLabel = Text("TokenValue", topRt, "—", T.Metric, P.Brass, TextAlignmentOptions.Right);
            tokenLabel.fontStyle = FontStyles.Normal;
            Anchor(tokenLabel.rectTransform, V(0.6f, 0), V(1, 0.52f), Vector2.zero, Vector2.zero);

            var idleBarBg = Img("IdleBar", topRt, P.Rule);
            Anchor(idleBarBg.rectTransform, V(0, 0), V(1, 0), V(0, -L.Unit * 2), V(0, -L.Unit));
            idleBarBg.sprite = _uiSprite;
            idleBarBg.raycastTarget = false;
            var idleFill = Img("Fill", idleBarBg.rectTransform, P.Alarm);
            Stretch(idleFill.rectTransform);
            idleFill.sprite = _uiSprite;
            idleFill.type = Image.Type.Filled;
            idleFill.fillMethod = Image.FillMethod.Horizontal;
            idleFill.fillAmount = 0f;
            idleFill.raycastTarget = false;

            // ---- 단서 패널 (항상 보임) ----
            var cluePanel = Img("CluePanel", root, P.Panel);
            var clueRt = cluePanel.rectTransform;
            Anchor(clueRt, V(1, 0), V(1, 1), V(L.ClueLeft, L.BandBottom), V(-L.Gutter, L.BandTop));

            var clueTitle = Text("Title", clueRt, "단서", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(clueTitle);
            Anchor(clueTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad, -L.Pad - 24f), V(-L.Pad, -L.Pad));

            var clueGrid = UI("Grid", clueRt);
            var gridRt = clueGrid.GetComponent<RectTransform>();
            Anchor(gridRt, V(0, 1), V(1, 1), V(L.Pad, -L.Unit * 44), V(-L.Pad, -L.Unit * 9));
            var glg = clueGrid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(96f, 96f);
            glg.spacing = new Vector2(L.Unit, L.Unit);
            glg.childAlignment = TextAnchor.UpperLeft;

            var tooltip = Img("Tooltip", clueRt, P.PanelSunk);
            Anchor(tooltip.rectTransform, V(0, 0), V(1, 0), V(L.Pad, L.Pad), V(-L.Pad, L.Unit * 52));
            tooltip.sprite = _uiSprite;

            var tipTitle = Text("TipTitle", tooltip.rectTransform, "", T.Choice, P.Ink, TextAlignmentOptions.TopLeft);
            Anchor(tipTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad, -L.Unit * 7), V(-L.Pad, -L.Unit * 2));

            var tipBody = Text("TipBody", tooltip.rectTransform, "", T.Caption + 1f, P.InkSoft, TextAlignmentOptions.TopLeft);
            tipBody.textWrappingMode = TextWrappingModes.Normal;
            tipBody.lineSpacing = 12f;
            Anchor(tipBody.rectTransform, V(0, 0), V(1, 1), V(L.Pad, L.Unit * 7), V(-L.Pad, -L.Unit * 8));

            var tipClose = PlainButton("Close", tooltip.rectTransform, "닫기", T.Caption);
            Anchor(tipClose.Item1, V(0.5f, 0), V(0.5f, 0), V(-64f, L.Unit * 2), V(64f, L.Unit * 6));

            var clueTrayView = cluePanel.gameObject.AddComponent<ClueTrayView>();
            clueTrayView.Set("container", gridRt)
                        .Set("slotPrefab", cluePrefab)
                        .Set("tooltipRoot", tooltip.gameObject)
                        .Set("tooltipTitle", tipTitle)
                        .Set("tooltipBody", tipBody)
                        .Set("closeButton", tipClose.Item2);
            tooltip.gameObject.SetActive(false);

            // ---- 대사창 (항상 보임) ----
            var dialoguePanel = Img("DialoguePanel", root, P.PanelSunk);
            var dlgRt = dialoguePanel.rectTransform;
            Anchor(dlgRt, V(0, 0), V(1, 0), V(L.Gutter, L.Gutter), V(-L.Gutter, L.DialogueTop));
            var dlgGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();

            var speaker = Text("Speaker", dlgRt, "", T.Choice, P.Brass, TextAlignmentOptions.Left);
            Anchor(speaker.rectTransform, V(0, 1), V(0.5f, 1), V(L.Unit * 4, -L.Unit * 7), V(0, -L.Unit * 3));

            var direction = Text("StageDirection", dlgRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            direction.fontStyle = FontStyles.Italic;
            Anchor(direction.rectTransform, V(0, 1), V(1, 1), V(L.Unit * 4, -L.Unit * 11), V(-L.Unit * 4, -L.Unit * 7));

            // 본문만 raycastTarget 을 켠다 — 밑줄 구간을 클릭해야 하기 때문이다.
            var body = Text("Body", dlgRt, "", T.Body, P.Ink, TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 16f;
            body.raycastTarget = true;
            Anchor(body.rectTransform, V(0, 0), V(1, 1), V(L.Unit * 4, L.Unit * 6), V(-L.Unit * 4, -L.Unit * 12));

            var advance = Text("AdvanceIndicator", dlgRt, "▼", T.Caption, P.Brass, TextAlignmentOptions.Right);
            Anchor(advance.rectTransform, V(1, 0), V(1, 0), V(-L.Unit * 8, L.Unit * 2), V(-L.Unit * 3, L.Unit * 5));

            var presenter = dialoguePanel.gameObject.AddComponent<DialoguePresenter>();
            presenter.Set("root", dlgGroup)
                     .Set("speakerLabel", speaker)
                     .Set("bodyLabel", body)
                     .Set("stageDirectionLabel", direction)
                     .Set("advanceIndicator", advance.gameObject)
                     .Set("uiCamera", (Camera)null);

            // ---- 하단 상태 줄 ----
            var status = Text("StatusLabel", root, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(status.rectTransform, V(0, 0), V(0.6f, 0), V(L.Gutter, L.Unit * 2), V(0, L.Unit * 5));

            // ======================================================= 수집 화면

            var collectScreen = UI("Screen_Collection", root);
            var collectRt = collectScreen.GetComponent<RectTransform>();
            Anchor(collectRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(L.ClueLeft - L.Gap, L.BandTop));

            var stage = Img("Stage", collectRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            Stretch(stage.rectTransform);
            stage.raycastTarget = false;

            var stageHint = Text("Hint", stage.rectTransform,
                "객실 테이블  —  살펴볼 곳을 눌러 보세요", T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF),
                TextAlignmentOptions.Center);
            Anchor(stageHint.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));

            // 핫스팟 여섯 개. 일반/특수의 겉모습은 완전히 같아야 한다.
            var hotspotRects = new (string id, float x0, float y0, float x1, float y1)[]
            {
                ("OBJ_PAPERS",     0.05f, 0.14f, 0.34f, 0.44f),
                ("OBJ_COFFEE",     0.38f, 0.26f, 0.53f, 0.52f),
                ("OBJ_TYPEWRITER", 0.58f, 0.10f, 0.88f, 0.40f),
                ("OBJ_RIBBON",     0.20f, 0.52f, 0.40f, 0.68f),
                ("OBJ_CAT",        0.56f, 0.50f, 0.86f, 0.84f),
                ("OBJ_GLIMPSE",    0.50f, 0.46f, 0.60f, 0.56f)   // 고양이 아래로 비죽 나온 부분
            };

            var sceneObjects = new List<InteractableObject>();
            foreach (var h in hotspotRects)
            {
                var def = AssetDatabase.LoadAssetAtPath<InteractableDefinition>(
                    $"{DataRoot}/Objects/Obj_{h.id}.asset");
                if (def == null) continue;

                var spot = Img("Hotspot_" + h.id, stage.rectTransform, new Color32(0x2E, 0x34, 0x3B, 0xFF));
                Anchor(spot.rectTransform, V(h.x0, h.y0), V(h.x1, h.y1), Vector2.zero, Vector2.zero);
                spot.sprite = _uiSprite;

                var spotLabel = Text("Label", spot.rectTransform, def.HoverLabel, T.Caption,
                    new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Center);
                Stretch(spotLabel.rectTransform);

                var outline = spot.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.97f, 0.88f, 0.55f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = false;

                var interactable = spot.gameObject.AddComponent<InteractableObject>();
                interactable.Set("definition", def)
                            .Set("outline", outline)
                            .Set("hitArea", spot);
                sceneObjects.Add(interactable);
            }

            var progressLabel = Text("Progress", collectRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(progressLabel.rectTransform, V(0, 0), V(0.5f, 0), V(L.Unit, L.Unit), V(0, L.Unit * 5));

            var goInterrogate = PlainButton("StartInterrogation", collectRt, "심문 시작", T.Choice);
            Anchor(goInterrogate.Item1, V(1, 0), V(1, 0), V(-L.Unit * 26, L.Unit), V(-L.Unit, L.Unit * 7));

            // ======================================================= 심문 화면

            var interrogateScreen = UI("Screen_Interrogation", root);
            var interRt = interrogateScreen.GetComponent<RectTransform>();
            Anchor(interRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(-L.Gutter, L.BandTop));

            // 선택지 패널
            var choicePanel = Img("ChoicePanel", interRt, P.Panel);
            var choiceRt = choicePanel.rectTransform;
            Anchor(choiceRt, V(0, 0), V(0, 1), V(0, 0), V(L.ChoiceWidth, 0));
            var choiceGroup = choicePanel.gameObject.AddComponent<CanvasGroup>();

            var choiceTitle = Text("Title", choiceRt, "심문", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(choiceTitle);
            Anchor(choiceTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad, -L.Pad - 24f), V(-L.Pad, -L.Pad));

            var choiceList = UI("List", choiceRt);
            var listRt = choiceList.GetComponent<RectTransform>();
            Anchor(listRt, V(0, 0), V(1, 1), V(L.Unit * 2, L.Unit * 2), V(-L.Unit * 2, -L.Unit * 9));
            var vlg = choiceList.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = L.Unit;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperLeft;

            var choiceListView = choicePanel.gameObject.AddComponent<ChoiceListView>();
            choiceListView.Set("container", listRt)
                          .Set("buttonPrefab", choicePrefab)
                          .Set("root", choiceGroup);

            // 캐릭터
            var characterGo = UI("Character_Sepi", interRt);
            var charRt = characterGo.GetComponent<RectTransform>();
            Anchor(charRt, V(0, 0), V(0, 0), V(L.ChoiceWidth + L.Gap, 0), V(L.ChoiceWidth + L.Gap + 416f, 616f));

            var charImg = characterGo.AddComponent<Image>();
            charImg.color = new Color32(0x3A, 0x40, 0x48, 0xFF);
            charImg.sprite = _uiSprite;
            var charGroup = characterGo.AddComponent<CanvasGroup>();
            var charOutline = characterGo.AddComponent<Outline>();
            charOutline.effectColor = P.Brass;
            charOutline.effectDistance = new Vector2(4f, -4f);
            charOutline.enabled = false;

            var charLabel = Text("Label", charRt, "세피\n(플레이스홀더)", T.Caption, P.InkSoft, TextAlignmentOptions.Center);
            Stretch(charLabel.rectTransform);

            var characterView = characterGo.AddComponent<PlaceholderCharacterView>();
            characterView.Set("characterId", "SEPI")
                         .Set("body", charImg)
                         .Set("reactionOutline", charOutline)
                         .Set("canvasGroup", charGroup);

            var clickTarget = characterGo.AddComponent<CharacterClickTarget>();

            var endBtn = PlainButton("EndInterrogation", interRt, "심문 종료", T.Choice);
            Anchor(endBtn.Item1, V(1, 0), V(1, 0), V(-L.Unit * 26 - L.ClueWidth - L.Gap, L.Unit),
                                                   V(-L.ClueWidth - L.Gap - L.Unit, L.Unit * 7));

            // ======================================================= 지목 화면

            var accuseScreen = UI("Screen_Accusation", root);
            var accuseRt = accuseScreen.GetComponent<RectTransform>();
            Anchor(accuseRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(-L.Gutter, L.BandTop));

            var accusePanel = Img("Panel", accuseRt, P.Panel);
            Anchor(accusePanel.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-320f, -200f), V(320f, 200f));
            accusePanel.sprite = _uiSprite;

            var accusePrompt = Text("Prompt", accusePanel.rectTransform, "", T.Body, P.Ink, TextAlignmentOptions.Center);
            Anchor(accusePrompt.rectTransform, V(0, 1), V(1, 1), V(L.Pad, -L.Unit * 12), V(-L.Pad, -L.Unit * 5));

            var accuseList = UI("List", accusePanel.rectTransform);
            var accuseListRt = accuseList.GetComponent<RectTransform>();
            Anchor(accuseListRt, V(0, 0), V(1, 1), V(L.Pad, L.Pad), V(-L.Pad, -L.Unit * 14));
            var avlg = accuseList.AddComponent<VerticalLayoutGroup>();
            avlg.spacing = L.Unit;
            avlg.childControlHeight = false;
            avlg.childControlWidth = true;
            avlg.childForceExpandWidth = true;
            avlg.childForceExpandHeight = false;
            avlg.childAlignment = TextAnchor.UpperCenter;

            // =================================================== 사건 이전 화면
            // 기획서의 [게임플레이 배경_캐릭터 씬]. 사건이 터지기 전의 객실이며,
            // 사건 직후의 인트로 대사도 같은 배경 위에서 이어진다.

            var talkScreen = UI("Screen_Talk", root);
            var talkRt = talkScreen.GetComponent<RectTransform>();
            Anchor(talkRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(-L.Gutter, L.BandTop));

            var talkBg = Img("Backdrop", talkRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            Stretch(talkBg.rectTransform);
            talkBg.raycastTarget = false;

            var talkTag = Text("Tag", talkBg.rectTransform, "객실  —  게임플레이 배경 (아트 대기)",
                T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Center);
            Anchor(talkTag.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));

            var talkSepi = Img("Sepi_Placeholder", talkRt, new Color32(0x33, 0x39, 0x41, 0xFF));
            Anchor(talkSepi.rectTransform, V(0.52f, 0f), V(0.82f, 0.92f), Vector2.zero, Vector2.zero);
            talkSepi.raycastTarget = false;
            var talkSepiTag = Text("Label", talkSepi.rectTransform, "세피\n(아트 대기)", T.Caption,
                P.InkSoft, TextAlignmentOptions.Center);
            Stretch(talkSepiTag.rectTransform);

            var talkCat = Img("Cat_Placeholder", talkRt, new Color32(0x2A, 0x2F, 0x36, 0xFF));
            Anchor(talkCat.rectTransform, V(0.18f, 0.08f), V(0.34f, 0.34f), Vector2.zero, Vector2.zero);
            talkCat.raycastTarget = false;
            var talkCatTag = Text("Label", talkCat.rectTransform, "검은 고양이\n(아트 대기)", T.Caption,
                new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Center);
            Stretch(talkCatTag.rectTransform);

            // ---- [일러스트 컷씬#1] ----
            var cutsceneScreen = UI("Screen_Cutscene", root);
            var cutsceneRt = cutsceneScreen.GetComponent<RectTransform>();
            Anchor(cutsceneRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(-L.Gutter, L.BandTop));

            var cutsceneImg = Img("Image", cutsceneRt, new Color32(0x15, 0x18, 0x1C, 0xFF));
            Stretch(cutsceneImg.rectTransform);
            cutsceneImg.raycastTarget = false;

            var cutsceneLabel = Text("Label", cutsceneRt, "", T.Display,
                new Color32(0x5A, 0x61, 0x69, 0xFF), TextAlignmentOptions.Center);
            cutsceneLabel.textWrappingMode = TextWrappingModes.Normal;
            Anchor(cutsceneLabel.rectTransform, V(0.1f, 0.35f), V(0.9f, 0.65f), Vector2.zero, Vector2.zero);

            cutsceneLabel.text = chapter.CutsceneName;
            cutsceneLabel.gameObject.SetActive(chapter.CutsceneImage == null);
            if (chapter.CutsceneImage != null)
            {
                cutsceneImg.sprite = chapter.CutsceneImage;
                cutsceneImg.color = Color.white;
                cutsceneImg.preserveAspect = true;
            }

            // 대사창은 새로 만든 전면 배경들보다 위에 있어야 한다.
            dialoguePanel.transform.SetAsLastSibling();

            // ================================================== 대화 도구
            // 로그와 창 숨기기. 평범한 대화 구간에서만 나타난다.
            var toolbar = UI("DialogueToolbar", root);
            var toolbarRt = toolbar.GetComponent<RectTransform>();
            Anchor(toolbarRt, V(1, 0), V(1, 0),
                V(-L.Gutter - 368f, L.DialogueTop + L.Unit), V(-L.Gutter, L.DialogueTop + L.Unit * 7));

            var toolbarLayout = toolbar.AddComponent<HorizontalLayoutGroup>();
            toolbarLayout.spacing = L.Unit;
            toolbarLayout.childControlWidth = true;
            toolbarLayout.childControlHeight = true;
            toolbarLayout.childForceExpandWidth = true;
            toolbarLayout.childForceExpandHeight = true;
            toolbarLayout.childAlignment = TextAnchor.MiddleRight;

            var logBtn = PlainButton("LogButton", toolbarRt, "지난 대사", T.Caption);
            var hideBtn = PlainButton("HideButton", toolbarRt, "대화창 숨기기", T.Caption);

            // ---- 로그 창 ----
            // 로그를 여는 순간만큼은 뒤의 장면을 눌러도 아무 일이 없어야 한다.
            var logRoot = UI("DialogueLog", root);
            var logRootRt = logRoot.GetComponent<RectTransform>();
            Stretch(logRootRt);

            var logScrim = Img("Scrim", logRootRt, new Color(0f, 0f, 0f, 0.66f));
            Stretch(logScrim.rectTransform);

            var logPanel = Img("Panel", logRootRt, new Color32(0x16, 0x19, 0x1D, 0xFF));
            var logRt = logPanel.rectTransform;
            Anchor(logRt, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-560f, -368f), V(560f, 368f));

            var logTitle = Text("Title", logRt, "지난 대사", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(logTitle);
            Anchor(logTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 9), V(-L.Pad * 2, -L.Unit * 4));

            var logRule = Img("Rule", logRt, P.Rule);
            Anchor(logRule.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 11), V(-L.Pad * 2, -L.Unit * 11 + 2f));
            logRule.raycastTarget = false;

            var scrollGo = UI("Scroll", logRt);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Anchor(scrollRt, V(0, 0), V(1, 1), V(L.Pad * 2, L.Unit * 11), V(-L.Pad * 2, -L.Unit * 13));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;

            var viewport = UI("Viewport", scrollRt);
            var viewportRt = viewport.GetComponent<RectTransform>();
            Stretch(viewportRt);
            viewport.AddComponent<RectMask2D>();

            var content = UI("Content", viewportRt);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var logBody = Text("Body", contentRt, "", T.Choice, P.Ink, TextAlignmentOptions.TopLeft);
            logBody.textWrappingMode = TextWrappingModes.Normal;
            logBody.lineSpacing = 12f;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            var logEmpty = Text("Empty", logRt, "아직 지나간 대사가 없습니다.", T.Caption, P.InkSoft,
                TextAlignmentOptions.Center);
            Anchor(logEmpty.rectTransform, V(0, 0.5f), V(1, 0.5f), V(0, -L.Unit * 3), V(0, L.Unit * 3));

            var logClose = PlainButton("Close", logRt, "닫기", T.Choice);
            Anchor(logClose.Item1, V(0.5f, 0), V(0.5f, 0), V(-96f, L.Unit * 3), V(96f, L.Unit * 3 + 56f));

            // ---- 창을 숨긴 동안 화면을 덮는 투명 버튼 ----
            var catcher = PlainButton("RestoreCatcher", root, "", T.Caption);
            Stretch(catcher.Item1);
            var catcherImg = catcher.Item1.GetComponent<Image>();
            catcherImg.color = new Color(0f, 0f, 0f, 0f);
            catcherImg.sprite = null;
            var catcherColors = catcher.Item2.colors;
            catcherColors.highlightedColor = Color.white;
            catcherColors.pressedColor = Color.white;
            catcher.Item2.colors = catcherColors;
            catcher.Item1.Find("Label").gameObject.SetActive(false);

            var catcherHint = Text("Hint", catcher.Item1, "아무 곳이나 누르면 대화창이 돌아옵니다",
                T.Caption, new Color32(0x8A, 0x90, 0x98, 0xFF), TextAlignmentOptions.BottomRight);
            Anchor(catcherHint.rectTransform, V(0.4f, 0), V(1, 0), V(0, L.Gutter), V(-L.Gutter, L.Gutter + 32f));

            // ---- 화면 전환 막 ----
            var faderImg = Img("ScreenFader", root, Color.black);
            Stretch(faderImg.rectTransform);
            var faderGroup = faderImg.gameObject.AddComponent<CanvasGroup>();
            var fader = faderImg.gameObject.AddComponent<ScreenFader>();
            fader.Set("group", faderGroup)
                 .Set("fadeSeconds", 0.4f)
                 .Set("holdSeconds", 0.25f);

            // ========================================================== 시스템

            var systems = new GameObject("Systems");
            var cursor = systems.AddComponent<CursorController>();
            clickTarget.Set("hitArea", charImg).Set("cursor", cursor);

            var investigation = systems.AddComponent<InvestigationController>();
            investigation.Set("sceneObjects", sceneObjects)
                         .Set("dialogue", presenter)
                         .Set("cursor", cursor)
                         .Set("clueTray", clueTrayView)
                         .Set("advanceButton", goInterrogate.Item2)
                         .Set("progressLabel", progressLabel);

            var interrogation = systems.AddComponent<InterrogationController>();
            interrogation.Set("database", db)
                         .Set("dialogue", presenter)
                         .Set("choiceList", choiceListView)
                         .Set("cursor", cursor);

            var flow = systems.AddComponent<GameFlowController>();
            flow.Set("database", db)
                .Set("chapter", chapter)
                .Set("fader", fader)
                .Set("investigation", investigation)
                .Set("interrogation", interrogation)
                .Set("dialogue", presenter)
                .Set("clueTray", clueTrayView)
                .Set("cursor", cursor)
                .Set("characterBindings", BuildCharacterBindings("SEPI", characterView));

            var accusation = accuseScreen.AddComponent<AccusationView>();
            accusation.Set("flow", flow)
                      .Set("chapter", chapter)
                      .Set("container", accuseListRt)
                      .Set("buttonPrefab", plainButtonPrefab)
                      .Set("prompt", accusePrompt);

            var binder = systems.AddComponent<PhaseScreenBinder>();
            binder.Set("flow", flow)
                  .Set("groups", BuildScreenGroups(talkScreen, cutsceneScreen,
                                                    collectScreen, interrogateScreen, accuseScreen,
                                                    cluePanel.gameObject))
                  .Set("alwaysVisible", new List<GameObject> { dialoguePanel.gameObject });

            var logView = systems.AddComponent<DialogueLogView>();
            logView.Set("dialogue", presenter)
                   .Set("root", logRoot)
                   .Set("body", logBody)
                   .Set("scroll", scrollRect)
                   .Set("closeButton", logClose.Item2)
                   .Set("emptyLabel", logEmpty);

            var toolbarView = systems.AddComponent<DialogueToolbar>();
            toolbarView.Set("flow", flow)
                       .Set("dialogue", presenter)
                       .Set("log", logView)
                       .Set("root", toolbar)
                       .Set("logButton", logBtn.Item2)
                       .Set("hideButton", hideBtn.Item2)
                       .Set("restoreCatcher", catcher.Item1.gameObject)
                       .Set("restoreButton", catcher.Item2)
                       .Set("visiblePhases", new List<GamePhase>
                       {
                           GamePhase.PreIncidentTalk,
                           GamePhase.Cutscene,
                           GamePhase.Intro
                       });

            var hud = systems.AddComponent<ChapterHud>();
            hud.Set("flow", flow)
               .Set("interrogation", interrogation)
               .Set("dialogue", presenter)
               .Set("characterClickTarget", clickTarget)
               .Set("clueTray", clueTrayView)
               .Set("tokenLabel", tokenLabel)
               .Set("tokenCaption", tokenCaption)
               .Set("idleFill", idleFill)
               .Set("phaseLabel", phaseLabel)
               .Set("statusLabel", status)
               .Set("chapterTitle", titleLabel)
               .Set("endInterrogationButton", endBtn.Item2);

            talkScreen.SetActive(false);
            cutsceneScreen.SetActive(false);
            collectScreen.SetActive(false);
            interrogateScreen.SetActive(false);
            accuseScreen.SetActive(false);
            logRoot.SetActive(false);
            catcher.Item1.gameObject.SetActive(false);
            toolbar.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 튜토리얼 챕터 씬 생성 완료 — {ScenePath}\n" +
                      "Play → 인트로 대사 → 단서 수집(핫스팟 6) → 심문 → 지목");
        }

        // ====================================================== 리플렉션 보조

        /// <summary>GameFlowController 안의 private 중첩 구조체 목록을 만든다.</summary>
        private static object BuildCharacterBindings(string suspectId, MonoBehaviour view)
        {
            var bindingType = typeof(GameFlowController).GetNestedType("CharacterBinding",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (bindingType == null) return null;

            var binding = System.Activator.CreateInstance(bindingType);
            bindingType.GetField("suspectId").SetValue(binding, suspectId);
            bindingType.GetField("view").SetValue(binding, view);

            var listType = typeof(List<>).MakeGenericType(bindingType);
            var list = (IList)System.Activator.CreateInstance(listType);
            list.Add(binding);
            return list;
        }

        private static object BuildScreenGroups(GameObject talk, GameObject cutscene,
            GameObject collect, GameObject interrogate, GameObject accuse, GameObject cluePanel)
        {
            var list = new List<PhaseScreenBinder.ScreenGroup>
            {
                // 사건 이전의 대화와 사건 직후의 인트로는 같은 배경을 쓴다.
                new PhaseScreenBinder.ScreenGroup {
                    root = talk,
                    phases = new List<GamePhase> { GamePhase.PreIncidentTalk, GamePhase.Intro }
                },
                new PhaseScreenBinder.ScreenGroup {
                    root = cutscene,
                    phases = new List<GamePhase> { GamePhase.Cutscene }
                },
                new PhaseScreenBinder.ScreenGroup {
                    root = collect,
                    phases = new List<GamePhase> { GamePhase.ClueCollection }
                },
                new PhaseScreenBinder.ScreenGroup {
                    root = interrogate,
                    phases = new List<GamePhase> { GamePhase.Interrogation1, GamePhase.Interrogation2 }
                },
                new PhaseScreenBinder.ScreenGroup {
                    root = accuse,
                    phases = new List<GamePhase> { GamePhase.Accusation }
                },
                // 단서 목록은 단서가 생기는 구간부터 보인다.
                // 사건이 터지기도 전에 빈 목록을 띄워 두면 무엇을 하는 화면인지 흐려진다.
                new PhaseScreenBinder.ScreenGroup {
                    root = cluePanel,
                    phases = new List<GamePhase> {
                        GamePhase.ClueCollection, GamePhase.Interrogation1,
                        GamePhase.Deduction, GamePhase.Interrogation2, GamePhase.Accusation
                    }
                }
            };
            return list;
        }

        // ============================================================ 프리팹

        private static ChoiceButton BuildChoiceButtonPrefab()
        {
            var go = UI("ChoiceButton", null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 88f);   // 11u

            var bg = go.AddComponent<Image>();
            bg.color = P.Surface;
            bg.sprite = _uiSprite;
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;

            var group = go.AddComponent<CanvasGroup>();

            var strip = Img("ToneStrip", rt, P.Brass);
            Anchor(strip.rectTransform, V(0, 0), V(0, 1), Vector2.zero, V(L.Unit * 0.5f, 0));
            strip.raycastTarget = false;

            var label = Text("Label", rt, "선택지", T.Choice, P.Ink, TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.lineSpacing = 8f;
            Anchor(label.rectTransform, V(0, 0), V(1, 1), V(L.Unit * 3, L.Unit), V(-L.Unit * 6, -L.Unit));

            var icon = Text("UnseenIcon", rt, "＋", T.Choice, P.Brass, TextAlignmentOptions.Center);
            Anchor(icon.rectTransform, V(1, 0.5f), V(1, 0.5f), V(-L.Unit * 5, -L.Unit * 2), V(-L.Unit, L.Unit * 2));

            var choiceButton = go.AddComponent<ChoiceButton>();
            choiceButton.Set("button", button)
                        .Set("label", label)
                        .Set("toneStrip", strip)
                        .Set("unseenContentIcon", icon.gameObject)
                        .Set("usedDim", group);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/ChoiceButton.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ChoiceButton>();
        }

        private static ClueSlot BuildClueSlotPrefab()
        {
            var go = UI("ClueSlot", null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96f, 96f);

            var bg = go.AddComponent<Image>();
            bg.color = P.Surface;
            bg.sprite = _uiSprite;
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var icon = Img("Icon", rt, new Color(1f, 1f, 1f, 0f));
            Anchor(icon.rectTransform, V(0, 0), V(1, 1), V(L.Unit, L.Unit), V(-L.Unit, -L.Unit));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 이름표 — 배경과 글자는 반드시 별개 오브젝트여야 한다(Graphic 은 하나만 허용).
            var hoverBg = Img("HoverLabel", rt, new Color(0f, 0f, 0f, 0.88f));
            Anchor(hoverBg.rectTransform, V(0, 0), V(1, 0), V(-L.Unit * 3, -L.Unit * 4), V(L.Unit * 3, 0));
            hoverBg.raycastTarget = false;
            hoverBg.sprite = _uiSprite;

            var hoverText = Text("Text", hoverBg.rectTransform, "???", T.Caption, P.Ink, TextAlignmentOptions.Center);
            Stretch(hoverText.rectTransform);

            var unread = Img("UnreadBadge", rt, P.Brass);
            Anchor(unread.rectTransform, V(1, 1), V(1, 1), V(-L.Unit * 2.5f, -L.Unit * 2.5f), V(-L.Unit, -L.Unit));
            unread.raycastTarget = false;

            var usable = Text("UsableBadge", rt, "!", T.Choice, P.Good, TextAlignmentOptions.Center);
            Anchor(usable.rectTransform, V(0, 1), V(0, 1), V(L.Unit, -L.Unit * 4), V(L.Unit * 4, -L.Unit));

            var slot = go.AddComponent<ClueSlot>();
            slot.Set("button", button)
                .Set("icon", icon)
                .Set("hoverLabel", hoverText)
                .Set("hoverRoot", hoverBg.gameObject)
                .Set("unreadBadge", unread.gameObject)
                .Set("usableBadge", usable.gameObject);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/ClueSlot.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ClueSlot>();
        }

        private static Button BuildPlainButtonPrefab()
        {
            var go = UI("PlainButton", null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 56f);   // 7u

            var img = go.AddComponent<Image>();
            img.color = P.Surface;
            img.sprite = _uiSprite;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var label = Text("Label", rt, "버튼", T.Choice, P.Ink, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/PlainButton.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Button>();
        }

        // ============================================================== 헬퍼

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static Image Img(string name, Transform parent, Color color)
        {
            var go = UI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>글자는 언제나 자기 전용 오브젝트에 만든다. Graphic 충돌을 피하기 위해서다.</summary>
        private static TMP_Text Text(string name, Transform parent, string content, float size,
            Color color, TextAlignmentOptions align)
        {
            var go = UI(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (_font != null) t.font = _font;
            return t;
        }

        /// <summary>구획 제목처럼 자간을 넓혀 쓰는 작은 라벨.</summary>
        private static void Spaced(TMP_Text t)
        {
            t.characterSpacing = 10f;
            t.fontStyle = FontStyles.UpperCase;
        }

        private static (RectTransform, Button) PlainButton(string name, Transform parent, string label, float size)
        {
            var go = UI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = P.Surface;
            img.sprite = _uiSprite;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var text = Text("Label", rt, label, size, P.Ink, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return (rt, button);
        }

        private static void Stretch(RectTransform rt)
            => Anchor(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
