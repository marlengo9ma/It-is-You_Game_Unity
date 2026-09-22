using System.Collections;
using System.Collections.Generic;
using ItIsYou.Characters;
using ItIsYou.Core;
using ItIsYou.Data;
using ItIsYou.Interrogation;
using ItIsYou.Deduction;
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
            var leadPrefab = BuildLeadSlotPrefab();
            var plainButtonPrefab = BuildPlainButtonPrefab();
            var candidatePrefab = BuildDeductionCandidatePrefab();

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
            // 2026-09-22 스케치: 우측 1/3 은 단서·정황 목록과 기능 아이콘, 좌측 2/3 이 무대.
            // 상대의 말은 위 말풍선, 주인공의 말·속마음·지문은 아래 수첩 — 시선이 아래로 떨어지지 않게.

            // ---- 좌상단: 시계 · 장소 · 페이즈 ----
            var topBar = UI("TopBar", root);
            var topRt = topBar.GetComponent<RectTransform>();
            Anchor(topRt, V(0, 1), V(0, 1), V(L.Gutter, -L.Gutter - 176f), V(L.Gutter + 480f, -L.Gutter));

            var clockLabel = Text("Clock", topRt, "10:24", 48f, P.Ink, TextAlignmentOptions.TopLeft);
            clockLabel.characterSpacing = 4f;
            Anchor(clockLabel.rectTransform, V(0, 1), V(0, 1), V(0, -56f), V(160f, 0f));

            var dateLabel = Text("Date", topRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(dateLabel.rectTransform, V(0, 1), V(1, 1), V(168f, -48f), V(0, -L.Unit));

            var locationLabel = Text("Location", topRt, "", T.Choice, P.Ink, TextAlignmentOptions.Left);
            Anchor(locationLabel.rectTransform, V(0, 1), V(1, 1), V(0, -88f), V(0, -60f));

            var phaseLabel = Text("PhaseLabel", topRt, "", T.Caption, P.Brass, TextAlignmentOptions.Left);
            Spaced(phaseLabel);
            Anchor(phaseLabel.rectTransform, V(0, 1), V(1, 1), V(0, -116f), V(0, -92f));

            // 타임 토큰과 무반응 게이지. 시계 아래에 작게 — 시간이 줄어드는 것이 시계 옆에 있어야 읽힌다.
            var tokenCaption = Text("TokenCaption", topRt, "남은 시간", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Spaced(tokenCaption);
            Anchor(tokenCaption.rectTransform, V(0, 1), V(0, 1), V(0, -144f), V(96f, -120f));

            var tokenLabel = Text("TokenValue", topRt, "—", T.Choice, P.Brass, TextAlignmentOptions.Left);
            tokenLabel.fontStyle = FontStyles.Normal;
            Anchor(tokenLabel.rectTransform, V(0, 1), V(0, 1), V(104f, -148f), V(240f, -118f));

            var idleBarBg = Img("IdleBar", topRt, P.Rule);
            Anchor(idleBarBg.rectTransform, V(0, 1), V(0, 1), V(0, -160f), V(240f, -154f));
            idleBarBg.sprite = _uiSprite; idleBarBg.type = Image.Type.Sliced;
            idleBarBg.raycastTarget = false;
            var idleFill = Img("Fill", idleBarBg.rectTransform, P.Alarm);
            Stretch(idleFill.rectTransform);
            idleFill.sprite = _uiSprite; idleFill.type = Image.Type.Sliced;
            idleFill.type = Image.Type.Filled;
            idleFill.fillMethod = Image.FillMethod.Horizontal;
            idleFill.fillAmount = 0f;
            idleFill.raycastTarget = false;

            // 챕터 제목은 주 영역의 우상단에 작게. 시계가 주인공이다.
            var titleLabel = Text("ChapterTitle", root, "튜토리얼 — 누가 그랬지?", T.Caption, P.InkSoft, TextAlignmentOptions.TopRight);
            Anchor(titleLabel.rectTransform, V(0, 1), V(1, 1), V(L.Gutter + 480f, -L.Gutter - 24f), V(L.MainRight, -L.Gutter));

            // ---- 우측 1/3: 물품 증거 · 정황/실마리 · 기능 아이콘 ----
            var cluePanel = Img("SidePanel", root, P.Panel);
            var clueRt = cluePanel.rectTransform;
            Anchor(clueRt, V(1, 0), V(1, 1), V(L.ClueLeft, 0), V(0, 0));

            var sideRule = Img("EdgeRule", clueRt, P.Rule);
            Anchor(sideRule.rectTransform, V(0, 0), V(0, 1), V(0, 0), V(2f, 0));
            sideRule.raycastTarget = false;

            var evidenceTitle = Text("EvidenceTitle", clueRt, "Evidence", 28f, P.Ink, TextAlignmentOptions.Left);
            evidenceTitle.fontStyle = FontStyles.Bold;
            Anchor(evidenceTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Gutter - 40f), V(-L.Pad * 2, -L.Gutter));
            var evidenceSub = Text("EvidenceSub", clueRt, "물품 증거", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Anchor(evidenceSub.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2 + 136f, -L.Gutter - 36f), V(-L.Pad * 2, -L.Gutter - 8f));

            // 3열 격자. 128 칸 + 24 간격 = 432, 좌우 여백 48 로 640 안에 든다.
            var evidenceScroll = BuildScroll("EvidenceScroll", clueRt, V(L.Pad * 2, -L.Unit * 71), V(-L.Pad * 2, -L.Unit * 13));
            evidenceScroll.scroll.GetComponent<RectTransform>().anchorMin = V(0, 1);
            evidenceScroll.scroll.GetComponent<RectTransform>().offsetMin = V(L.Pad * 2, -L.Unit * 71);
            Object.DestroyImmediate(evidenceScroll.content.GetComponent<VerticalLayoutGroup>());
            var glg = evidenceScroll.content.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(128f, 128f);
            glg.spacing = new Vector2(L.Gap, L.Gap);
            glg.childAlignment = TextAnchor.UpperLeft;
            var gridRt = evidenceScroll.content;

            var sideDivider = Img("Divider", clueRt, P.Rule);
            Anchor(sideDivider.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 75), V(-L.Pad * 2, -L.Unit * 75 + 2f));
            sideDivider.raycastTarget = false;

            var leadsTitle = Text("LeadsTitle", clueRt, "Leads", 28f, P.Ink, TextAlignmentOptions.Left);
            leadsTitle.fontStyle = FontStyles.Bold;
            Anchor(leadsTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 83), V(-L.Pad * 2, -L.Unit * 78));
            var leadsSub = Text("LeadsSub", clueRt, "정황 / 실마리", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Anchor(leadsSub.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2 + 96f, -L.Unit * 82.5f), V(-L.Pad * 2, -L.Unit * 79));

            var leadsScroll = BuildScroll("LeadsScroll", clueRt, V(L.Pad * 2, L.Unit * 20), V(-L.Pad * 2, -L.Unit * 86));
            leadsScroll.content.GetComponent<VerticalLayoutGroup>().spacing = L.Unit;
            var leadsRt = leadsScroll.content;

            var clueHint = Text("Hint", clueRt, "칸을 누르면 열람", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Anchor(clueHint.rectTransform, V(0, 0), V(1, 0), V(L.Pad * 2, L.Unit * 15), V(-L.Pad * 2, L.Unit * 18));

            // 기능 아이콘 — 기록(돋보기 자리) · 설정(톱니 자리). 아이콘 소스가 오면 글자를 그림으로 바꾼다.
            var sideLogBtn = PlainButton("LogButton", clueRt, "기록", T.Caption);
            Anchor(sideLogBtn.Item1, V(0, 0), V(0, 0), V(L.Pad * 2, L.Gutter), V(L.Pad * 2 + 72f, L.Gutter + 72f));
            var sideSettingsBtn = PlainButton("SettingsButton", clueRt, "설정", T.Caption);
            Anchor(sideSettingsBtn.Item1, V(1, 0), V(1, 0), V(-L.Pad * 2 - 72f, L.Gutter), V(-L.Pad * 2, L.Gutter + 72f));

            var clueTrayView = cluePanel.gameObject.AddComponent<ClueTrayView>();
            clueTrayView.Set("container", gridRt)
                        .Set("slotPrefab", cluePrefab)
                        .Set("leadsContainer", leadsRt)
                        .Set("leadPrefab", leadPrefab);

            // ---- 아래: 주인공의 수첩 (항상 보임) ----
            // 주인공의 말 · 속마음 · 지문. 스케치의 수첩 자리 — 좌측 손과 스프링은 아트 대기.
            var dialoguePanel = Img("DialoguePanel", root, P.PanelSunk);
            var dlgRt = dialoguePanel.rectTransform;
            Anchor(dlgRt, V(0, 0), V(0, 0), V(L.Gutter + 40f, L.Unit * 2), V(L.Gutter + 1064f, L.Unit * 30));
            var dlgGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();

            var notebookEdge = Img("NotebookEdge", dlgRt, P.Rule);
            Anchor(notebookEdge.rectTransform, V(0, 0), V(0, 1), V(0, 0), V(L.Unit * 3, 0));
            notebookEdge.raycastTarget = false;
            var notebookTag = Text("Tag", dlgRt, "수첩 (아트 대기)", T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.TopRight);
            Anchor(notebookTag.rectTransform, V(1, 1), V(1, 1), V(-200f, -28f), V(-L.Unit * 2, -L.Unit));

            var speaker = Text("Speaker", dlgRt, "", T.Choice, P.Brass, TextAlignmentOptions.Left);
            Anchor(speaker.rectTransform, V(0, 1), V(0.5f, 1), V(L.Unit * 6, -L.Unit * 7), V(0, -L.Unit * 3));

            var direction = Text("StageDirection", dlgRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            direction.fontStyle = FontStyles.Italic;
            Anchor(direction.rectTransform, V(0, 1), V(1, 1), V(L.Unit * 6, -L.Unit * 11), V(-L.Unit * 4, -L.Unit * 7));

            // 본문만 raycastTarget 을 켠다 — 밑줄 구간을 클릭해야 하기 때문이다.
            var body = Text("Body", dlgRt, "", T.Body, P.Ink, TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 16f;
            body.raycastTarget = true;
            Anchor(body.rectTransform, V(0, 0), V(1, 1), V(L.Unit * 6, L.Unit * 5), V(-L.Unit * 4, -L.Unit * 12));

            var advance = Text("AdvanceIndicator", dlgRt, "▼", T.Caption, P.Brass, TextAlignmentOptions.Right);
            Anchor(advance.rectTransform, V(1, 0), V(1, 0), V(-L.Unit * 8, L.Unit * 2), V(-L.Unit * 3, L.Unit * 5));

            // ---- 위: 상대의 말풍선 (심문·조합에서만 쓰인다) ----
            // 무대 위 인물의 머리 옆. 밝은 판에 어두운 글자 — 아래 수첩과 반대 명암이라 누가 말하는지 색으로 갈린다.
            var bubbleRoot = UI("SpeechBubble", root);
            var bubbleRt = bubbleRoot.GetComponent<RectTransform>();
            Anchor(bubbleRt, V(0, 1), V(0, 1), V(L.Gutter + 560f, -L.Gutter - 208f), V(L.Gutter + 1208f, -L.Gutter - 8f));
            var bubbleGroup = bubbleRoot.AddComponent<CanvasGroup>();

            var bubbleBg = Img("Panel", bubbleRt, new Color32(0xE8, 0xE1, 0xD2, 0xFF));
            Stretch(bubbleBg.rectTransform);
            bubbleBg.sprite = _uiSprite; bubbleBg.type = Image.Type.Sliced;
            var bubbleTail = Img("Tail", bubbleRt, new Color32(0xE8, 0xE1, 0xD2, 0xFF));
            Anchor(bubbleTail.rectTransform, V(0, 0), V(0, 0), V(72f, -20f), V(112f, 20f));
            bubbleTail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            bubbleTail.raycastTarget = false;
            bubbleRoot.AddComponent<DialogueClickRelay>();

            var bubbleInk = new Color32(0x1E, 0x1B, 0x16, 0xFF);
            var bubbleSpeaker = Text("Speaker", bubbleRt, "", T.Caption, new Color32(0x6B, 0x5A, 0x2A, 0xFF), TextAlignmentOptions.Left);
            Spaced(bubbleSpeaker);
            Anchor(bubbleSpeaker.rectTransform, V(0, 1), V(0.5f, 1), V(L.Pad, -L.Unit * 6), V(0, -L.Unit * 2));

            var bubbleBody = Text("Body", bubbleRt, "", T.Body, bubbleInk, TextAlignmentOptions.TopLeft);
            bubbleBody.textWrappingMode = TextWrappingModes.Normal;
            bubbleBody.lineSpacing = 14f;
            bubbleBody.raycastTarget = true;
            Anchor(bubbleBody.rectTransform, V(0, 0), V(1, 1), V(L.Pad, L.Unit * 6), V(-L.Pad, -L.Unit * 7));

            var bubbleDirection = Text("StageDirection", bubbleRt, "", T.Caption, new Color32(0x6B, 0x66, 0x5C, 0xFF), TextAlignmentOptions.Left);
            bubbleDirection.fontStyle = FontStyles.Italic;
            Anchor(bubbleDirection.rectTransform, V(0, 0), V(1, 0), V(L.Pad, L.Unit * 2), V(-L.Unit * 10, L.Unit * 6));

            var bubbleAdvance = Text("AdvanceIndicator", bubbleRt, "▼", T.Caption, new Color32(0x6B, 0x5A, 0x2A, 0xFF), TextAlignmentOptions.Right);
            Anchor(bubbleAdvance.rectTransform, V(1, 0), V(1, 0), V(-L.Unit * 8, L.Unit * 2), V(-L.Unit * 3, L.Unit * 5));

            var presenter = dialoguePanel.gameObject.AddComponent<DialoguePresenter>();
            presenter.Set("root", dlgGroup)
                     .Set("speakerLabel", speaker)
                     .Set("bodyLabel", body)
                     .Set("stageDirectionLabel", direction)
                     .Set("advanceIndicator", advance.gameObject)
                     .Set("uiCamera", (Camera)null)
                     .Set("bubbleRoot", bubbleGroup)
                     .Set("bubbleSpeakerLabel", bubbleSpeaker)
                     .Set("bubbleBodyLabel", bubbleBody)
                     .Set("bubbleDirectionLabel", bubbleDirection)
                     .Set("bubbleAdvanceIndicator", bubbleAdvance.gameObject);
            bubbleRoot.GetComponent<DialogueClickRelay>().Set("target", presenter);

            // ---- 하단 상태 줄 ----
            var status = Text("StatusLabel", root, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(status.rectTransform, V(0, 0), V(0, 0), V(L.Gutter + 40f, L.Unit * 31), V(L.Gutter + 1064f, L.Unit * 34));

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

            // 고양이는 클릭 대상이 아니라 무대 위 인물이다. 그 아래로 비죽 나온 것만 핫스팟.
            var stageCat = Img("Cat_Placeholder", stage.rectTransform, new Color32(0x24, 0x29, 0x2F, 0xFF));
            Anchor(stageCat.rectTransform, V(0.56f, 0.50f), V(0.86f, 0.84f), Vector2.zero, Vector2.zero);
            stageCat.raycastTarget = false;
            var stageCatTag = Text("Label", stageCat.rectTransform, "검은 고양이\n(아트 대기)", T.Caption,
                new Color32(0x5A, 0x61, 0x69, 0xFF), TextAlignmentOptions.Center);
            Stretch(stageCatTag.rectTransform);

            // 핫스팟 여섯 개. 일반/특수의 겉모습은 완전히 같아야 한다.
            // 〈시스템 기획〉: 리본 · 커피잔 · 언듯 보이는 무언가(특수) / 신문 · 타자기 · 객실 정보(일반)
            var hotspotRects = new (string id, float x0, float y0, float x1, float y1)[]
            {
                ("OBJ_NEWSPAPER",  0.05f, 0.14f, 0.34f, 0.44f),
                ("OBJ_COFFEE",     0.38f, 0.26f, 0.53f, 0.52f),
                ("OBJ_TYPEWRITER", 0.58f, 0.10f, 0.88f, 0.40f),
                ("OBJ_RIBBON",     0.20f, 0.52f, 0.40f, 0.68f),
                ("OBJ_ROOMINFO",   0.90f, 0.56f, 0.99f, 0.86f),   // 벽의 객실 안내판
                ("OBJ_GLIMPSE",    0.50f, 0.46f, 0.60f, 0.56f)    // 고양이 아래로 비죽 나온 부분
            };

            var sceneObjects = new List<InteractableObject>();
            foreach (var h in hotspotRects)
            {
                var def = AssetDatabase.LoadAssetAtPath<InteractableDefinition>(
                    $"{DataRoot}/Objects/Obj_{h.id}.asset");
                if (def == null) continue;

                var spot = Img("Hotspot_" + h.id, stage.rectTransform, new Color32(0x2E, 0x34, 0x3B, 0xFF));
                Anchor(spot.rectTransform, V(h.x0, h.y0), V(h.x1, h.y1), Vector2.zero, Vector2.zero);
                spot.sprite = _uiSprite; spot.type = Image.Type.Sliced;

                var spotLabel = Text("Label", spot.rectTransform, def.HoverLabel, T.Caption,
                    new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Center);
                spotLabel.textWrappingMode = TextWrappingModes.Normal;
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
            // 주 영역(좌측 2/3) 전체. 선택지는 좌측, 인물은 가운데, 말풍선은 위, 수첩은 아래.

            var interrogateScreen = UI("Screen_Interrogation", root);
            var interRt = interrogateScreen.GetComponent<RectTransform>();
            Anchor(interRt, V(0, 0), V(1, 1), V(L.Gutter, L.Gutter), V(L.MainRight, -L.Gutter));

            var interBg = Img("Backdrop", interRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            Stretch(interBg.rectTransform);
            interBg.raycastTarget = false;
            var interTag = Text("Tag", interBg.rectTransform, "객실  —  게임플레이 배경 (아트 대기)",
                T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Bottom);
            Anchor(interTag.rectTransform, V(0.4f, 0), V(0.9f, 0), V(0, L.Unit * 31), V(0, L.Unit * 34));

            // 캐릭터 — 가운데, 수첩이 아래를 살짝 덮는다.
            var characterGo = UI("Character_Sepi", interRt);
            var charRt = characterGo.GetComponent<RectTransform>();
            Anchor(charRt, V(0.5f, 0), V(0.5f, 0), V(-256f, L.Unit * 12), V(256f, 784f));

            var charImg = characterGo.AddComponent<Image>();
            charImg.color = new Color32(0x3A, 0x40, 0x48, 0xFF);
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

            // 선택지 — 좌측. 스케치의 "Choice".
            var choicePanel = UI("ChoicePanel", interRt);
            var choiceRt = choicePanel.GetComponent<RectTransform>();
            Anchor(choiceRt, V(0, 0), V(0, 1), V(0, L.Unit * 36), V(L.ChoiceWidth, -L.Unit * 26));
            var choiceGroup = choicePanel.gameObject.AddComponent<CanvasGroup>();

            var choiceTitle = Text("Title", choiceRt, "Choice", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(choiceTitle);
            Anchor(choiceTitle.rectTransform, V(0, 1), V(1, 1), V(0, -24f), V(0, 0));

            var choiceList = UI("List", choiceRt);
            var listRt = choiceList.GetComponent<RectTransform>();
            Anchor(listRt, V(0, 0), V(1, 1), V(0, L.Unit * 9), V(0, -L.Unit * 5));
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

            // '심문을 마친다' — 선택지 목록의 맨 아래, 같은 어휘로.
            var endBtn = PlainButton("EndInterrogation", choiceRt, "심문을 마친다", T.Caption);
            Anchor(endBtn.Item1, V(0, 0), V(1, 0), V(0, 0), V(0, L.Unit * 6));

            // ======================================================= 지목 화면

            var accuseScreen = UI("Screen_Accusation", root);
            var accuseRt = accuseScreen.GetComponent<RectTransform>();
            Anchor(accuseRt, V(0, 0), V(1, 1), V(L.Gutter, L.Gutter), V(L.MainRight, -L.Gutter));

            var accusePanel = Img("Panel", accuseRt, P.Panel);
            Anchor(accusePanel.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-320f, -200f), V(320f, 200f));
            // 둥근 모서리 스프라이트를 이 크기로 늘리면 모서리 반경까지 커져 거대한 버튼처럼 보인다.
            accusePanel.color = new Color32(0x16, 0x19, 0x1D, 0xFF);

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

            // ======================================================= 조합 화면
            // 개선안 §II-A 조수 문답. 심문과 같은 구도 — 좌측 후보(아이콘), 무대에 팝업 #2, 우측 단서 목록.

            var deductScreen = UI("Screen_Deduction", root);
            var deductRt = deductScreen.GetComponent<RectTransform>();
            Anchor(deductRt, V(0, 0), V(1, 1), V(L.Gutter, L.Gutter), V(L.MainRight, -L.Gutter));

            // 무대 — 조수 자리 + 잉크 + 팝업 #2 (배경은 화면 전체)
            var deductStage = Img("Stage", deductRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            var dStageRt = deductStage.rectTransform;
            Stretch(dStageRt);
            deductStage.raycastTarget = false;

            var candPanel = UI("CandidatePanel", deductRt);
            var candRt = candPanel.GetComponent<RectTransform>();
            Anchor(candRt, V(0, 0), V(0, 1), V(0, L.Unit * 36), V(L.ChoiceWidth, -L.Unit * 26));
            var candGroup = candPanel.gameObject.AddComponent<CanvasGroup>();

            var candTitle = Text("Title", candRt, "Answer — 단서로 답한다", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(candTitle);
            Anchor(candTitle.rectTransform, V(0, 1), V(1, 1), V(0, -24f), V(0, 0));

            var candList = UI("List", candRt);
            var candListRt = candList.GetComponent<RectTransform>();
            Anchor(candListRt, V(0, 0), V(1, 1), V(0, L.Unit * 6), V(0, -L.Unit * 5));
            var cvlg = candList.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing = L.Unit;
            cvlg.childControlHeight = false;
            cvlg.childControlWidth = true;
            cvlg.childForceExpandHeight = false;
            cvlg.childForceExpandWidth = true;
            cvlg.childAlignment = TextAnchor.UpperLeft;

            var candHint = Text("Hint", candRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(candHint.rectTransform, V(0, 0), V(1, 0), V(0, 0), V(0, L.Unit * 4));

            var candidateView = candPanel.gameObject.AddComponent<DeductionCandidateView>();
            candidateView.Set("container", candListRt)
                         .Set("buttonPrefab", candidatePrefab)
                         .Set("root", candGroup)
                         .Set("hint", candHint);

            var dStageTag = Text("Tag", dStageRt, "객실  —  게임플레이 배경 (아트 대기)", T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Bottom);
            Anchor(dStageTag.rectTransform, V(0.4f, 0), V(0.9f, 0), V(0, L.Unit * 31), V(0, L.Unit * 34));

            var dSepi = Img("Sepi_Placeholder", dStageRt, new Color32(0x3A, 0x40, 0x48, 0xFF));
            Anchor(dSepi.rectTransform, V(0.5f, 0), V(0.5f, 0), V(-256f, L.Unit * 12), V(256f, 784f));
            dSepi.raycastTarget = false;
            var dSepiTag = Text("Label", dSepi.rectTransform, "세피\n(아트 대기 — 묻는 쪽)", T.Caption, P.InkSoft, TextAlignmentOptions.Center);
            Stretch(dSepiTag.rectTransform);

            // 조수의 질문 — 대사창이 닫힌 뒤에도 무대 위에 남는다.
            var questionText = Text("Question", dStageRt, "", T.Choice, P.Ink, TextAlignmentOptions.Center);
            questionText.textWrappingMode = TextWrappingModes.Normal;
            Anchor(questionText.rectTransform, V(0.3f, 1), V(1, 1), V(0, -L.Unit * 40), V(0, -L.Unit * 28));

            // 잉크 방울 — 상단 우측. 숫자가 아니라 방울이다.
            var inkCaption = Text("InkCaption", dStageRt, "잉크", T.Caption, P.InkSoft, TextAlignmentOptions.Right);
            Spaced(inkCaption);
            Anchor(inkCaption.rectTransform, V(0, 1), V(0, 1), V(L.ChoiceWidth + L.Gap, -L.Unit * 5), V(L.ChoiceWidth + L.Gap + 56f, -L.Unit * 2));
            var inkRow = UI("InkDrops", dStageRt);
            var inkRowRt = inkRow.GetComponent<RectTransform>();
            Anchor(inkRowRt, V(0, 1), V(0, 1), V(L.ChoiceWidth + L.Gap + 56f, -L.Unit * 6), V(L.ChoiceWidth + L.Gap + 200f, -L.Unit));
            var inkLayout = inkRow.AddComponent<HorizontalLayoutGroup>();
            inkLayout.spacing = L.Unit;
            inkLayout.childAlignment = TextAnchor.MiddleLeft;
            inkLayout.childControlWidth = false;
            inkLayout.childControlHeight = false;
            inkLayout.childForceExpandWidth = false;
            inkLayout.childForceExpandHeight = false;
            var dropTemplate = Img("Drop", inkRowRt, P.Brass);
            dropTemplate.rectTransform.sizeDelta = new Vector2(28f, 28f);
            dropTemplate.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            dropTemplate.raycastTarget = false;
            dropTemplate.gameObject.SetActive(false);
            var inkView = inkRow.AddComponent<InkDropsView>();
            inkView.Set("container", inkRowRt).Set("dropPrefab", dropTemplate);

            // 이벤트 팝업 #2 — 째려보는 주인공. 512² 자리.
            var glareRoot = UI("GlarePopup", dStageRt);
            var glareRt = glareRoot.GetComponent<RectTransform>();
            Anchor(glareRt, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-256f, -256f), V(256f, 256f));
            var glareGroup = glareRoot.AddComponent<CanvasGroup>();
            var glareFrame = Img("Frame", glareRt, new Color32(0x16, 0x19, 0x1D, 0xFF));
            Stretch(glareFrame.rectTransform);
            glareFrame.sprite = _uiSprite; glareFrame.type = Image.Type.Sliced;
            var glareImg = Img("Image", glareRt, new Color32(0x2E, 0x34, 0x3B, 0xFF));
            Anchor(glareImg.rectTransform, V(0, 0), V(1, 1), V(L.Unit, L.Unit * 7), V(-L.Unit, -L.Unit));
            glareImg.preserveAspect = true;
            glareImg.raycastTarget = false;
            var glarePlaceholder = Text("Placeholder", glareImg.rectTransform, "이벤트 팝업 #2\n째려보는 주인공\n(아트 대기)", T.Caption, P.InkSoft, TextAlignmentOptions.Center);
            Stretch(glarePlaceholder.rectTransform);
            var glareCaption = Text("Caption", glareRt, "누르면 닫힘", T.Caption, P.InkSoft, TextAlignmentOptions.Center);
            Anchor(glareCaption.rectTransform, V(0, 0), V(1, 0), V(0, L.Unit), V(0, L.Unit * 6));
            var glareView = glareRoot.AddComponent<GlarePopupView>();
            glareView.Set("root", glareRoot)
                     .Set("group", glareGroup)
                     .Set("image", glareImg)
                     .Set("placeholder", glarePlaceholder)
                     .Set("caption", glareCaption);
            glareRoot.SetActive(false);

            // 추론 카드가 생길 때 단서 패널이 잠깐 놋쇠 빛으로 물든다.
            var flashImg = Img("ConfirmFlash", clueRt, new Color(0.77f, 0.65f, 0.36f, 0.35f));
            Stretch(flashImg.rectTransform);
            flashImg.sprite = _uiSprite; flashImg.type = Image.Type.Sliced;
            flashImg.raycastTarget = false;
            var flashGroup = flashImg.gameObject.AddComponent<CanvasGroup>();
            flashGroup.alpha = 0f;
            flashGroup.blocksRaycasts = false;
            flashGroup.interactable = false;

            // ======================================================= 게임오버 화면
            // 컷씬 #GameOver 자리. 복귀 지점은 실패한 페이즈의 시작(조합 실패 → 조합 시작).

            var gameOverScreen = UI("Screen_GameOver", root);
            Stretch(gameOverScreen.GetComponent<RectTransform>());
            var goScrim = Img("Scrim", gameOverScreen.transform, new Color(0f, 0f, 0f, 0.82f));
            Stretch(goScrim.rectTransform);
            var goPanel = Img("Panel", gameOverScreen.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            Anchor(goPanel.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-360f, -220f), V(360f, 220f));
            var goTitle = Text("Title", goPanel.rectTransform, "게임 오버", T.Display, P.Alarm, TextAlignmentOptions.Center);
            Anchor(goTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad, -L.Unit * 14), V(-L.Pad, -L.Unit * 4));
            var goBody = Text("Body", goPanel.rectTransform, "", T.Body, P.Ink, TextAlignmentOptions.Center);
            goBody.textWrappingMode = TextWrappingModes.Normal;
            Anchor(goBody.rectTransform, V(0, 0), V(1, 1), V(L.Pad * 2, L.Unit * 14), V(-L.Pad * 2, -L.Unit * 15));
            var goRetry = PlainButton("Retry", goPanel.rectTransform, "다시 시도", T.Choice);
            Anchor(goRetry.Item1, V(0.5f, 0), V(0.5f, 0), V(-300f, L.Unit * 4), V(-20f, L.Unit * 4 + 56f));
            var goTitleBtn = PlainButton("ToTitle", goPanel.rectTransform, "타이틀로", T.Choice);
            Anchor(goTitleBtn.Item1, V(0.5f, 0), V(0.5f, 0), V(20f, L.Unit * 4), V(300f, L.Unit * 4 + 56f));

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

            // ================================================== 용의자 선택 화면
            // 심문과 같은 구획을 쓴다. 좌측은 대화 구간처럼 비워 두고,
            // 가운데 무대에 두 사람이 나란히 앉아 있다.

            var selectScreen = UI("Screen_SuspectSelect", root);
            var selectRt = selectScreen.GetComponent<RectTransform>();
            // 단서 패널 앞에서 멈춘다. 배경이 화면 끝까지 가면 목록을 덮어 버린다.
            Anchor(selectRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(L.ClueLeft - L.Gap, L.BandTop));

            var selectBg = Img("Backdrop", selectRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            Stretch(selectBg.rectTransform);
            selectBg.raycastTarget = false;

            var selectTag = Text("Tag", selectBg.rectTransform, "객실  —  게임플레이 배경 (아트 대기)",
                T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Center);
            Anchor(selectTag.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));

            var selectPrompt = Text("Prompt", selectRt, "", T.Body, P.Brass, TextAlignmentOptions.Center);
            Anchor(selectPrompt.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 14), V(0, -L.Unit * 7));

            // 두 사람. 자리는 심문 화면의 인물 위치와 어긋나지 않게 잡는다.
            var cardSlots = new (string name, float x0, float x1)[]
            {
                ("Card_0", 0.30f, 0.50f),
                ("Card_1", 0.56f, 0.72f),
            };

            var suspectCards = new List<SuspectCard>();
            foreach (var slot in cardSlots)
            {
                var card = UI(slot.name, selectRt);
                var cardRt = card.GetComponent<RectTransform>();
                Anchor(cardRt, V(slot.x0, 0.06f), V(slot.x1, 0.86f), Vector2.zero, Vector2.zero);

                var portrait = card.AddComponent<Image>();
                portrait.color = new Color32(0x33, 0x39, 0x41, 0xFF);
                var cardButton = card.AddComponent<Button>();
                cardButton.targetGraphic = portrait;
                var cardColors = cardButton.colors;
                cardColors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                cardColors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
                cardColors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 1f);
                cardButton.colors = cardColors;

                var cardGroup = card.AddComponent<CanvasGroup>();
                var cardOutline = card.AddComponent<Outline>();
                cardOutline.effectColor = P.Brass;
                cardOutline.effectDistance = new Vector2(3f, -3f);
                cardOutline.enabled = false;

                var cardName = Text("Name", cardRt, "", T.Choice, P.Ink, TextAlignmentOptions.Bottom);
                Anchor(cardName.rectTransform, V(0, 0), V(1, 0), V(0, L.Unit * 2), V(0, L.Unit * 8));

                var cardState = Text("State", cardRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.Center);
                cardState.textWrappingMode = TextWrappingModes.Normal;
                Anchor(cardState.rectTransform, V(0, 0.42f), V(1, 0.58f), Vector2.zero, Vector2.zero);

                var art = Text("Art", cardRt, "(아트 대기)", T.Caption,
                    new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Top);
                Anchor(art.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));

                var cardView = card.AddComponent<SuspectCard>();
                cardView.Set("button", cardButton)
                        .Set("portrait", portrait)
                        .Set("nameLabel", cardName)
                        .Set("stateLabel", cardState)
                        .Set("highlight", cardOutline)
                        .Set("group", cardGroup);
                suspectCards.Add(cardView);
            }

            // ================================================== 클로즈업 팝업
            // 수집 페이즈에서 오브젝트를 누르면 화면 중앙에 뜨는 이미지. 대사창보다 아래에 둔다 —
            // 설명 대사가 팝업 위로 읽혀야 하기 때문이다.
            var closeUpRoot = UI("CloseUpPopup", root);
            Stretch(closeUpRoot.GetComponent<RectTransform>());

            var closeUpScrim = Img("Scrim", closeUpRoot.transform, new Color(0f, 0f, 0f, 0.55f));
            Stretch(closeUpScrim.rectTransform);

            var closeUpPanel = Img("Panel", closeUpRoot.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            Anchor(closeUpPanel.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-344f, -232f), V(344f, 296f));

            var closeUpImg = Img("Image", closeUpPanel.rectTransform, new Color32(0x2E, 0x34, 0x3B, 0xFF));
            Anchor(closeUpImg.rectTransform, V(0, 0), V(1, 1), V(L.Pad, L.Unit * 9), V(-L.Pad, -L.Pad));
            closeUpImg.preserveAspect = true;
            closeUpImg.raycastTarget = false;

            var closeUpPlaceholder = Text("Placeholder", closeUpImg.rectTransform, "(이미지 대기)", T.Caption,
                new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Center);
            closeUpPlaceholder.textWrappingMode = TextWrappingModes.Normal;
            Stretch(closeUpPlaceholder.rectTransform);

            var closeUpCaption = Text("Caption", closeUpPanel.rectTransform, "", T.Choice, P.Ink, TextAlignmentOptions.Center);
            Anchor(closeUpCaption.rectTransform, V(0, 0), V(1, 0), V(L.Pad, L.Unit * 3), V(-L.Pad, L.Unit * 8));

            var closeUpHint = Text("Hint", closeUpPanel.rectTransform, "누르면 닫힘", T.Caption, P.InkSoft, TextAlignmentOptions.Right);
            Anchor(closeUpHint.rectTransform, V(0, 0), V(1, 0), V(L.Pad, L.Unit * 0.5f), V(-L.Pad, L.Unit * 3));

            var closeUpView = closeUpRoot.AddComponent<CloseUpPopup>();
            closeUpView.Set("root", closeUpRoot)
                       .Set("image", closeUpImg)
                       .Set("caption", closeUpCaption)
                       .Set("placeholder", closeUpPlaceholder)
                       .Set("continueHint", closeUpHint.gameObject);

            // 대사창은 새로 만든 전면 배경들보다 위에 있어야 한다.
            dialoguePanel.transform.SetAsLastSibling();
            bubbleRoot.transform.SetAsLastSibling();
            topBar.transform.SetAsLastSibling();
            titleLabel.transform.SetAsLastSibling();

            // ================================================== 대화 도구
            // 로그와 창 숨기기. 평범한 대화 구간에서만 나타난다.
            var toolbar = UI("DialogueToolbar", root);
            var toolbarRt = toolbar.GetComponent<RectTransform>();
            Anchor(toolbarRt, V(0, 0), V(0, 0),
                V(L.Gutter + 1064f - 368f, L.Unit * 31), V(L.Gutter + 1064f, L.Unit * 36));

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

            var logScroll = BuildScroll("Scroll", logRt, V(L.Pad * 2, L.Unit * 11), V(-L.Pad * 2, -L.Unit * 13));
            var logBody = Text("Body", logScroll.content, "", T.Choice, P.Ink, TextAlignmentOptions.TopLeft);
            logBody.textWrappingMode = TextWrappingModes.Normal;
            logBody.lineSpacing = 12f;

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

            // ================================================== 단서 열람창
            // 〈시스템 기획〉: 기존 화면이 반투명하게 어두워지며, 화면 중앙에 단서의 이미지 및
            // 우측에 스크롤 가능한 텍스트 툴팁 창이 팝업된다. 강조 구간을 누르면 단서 오브젝트가 된다.
            var readingRoot = UI("ClueReadingWindow", root);
            Stretch(readingRoot.GetComponent<RectTransform>());

            var readingScrim = Img("Scrim", readingRoot.transform, new Color(0f, 0f, 0f, 0.72f));
            Stretch(readingScrim.rectTransform);

            var readingPanel = Img("Panel", readingRoot.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            var readingRt = readingPanel.rectTransform;
            Anchor(readingRt, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-640f, -360f), V(640f, 360f));

            var readingImg = Img("Image", readingRt, new Color32(0x2E, 0x34, 0x3B, 0xFF));
            Anchor(readingImg.rectTransform, V(0, 0), V(0.5f, 1), V(L.Pad * 2, L.Unit * 12), V(-L.Unit, -L.Pad * 2));
            readingImg.preserveAspect = true;
            readingImg.raycastTarget = false;

            var readingPlaceholder = Text("Placeholder", readingImg.rectTransform, "(이미지 대기)", T.Caption,
                new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Center);
            readingPlaceholder.textWrappingMode = TextWrappingModes.Normal;
            Stretch(readingPlaceholder.rectTransform);

            var readingTitle = Text("Title", readingRt, "", T.Display, P.Ink, TextAlignmentOptions.TopLeft);
            Anchor(readingTitle.rectTransform, V(0.5f, 1), V(1, 1), V(L.Unit, -L.Unit * 10), V(-L.Pad * 2, -L.Pad * 2));

            var readingRule = Img("Rule", readingRt, P.Rule);
            Anchor(readingRule.rectTransform, V(0.5f, 1), V(1, 1), V(L.Unit, -L.Unit * 11), V(-L.Pad * 2, -L.Unit * 11 + 2f));
            readingRule.raycastTarget = false;

            var readingScroll = BuildScroll("Scroll", readingRt, V(-1f, L.Unit * 12), V(-L.Pad * 2, -L.Unit * 13));
            readingScroll.scroll.GetComponent<RectTransform>().anchorMin = V(0.5f, 0);
            readingScroll.scroll.GetComponent<RectTransform>().offsetMin = V(L.Unit, L.Unit * 12);

            var readingBody = Text("Body", readingScroll.content, "", T.Body, P.Ink, TextAlignmentOptions.TopLeft);
            readingBody.textWrappingMode = TextWrappingModes.Normal;
            readingBody.lineSpacing = 14f;
            readingBody.raycastTarget = true;   // 강조 구간을 눌러야 한다
            readingBody.gameObject.AddComponent<TmpLinkClickRelay>();

            var promoteHint = Text("PromoteHint", readingRt, "", T.Caption, P.Brass, TextAlignmentOptions.BottomLeft);
            Anchor(promoteHint.rectTransform, V(0.5f, 0), V(1, 0), V(L.Unit, L.Unit * 5), V(-L.Pad * 2 - 160f, L.Unit * 10));

            var readingClose = PlainButton("Close", readingRt, "닫기", T.Choice);
            Anchor(readingClose.Item1, V(1, 0), V(1, 0), V(-L.Pad * 2 - 144f, L.Unit * 4), V(-L.Pad * 2, L.Unit * 4 + 56f));

            clueTrayView.Set("tooltipRoot", readingRoot)
                        .Set("tooltipImage", readingImg)
                        .Set("tooltipImagePlaceholder", readingPlaceholder)
                        .Set("tooltipTitle", readingTitle)
                        .Set("tooltipBody", readingBody)
                        .Set("tooltipScroll", readingScroll.scroll)
                        .Set("promoteHint", promoteHint)
                        .Set("closeButton", readingClose.Item2);

            // ================================================== 튜토리얼 팝업
            var popupRoot = UI("TutorialPopup", root);
            Stretch(popupRoot.GetComponent<RectTransform>());

            var popupScrim = Img("Scrim", popupRoot.transform, new Color(0f, 0f, 0f, 0.7f));
            Stretch(popupScrim.rectTransform);

            var popupPanel = Img("Panel", popupRoot.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            var popupRt = popupPanel.rectTransform;
            Anchor(popupRt, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-480f, -320f), V(480f, 320f));

            var popupTitle = Text("Title", popupRt, "", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(popupTitle);
            Anchor(popupTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 9), V(-L.Pad * 2, -L.Unit * 4));

            var popupRule = Img("Rule", popupRt, P.Rule);
            Anchor(popupRule.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 11), V(-L.Pad * 2, -L.Unit * 11 + 2f));
            popupRule.raycastTarget = false;

            var popupScroll = BuildScroll("Scroll", popupRt, V(L.Pad * 2, L.Unit * 12), V(-L.Pad * 2, -L.Unit * 13));
            var popupBody = Text("Body", popupScroll.content, "", T.Body, P.Ink, TextAlignmentOptions.TopLeft);
            popupBody.textWrappingMode = TextWrappingModes.Normal;
            popupBody.lineSpacing = 14f;

            var popupPage = Text("Page", popupRt, "", T.Caption, P.InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(popupPage.rectTransform, V(0, 0), V(0.5f, 0), V(L.Pad * 2, L.Unit * 4), V(0, L.Unit * 10));

            var popupNext = PlainButton("Next", popupRt, "다음", T.Choice);
            Anchor(popupNext.Item1, V(1, 0), V(1, 0), V(-L.Pad * 2 - 160f, L.Unit * 4), V(-L.Pad * 2, L.Unit * 4 + 56f));

            var popupView = popupRoot.AddComponent<TutorialPopupView>();
            popupView.Set("root", popupRoot)
                     .Set("titleLabel", popupTitle)
                     .Set("bodyLabel", popupBody)
                     .Set("pageLabel", popupPage)
                     .Set("scroll", popupScroll.scroll)
                     .Set("nextButton", popupNext.Item2)
                     .Set("nextLabel", popupNext.Item1.Find("Label").GetComponent<TMP_Text>());

            // ================================================== 심문 시작 제목
            var titleRoot = UI("TitleCard", root);
            Stretch(titleRoot.GetComponent<RectTransform>());
            var titleGroup = titleRoot.AddComponent<CanvasGroup>();

            var titleBand = Img("Band", titleRoot.transform, new Color(0f, 0f, 0f, 0.78f));
            Anchor(titleBand.rectTransform, V(0, 0.5f), V(1, 0.5f), V(0, -96f), V(0, 96f));

            var titleText = Text("Label", titleBand.rectTransform, "", T.Display + 12f, P.Brass, TextAlignmentOptions.Center);
            titleText.characterSpacing = 6f;
            Stretch(titleText.rectTransform);

            var titleCard = titleRoot.AddComponent<TitleCardView>();
            titleCard.Set("group", titleGroup)
                     .Set("card", titleBand.rectTransform)
                     .Set("label", titleText);

            // ---- 설정 / 일시 정지 (막 아래, 다른 팝업 위) ----
            var systems = new GameObject("Systems");
            ItIsYou.EditorTools.UiKit.Prepare();
            var settingsMenu = ItIsYou.EditorTools.SettingsUiBuilder.BuildSettings(root, systems);
            var pauseMenu = ItIsYou.EditorTools.SettingsUiBuilder.BuildPause(root, systems, settingsMenu);
            settingsMenu.Set("openButton", sideSettingsBtn.Item2);

            // ---- 화면 전환 막 ----
            var faderImg = Img("ScreenFader", root, Color.black);
            Stretch(faderImg.rectTransform);
            var faderGroup = faderImg.gameObject.AddComponent<CanvasGroup>();
            // 알파 1로 저장되면 플레이 전 게임 뷰가 통째로 까맣게 보인다.
            faderGroup.alpha = 0f;
            faderGroup.blocksRaycasts = false;
            var fader = faderImg.gameObject.AddComponent<ScreenFader>();
            fader.Set("group", faderGroup)
                 .Set("fadeSeconds", 0.4f)
                 .Set("holdSeconds", 0.25f);

            // ========================================================== 시스템

            var cursor = systems.AddComponent<CursorController>();
            clickTarget.Set("hitArea", charImg).Set("cursor", cursor);

            var investigation = systems.AddComponent<InvestigationController>();
            investigation.Set("sceneObjects", sceneObjects)
                         .Set("dialogue", presenter)
                         .Set("cursor", cursor)
                         .Set("clueTray", clueTrayView)
                         .Set("closeUp", closeUpView)
                         .Set("advanceButton", goInterrogate.Item2)
                         .Set("progressLabel", progressLabel);

            var selectController = systems.AddComponent<SuspectSelectController>();
            selectController.Set("dialogue", presenter)
                            .Set("cards", suspectCards)
                            .Set("promptLabel", selectPrompt);

            // ---- 무반응 연출 사운드 (클립은 아직 없음 — ClockTick 의 AudioSource 에 초침 루프를 끼우면 된다) ----
            var audioRoot = new GameObject("Audio");
            audioRoot.transform.SetParent(systems.transform, false);
            var clockGo = new GameObject("ClockTick", typeof(AudioSource), typeof(ItIsYou.Core.SoundChannel));
            clockGo.transform.SetParent(audioRoot.transform, false);
            var clockSrc = clockGo.GetComponent<AudioSource>();
            clockSrc.playOnAwake = false; clockSrc.loop = true; clockSrc.volume = 0f;
            clockGo.GetComponent<ItIsYou.Core.SoundChannel>().Set("kind", ItIsYou.Core.SoundChannelKind.Sfx).Set("baseVolume", 0f).Set("duckable", false);
            var bgmGo = new GameObject("Bgm", typeof(AudioSource), typeof(ItIsYou.Core.SoundChannel));
            bgmGo.transform.SetParent(audioRoot.transform, false);
            var bgmSrc = bgmGo.GetComponent<AudioSource>();
            bgmSrc.playOnAwake = true; bgmSrc.loop = true; bgmSrc.volume = 1f;
            bgmGo.GetComponent<ItIsYou.Core.SoundChannel>().Set("kind", ItIsYou.Core.SoundChannelKind.Bgm).Set("baseVolume", 1f).Set("duckable", true);
            var tension = systems.AddComponent<IdleTensionAudio>();
            tension.Set("clockTick", clockSrc).Set("startSeconds", 7f).Set("clockMaxVolume", 0.85f).Set("duckFloor", 0.25f);

            var interrogation = systems.AddComponent<InterrogationController>();
            interrogation.Set("database", db)
                         .Set("dialogue", presenter)
                         .Set("choiceList", choiceListView)
                         .Set("cursor", cursor)
                         .Set("titleCard", titleCard)
                         .Set("tutorialPopup", popupView)
                         .Set("pauseMenu", pauseMenu)
                         .Set("tension", tension);

            var deductionCtl = systems.AddComponent<DeductionController>();
            deductionCtl.Set("dialogue", presenter)
                        .Set("candidates", candidateView)
                        .Set("ink", inkView)
                        .Set("glarePopup", glareView)
                        .Set("tutorialPopup", popupView)
                        .Set("cursor", cursor)
                        .Set("confirmFlash", flashGroup)
                        .Set("questionLabel", questionText);

            var gameOverView = systems.AddComponent<GameOverView>();
            gameOverView.Set("root", gameOverScreen)
                        .Set("title", goTitle)
                        .Set("body", goBody)
                        .Set("retryButton", goRetry.Item2)
                        .Set("retryLabel", goRetry.Item1.Find("Label").GetComponent<TMP_Text>())
                        .Set("titleButton", goTitleBtn.Item2);

            var flow = systems.AddComponent<GameFlowController>();
            flow.Set("database", db)
                .Set("chapter", chapter)
                .Set("fader", fader)
                .Set("investigation", investigation)
                .Set("suspectSelect", selectController)
                .Set("interrogation", interrogation)
                .Set("deduction", deductionCtl)
                .Set("gameOverView", gameOverView)
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
                  .Set("groups", BuildScreenGroups(talkScreen, cutsceneScreen, selectScreen,
                                                    collectScreen, interrogateScreen, deductScreen, accuseScreen,
                                                    gameOverScreen, cluePanel.gameObject))
                  .Set("alwaysVisible", new List<GameObject> { dialoguePanel.gameObject });

            var logView = systems.AddComponent<DialogueLogView>();
            logView.Set("dialogue", presenter)
                   .Set("root", logRoot)
                   .Set("body", logBody)
                   .Set("scroll", logScroll.scroll)
                   .Set("closeButton", logClose.Item2)
                   .Set("emptyLabel", logEmpty);

            var toolbarView = systems.AddComponent<DialogueToolbar>();
            toolbarView.Set("flow", flow)
                       .Set("dialogue", presenter)
                       .Set("log", logView)
                       .Set("root", toolbar)
                       .Set("logButton", logBtn.Item2)
                       .Set("sideLogButton", sideLogBtn.Item2)
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
               .Set("clockLabel", clockLabel)
               .Set("dateLabel", dateLabel)
               .Set("locationLabel", locationLabel)
               .Set("endInterrogationButton", endBtn.Item2);

            talkScreen.SetActive(false);
            cutsceneScreen.SetActive(false);
            selectScreen.SetActive(false);
            collectScreen.SetActive(false);
            interrogateScreen.SetActive(false);
            accuseScreen.SetActive(false);
            logRoot.SetActive(false);
            catcher.Item1.gameObject.SetActive(false);
            toolbar.SetActive(false);
            closeUpRoot.SetActive(false);
            readingRoot.SetActive(false);
            popupRoot.SetActive(false);
            titleRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 튜토리얼 챕터 씬 생성 완료 — {ScenePath}\n" +
                      "Play → 인트로 대사 → 단서 수집(핫스팟 6 · 클로즈업 · 열람창) → 심문(제목 · 튜토리얼 팝업) → 지목");
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

        private static object BuildScreenGroups(GameObject talk, GameObject cutscene, GameObject select,
            GameObject collect, GameObject interrogate, GameObject deduct, GameObject accuse, GameObject gameOver, GameObject cluePanel)
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
                    root = select,
                    phases = new List<GamePhase> { GamePhase.SuspectSelect }
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
                    root = deduct,
                    phases = new List<GamePhase> { GamePhase.Deduction }
                },
                new PhaseScreenBinder.ScreenGroup {
                    root = accuse,
                    phases = new List<GamePhase> { GamePhase.Accusation }
                },
                new PhaseScreenBinder.ScreenGroup {
                    root = gameOver,
                    phases = new List<GamePhase> { GamePhase.GameOver }
                },
                // 단서 목록은 단서가 생기는 구간부터 보인다.
                // 사건이 터지기도 전에 빈 목록을 띄워 두면 무엇을 하는 화면인지 흐려진다.
                new PhaseScreenBinder.ScreenGroup {
                    root = cluePanel,
                    phases = new List<GamePhase> {
                        GamePhase.ClueCollection, GamePhase.SuspectSelect, GamePhase.Interrogation1,
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
            rt.sizeDelta = new Vector2(0f, 96f);   // 12u

            var bg = go.AddComponent<Image>();
            bg.color = P.Surface;
            bg.sprite = _uiSprite; bg.type = Image.Type.Sliced;
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
            rt.sizeDelta = new Vector2(128f, 128f);

            var bg = go.AddComponent<Image>();
            bg.color = P.Surface;
            bg.sprite = _uiSprite; bg.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            // 승격된 칸의 금색 테두리. 바깥으로 3px 나온 놋쇠 판 위에 바탕색 판을 겹쳐 테두리만 남긴다.
            var frame = Img("PromotedFrame", rt, P.Brass);
            Anchor(frame.rectTransform, V(0, 0), V(1, 1), V(-3f, -3f), V(3f, 3f));
            frame.sprite = _uiSprite; frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var frameInner = Img("Inner", frame.rectTransform, P.Surface);
            Anchor(frameInner.rectTransform, V(0, 0), V(1, 1), V(3f, 3f), V(-3f, -3f));
            frameInner.sprite = _uiSprite; frameInner.type = Image.Type.Sliced;
            frameInner.raycastTarget = false;

            var icon = Img("Icon", rt, new Color(1f, 1f, 1f, 0f));
            Anchor(icon.rectTransform, V(0, 0), V(1, 1), V(L.Unit, L.Unit), V(-L.Unit, -L.Unit));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 이름표 — 배경과 글자는 반드시 별개 오브젝트여야 한다(Graphic 은 하나만 허용).
            var hoverBg = Img("HoverLabel", rt, new Color(0f, 0f, 0f, 0.88f));
            Anchor(hoverBg.rectTransform, V(0, 0), V(1, 0), V(-L.Unit * 3, -L.Unit * 4), V(L.Unit * 3, 0));
            hoverBg.raycastTarget = false;
            hoverBg.sprite = _uiSprite; hoverBg.type = Image.Type.Sliced;

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
                .Set("usableBadge", usable.gameObject)
                .Set("promotedFrame", frame.gameObject);
            frame.gameObject.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/ClueSlot.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ClueSlot>();
        }

        private static DeductionCandidateButton BuildDeductionCandidatePrefab()
        {
            var go = UI("DeductionCandidate", null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 88f);   // 11u

            var bg = go.AddComponent<Image>();
            bg.color = P.Surface;
            bg.sprite = _uiSprite; bg.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            var group = go.AddComponent<CanvasGroup>();

            var iconBg = Img("IconBg", rt, P.PanelSunk);
            Anchor(iconBg.rectTransform, V(0, 0.5f), V(0, 0.5f), V(L.Unit * 1.5f, -32f), V(L.Unit * 1.5f + 64f, 32f));
            iconBg.sprite = _uiSprite; iconBg.type = Image.Type.Sliced;
            iconBg.raycastTarget = false;
            var icon = Img("Icon", iconBg.rectTransform, Color.white);
            Anchor(icon.rectTransform, V(0, 0), V(1, 1), V(4f, 4f), V(-4f, -4f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconPlaceholder = Text("Placeholder", iconBg.rectTransform, "?", T.Choice, P.InkSoft, TextAlignmentOptions.Center);
            Stretch(iconPlaceholder.rectTransform);

            var label = Text("Label", rt, "???", T.Choice, P.Ink, TextAlignmentOptions.Left);
            Anchor(label.rectTransform, V(0, 0), V(1, 1), V(L.Unit * 1.5f + 64f + L.Unit * 2, 0), V(-L.Unit * 2, 0));

            var strip = Img("Strip", rt, P.Brass);
            Anchor(strip.rectTransform, V(0, 0), V(0, 1), Vector2.zero, V(L.Unit * 0.5f, 0));
            strip.raycastTarget = false;

            var cand = go.AddComponent<DeductionCandidateButton>();
            cand.Set("button", button)
                .Set("icon", icon)
                .Set("iconPlaceholder", iconPlaceholder)
                .Set("label", label)
                .Set("group", group);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/DeductionCandidate.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<DeductionCandidateButton>();
        }

        /// <summary>정황/실마리 목록의 한 줄 — 아이콘 + 이름이 항상 보인다.</summary>
        private static ClueSlot BuildLeadSlotPrefab()
        {
            var go = UI("LeadSlot", null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 64f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;
            le.minHeight = 64f;

            var bg = go.AddComponent<Image>();
            bg.color = P.Surface;
            bg.sprite = _uiSprite; bg.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var frame = Img("PromotedFrame", rt, P.Brass);
            Anchor(frame.rectTransform, V(0, 0), V(1, 1), V(-3f, -3f), V(3f, 3f));
            frame.sprite = _uiSprite; frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var frameInner = Img("Inner", frame.rectTransform, P.Surface);
            Anchor(frameInner.rectTransform, V(0, 0), V(1, 1), V(3f, 3f), V(-3f, -3f));
            frameInner.sprite = _uiSprite; frameInner.type = Image.Type.Sliced;
            frameInner.raycastTarget = false;

            var iconBg = Img("IconBg", rt, P.PanelSunk);
            Anchor(iconBg.rectTransform, V(0, 0.5f), V(0, 0.5f), V(L.Unit, -24f), V(L.Unit + 48f, 24f));
            iconBg.sprite = _uiSprite; iconBg.type = Image.Type.Sliced;
            iconBg.raycastTarget = false;
            var icon = Img("Icon", iconBg.rectTransform, new Color(1f, 1f, 1f, 0f));
            Anchor(icon.rectTransform, V(0, 0), V(1, 1), V(4f, 4f), V(-4f, -4f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var labelRoot = UI("LabelRoot", rt);
            var labelRt = labelRoot.GetComponent<RectTransform>();
            Anchor(labelRt, V(0, 0), V(1, 1), V(L.Unit * 2 + 48f, 0), V(-L.Unit * 5, 0));
            var label = Text("Text", labelRt, "???", T.Choice, P.Ink, TextAlignmentOptions.Left);
            Stretch(label.rectTransform);

            var unread = Img("UnreadBadge", rt, P.Brass);
            Anchor(unread.rectTransform, V(1, 0.5f), V(1, 0.5f), V(-L.Unit * 3, -6f), V(-L.Unit * 3 + 12f, 6f));
            unread.raycastTarget = false;

            var usable = Text("UsableBadge", rt, "!", T.Choice, P.Good, TextAlignmentOptions.Center);
            Anchor(usable.rectTransform, V(1, 0.5f), V(1, 0.5f), V(-L.Unit * 5, -14f), V(-L.Unit * 2, 14f));

            var slot = go.AddComponent<ClueSlot>();
            slot.Set("button", button)
                .Set("icon", icon)
                .Set("hoverLabel", label)
                .Set("hoverRoot", labelRoot)
                .Set("unreadBadge", unread.gameObject)
                .Set("usableBadge", usable.gameObject)
                .Set("promotedFrame", frame.gameObject)
                .Set("alwaysShowLabel", true);
            frame.gameObject.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/LeadSlot.prefab");
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
            img.sprite = _uiSprite; img.type = Image.Type.Sliced;
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
            img.sprite = _uiSprite; img.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var text = Text("Label", rt, label, size, P.Ink, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return (rt, button);
        }

        /// <summary>세로 스크롤 영역. 내용은 위에서 아래로 쌓이고 높이에 맞춰 늘어난다.</summary>
        private static (ScrollRect scroll, RectTransform content) BuildScroll(string name, Transform parent,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var scrollGo = UI(name, parent);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Anchor(scrollRt, V(0, 0), V(1, 1), offsetMin, offsetMax);
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

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            return (scrollRect, contentRt);
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
