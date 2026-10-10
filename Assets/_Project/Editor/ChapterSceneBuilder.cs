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
            _art = LoadArtSet();

            var chapter = TutorialDataBuilder.Build();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataRoot + "/GameDatabase.asset");

            Ed.EnsureFolder(SceneFolder);
            Ed.EnsureFolder(PrefabFolder);

            var choicePrefab = BuildChoiceButtonPrefab();
            var cluePrefab = BuildClueSlotPrefab();
            var leadPrefab = BuildLeadSlotPrefab();
            var plainButtonPrefab = BuildPlainButtonPrefab();
            var hypothesisPrefab = BuildHypothesisSlotPrefab();

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

            // 3열 격자, 2행이 보인다. 128 칸 + 24 간격 = 432, 좌우 여백 48 로 640 안에 든다.
            var evidenceScroll = BuildScroll("EvidenceScroll", clueRt, V(L.Pad * 2, -L.Unit * 49), V(-L.Pad * 2, -L.Unit * 13));
            evidenceScroll.scroll.GetComponent<RectTransform>().anchorMin = V(0, 1);
            evidenceScroll.scroll.GetComponent<RectTransform>().offsetMin = V(L.Pad * 2, -L.Unit * 49);
            Object.DestroyImmediate(evidenceScroll.content.GetComponent<VerticalLayoutGroup>());
            var glg = evidenceScroll.content.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(128f, 128f);
            glg.spacing = new Vector2(L.Gap, L.Gap);
            glg.childAlignment = TextAnchor.UpperLeft;
            var gridRt = evidenceScroll.content;

            var sideDivider = Img("Divider", clueRt, P.Rule);
            Anchor(sideDivider.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 50), V(-L.Pad * 2, -L.Unit * 50 + 2f));
            sideDivider.raycastTarget = false;

            var leadsTitle = Text("LeadsTitle", clueRt, "Leads", 28f, P.Ink, TextAlignmentOptions.Left);
            leadsTitle.fontStyle = FontStyles.Bold;
            Anchor(leadsTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 56), V(-L.Pad * 2, -L.Unit * 51));
            var leadsSub = Text("LeadsSub", clueRt, "정황 / 실마리", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Anchor(leadsSub.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2 + 96f, -L.Unit * 55.5f), V(-L.Pad * 2, -L.Unit * 52));

            var leadsScroll = BuildScroll("LeadsScroll", clueRt, V(L.Pad * 2, L.Unit * 55), V(-L.Pad * 2, -L.Unit * 57));
            leadsScroll.content.GetComponent<VerticalLayoutGroup>().spacing = L.Unit;
            var leadsRt = leadsScroll.content;

            // ---- 가설 (단서 조합의 결과). 조합 페이즈가 끝나면 잠긴다 — 열람만 된다. ----
            var sideDivider2 = Img("Divider2", clueRt, P.Rule);
            Anchor(sideDivider2.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 81), V(-L.Pad * 2, -L.Unit * 81 + 2f));
            sideDivider2.raycastTarget = false;

            var hypTitle = Text("HypothesesTitle", clueRt, "Hypotheses", 28f, P.Ink, TextAlignmentOptions.Left);
            hypTitle.fontStyle = FontStyles.Bold;
            Anchor(hypTitle.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2, -L.Unit * 87), V(-L.Pad * 2, -L.Unit * 82));
            var hypSub = Text("HypothesesSub", clueRt, "가설", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Anchor(hypSub.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2 + 176f, -L.Unit * 86.5f), V(-L.Pad * 2, -L.Unit * 83));

            var hypScroll = BuildScroll("HypothesesScroll", clueRt, V(L.Pad * 2, L.Unit * 25), V(-L.Pad * 2, -L.Unit * 88));
            hypScroll.content.GetComponent<VerticalLayoutGroup>().spacing = L.Unit;
            var hypRt = hypScroll.content;
            var hypEmpty = Text("HypothesesEmpty", clueRt, "아직 세운 가설이 없다", T.Caption, P.InkSoft, TextAlignmentOptions.TopLeft);
            Anchor(hypEmpty.rectTransform, V(0, 1), V(1, 1), V(L.Pad * 2 + L.Unit, -L.Unit * 92), V(-L.Pad * 2, -L.Unit * 88));

            var clueHint = Text("Hint", clueRt, "칸을 누르면 열람", T.Caption, P.InkSoft, TextAlignmentOptions.Left);
            Anchor(clueHint.rectTransform, V(0, 0), V(1, 0), V(L.Pad * 2, L.Unit * 15), V(-L.Pad * 2, L.Unit * 18));

            // 기능 아이콘 — 기록(돋보기 자리) · 설정(톱니 자리). 아이콘 소스가 오면 글자를 그림으로 바꾼다.
            var sideLogBtn = PlainButton("LogButton", clueRt, "기록", T.Caption);
            Anchor(sideLogBtn.Item1, V(0, 0), V(0, 0), V(L.Pad * 2, L.Gutter), V(L.Pad * 2 + 72f, L.Gutter + 72f));
            var sideSettingsBtn = PlainButton("SettingsButton", clueRt, "설정", T.Caption);
            Anchor(sideSettingsBtn.Item1, V(1, 0), V(1, 0), V(-L.Pad * 2 - 72f, L.Gutter), V(-L.Pad * 2, L.Gutter + 72f));
            ArtIcon(sideLogBtn, _art.iconRecord);
            ArtIcon(sideSettingsBtn, _art.iconSettings);

            var clueTrayView = cluePanel.gameObject.AddComponent<ClueTrayView>();
            clueTrayView.Set("container", gridRt)
                        .Set("slotPrefab", cluePrefab)
                        .Set("leadsContainer", leadsRt)
                        .Set("leadPrefab", leadPrefab)
                        .Set("hypothesesContainer", hypRt)
                        .Set("hypothesisPrefab", hypothesisPrefab)
                        .Set("hypothesesEmpty", hypEmpty.gameObject);

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
            Art(dialoguePanel, _art.notebook, false, false, notebookTag, notebookEdge);

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
            Art(bubbleBg, _art.speechBubble);
            if (Art(bubbleTail, _art.bubbleTail)) bubbleTail.rectTransform.localRotation = Quaternion.identity;
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

            // ---- 시네마틱 대사창 (평범한 대화 구간: 사건 이전 · 컷씬 · 인트로) ----
            // 2026-09-22 레퍼런스: 배경 전체 위에 아래 어두운 그라데이션, 색 붓자국 이름표 + 역할 캡션, 본문.
            // 화면 아무 곳이나 눌러 넘기고, 길게 누르면 SKIP.
            var cineRoot = UI("CinematicDialogue", root);
            var cineRt = cineRoot.GetComponent<RectTransform>();
            Stretch(cineRt);
            var cineGroup = cineRoot.AddComponent<CanvasGroup>();

            var cineCatcher = Img("ClickCatcher", cineRt, new Color(0f, 0f, 0f, 0f));
            Stretch(cineCatcher.rectTransform);
            cineCatcher.raycastTarget = true;
            cineCatcher.gameObject.AddComponent<DialogueClickRelay>();

            var cineGradient = Img("Gradient", cineRt, Color.white);
            Anchor(cineGradient.rectTransform, V(0, 0), V(1, 0), V(0, 0), V(0, 440f));
            cineGradient.sprite = _art.bottomGradient != null ? _art.bottomGradient : EnsureGradientSprite();
            cineGradient.type = Image.Type.Simple;
            cineGradient.raycastTarget = false;

            // 이름표 — 붓자국 판 위에 이름(굵게) + "_ 역할". 글자 길이에 맞춰 판이 늘어난다.
            var cineTag = Img("NameTag", cineRt, new Color32(0x3F, 0x8A, 0x4E, 0xFF));
            var cineTagRt = cineTag.rectTransform;
            cineTagRt.anchorMin = V(0, 0); cineTagRt.anchorMax = V(0, 0); cineTagRt.pivot = V(0, 0);
            cineTagRt.anchoredPosition = V(L.Gutter + 56f, 268f);
            cineTagRt.sizeDelta = V(320f, 60f);
            cineTag.sprite = EnsureBrushSprite(); cineTag.type = Image.Type.Sliced;   // 아트 세트에 붓자국이 없으면 만들어 둔 흰 붓자국
            cineTag.raycastTarget = false;
            Art(cineTag, _art.nameTagBrush, false, keepColor: true);   // 흰 붓자국이 화자 색으로 물든다
            var cineTagLayout = cineTag.gameObject.AddComponent<HorizontalLayoutGroup>();
            cineTagLayout.padding = new RectOffset(28, 36, 0, 0);
            cineTagLayout.spacing = 14f;
            cineTagLayout.childAlignment = TextAnchor.MiddleLeft;
            cineTagLayout.childControlWidth = true;
            cineTagLayout.childControlHeight = true;
            cineTagLayout.childForceExpandWidth = false;
            cineTagLayout.childForceExpandHeight = true;
            var cineTagFit = cineTag.gameObject.AddComponent<ContentSizeFitter>();
            cineTagFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var cineSpeaker = Text("Name", cineTagRt, "", 34f, Color.white, TextAlignmentOptions.Left);
            cineSpeaker.fontStyle = FontStyles.Bold;
            var cineRole = Text("Role", cineTagRt, "", T.Caption + 2f, new Color(1f, 1f, 1f, 0.9f), TextAlignmentOptions.Left);

            var cineDirection = Text("StageDirection", cineRt, "", T.Caption, new Color32(0xB8, 0xB4, 0xAA, 0xFF), TextAlignmentOptions.Left);
            cineDirection.fontStyle = FontStyles.Italic;
            Anchor(cineDirection.rectTransform, V(0, 0), V(1, 0), V(L.Gutter + 64f, 232f), V(-L.Gutter - 64f, 262f));

            var cineBody = Text("Body", cineRt, "", T.Body + 2f, Color.white, TextAlignmentOptions.TopLeft);
            cineBody.textWrappingMode = TextWrappingModes.Normal;
            cineBody.lineSpacing = 18f;
            cineBody.raycastTarget = true;
            Anchor(cineBody.rectTransform, V(0, 0), V(1, 0), V(L.Gutter + 64f, 104f), V(-L.Gutter - 64f, 232f));

            var cineAdvance = Text("AdvanceIndicator", cineRt, "▼", T.Caption, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Right);
            Anchor(cineAdvance.rectTransform, V(1, 0), V(1, 0), V(-L.Gutter - 112f, 104f), V(-L.Gutter - 64f, 128f));

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
                     .Set("bubbleAdvanceIndicator", bubbleAdvance.gameObject)
                     .Set("cinematicRoot", cineGroup)
                     .Set("cinematicSpeakerLabel", cineSpeaker)
                     .Set("cinematicRoleLabel", cineRole)
                     .Set("cinematicStroke", cineTag)
                     .Set("cinematicBodyLabel", cineBody)
                     .Set("cinematicDirectionLabel", cineDirection)
                     .Set("cinematicAdvanceIndicator", cineAdvance.gameObject);
            bubbleRoot.GetComponent<DialogueClickRelay>().Set("target", presenter);
            cineCatcher.GetComponent<DialogueClickRelay>().Set("target", presenter);

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
            var stageHasArt = Art(stage, _art.collectionStage, _art.collectionStagePlace);

            var stageHint = Text("Hint", stage.rectTransform,
                "객실 테이블  —  살펴볼 곳을 눌러 보세요", T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF),
                TextAlignmentOptions.Center);
            Anchor(stageHint.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));
            if (stageHasArt) stageHint.gameObject.SetActive(false);

            // 고양이는 클릭 대상이 아니라 무대 위 인물이다. 그 아래로 비죽 나온 것만 핫스팟.
            var stageCat = Img("Cat_Placeholder", stage.rectTransform, new Color32(0x24, 0x29, 0x2F, 0xFF));
            Anchor(stageCat.rectTransform, V(0.56f, 0.50f), V(0.86f, 0.84f), Vector2.zero, Vector2.zero);
            stageCat.raycastTarget = false;
            var stageCatTag = Text("Label", stageCat.rectTransform, "검은 고양이\n(아트 대기)", T.Caption,
                new Color32(0x5A, 0x61, 0x69, 0xFF), TextAlignmentOptions.Center);
            Stretch(stageCatTag.rectTransform);
            if (!Art(stageCat, _art.catStage, _art.catStagePlace, false, stageCatTag) && stageHasArt) stageCat.gameObject.SetActive(false);

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

                // 진짜 무대 그림이 있으면 색 판은 투명해지고, 마우스 오버 테두리만 남는다.
                if (stageHasArt) { spot.color = new Color(0f, 0f, 0f, 0f); spotLabel.gameObject.SetActive(false); }

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
            Art(interBg, _art.StageOrRoom, _art.stageBackgroundPlace, false, interTag);

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

            var sepiHasArt = Art(charImg, _art.SepiOnStage, _art.sepiStagePlace, false, charLabel);
            var characterView = characterGo.AddComponent<PlaceholderCharacterView>();
            characterView.Set("characterId", "SEPI")
                         .Set("body", charImg)
                         .Set("reactionOutline", charOutline)
                         .Set("canvasGroup", charGroup)
                         .Set("hasArt", sepiHasArt)
                         .Set("defaultSprite", _art.SepiOnStage);
            if (sepiHasArt) characterView.Set("expressions", BuildExpressionList(_art.sepiExpressions));

            var clickTarget = characterGo.AddComponent<CharacterClickTarget>();

            // ---- 심문 오브젝트 — 인물·배경 위의 핫스팟. 한 번 고르면 꺼진다 (2026-10-11) ----
            // 고양이는 세피 오른쪽 테이블 위, 프로필 서류는 세피 앞 책상 위. 무대 그림이 오면 판은 투명해지고 테두리만 남는다.
            var interCat = Img("Cat_Placeholder", interRt, new Color32(0x24, 0x29, 0x2F, 0xFF));
            Anchor(interCat.rectTransform, V(0.70f, 0.28f), V(0.96f, 0.62f), Vector2.zero, Vector2.zero);
            interCat.raycastTarget = false;
            var interCatTag = Text("Label", interCat.rectTransform, "검은 고양이\n(아트 대기)", T.Caption,
                new Color32(0x5A, 0x61, 0x69, 0xFF), TextAlignmentOptions.Center);
            Stretch(interCatTag.rectTransform);
            var interStageHasArt = _art.StageOrRoom != null;
            if (!Art(interCat, _art.catStage, _art.catStagePlace, false, interCatTag) && interStageHasArt) interCat.gameObject.SetActive(false);

            var interHotspotRects = new (string id, float x0, float y0, float x1, float y1)[]
            {
                ("OBJ_INT_CAT",     0.72f, 0.30f, 0.94f, 0.50f),   // 고양이 엉덩이
                ("OBJ_INT_PROFILE", 0.29f, 0.06f, 0.43f, 0.24f),   // 책상 위 보조 수사관 프로필 서류
            };
            var interHotspots = new List<InteractableObject>();
            foreach (var h in interHotspotRects)
            {
                var def = AssetDatabase.LoadAssetAtPath<InteractableDefinition>($"{DataRoot}/Objects/Obj_{h.id}.asset");
                if (def == null) continue;

                var spot = Img("Hotspot_" + h.id, interRt, new Color32(0x2E, 0x34, 0x3B, 0xC8));
                Anchor(spot.rectTransform, V(h.x0, h.y0), V(h.x1, h.y1), Vector2.zero, Vector2.zero);
                spot.sprite = _uiSprite; spot.type = Image.Type.Sliced;
                spot.gameObject.AddComponent<CanvasGroup>();

                var spotLabel = Text("Label", spot.rectTransform, def.HoverLabel, T.Caption,
                    new Color32(0x8A, 0x90, 0x97, 0xFF), TextAlignmentOptions.Center);
                spotLabel.textWrappingMode = TextWrappingModes.Normal;
                Stretch(spotLabel.rectTransform);
                if (interStageHasArt) { spot.color = new Color(0f, 0f, 0f, 0f); spotLabel.gameObject.SetActive(false); }

                var outline = spot.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.97f, 0.88f, 0.55f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = false;

                var io = spot.gameObject.AddComponent<InteractableObject>();
                io.Set("definition", def).Set("outline", outline).Set("hitArea", spot);
                interHotspots.Add(io);
            }

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
            // 용의자 선택과 같은 구성. 자리에 앉은 인물 중 하나를 고르면 주인공이 이유를 말하고 엔딩으로 간다.

            var accuseScreen = UI("Screen_Accusation", root);
            var accuseRt = accuseScreen.GetComponent<RectTransform>();
            Anchor(accuseRt, V(0, 0), V(1, 1), V(L.Gutter, L.BandBottom), V(L.ClueLeft - L.Gap, L.BandTop));

            var accuseBg = Img("Backdrop", accuseRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            Stretch(accuseBg.rectTransform);
            accuseBg.raycastTarget = false;
            var accuseTag = Text("Tag", accuseBg.rectTransform, "객실  —  게임플레이 배경 (아트 대기)",
                T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Center);
            Anchor(accuseTag.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));
            Art(accuseBg, _art.StageOrRoom, _art.stageBackgroundPlace, false, accuseTag);

            var accusePrompt = Text("Prompt", accuseRt, "", T.Body, P.Alarm, TextAlignmentOptions.Center);
            Anchor(accusePrompt.rectTransform, V(0.3f, 1), V(1, 1), V(0, -L.Unit * 14), V(0, -L.Unit * 7));

            var accuseSummary = Text("Summary", accuseRt, "", T.Caption, P.Brass, TextAlignmentOptions.Center);
            Spaced(accuseSummary);
            Anchor(accuseSummary.rectTransform, V(0.3f, 0), V(1, 0), V(0, L.Unit * 36), V(0, L.Unit * 40));

            // 인물은 좌측 목록 오른쪽의 무대에 앉는다.
            var accuseStage = UI("Stage", accuseRt);
            var accuseStageRt = accuseStage.GetComponent<RectTransform>();
            Anchor(accuseStageRt, V(0.3f, 0), V(1, 1), Vector2.zero, Vector2.zero);
            var accuseCards = BuildSuspectCards(accuseStageRt);

            // 좌측 목록 — 가설, 그다음 추론 갈래. 심문의 Choice 열과 같은 자리.
            var accuseListRoot = UI("ListPanel", accuseRt);
            var accuseListRootRt = accuseListRoot.GetComponent<RectTransform>();
            Anchor(accuseListRootRt, V(0, 0), V(0, 1), V(0, L.Unit * 36), V(L.ChoiceWidth, -L.Unit * 26));
            var accuseListTitle = Text("Title", accuseListRootRt, "Hypothesis", T.Label, P.Brass, TextAlignmentOptions.Left);
            Spaced(accuseListTitle);
            Anchor(accuseListTitle.rectTransform, V(0, 1), V(1, 1), V(0, -24f), V(0, 0));
            var accuseList = UI("List", accuseListRootRt);
            var accuseListRt = accuseList.GetComponent<RectTransform>();
            Anchor(accuseListRt, V(0, 0), V(1, 1), V(0, 0), V(0, -L.Unit * 5));
            var alvg = accuseList.AddComponent<VerticalLayoutGroup>();
            alvg.spacing = L.Unit;
            alvg.childControlHeight = false;
            alvg.childControlWidth = true;
            alvg.childForceExpandWidth = true;
            alvg.childForceExpandHeight = false;
            alvg.childAlignment = TextAnchor.UpperLeft;
            accuseListRoot.SetActive(false);

            // ======================================================= 추론 화면
            // 2026-10-11 제작자 스케치: 나무 책상 위에 펼친 서류 두 장.
            //   왼쪽 Evidence — 단서 수집에서 모은 물품 / 오른쪽 Leads — 심문에서 얻은 정황(그 말을 한 사람의 인적 사항 아래).
            //   하나씩 고르면 두 장 사이 골을 따라 붉은 선 → 선 위 「가설 추론」 → 어두워지며 추론 대사 + 「가설 도출 성공!」
            //   → 오른쪽 끝 가설 탭에 꽂힌다. 좌상단 「추론」 판과 「돌아가기」, 위 가운데 잉크, 우상단 시계.
            // 화면 전체를 덮는다 — 이 페이즈에서는 상단 바와 우측 단서 목록이 숨는다(서류 두 장이 그 역할을 한다).

            var deductScreen = UI("Screen_Deduction", root);
            var deductRt = deductScreen.GetComponent<RectTransform>();
            Stretch(deductRt);
            var deductGroup = deductScreen.AddComponent<CanvasGroup>();

            var desk = Img("Desk", deductRt, new Color32(0x2A, 0x1C, 0x13, 0xFF));
            Stretch(desk.rectTransform);
            desk.raycastTarget = true;   // 뒤의 화면이 눌리지 않게
            Art(desk, _art.deductionDesk, _art.deductionDeskPlace);

            // 서류철은 그림 한 장처럼 배치한다 — 1920×1080 판 하나에 전부 올리고, 판을 화면에 맞춰 통째로 줄였다 늘린다.
            // 요소마다 앵커를 잡으면 창 비율(16:9 ↔ 4:3)이 바뀔 때 서류·선·탭이 제각각 밀린다.
            var dFrame = UI("Frame", deductRt);
            var dFrameRt = dFrame.GetComponent<RectTransform>();
            dFrameRt.anchorMin = dFrameRt.anchorMax = V(0.5f, 0.5f);
            dFrameRt.sizeDelta = new Vector2(1920f, 1080f);
            dFrame.AddComponent<UniformFitScaler>();

            Color paper = new Color32(0x8E, 0x84, 0x7B, 0xFF);
            Color paperBack = new Color32(0x6B, 0x62, 0x5B, 0xFF);
            Color paperInk = new Color32(0x2E, 0x24, 0x1D, 0xFF);
            Color paperSoft = new Color32(0x55, 0x48, 0x3E, 0xFF);
            Color deskInk = new Color32(0xE3, 0xDE, 0xD6, 0xFF);
            Color deskSoft = new Color32(0xB4, 0xA6, 0x98, 0xFF);
            Color linkRed = new Color32(0xC8, 0x24, 0x1C, 0xFF);

            Image Sheet(string name, Vector2 min, Vector2 max, Color c)
            {
                var s = Img(name, dFrameRt, c);
                Anchor(s.rectTransform, V(0, 0), V(0, 0), min, max);
                s.sprite = _art.dossierPaper != null ? _art.dossierPaper : _uiSprite;
                s.type = Image.Type.Sliced;
                s.raycastTarget = false;
                if (_art.dossierPaper != null && c != paperBack) s.color = Color.white;
                return s;
            }

            TMP_Text Plaque(string name, string label, Vector2 min, Vector2 max, float rotate, Color fill, Color ink, float size)
            {
                var p = Img(name, dFrameRt, fill);
                Anchor(p.rectTransform, V(0, 0), V(0, 0), min, max);
                p.sprite = _uiSprite; p.type = Image.Type.Sliced;
                p.raycastTarget = false;
                p.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotate);
                var t = Text("Label", p.rectTransform, label, size, ink, TextAlignmentOptions.Center);
                Stretch(t.rectTransform);
                return t;
            }

            // 뒤에 깔린 종이 — 겹쳐 놓은 서류처럼.
            Sheet("EvidenceBack", V(196, 116), V(812, 980), paperBack);
            Sheet("LeadsBack2", V(952, 108), V(1600, 972), paperBack);
            Sheet("LeadsBack", V(944, 126), V(1590, 990), new Color32(0x7C, 0x73, 0x6B, 0xFF));
            var evPage = Sheet("EvidencePage", V(176, 136), V(792, 1000), paper);
            var ldPage = Sheet("LeadsPage", V(928, 144), V(1576, 1008), paper);

            // ---- Evidence 장 ----
            var evBody = UI("Body", evPage.rectTransform).GetComponent<RectTransform>();
            Anchor(evBody, V(0, 0), V(1, 1), V(32, 96), V(-32, -40));
            var evEmpty = Text("Empty", evPage.rectTransform, "펼쳐 둘 물품이 없다", T.Choice, paperSoft, TextAlignmentOptions.Center);
            Stretch(evEmpty.rectTransform);
            var evPager = Text("Pager", evPage.rectTransform, "1 / 1", 26f, paperInk, TextAlignmentOptions.Center);
            Anchor(evPager.rectTransform, V(0.5f, 0), V(0.5f, 0), V(-80, 28), V(80, 76));
            var evPrev = PlainButton("Prev", evPage.rectTransform, "<", 30f);
            Anchor(evPrev.Item1, V(0.5f, 0), V(0.5f, 0), V(-144, 28), V(-96, 76));
            var evNext = PlainButton("Next", evPage.rectTransform, ">", 30f);
            Anchor(evNext.Item1, V(0.5f, 0), V(0.5f, 0), V(96, 28), V(144, 76));
            foreach (var b in new[] { evPrev, evNext })
            {
                b.Item1.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                b.Item1.Find("Label").GetComponent<TMP_Text>().color = paperInk;
            }

            // ---- Leads 장 — 위에 인물의 인적 사항, 아래에 그 사람에게서 나온 정황 ----
            var profileRoot = UI("Profile", ldPage.rectTransform);
            var profileRt = profileRoot.GetComponent<RectTransform>();
            Anchor(profileRt, V(0, 1), V(1, 1), V(36, -292), V(-36, -40));
            var portraitFrame = Img("PortraitFrame", profileRt, new Color32(0x3E, 0x33, 0x2C, 0xFF));
            Anchor(portraitFrame.rectTransform, V(0, 0), V(0, 0), V(0, 0), V(184, 252));
            portraitFrame.sprite = _uiSprite; portraitFrame.type = Image.Type.Sliced;
            portraitFrame.raycastTarget = false;
            var portrait = Img("Portrait", portraitFrame.rectTransform, new Color32(0x4E, 0x40, 0x38, 0xFF));
            Anchor(portrait.rectTransform, V(0, 0), V(1, 1), V(8, 8), V(-8, -8));
            portrait.raycastTarget = false;
            var profileName = Text("Name", profileRt, "", 32f, paperInk, TextAlignmentOptions.TopLeft);
            profileName.fontStyle = FontStyles.Bold;
            Anchor(profileName.rectTransform, V(0, 1), V(1, 1), V(212, -52), V(0, -4));
            var profileText = Text("Text", profileRt, "", 19f, paperSoft, TextAlignmentOptions.TopLeft);
            profileText.textWrappingMode = TextWrappingModes.Normal;
            profileText.overflowMode = TextOverflowModes.Ellipsis;
            profileText.lineSpacing = 6f;
            Anchor(profileText.rectTransform, V(0, 0), V(1, 1), V(212, 0), V(0, -64));
            var profileRule = Img("Rule", ldPage.rectTransform, new Color32(0x4A, 0x3E, 0x36, 0xFF));
            Anchor(profileRule.rectTransform, V(0, 1), V(1, 1), V(36, -312), V(-36, -306));
            profileRule.raycastTarget = false;

            var ldBody = UI("Body", ldPage.rectTransform).GetComponent<RectTransform>();
            Anchor(ldBody, V(0, 0), V(1, 1), V(28, 96), V(-28, -324));
            var ldEmpty = Text("Empty", ldPage.rectTransform, "심문에서 얻은 정황이 없다", T.Choice, paperSoft, TextAlignmentOptions.Center);
            Stretch(ldEmpty.rectTransform);
            var ldPager = Text("Pager", ldPage.rectTransform, "1 / 1", 26f, paperInk, TextAlignmentOptions.Center);
            Anchor(ldPager.rectTransform, V(0.5f, 0), V(0.5f, 0), V(-80, 28), V(80, 76));
            var ldPrev = PlainButton("Prev", ldPage.rectTransform, "<", 30f);
            Anchor(ldPrev.Item1, V(0.5f, 0), V(0.5f, 0), V(-144, 28), V(-96, 76));
            var ldNext = PlainButton("Next", ldPage.rectTransform, ">", 30f);
            Anchor(ldNext.Item1, V(0.5f, 0), V(0.5f, 0), V(96, 28), V(144, 76));
            foreach (var b in new[] { ldPrev, ldNext })
            {
                b.Item1.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                b.Item1.Find("Label").GetComponent<TMP_Text>().color = paperInk;
            }

            // 서류에 꽂힌 꼬리표 — 스케치의 기울어진 Evidence / Leads 판.
            Plaque("EvidenceTab", "Evidence", V(700, 916), V(860, 970), -6f, new Color32(0x2B, 0x24, 0x1F, 0xFF), deskInk, 22f);
            Plaque("LeadsTab", "Leads", V(816, 704), V(964, 760), -8f, new Color32(0x2B, 0x24, 0x1F, 0xFF), deskInk, 22f);

            // ---- 잇는 선 · 「가설 추론」 도장 ----
            var linkLayer = UI("LinkLayer", dFrameRt);
            var linkRt = linkLayer.GetComponent<RectTransform>();
            Stretch(linkRt);
            Image LinkLine(string name)
            {
                var l = Img(name, linkRt, linkRed);
                l.sprite = null;
                l.raycastTarget = false;
                l.enabled = false;
                return l;
            }
            var lineA = LinkLine("LineA");
            var lineB = LinkLine("LineB");
            var lineC = LinkLine("LineC");

            var stampImg = Img("Stamp", linkRt, new Color32(0x8E, 0x1B, 0x16, 0xFF));
            stampImg.sprite = _uiSprite; stampImg.type = Image.Type.Sliced;
            stampImg.rectTransform.anchorMin = stampImg.rectTransform.anchorMax = V(0.5f, 0.5f);
            stampImg.rectTransform.sizeDelta = new Vector2(232f, 76f);
            stampImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, -3f);
            var stampBtn = stampImg.gameObject.AddComponent<Button>();
            stampBtn.targetGraphic = stampImg;
            var stampColors = stampBtn.colors;
            stampColors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            stampColors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            stampBtn.colors = stampColors;
            var stampLabel = Text("Label", stampImg.rectTransform, "가설 추론", 34f, new Color32(0xF2, 0xA2, 0x3A, 0xFF), TextAlignmentOptions.Center);
            stampLabel.fontStyle = FontStyles.Bold;
            Stretch(stampLabel.rectTransform);
            stampImg.gameObject.SetActive(false);

            var dHint = Text("Hint", dFrameRt, "", 19f, deskSoft, TextAlignmentOptions.Center);
            Anchor(dHint.rectTransform, V(0, 0), V(0, 0), V(560, 44), V(1360, 96));

            // ---- 좌상단: 「추론」 판 · 돌아가기 ----
            var titlePlaque = Img("TitlePlaque", dFrameRt, new Color32(0xD9, 0xD1, 0xC5, 0xFF));
            Anchor(titlePlaque.rectTransform, V(0, 0), V(0, 0), V(24, 984), V(424, 1060));
            titlePlaque.sprite = _uiSprite; titlePlaque.type = Image.Type.Sliced;
            titlePlaque.raycastTarget = false;
            var titlePlaqueOutline = titlePlaque.gameObject.AddComponent<Outline>();
            titlePlaqueOutline.effectColor = new Color32(0x2B, 0x24, 0x1F, 0xFF);
            titlePlaqueOutline.effectDistance = new Vector2(3f, -3f);
            var titlePlaqueLabel = Text("Label", titlePlaque.rectTransform, "추     론", 42f, new Color32(0x2B, 0x24, 0x1F, 0xFF), TextAlignmentOptions.Center);
            titlePlaqueLabel.fontStyle = FontStyles.Bold;
            Stretch(titlePlaqueLabel.rectTransform);

            var backBtn = PlainButton("Back", dFrameRt, "돌아가기", 26f);
            Anchor(backBtn.Item1, V(0, 0), V(0, 0), V(0, 900), V(216, 964));
            backBtn.Item1.GetComponent<Image>().color = new Color32(0x5A, 0x2E, 0x34, 0xFF);
            backBtn.Item1.Find("Label").GetComponent<TMP_Text>().color = deskInk;

            // ---- 위 가운데: 잉크 ----
            var inkBlot = Img("InkBlot", dFrameRt, new Color32(0x12, 0x0C, 0x0A, 0xF2));
            Anchor(inkBlot.rectTransform, V(0, 0), V(0, 0), V(772, 968), V(1148, 1096));
            inkBlot.sprite = _uiSprite; inkBlot.type = Image.Type.Sliced;
            inkBlot.raycastTarget = false;
            var inkRow = UI("InkDrops", dFrameRt);
            var inkRowRt = inkRow.GetComponent<RectTransform>();
            Anchor(inkRowRt, V(0, 0), V(0, 0), V(800, 984), V(1120, 1064));
            var inkLayout = inkRow.AddComponent<HorizontalLayoutGroup>();
            inkLayout.spacing = 10f;
            inkLayout.childAlignment = TextAnchor.MiddleCenter;
            inkLayout.childControlWidth = false;
            inkLayout.childControlHeight = false;
            inkLayout.childForceExpandWidth = false;
            inkLayout.childForceExpandHeight = false;
            var dropTemplate = Img("Drop", inkRowRt, Color.white);
            dropTemplate.rectTransform.sizeDelta = _art.inkDrop != null ? new Vector2(44f, 56f) : new Vector2(40f, 40f);
            dropTemplate.sprite = _art.inkDrop != null ? _art.inkDrop : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            dropTemplate.type = Image.Type.Simple;
            dropTemplate.preserveAspect = true;
            dropTemplate.raycastTarget = false;
            var dropOutline = dropTemplate.gameObject.AddComponent<Outline>();
            dropOutline.effectColor = new Color(1f, 1f, 1f, 0.85f);
            dropOutline.effectDistance = new Vector2(2f, -2f);
            dropTemplate.gameObject.SetActive(false);
            var inkView = inkRow.AddComponent<InkDropsView>();
            inkView.Set("container", inkRowRt).Set("dropPrefab", dropTemplate)
                   .Set("fullColor", (Color)new Color32(0x52, 0x3C, 0x74, 0xFF))
                   .Set("driedColor", (Color)new Color32(0xD8, 0xC8, 0xA6, 0xFF));

            // ---- 우상단: 시계 · 장소 ----
            var dClock = Text("Clock", dFrameRt, "", 64f, deskInk, TextAlignmentOptions.Right);
            dClock.characterSpacing = 6f;
            Anchor(dClock.rectTransform, V(0, 0), V(0, 0), V(1440, 984), V(1880, 1064));
            var dLocation = Text("Location", dFrameRt, "", 20f, deskSoft, TextAlignmentOptions.Right);
            Anchor(dLocation.rectTransform, V(0, 0), V(0, 0), V(1440, 944), V(1880, 980));

            // ---- 오른쪽 끝: 가설 탭 ----
            // 2026-10-11 2차: 칸은 늘 세 개 — 이 챕터에서 못 쓰는 칸은 빗금. 탭마다 쓰레기통, 맨 아래 「추리를 마친다」.
            // 묶음 하나로 둬야 가설 교체 때 어두운 막 위로 통째로 올릴 수 있다.
            var hypList = UI("HypothesisList", dFrameRt);
            var hypListRt = hypList.GetComponent<RectTransform>();
            Stretch(hypListRt);
            var hatchSprite = EnsureHatchSprite();
            var trashSprite = EnsureTrashSprite();
            var hypTabs = new List<DeductionDossierView.HypothesisTab>();
            for (int i = 0; i < 3; i++)
            {
                var top = 792f - i * 128f;
                var tabGo = UI("HypothesisTab_" + (i + 1), hypListRt);
                var tabRt = tabGo.GetComponent<RectTransform>();
                Anchor(tabRt, V(0, 0), V(0, 0), V(1608, top - 100), V(1916, top));

                var barFrame = Img("BarFrame", tabRt, new Color(0f, 0f, 0f, 0f));
                Anchor(barFrame.rectTransform, V(0, 0), V(0, 0), V(16, -4), V(312, 64));
                barFrame.sprite = _uiSprite; barFrame.type = Image.Type.Sliced;
                barFrame.raycastTarget = false;
                var bar = Img("Bar", tabRt, paper);
                Anchor(bar.rectTransform, V(0, 0), V(0, 0), V(20, 0), V(308, 60));
                bar.sprite = _uiSprite; bar.type = Image.Type.Sliced;
                var barBtn = bar.gameObject.AddComponent<Button>();
                barBtn.targetGraphic = bar;
                var pill = Img("Pill", tabRt, new Color32(0x3A, 0x2F, 0x28, 0xFF));
                Anchor(pill.rectTransform, V(0, 0), V(0, 0), V(0, 56), V(112, 100));
                pill.sprite = _uiSprite; pill.type = Image.Type.Sliced;
                pill.raycastTarget = false;
                var pillLabel = Text("Label", pill.rectTransform, "가설" + (i + 1), 20f, deskInk, TextAlignmentOptions.Center);
                Stretch(pillLabel.rectTransform);
                var lens = Img("Lens", bar.rectTransform, paperInk);
                Anchor(lens.rectTransform, V(0, 0.5f), V(0, 0.5f), V(10, -18), V(46, 18));
                lens.sprite = _art.iconRecord != null ? _art.iconRecord : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                lens.preserveAspect = true;
                lens.raycastTarget = false;
                lens.enabled = false;
                var tabTitle = Text("Title", bar.rectTransform, "", 16f, paperInk, TextAlignmentOptions.Left);
                tabTitle.overflowMode = TextOverflowModes.Ellipsis;
                tabTitle.textWrappingMode = TextWrappingModes.Normal;
                tabTitle.lineSpacing = -8f;
                Anchor(tabTitle.rectTransform, V(0, 0), V(1, 1), V(54, 0), V(-50, 0));

                // 쓰레기통 — 막대 오른쪽 끝. 막대의 버튼보다 위에 있어 먼저 눌린다.
                var trashImg = Img("Trash", bar.rectTransform, paperInk);
                Anchor(trashImg.rectTransform, V(1, 0.5f), V(1, 0.5f), V(-44, -17), V(-10, 17));
                trashImg.sprite = trashSprite;
                trashImg.type = Image.Type.Simple;
                trashImg.preserveAspect = true;
                var trashBtn = trashImg.gameObject.AddComponent<Button>();
                trashBtn.targetGraphic = trashImg;
                var trashColors = trashBtn.colors;
                trashColors.highlightedColor = new Color(2.2f, 0.9f, 0.8f, 1f);   // 올리면 붉게
                trashColors.pressedColor = new Color(1.6f, 0.5f, 0.4f, 1f);
                trashColors.colorMultiplier = 2f;
                trashBtn.colors = trashColors;

                // 빗금 — 이 챕터에서 쓸 수 없는 칸.
                var hatch = Img("Hatch", bar.rectTransform, new Color32(0x2B, 0x22, 0x1C, 0xFF));
                Stretch(hatch.rectTransform);
                hatch.sprite = hatchSprite;
                hatch.type = Image.Type.Simple;
                hatch.raycastTarget = false;
                hatch.gameObject.SetActive(false);

                hypTabs.Add(new DeductionDossierView.HypothesisTab
                {
                    root = tabRt, bar = bar, barFrame = barFrame, title = tabTitle, lens = lens, button = barBtn,
                    trash = trashBtn, hatch = hatch.gameObject, pill = pillLabel
                });
            }

            // 「추리를 마친다」 — 가설 목록 맨 아래, 한 칸 띄워서.
            var endDeductBtn = PlainButton("EndDeduction", hypListRt, "추리를 마친다", 24f);
            Anchor(endDeductBtn.Item1, V(0, 0), V(0, 0), V(1628, 340), V(1916, 400));
            endDeductBtn.Item1.GetComponent<Image>().color = new Color32(0x5A, 0x2E, 0x34, 0xFF);
            endDeductBtn.Item1.Find("Label").GetComponent<TMP_Text>().color = deskInk;

            // ---- 어둡게 · 가운데 한 줄 · 지우기 막대 ----
            var scrimImg = Img("Scrim", dFrameRt, new Color(0f, 0f, 0f, 0.6f));
            Anchor(scrimImg.rectTransform, V(0, 0), V(1, 1), V(-2000, -2000), V(2000, 2000));   // 판 밖까지 — 화면 전체
            scrimImg.raycastTarget = true;   // 덧창 · 교체 때 그 아래를 막는다 (평소엔 CanvasGroup 이 꺼 둔다)
            var scrimGroup = scrimImg.gameObject.AddComponent<CanvasGroup>();
            scrimGroup.alpha = 0f; scrimGroup.blocksRaycasts = false; scrimGroup.interactable = false;

            var bannerGo = UI("Banner", dFrameRt);
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            Anchor(bannerRt, V(0, 0), V(0, 0), V(480, 520), V(1440, 680));
            var bannerBand = Img("Band", bannerRt, new Color(0f, 0f, 0f, 0.35f));
            Stretch(bannerBand.rectTransform);
            bannerBand.raycastTarget = false;
            var bannerLabel = Text("Label", bannerRt, "", 68f, new Color32(0xF2, 0xEA, 0xDC, 0xFF), TextAlignmentOptions.Center);
            bannerLabel.fontStyle = FontStyles.Bold;
            Stretch(bannerLabel.rectTransform);
            var bannerGroup = bannerGo.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f; bannerGroup.blocksRaycasts = false; bannerGroup.interactable = false;

            // ---- 가설 교체 — 가운데 새 가설 카드 + 교체 취소 (막 위. 우측 가설 목록도 그때만 막 위로 올라온다) ----
            var replaceRoot = UI("ReplaceOverlay", dFrameRt);
            Stretch(replaceRoot.GetComponent<RectTransform>());
            var replaceRootRt = replaceRoot.GetComponent<RectTransform>();
            var replacePrompt = Text("Prompt", replaceRootRt, "", 34f, deskInk, TextAlignmentOptions.Center);
            replacePrompt.fontStyle = FontStyles.Bold;
            Anchor(replacePrompt.rectTransform, V(0, 0), V(0, 0), V(360, 712), V(1560, 776));

            var newCard = UI("NewHypothesis", replaceRootRt);
            var newCardRt = newCard.GetComponent<RectTransform>();
            Anchor(newCardRt, V(0, 0), V(0, 0), V(680, 520), V(1240, 676));
            var newPill = Img("Pill", newCardRt, new Color32(0x3A, 0x2F, 0x28, 0xFF));
            Anchor(newPill.rectTransform, V(0, 0), V(0, 0), V(0, 96), V(184, 156));
            newPill.sprite = _uiSprite; newPill.type = Image.Type.Sliced;
            newPill.raycastTarget = false;
            var newPillLabel = Text("Label", newPill.rectTransform, "새 가설", 28f, deskInk, TextAlignmentOptions.Center);
            Stretch(newPillLabel.rectTransform);
            var newFrame = Img("BarFrame", newCardRt, linkRed);
            Anchor(newFrame.rectTransform, V(0, 0), V(0, 0), V(20, -6), V(560, 98));
            newFrame.sprite = _uiSprite; newFrame.type = Image.Type.Sliced;
            newFrame.raycastTarget = false;
            var newBar = Img("Bar", newCardRt, paper);
            Anchor(newBar.rectTransform, V(0, 0), V(0, 0), V(26, 0), V(554, 92));
            newBar.sprite = _uiSprite; newBar.type = Image.Type.Sliced;
            newBar.raycastTarget = false;
            var newLens = Img("Lens", newBar.rectTransform, paperInk);
            Anchor(newLens.rectTransform, V(0, 0.5f), V(0, 0.5f), V(16, -27), V(70, 27));
            newLens.sprite = _art.iconRecord != null ? _art.iconRecord : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            newLens.preserveAspect = true;
            newLens.raycastTarget = false;
            var newTitle = Text("Title", newBar.rectTransform, "", 26f, paperInk, TextAlignmentOptions.Left);
            newTitle.textWrappingMode = TextWrappingModes.Normal;
            newTitle.overflowMode = TextOverflowModes.Ellipsis;
            newTitle.lineSpacing = -6f;
            Anchor(newTitle.rectTransform, V(0, 0), V(1, 1), V(86, 0), V(-20, 0));

            var replaceCancel = PlainButton("Cancel", replaceRootRt, "교체 취소", 26f);
            Anchor(replaceCancel.Item1, V(0, 0), V(0, 0), V(840, 404), V(1080, 468));
            replaceCancel.Item1.GetComponent<Image>().color = new Color32(0x5A, 0x2E, 0x34, 0xFF);
            replaceCancel.Item1.Find("Label").GetComponent<TMP_Text>().color = deskInk;
            replaceRoot.SetActive(false);

            // ---- 가설 지우기 경고 (쓰레기통) ----
            var confirmRoot = UI("DeleteConfirm", dFrameRt);
            Stretch(confirmRoot.GetComponent<RectTransform>());
            var confirmPanel = Img("Panel", confirmRoot.transform, new Color32(0x16, 0x10, 0x0D, 0xF6));
            Anchor(confirmPanel.rectTransform, V(0, 0), V(0, 0), V(640, 400), V(1280, 680));
            confirmPanel.sprite = _uiSprite; confirmPanel.type = Image.Type.Sliced;
            var confirmOutline = confirmPanel.gameObject.AddComponent<Outline>();
            confirmOutline.effectColor = new Color32(0x8E, 0x1B, 0x16, 0xFF);
            confirmOutline.effectDistance = new Vector2(3f, -3f);
            var confirmText = Text("Message", confirmPanel.rectTransform, "", 28f, deskInk, TextAlignmentOptions.Center);
            confirmText.textWrappingMode = TextWrappingModes.Normal;
            confirmText.lineSpacing = 10f;
            Anchor(confirmText.rectTransform, V(0, 0), V(1, 1), V(40, 112), V(-40, -32));
            var confirmYes = PlainButton("Delete", confirmPanel.rectTransform, "지우기", 24f);
            Anchor(confirmYes.Item1, V(0.5f, 0), V(0.5f, 0), V(-196, 32), V(-20, 92));
            confirmYes.Item1.GetComponent<Image>().color = new Color32(0x8E, 0x1B, 0x16, 0xFF);
            confirmYes.Item1.Find("Label").GetComponent<TMP_Text>().color = deskInk;
            var confirmNo = PlainButton("Cancel", confirmPanel.rectTransform, "취소", 24f);
            Anchor(confirmNo.Item1, V(0.5f, 0), V(0.5f, 0), V(20, 32), V(196, 92));
            confirmNo.Item1.GetComponent<Image>().color = new Color32(0x3A, 0x2F, 0x28, 0xFF);
            confirmNo.Item1.Find("Label").GetComponent<TMP_Text>().color = deskInk;
            confirmRoot.SetActive(false);

            var dossierView = deductScreen.AddComponent<DeductionDossierView>();
            dossierView.Set("group", deductGroup)
                       .Set("overlay", linkRt)
                       .Set("font", _font)
                       .Set("panelSprite", _uiSprite)
                       .Set("evidencePage", evPage.rectTransform)
                       .Set("evidenceBody", evBody)
                       .Set("evidencePager", evPager)
                       .Set("evidencePrev", evPrev.Item2)
                       .Set("evidenceNext", evNext.Item2)
                       .Set("evidenceEmpty", evEmpty)
                       .Set("leadsPage", ldPage.rectTransform)
                       .Set("leadsBody", ldBody)
                       .Set("profileRoot", profileRoot)
                       .Set("profilePortrait", portrait)
                       .Set("profileName", profileName)
                       .Set("profileText", profileText)
                       .Set("leadsPager", ldPager)
                       .Set("leadsPrev", ldPrev.Item2)
                       .Set("leadsNext", ldNext.Item2)
                       .Set("leadsEmpty", ldEmpty)
                       .Set("portraits", new List<DeductionDossierView.PortraitBinding>
                       {
                           new DeductionDossierView.PortraitBinding { suspectId = "SEPI", portrait = _art.SepiOnTalk },
                           new DeductionDossierView.PortraitBinding { suspectId = "CAT", portrait = _art.CatOnTalk },
                       })
                       .Set("lineA", lineA).Set("lineB", lineB).Set("lineC", lineC)
                       .Set("stamp", stampBtn)
                       .Set("stampLabel", stampLabel)
                       .Set("hintLabel", dHint)
                       .Set("backButton", backBtn.Item2)
                       .Set("endButton", endDeductBtn.Item2)
                       .Set("hypothesisList", hypListRt)
                       .Set("ink", inkView)
                       .Set("clockLabel", dClock)
                       .Set("locationLabel", dLocation)
                       .Set("tabs", hypTabs)
                       .Set("scrim", scrimGroup)
                       .Set("banner", bannerGroup)
                       .Set("bannerLabel", bannerLabel)
                       .Set("replaceRoot", replaceRoot)
                       .Set("replacePrompt", replacePrompt)
                       .Set("replaceTitle", newTitle)
                       .Set("replaceCancel", replaceCancel.Item2)
                       .Set("confirmRoot", confirmRoot)
                       .Set("confirmText", confirmText)
                       .Set("confirmYes", confirmYes.Item2)
                       .Set("confirmNo", confirmNo.Item2);

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

            // ======================================================= 엔딩 화면
            // 엔딩 컷씬 · 이벤트 컷씬 자리 (아트 대기). 지목 결과 한 줄과 '타이틀로'.

            var endingScreen = UI("Screen_Ending", root);
            Stretch(endingScreen.GetComponent<RectTransform>());
            var endBg = Img("Backdrop", endingScreen.transform, new Color32(0x0F, 0x11, 0x14, 0xFF));
            Stretch(endBg.rectTransform);
            Art(endBg, _art.endingBackground, _art.endingBackgroundPlace);
            var endTitle = Text("Title", endingScreen.transform, "엔딩 컷씬 (아트 대기)", T.Display, P.Brass, TextAlignmentOptions.Center);
            Anchor(endTitle.rectTransform, V(0, 0.5f), V(1, 0.5f), V(0, L.Unit * 8), V(0, L.Unit * 20));
            var endBody = Text("Body", endingScreen.transform, "", T.Body, P.Ink, TextAlignmentOptions.Center);
            endBody.textWrappingMode = TextWrappingModes.Normal;
            Anchor(endBody.rectTransform, V(0.2f, 0.5f), V(0.8f, 0.5f), V(0, -L.Unit * 12), V(0, L.Unit * 6));
            var endTitleBtn = PlainButton("ToTitle", endingScreen.transform, "타이틀로", T.Choice);
            Anchor(endTitleBtn.Item1, V(0.5f, 0), V(0.5f, 0), V(-140f, L.Unit * 12), V(140f, L.Unit * 12 + 56f));

            // =================================================== 사건 이전 화면
            // 기획서의 [게임플레이 배경_캐릭터 씬]. 사건이 터지기 전의 객실이며,
            // 사건 직후의 인트로 대사도 같은 배경 위에서 이어진다.

            var talkScreen = UI("Screen_Talk", root);
            var talkRt = talkScreen.GetComponent<RectTransform>();
            Stretch(talkRt);   // 평범한 대화 구간은 배경이 화면 전체를 쓴다

            var talkBg = Img("Backdrop", talkRt, new Color32(0x1E, 0x22, 0x27, 0xFF));
            Stretch(talkBg.rectTransform);
            talkBg.raycastTarget = false;

            var talkTag = Text("Tag", talkBg.rectTransform, "객실  —  게임플레이 배경 (아트 대기)",
                T.Caption, new Color32(0x4A, 0x50, 0x57, 0xFF), TextAlignmentOptions.Center);
            Anchor(talkTag.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 6), V(0, -L.Unit * 2));
            Art(talkBg, _art.roomBackground, _art.roomBackgroundPlace, false, talkTag);

            var talkSepi = Img("Sepi_Placeholder", talkRt, new Color32(0x33, 0x39, 0x41, 0xFF));
            Anchor(talkSepi.rectTransform, V(0.52f, 0f), V(0.82f, 0.92f), Vector2.zero, Vector2.zero);
            talkSepi.raycastTarget = false;
            var talkSepiTag = Text("Label", talkSepi.rectTransform, "세피\n(아트 대기)", T.Caption,
                P.InkSoft, TextAlignmentOptions.Center);
            Stretch(talkSepiTag.rectTransform);
            Art(talkSepi, _art.SepiOnTalk, _art.sepiTalkPlace, false, talkSepiTag);

            var talkCat = Img("Cat_Placeholder", talkRt, new Color32(0x2A, 0x2F, 0x36, 0xFF));
            Anchor(talkCat.rectTransform, V(0.18f, 0.08f), V(0.34f, 0.34f), Vector2.zero, Vector2.zero);
            talkCat.raycastTarget = false;
            var talkCatTag = Text("Label", talkCat.rectTransform, "검은 고양이\n(아트 대기)", T.Caption,
                new Color32(0x6E, 0x74, 0x7B, 0xFF), TextAlignmentOptions.Center);
            Stretch(talkCatTag.rectTransform);
            Art(talkCat, _art.CatOnTalk, _art.catTalkPlace, false, talkCatTag);

            // ---- [일러스트 컷씬#1] ----
            var cutsceneScreen = UI("Screen_Cutscene", root);
            var cutsceneRt = cutsceneScreen.GetComponent<RectTransform>();
            Stretch(cutsceneRt);

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
            Art(selectBg, _art.StageOrRoom, _art.stageBackgroundPlace, false, selectTag);

            var selectPrompt = Text("Prompt", selectRt, "", T.Body, P.Brass, TextAlignmentOptions.Center);
            Anchor(selectPrompt.rectTransform, V(0, 1), V(1, 1), V(0, -L.Unit * 14), V(0, -L.Unit * 7));

            var suspectCards = BuildSuspectCards(selectRt);

            // 「추론 진행하기」 — 추론에서 「돌아가기」로 넘어왔을 때만. 대사창(수첩) 오른쪽 끝 바로 위.
            var toDeductionBtn = PlainButton("ToDeduction", selectRt, "추론 진행하기", T.Choice);
            var toDeductionBottom = L.Unit * 30f - L.BandBottom + 16f;
            Anchor(toDeductionBtn.Item1, V(0, 0), V(0, 0), V(1064f - 300f, toDeductionBottom), V(1064f, toDeductionBottom + 64f));
            toDeductionBtn.Item1.GetComponent<Image>().color = new Color32(0x52, 0x3C, 0x74, 0xFF);
            toDeductionBtn.Item1.Find("Label").GetComponent<TMP_Text>().color = new Color32(0xF2, 0xEA, 0xDC, 0xFF);
            toDeductionBtn.Item1.gameObject.SetActive(false);

            // 2차 심문 — 상대를 고른 뒤 들고 갈 가설을 고르는 덧창.
            var hypPickRoot = UI("HypothesisPick", selectRt);
            Stretch(hypPickRoot.GetComponent<RectTransform>());
            var hypPickScrim = Img("Scrim", hypPickRoot.transform, new Color(0f, 0f, 0f, 0.6f));
            Stretch(hypPickScrim.rectTransform);
            var hypPickPanel = Img("Panel", hypPickRoot.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            Anchor(hypPickPanel.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(-360f, -200f), V(360f, 200f));
            var hypPickPrompt = Text("Prompt", hypPickPanel.rectTransform, "", T.Body, P.Ink, TextAlignmentOptions.Center);
            Anchor(hypPickPrompt.rectTransform, V(0, 1), V(1, 1), V(L.Pad, -L.Unit * 11), V(-L.Pad, -L.Unit * 4));
            var hypPickList = UI("List", hypPickPanel.rectTransform);
            var hypPickListRt = hypPickList.GetComponent<RectTransform>();
            Anchor(hypPickListRt, V(0, 0), V(1, 1), V(L.Pad * 2, L.Pad), V(-L.Pad * 2, -L.Unit * 12));
            var hvlg = hypPickList.AddComponent<VerticalLayoutGroup>();
            hvlg.spacing = L.Unit;
            hvlg.childControlHeight = false;
            hvlg.childControlWidth = true;
            hvlg.childForceExpandWidth = true;
            hvlg.childForceExpandHeight = false;
            hvlg.childAlignment = TextAnchor.UpperCenter;
            hypPickRoot.SetActive(false);

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
            cineRoot.transform.SetAsLastSibling();
            topBar.transform.SetAsLastSibling();
            titleLabel.transform.SetAsLastSibling();

            // ================================================== 대화 도구
            // LOG(좌하) · SKIP ▶ · AUTO(우하) · 톱니(우상). 평범한 대화 구간에서만 나타난다.
            var toolbar = UI("DialogueToolbar", root);
            var toolbarRt = toolbar.GetComponent<RectTransform>();
            Stretch(toolbarRt);

            var logBtn = PlainButton("LogButton", toolbarRt, "LOG", T.Caption);
            Anchor(logBtn.Item1, V(0, 0), V(0, 0), V(L.Gutter, 32f), V(L.Gutter + 88f, 32f + 64f));
            StyleCinematicButton(logBtn);

            var autoBtn = PlainButton("AutoButton", toolbarRt, "AUTO", T.Caption);
            Anchor(autoBtn.Item1, V(1, 0), V(1, 0), V(-L.Gutter - 128f, 32f), V(-L.Gutter, 32f + 56f));
            StyleCinematicButton(autoBtn);

            var skipBtn = PlainButton("SkipButton", toolbarRt, "SKIP  ▶", T.Caption);
            Anchor(skipBtn.Item1, V(1, 0), V(1, 0), V(-L.Gutter - 128f - 16f - 144f, 32f), V(-L.Gutter - 128f - 16f, 32f + 56f));
            StyleCinematicButton(skipBtn);
            Object.DestroyImmediate(skipBtn.Item2);                // 누르고 있는 동안만 켜지므로 Button 대신 HoldButton
            var skipHold = skipBtn.Item1.gameObject.AddComponent<HoldButton>();

            var gearBtn = PlainButton("GearButton", toolbarRt, "설정", T.Caption);   // 톱니 아이콘은 아트 대기
            Anchor(gearBtn.Item1, V(1, 1), V(1, 1), V(-L.Gutter - 64f, -L.Gutter - 64f), V(-L.Gutter, -L.Gutter));
            StyleCinematicButton(gearBtn);
            ArtIcon(logBtn, _art.iconLog);
            ArtIcon(gearBtn, _art.iconSettings);

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
            settingsMenu.Set("openButtons", new List<Button> { sideSettingsBtn.Item2, gearBtn.Item2 });

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
            cursor.Set("penCursor", _art.penCursor).Set("eyeCursor", _art.eyeCursor)
                  .Set("eyeCursorBlink", _art.eyeCursorBlink).Set("reactionCursor", _art.reactionCursor)
                  .Set("penHotspot", _art.penHotspot).Set("eyeHotspot", _art.eyeHotspot).Set("reactionHotspot", _art.reactionHotspot);
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
                            .Set("promptLabel", selectPrompt)
                            .Set("deductionButton", toDeductionBtn.Item2)
                            .Set("hypothesisRoot", hypPickRoot)
                            .Set("hypothesisPrompt", hypPickPrompt)
                            .Set("hypothesisContainer", hypPickListRt)
                            .Set("hypothesisButtonPrefab", plainButtonPrefab);

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
                         .Set("tension", tension)
                         .Set("objectHotspots", interHotspots);

            var deductionCtl = systems.AddComponent<DeductionController>();
            deductionCtl.Set("dialogue", presenter)
                        .Set("dossier", dossierView)
                        .Set("clueTray", clueTrayView)
                        .Set("tutorialPopup", popupView)
                        .Set("cursor", cursor);

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
            accusation.Set("dialogue", presenter)
                      .Set("clueTray", clueTrayView)
                      .Set("cards", accuseCards)
                      .Set("prompt", accusePrompt)
                      .Set("listRoot", accuseListRoot)
                      .Set("listTitle", accuseListTitle)
                      .Set("listContainer", accuseListRt)
                      .Set("buttonPrefab", plainButtonPrefab)
                      .Set("summary", accuseSummary);

            var endingView = systems.AddComponent<EndingView>();
            endingView.Set("root", endingScreen)
                      .Set("title", endTitle)
                      .Set("body", endBody)
                      .Set("titleButton", endTitleBtn.Item2);
            flow.Set("accusation", accusation).Set("ending", endingView);

            var binder = systems.AddComponent<PhaseScreenBinder>();
            binder.Set("flow", flow)
                  .Set("groups", BuildScreenGroups(talkScreen, cutsceneScreen, selectScreen,
                                                    collectScreen, interrogateScreen, deductScreen, accuseScreen,
                                                    gameOverScreen, endingScreen, cluePanel.gameObject,
                                                    topBar, titleLabel.gameObject))
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
                       .Set("skipButton", skipHold)
                       .Set("skipGraphic", skipBtn.Item1.GetComponent<Image>())
                       .Set("skipLabel", skipBtn.Item1.Find("Label").GetComponent<TMP_Text>())
                       .Set("autoButton", autoBtn.Item2)
                       .Set("autoGraphic", autoBtn.Item1.GetComponent<Image>())
                       .Set("autoLabel", autoBtn.Item1.Find("Label").GetComponent<TMP_Text>())
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
            endingScreen.SetActive(false);
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
            GameObject collect, GameObject interrogate, GameObject deduct, GameObject accuse, GameObject gameOver, GameObject ending, GameObject cluePanel,
            GameObject topBar, GameObject chapterTitle)
        {
            // 추론 화면은 화면 전체를 쓰는 서류철이다 — 그 동안만 상단 바와 챕터 제목이 숨는다.
            var allButDeduction = new List<GamePhase>
            {
                GamePhase.PreIncidentTalk, GamePhase.Cutscene, GamePhase.Intro, GamePhase.ClueCollection,
                GamePhase.SuspectSelect, GamePhase.Interrogation1, GamePhase.Interrogation2,
                GamePhase.Accusation, GamePhase.Ending, GamePhase.GameOver
            };

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
                new PhaseScreenBinder.ScreenGroup {
                    root = ending,
                    phases = new List<GamePhase> { GamePhase.Ending }
                },
                // 단서 목록은 단서가 생기는 구간부터 보인다.
                // 사건이 터지기도 전에 빈 목록을 띄워 두면 무엇을 하는 화면인지 흐려진다.
                new PhaseScreenBinder.ScreenGroup {
                    root = cluePanel,
                    phases = new List<GamePhase> {
                        GamePhase.ClueCollection, GamePhase.SuspectSelect, GamePhase.Interrogation1,
                        GamePhase.Interrogation2, GamePhase.Accusation
                    }
                },
                new PhaseScreenBinder.ScreenGroup { root = topBar, phases = new List<GamePhase>(allButDeduction) },
                new PhaseScreenBinder.ScreenGroup { root = chapterTitle, phases = new List<GamePhase>(allButDeduction) }
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

            var selectedMark = Img("SelectedMark", rt, new Color(0.77f, 0.65f, 0.36f, 0.32f));
            Stretch(selectedMark.rectTransform);
            selectedMark.sprite = _uiSprite; selectedMark.type = Image.Type.Sliced;
            selectedMark.raycastTarget = false;
            selectedMark.gameObject.SetActive(false);

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
                .Set("promotedFrame", frame.gameObject)
                .Set("selectedMark", selectedMark.gameObject);
            frame.gameObject.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/ClueSlot.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ClueSlot>();
        }

        /// <summary>정황/실마리 목록의 한 줄 — 아이콘 + 이름이 항상 보인다.</summary>
        private static ClueSlot BuildLeadSlotPrefab() => BuildLeadLikePrefab("LeadSlot", P.Surface);

        /// <summary>가설 칸 — 정황 줄과 같은 꼴, 바탕만 조금 다르다.</summary>
        private static ClueSlot BuildHypothesisSlotPrefab() => BuildLeadLikePrefab("HypothesisSlot", new Color32(0x2A, 0x2A, 0x22, 0xFF));

        private static ClueSlot BuildLeadLikePrefab(string name, Color surface)
        {
            var go = UI(name, null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 64f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;
            le.minHeight = 64f;

            var bg = go.AddComponent<Image>();
            bg.color = surface;
            bg.sprite = _uiSprite; bg.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var frame = Img("PromotedFrame", rt, P.Brass);
            Anchor(frame.rectTransform, V(0, 0), V(1, 1), V(-3f, -3f), V(3f, 3f));
            frame.sprite = _uiSprite; frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var frameInner = Img("Inner", frame.rectTransform, surface);
            Anchor(frameInner.rectTransform, V(0, 0), V(1, 1), V(3f, 3f), V(-3f, -3f));
            frameInner.sprite = _uiSprite; frameInner.type = Image.Type.Sliced;
            frameInner.raycastTarget = false;

            var selected = Img("SelectedMark", rt, new Color(0.77f, 0.65f, 0.36f, 0.28f));
            Stretch(selected.rectTransform);
            selected.sprite = _uiSprite; selected.type = Image.Type.Sliced;
            selected.raycastTarget = false;
            selected.gameObject.SetActive(false);

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
                .Set("selectedMark", selected.gameObject)
                .Set("alwaysShowLabel", true);
            frame.gameObject.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + name + ".prefab");
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

        /// <summary>자리에 앉은 두 사람. 용의자 선택과 범인 지목이 같은 구성을 쓴다.</summary>
        private static List<SuspectCard> BuildSuspectCards(RectTransform parent)
        {
            var cardSlots = new (string name, float x0, float x1)[]
            {
                ("Card_0", 0.30f, 0.50f),
                ("Card_1", 0.56f, 0.72f),
            };

            var cards = new List<SuspectCard>();
            foreach (var slot in cardSlots)
            {
                var card = UI(slot.name, parent);
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
                art.gameObject.name = "ArtPlaceholder";   // SuspectCard 가 초상이 있으면 끈다

                var cardView = card.AddComponent<SuspectCard>();
                cardView.Set("button", cardButton)
                        .Set("portrait", portrait)
                        .Set("nameLabel", cardName)
                        .Set("stateLabel", cardState)
                        .Set("highlight", cardOutline)
                        .Set("group", cardGroup);
                cards.Add(cardView);
            }
            return cards;
        }

        // ============================================================ 아트 세트
        // Assets/_Project/Art/ArtSet.asset 에 끼워 둔 스프라이트를 씬을 만들 때 읽어 간다. 비어 있으면 색 판이 남는다.

        private static ArtSet _art;

        private static ArtSet LoadArtSet()
        {
            Ed.EnsureFolder("Assets/_Project/Art");
            var art = Ed.CreateOrLoad<ArtSet>("Assets/_Project/Art", "ArtSet");
            Ed.Save(art);
            return art;
        }

        /// <summary>
        /// 스프라이트가 있으면 판을 그림으로 바꾸고, 아트 세트에 적어 둔 자리(offset · scale · 좌우 반전)를 적용한 뒤
        /// 안내 글자를 지운다. 그림이 없으면 아무것도 하지 않는다 — 색 판이 그대로 남는다.
        /// </summary>
        private static bool Art(Image img, Sprite sprite, ArtSet.Placement place, bool keepColor = false, params Component[] hide)
        {
            if (!Art(img, sprite, place.preserveAspect, keepColor, hide)) return false;
            Place(img.rectTransform, place);
            return true;
        }

        /// <summary>아트 세트의 자리 값을 RectTransform 에 적용한다. 씬을 다시 만들어도 이 값은 아트 세트에 남는다.</summary>
        private static void Place(RectTransform rt, ArtSet.Placement place)
        {
            if (rt == null) return;
            rt.anchoredPosition += place.offset;
            var scale = place.Scale;
            rt.localScale = new Vector3(place.flipX ? -scale : scale, scale, 1f);
        }

        /// <summary>스프라이트가 있으면 판을 그림으로 바꾸고 안내 글자를 지운다. 없으면 그대로 둔다. 바꿨으면 true.</summary>
        private static bool Art(Image img, Sprite sprite, bool preserveAspect = false, bool keepColor = false, params Component[] hide)
        {
            if (img == null || sprite == null) return false;
            img.sprite = sprite;
            if (!keepColor) img.color = Color.white;
            // Sprite Editor 에서 border 를 잡아 두면 9-slice, 아니면 그냥 그림.
            img.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = preserveAspect;
            foreach (var h in hide) if (h != null) h.gameObject.SetActive(false);
            return true;
        }

        /// <summary>글자 버튼을 아이콘 버튼으로. 아이콘이 없으면 글자 그대로.</summary>
        private static void ArtIcon((RectTransform, Button) btn, Sprite icon)
        {
            if (icon == null) return;
            var img = btn.Item1.GetComponent<Image>();
            img.sprite = icon; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
            var label = btn.Item1.Find("Label");
            if (label != null) label.gameObject.SetActive(false);
        }

        /// <summary>PlaceholderCharacterView 의 표정 목록(private 중첩 구조체)을 아트 세트로 채운다.</summary>
        private static object BuildExpressionList(List<ArtSet.ExpressionSprite> source)
        {
            var t = typeof(PlaceholderCharacterView).GetNestedType("ExpressionSprite",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (t == null) return null;
            var listType = typeof(List<>).MakeGenericType(t);
            var list = (IList)System.Activator.CreateInstance(listType);
            if (source == null) return list;
            foreach (var e in source)
            {
                if (e.sprite == null) continue;
                var item = System.Activator.CreateInstance(t);
                t.GetField("expression").SetValue(item, e.expression);
                t.GetField("sprite").SetValue(item, e.sprite);
                t.GetField("tint").SetValue(item, Color.white);
                list.Add(item);
            }
            return list;
        }

        // ============================================================== 헬퍼

        /// <summary>시네마틱 대사창의 버튼 — 어두운 반투명 판에 밝은 글자.</summary>
        private static void StyleCinematicButton((RectTransform, Button) btn)
        {
            var img = btn.Item1.GetComponent<Image>();
            img.color = new Color(0.16f, 0.14f, 0.12f, 0.85f);
            var label = btn.Item1.Find("Label").GetComponent<TMP_Text>();
            label.color = new Color(0.93f, 0.90f, 0.84f, 1f);
            label.characterSpacing = 4f;
            if (btn.Item2 != null)
            {
                var c = btn.Item2.colors;
                c.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
                c.pressedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
                btn.Item2.colors = c;
            }
        }

        /// <summary>아래로 갈수록 어두워지는 세로 그라데이션 스프라이트. 없으면 PNG 로 만들어 둔다.</summary>
        private static Sprite EnsureGradientSprite()
        {
            const string folder = "Assets/_Project/Art/Generated";
            const string path = folder + "/BottomGradient.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Ed.EnsureFolder(folder);
            const int h = 256;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
            for (var y = 0; y < h; y++)
            {
                var t = y / (float)(h - 1);              // 0 = 아래, 1 = 위
                var a = Mathf.Lerp(0.92f, 0f, Mathf.SmoothStep(0f, 1f, t));
                for (var x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(0.04f, 0.035f, 0.03f, a));
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }


        // ---------------------------------------------------------------- 만들어 두는 그림 (아트가 오면 아트 세트가 덮는다)

        /// <summary>흰 그림을 코드로 그려 PNG 스프라이트로 저장한다. 이미 있으면 그것을 쓴다(지우면 다시 그린다).</summary>
        private static Sprite EnsureGeneratedSprite(string file, int w, int h, System.Func<int, int, float> alpha, Vector4 border)
        {
            const string folder = "Assets/_Project/Art/Generated";
            var path = folder + "/" + file;
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Ed.EnsureFolder(folder);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>부드러운 1차원 잡음 — 값은 0..1. 같은 seed 면 같은 붓자국이 나온다.</summary>
        private static float[] SmoothNoise(int length, int seed, int step)
        {
            var rnd = new System.Random(seed);
            var keys = new float[length / step + 3];
            for (var i = 0; i < keys.Length; i++) keys[i] = (float)rnd.NextDouble();
            var outv = new float[length];
            for (var i = 0; i < length; i++)
            {
                var f = i / (float)step;
                var k = Mathf.FloorToInt(f);
                outv[i] = Mathf.Lerp(keys[k], keys[k + 1], Mathf.SmoothStep(0f, 1f, f - k));
            }
            return outv;
        }

        /// <summary>
        /// 이름표 붓자국 — 흰색이라 화자 색으로 물든다(주인공 「당신」 = 보라). 가로로만 늘어나도록 양 끝을 9-slice 로 고정.
        /// </summary>
        private static Sprite EnsureBrushSprite()
        {
            if (_art != null && _art.nameTagBrush != null) return _art.nameTagBrush;
            const int w = 512, h = 128;
            var top = SmoothNoise(w, 11, 9);
            var bottom = SmoothNoise(w, 23, 7);
            var start = SmoothNoise(h, 37, 6);
            var fray = SmoothNoise(h, 41, 3);
            var streak = SmoothNoise(h, 53, 2);
            return EnsureGeneratedSprite("NameTagBrush.png", w, h, (x, y) =>
            {
                var yTop = h * (0.90f - 0.08f * top[x]);
                var yBot = h * (0.10f + 0.08f * bottom[x]);
                var xL = 6f + 26f * start[y];
                var xR = w - 6f - 90f * fray[y] * fray[y];
                var a = Mathf.Clamp01(Mathf.Min(yTop - y, y - yBot) / 1.5f) * Mathf.Clamp01(Mathf.Min(x - xL, xR - x) / 1.5f);
                a *= 0.86f + 0.14f * streak[y];                           // 결
                if (streak[y] < 0.35f && x > xR - 70f) a *= Mathf.Clamp01((xR - x) / 70f);   // 마른 붓 끝
                return a;
            }, new Vector4(56f, 0f, 96f, 0f));
        }

        /// <summary>쓸 수 없는 가설 칸의 빗금.</summary>
        private static Sprite EnsureHatchSprite()
            => EnsureGeneratedSprite("HatchLocked.png", 288, 60, (x, y) =>
            {
                var d = (x + y) % 16;
                var line = d < 5 ? 0.85f : 0f;
                return Mathf.Max(line, 0.22f);
            }, Vector4.zero);

        /// <summary>쓰레기통 아이콘(흰색 — 버튼 색으로 물든다).</summary>
        private static Sprite EnsureTrashSprite()
            => EnsureGeneratedSprite("IconTrash.png", 64, 64, (x, y) =>
            {
                // 뚜껑 + 손잡이
                if (y >= 45 && y <= 50 && x >= 12 && x <= 52) return 1f;
                if (y > 50 && y <= 56 && x >= 25 && x <= 39 && !(y <= 53 && x >= 29 && x <= 35)) return 1f;
                // 몸통 — 아래가 좁은 사다리꼴, 세로 홈 셋
                if (y >= 6 && y <= 42)
                {
                    var k = (y - 6) / 36f;
                    var l = Mathf.Lerp(21f, 17f, k);
                    var r = Mathf.Lerp(43f, 47f, k);
                    if (x < l || x > r) return 0f;
                    if (y >= 12 && y <= 36 && (Mathf.Abs(x - 26) <= 1 || Mathf.Abs(x - 32) <= 1 || Mathf.Abs(x - 38) <= 1)) return 0f;
                    return 1f;
                }
                return 0f;
            }, Vector4.zero);

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
