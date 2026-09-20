using System.Collections.Generic;
using ItIsYou.Characters;
using ItIsYou.Core;
using ItIsYou.Data;
using ItIsYou.Interrogation;
using ItIsYou.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 심문 화면을 통째로 조립한다.
    ///
    /// 손으로 배치하지 않고 스크립트로 짓는 이유는, 레이아웃을 고칠 때마다
    /// 이 메뉴를 다시 실행하면 되기 때문이다. 아트가 들어오기 전 단계에서는
    /// 배치를 자주 갈아엎게 되는데, 그때마다 인스펙터에서 참조를 다시 연결하는 게
    /// 제일 지루하고 실수가 잦은 작업이다.
    ///
    /// 주의: Unity 는 한 GameObject 에 Graphic(Image, TextMeshProUGUI …)을 하나만 허용한다.
    /// 글자에 배경을 깔려면 반드시 배경용 오브젝트를 따로 만들고 글자를 자식으로 넣어야 한다.
    /// </summary>
    public static class InterrogationSceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string ScenePath = SceneFolder + "/Interrogation_Tutorial.unity";

        // 아트 디렉션: 모노톤, 빛바랜 단채색.
        private static readonly Color Bg        = new Color32(0x14, 0x16, 0x1A, 0xFF);
        private static readonly Color Panel     = new Color32(0x1C, 0x20, 0x25, 0xE6);
        private static readonly Color PanelSunk = new Color32(0x11, 0x13, 0x17, 0xF2);
        private static readonly Color Ink       = new Color32(0xDE, 0xD9, 0xCF, 0xFF);
        private static readonly Color InkSoft   = new Color32(0x9A, 0x96, 0x8D, 0xFF);
        private static readonly Color Brass     = new Color32(0xC4, 0xA7, 0x5C, 0xFF);
        private static readonly Color Rule      = new Color32(0x33, 0x38, 0x3D, 0xFF);

        private static TMP_FontAsset _font;

        [MenuItem("It is You/2. 심문 씬 생성", priority = 2)]
        public static void Build()
        {
            _font = FontSetup.Ensure();
            var chapter = TutorialDataBuilder.Build();

            Ed.EnsureFolder(SceneFolder);
            Ed.EnsureFolder(PrefabFolder);

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            var suspect = AssetDatabase.LoadAssetAtPath<SuspectDefinition>(
                "Assets/_Project/Data/Suspects/Suspect_Sepi.asset");

            var choiceButtonPrefab = BuildChoiceButtonPrefab();
            var clueSlotPrefab = BuildClueSlotPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------------------------------------------------------- 카메라
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            // ------------------------------------------------------ 이벤트 시스템
            // 이 프로젝트는 새 Input System 을 쓴다. 구형 StandaloneInputModule 을 붙이면
            // EventSystem 이 매 프레임 예외를 던지고 모든 클릭이 죽는다.
            new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            // --------------------------------------------------------- 캔버스
            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.transform as RectTransform;

            var bg = Img("Background", root, Bg);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            // ------------------------------------------------------- 캐릭터
            var characterGo = UI("Character_Sepi", root);
            var charRt = characterGo.GetComponent<RectTransform>();
            charRt.anchorMin = new Vector2(0.5f, 0f);
            charRt.anchorMax = new Vector2(0.5f, 0f);
            charRt.pivot = new Vector2(0.5f, 0f);
            charRt.anchoredPosition = new Vector2(120f, 250f);
            charRt.sizeDelta = new Vector2(420f, 620f);

            var charImg = characterGo.AddComponent<Image>();
            charImg.color = new Color32(0x3A, 0x40, 0x48, 0xFF);
            var charGroup = characterGo.AddComponent<CanvasGroup>();
            var charOutline = characterGo.AddComponent<Outline>();
            charOutline.effectColor = Brass;
            charOutline.effectDistance = new Vector2(4f, -4f);
            charOutline.enabled = false;

            var charLabel = Text("Label", charRt, "세피\n(플레이스홀더)", 26, InkSoft, TextAlignmentOptions.Center);
            Stretch(charLabel.rectTransform);

            var characterView = characterGo.AddComponent<PlaceholderCharacterView>();
            characterView.Set("characterId", "SEPI")
                         .Set("body", charImg)
                         .Set("reactionOutline", charOutline)
                         .Set("canvasGroup", charGroup);

            var clickTarget = characterGo.AddComponent<CharacterClickTarget>();

            // -------------------------------------------------------- 상단 바
            var topBar = UI("TopBar", root);
            var topRt = topBar.GetComponent<RectTransform>();
            Anchor(topRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -110f), new Vector2(-40f, -40f));

            var suspectLabel = Text("SuspectName", topRt, "세피", 34, Ink, TextAlignmentOptions.Left);
            Anchor(suspectLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), Vector2.zero, Vector2.zero);

            var tokenCaption = Text("TokenCaption", topRt, "남은 시간", 18, InkSoft, TextAlignmentOptions.Right);
            Anchor(tokenCaption.rectTransform, new Vector2(0.6f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var tokenLabel = Text("TokenValue", topRt, "6 / 6", 40, Brass, TextAlignmentOptions.Right);
            Anchor(tokenLabel.rectTransform, new Vector2(0.6f, 0f), new Vector2(1f, 0.55f), Vector2.zero, Vector2.zero);

            // 무반응 게이지 — 실제 게임에서는 초침 소리가 이 역할을 한다.
            var idleBarBg = Img("IdleBar", topRt, Rule);
            Anchor(idleBarBg.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0f, -14f), new Vector2(0f, -8f));
            idleBarBg.raycastTarget = false;
            var idleFill = Img("Fill", idleBarBg.rectTransform, new Color32(0xC0, 0x8A, 0x85, 0xFF));
            Stretch(idleFill.rectTransform);
            idleFill.type = Image.Type.Filled;
            idleFill.fillMethod = Image.FillMethod.Horizontal;
            idleFill.fillAmount = 0f;
            idleFill.raycastTarget = false;
            // Image.Type.Filled 는 스프라이트가 있어야 동작한다. 없으면 fillAmount 가 통째로 무시된다.
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            idleFill.sprite = uiSprite;
            idleBarBg.sprite = uiSprite;

            // ------------------------------------------------------ 선택지 패널
            var choicePanel = Img("ChoicePanel", root, Panel);
            var choiceRt = choicePanel.rectTransform;
            Anchor(choiceRt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(40f, 300f), new Vector2(620f, -140f));
            var choiceGroup = choicePanel.gameObject.AddComponent<CanvasGroup>();

            var choiceTitle = Text("Title", choiceRt, "심 문", 20, Brass, TextAlignmentOptions.Left);
            Anchor(choiceTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -46f), new Vector2(-20f, -16f));

            var choiceList = UI("List", choiceRt);
            var listRt = choiceList.GetComponent<RectTransform>();
            Anchor(listRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(-16f, -56f));
            var vlg = choiceList.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperLeft;

            var choiceListView = choicePanel.gameObject.AddComponent<ChoiceListView>();
            choiceListView.Set("container", listRt)
                          .Set("buttonPrefab", choiceButtonPrefab)
                          .Set("root", choiceGroup);

            // -------------------------------------------------------- 단서 패널
            var cluePanel = Img("CluePanel", root, Panel);
            var clueRt = cluePanel.rectTransform;
            Anchor(clueRt, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-420f, 300f), new Vector2(-40f, -140f));

            var clueTitle = Text("Title", clueRt, "단 서", 20, Brass, TextAlignmentOptions.Left);
            Anchor(clueTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -46f), new Vector2(-20f, -16f));

            var clueGrid = UI("Grid", clueRt);
            var gridRt = clueGrid.GetComponent<RectTransform>();
            Anchor(gridRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -300f), new Vector2(-16f, -56f));
            var glg = clueGrid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(104f, 104f);
            glg.spacing = new Vector2(10f, 10f);
            glg.childAlignment = TextAnchor.UpperLeft;

            var tooltip = Img("Tooltip", clueRt, PanelSunk);
            Anchor(tooltip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 420f));
            var tipTitle = Text("TipTitle", tooltip.rectTransform, "", 22, Ink, TextAlignmentOptions.TopLeft);
            Anchor(tipTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -48f), new Vector2(-16f, -14f));
            var tipBody = Text("TipBody", tooltip.rectTransform, "", 17, InkSoft, TextAlignmentOptions.TopLeft);
            Anchor(tipBody.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 50f), new Vector2(-16f, -54f));
            tipBody.textWrappingMode = TextWrappingModes.Normal;

            var tipClose = Btn("Close", tooltip.rectTransform, "닫기", 16);
            Anchor(tipClose.Item1, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, 10f), new Vector2(60f, 42f));

            var clueTrayView = cluePanel.gameObject.AddComponent<ClueTrayView>();
            clueTrayView.Set("container", gridRt)
                        .Set("slotPrefab", clueSlotPrefab)
                        .Set("tooltipRoot", tooltip.gameObject)
                        .Set("tooltipTitle", tipTitle)
                        .Set("tooltipBody", tipBody);
            tipClose.Item2.onClick.AddListener(clueTrayView.CloseTooltip);
            tooltip.gameObject.SetActive(false);

            // -------------------------------------------------------- 대사창
            var dialoguePanel = Img("DialoguePanel", root, PanelSunk);
            var dlgRt = dialoguePanel.rectTransform;
            Anchor(dlgRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 40f), new Vector2(-40f, 280f));
            var dlgGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();

            var speaker = Text("Speaker", dlgRt, "", 24, Brass, TextAlignmentOptions.Left);
            Anchor(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(28f, -52f), new Vector2(0f, -16f));

            var direction = Text("StageDirection", dlgRt, "", 18, InkSoft, TextAlignmentOptions.Left);
            direction.fontStyle = FontStyles.Italic;
            Anchor(direction.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -84f), new Vector2(-28f, -52f));

            // 본문만 raycastTarget 을 켠다 — 밑줄 구간을 클릭해야 하기 때문이다.
            var body = Text("Body", dlgRt, "", 26, Ink, TextAlignmentOptions.TopLeft);
            Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 44f), new Vector2(-28f, -88f));
            body.textWrappingMode = TextWrappingModes.Normal;

            var advance = Text("AdvanceIndicator", dlgRt, "▼", 20, Brass, TextAlignmentOptions.Right);
            Anchor(advance.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 12f), new Vector2(-24f, 44f));

            var presenter = dialoguePanel.gameObject.AddComponent<DialoguePresenter>();
            presenter.Set("root", dlgGroup)
                     .Set("speakerLabel", speaker)
                     .Set("bodyLabel", body)
                     .Set("stageDirectionLabel", direction)
                     .Set("advanceIndicator", advance.gameObject)
                     .Set("uiCamera", (Camera)null);   // Overlay 캔버스는 카메라가 필요 없다

            // ------------------------------------------------------- 상태 표시
            var status = Text("StatusLabel", root, "", 18, InkSoft, TextAlignmentOptions.BottomLeft);
            Anchor(status.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(44f, 8f), new Vector2(0f, 36f));

            var endBtn = Btn("EndButton", root, "심문 종료", 20);
            Anchor(endBtn.Item1, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-220f, 8f), new Vector2(-40f, 48f));

            // --------------------------------------------------------- 시스템
            var systems = new GameObject("Systems");
            var cursor = systems.AddComponent<CursorController>();
            clickTarget.Set("hitArea", charImg).Set("cursor", cursor);

            var interrogation = systems.AddComponent<InterrogationController>();
            interrogation.Set("database", db)
                         .Set("dialogue", presenter)
                         .Set("choiceList", choiceListView)
                         .Set("cursor", cursor);

            var driver = systems.AddComponent<InterrogationTestDriver>();
            driver.Set("chapter", chapter)
                  .Set("suspect", suspect)
                  .Set("startingClues", new List<ClueDefinition>
                  {
                      AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/Clue_CLUE_RIBBON.asset"),
                      AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/Clue_CLUE_COFFEE.asset"),
                      AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/Clue_CLUE_GLIMPSE.asset")
                  })
                  .Set("interrogation", interrogation)
                  .Set("characterView", characterView)
                  .Set("characterClickTarget", clickTarget)
                  .Set("clueTray", clueTrayView)
                  .Set("dialogue", presenter)
                  .Set("tokenLabel", tokenLabel)
                  .Set("suspectLabel", suspectLabel)
                  .Set("idleFill", idleFill)
                  .Set("statusLabel", status)
                  .Set("endButton", endBtn.Item2);

            // ---------------------------------------------------------- 저장
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[It is You] 심문 씬 생성 완료 — {ScenePath}\n" +
                      "Play 를 눌러 좌측 선택지를 클릭해 보세요.");
        }

        // ============================================================ 프리팹

        private static ChoiceButton BuildChoiceButtonPrefab()
        {
            var go = UI("ChoiceButton", null);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 84f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color32(0x26, 0x2B, 0x31, 0xFF);
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var colors = button.colors;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;

            var group = go.AddComponent<CanvasGroup>();

            // 성향 색 띠 — 친근/강압을 문구를 읽기 전에 색으로 알린다.
            var strip = Img("ToneStrip", rt, Brass);
            Anchor(strip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f));
            strip.raycastTarget = false;

            var label = Text("Label", rt, "선택지", 20, Ink, TextAlignmentOptions.Left);
            Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 8f), new Vector2(-44f, -8f));
            label.textWrappingMode = TextWrappingModes.Normal;

            // 아직 볼 내용이 남았다는 표시.
            var icon = Text("UnseenIcon", rt, "＋", 22, Brass, TextAlignmentOptions.Center);
            Anchor(icon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-38f, -16f), new Vector2(-8f, 16f));

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
            rt.sizeDelta = new Vector2(104f, 104f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color32(0x26, 0x2B, 0x31, 0xFF);
            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var icon = Img("Icon", rt, new Color(1f, 1f, 1f, 0f));
            Anchor(icon.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(-8f, -8f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 호버 이름표 — 배경과 글자는 반드시 별개 오브젝트여야 한다.
            var hoverBg = Img("HoverLabel", rt, new Color(0f, 0f, 0f, 0.82f));
            Anchor(hoverBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-10f, -32f), new Vector2(10f, -2f));
            hoverBg.raycastTarget = false;

            var hoverText = Text("Text", hoverBg.rectTransform, "???", 15, Ink, TextAlignmentOptions.Center);
            Stretch(hoverText.rectTransform);

            // 읽지 않은 단서 표시.
            var unread = Img("UnreadBadge", rt, Brass);
            Anchor(unread.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(-6f, -6f));
            unread.raycastTarget = false;

            // 심문에 제시할 수 있는 단서 표시. 툴팁을 읽은 뒤에만 켜진다.
            var usable = Text("UsableBadge", rt, "!", 20, new Color32(0x8F, 0xB0, 0x98, 0xFF), TextAlignmentOptions.Center);
            Anchor(usable.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -26f), new Vector2(26f, -6f));

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

        // ============================================================ 헬퍼

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

        private static (RectTransform, Button) Btn(string name, Transform parent, string label, float size)
        {
            var go = UI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color32(0x2E, 0x34, 0x3A, 0xFF);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var text = Text("Label", rt, label, size, Ink, TextAlignmentOptions.Center);
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
