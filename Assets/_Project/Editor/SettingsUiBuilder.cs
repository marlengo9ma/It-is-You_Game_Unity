using ItIsYou.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using L = ItIsYou.EditorTools.Layout;
using P = ItIsYou.EditorTools.Layout.Palette;
using T = ItIsYou.EditorTools.Layout.Type;
using K = ItIsYou.EditorTools.UiKit;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 설정 창과 일시 정지 메뉴를 조립한다. 세 씬이 같은 모양을 쓰도록 한 곳에 둔다.
    ///
    /// 컴포넌트는 항상 켜져 있는 호스트(Systems)에 붙이고, 창 자체는 캔버스 아래 별도 오브젝트로 둔다.
    /// 그래야 창이 꺼져 있어도 ESC 입력을 받을 수 있다.
    /// </summary>
    public static class SettingsUiBuilder
    {
        private const float PanelW = 1120f;   // 140u
        private const float PanelH = 720f;    // 90u
        private const float TabW = 224f;      // 28u
        private const float RowH = 64f;       // 8u
        private const float LabelW = 240f;    // 30u
        private const float ControlW = 480f;  // 60u

        // ------------------------------------------------------------ 설정 창

        public static SettingsMenu BuildSettings(RectTransform canvasRoot, GameObject host)
        {
            var root = K.UI("SettingsMenu", canvasRoot);
            K.Stretch(root.GetComponent<RectTransform>());

            var scrim = K.Img("Scrim", root.transform, new Color(0f, 0f, 0f, 0.72f));
            K.Stretch(scrim.rectTransform);

            var panel = K.Img("Panel", root.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            var panelRt = panel.rectTransform;
            K.Anchor(panelRt, K.V(0.5f, 0.5f), K.V(0.5f, 0.5f),
                K.V(-PanelW / 2f, -PanelH / 2f), K.V(PanelW / 2f, PanelH / 2f));

            // 제목
            var title = K.Text("Title", panelRt, "설정", T.Display, P.Ink, TextAlignmentOptions.Left);
            K.Anchor(title.rectTransform, K.V(0, 1), K.V(1, 1), K.V(L.Pad * 2, -L.Unit * 11), K.V(-L.Pad * 2, -L.Pad));

            var rule = K.Img("Rule", panelRt, P.Rule);
            K.Anchor(rule.rectTransform, K.V(0, 1), K.V(1, 1), K.V(L.Pad * 2, -L.Unit * 12), K.V(-L.Pad * 2, -L.Unit * 12 + 2f));
            rule.raycastTarget = false;

            // 탭 열
            var tabs = K.UI("Tabs", panelRt);
            var tabsRt = tabs.GetComponent<RectTransform>();
            K.Anchor(tabsRt, K.V(0, 0), K.V(0, 1), K.V(L.Pad * 2, L.Unit * 12), K.V(L.Pad * 2 + TabW, -L.Unit * 14));
            var tabLayout = tabs.AddComponent<VerticalLayoutGroup>();
            tabLayout.spacing = L.Unit;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = false;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = false;
            tabLayout.childAlignment = TextAnchor.UpperLeft;

            var graphicsTab = Tab(tabsRt, "Tab_Graphics", "그래픽");
            var soundTab = Tab(tabsRt, "Tab_Sound", "사운드");
            var gameTab = Tab(tabsRt, "Tab_Game", "게임");

            // 내용 영역
            var content = K.UI("Content", panelRt);
            var contentRt = content.GetComponent<RectTransform>();
            K.Anchor(contentRt, K.V(0, 0), K.V(1, 1),
                K.V(L.Pad * 2 + TabW + L.Gap * 2, L.Unit * 12), K.V(-L.Pad * 2, -L.Unit * 14));

            // ---- 그래픽 ----
            var gPage = Page(contentRt, "Page_Graphics");
            var displayMode = CyclerRow(gPage, "화면 모드");
            var aspect = CyclerRow(gPage, "화면 비율");
            var resolution = CyclerRow(gPage, "해상도");
            var note = K.Text("Note", gPage, "", T.Caption, P.InkSoft, TextAlignmentOptions.TopLeft);
            note.textWrappingMode = TextWrappingModes.Normal;
            var noteLe = note.gameObject.AddComponent<LayoutElement>();
            noteLe.preferredHeight = 48f;
            var applyRow = K.UI("Row_Apply", gPage);
            var applyLe = applyRow.AddComponent<LayoutElement>();
            applyLe.preferredHeight = RowH;
            var apply = K.Button("Apply", applyRow.transform, "적용", T.Choice);
            K.Anchor(apply.rect, K.V(0, 0.5f), K.V(0, 0.5f), K.V(LabelW, -28f), K.V(LabelW + 192f, 28f));

            // ---- 사운드 ----
            var sPage = Page(contentRt, "Page_Sound");
            var master = SliderRow(sPage, "전체 음량", 0f, 1f);
            var bgm = SliderRow(sPage, "배경 음악", 0f, 1f);
            var sfx = SliderRow(sPage, "효과음", 0f, 1f);
            var voice = SliderRow(sPage, "음성", 0f, 1f);
            var mute = CyclerRow(sPage, "음소거");

            // ---- 게임 ----
            var gmPage = Page(contentRt, "Page_Game");
            var textSpeed = SliderRow(gmPage, "글자 속도", 0.5f, 2f);
            var autoSpeed = SliderRow(gmPage, "자동 넘김 속도", 0.5f, 5f);
            var guide = CyclerRow(gmPage, "가이드 모드 (준비 중)");
            var gmNote = K.Text("Note", gmPage,
                "글자 속도는 평범한 대화에만 적용됩니다. 심문 중의 말하는 속도는 연출이라 고정입니다.\n" +
                "자동 넘김 속도는 AUTO 를 켰을 때 한 줄이 다 나온 뒤 다음 줄까지 기다리는 시간입니다.\n" +
                "가이드 모드는 무반응 판정을 시간 차감 대신 선택지로 바꾸는 모드로, 아직 기획 중입니다.",
                T.Caption, P.InkSoft, TextAlignmentOptions.TopLeft);
            gmNote.textWrappingMode = TextWrappingModes.Normal;
            var gmNoteLe = gmNote.gameObject.AddComponent<LayoutElement>();
            gmNoteLe.preferredHeight = 96f;

            // 하단 버튼
            var defaults = K.Button("Defaults", panelRt, "기본값으로", T.Choice);
            K.Anchor(defaults.rect, K.V(0, 0), K.V(0, 0), K.V(L.Pad * 2, L.Unit * 4), K.V(L.Pad * 2 + TabW, L.Unit * 4 + 56f));

            var close = K.Button("Close", panelRt, "닫기", T.Choice);
            K.Anchor(close.rect, K.V(1, 0), K.V(1, 0), K.V(-L.Pad * 2 - 192f, L.Unit * 4), K.V(-L.Pad * 2, L.Unit * 4 + 56f));

            var menu = host.AddComponent<SettingsMenu>();
            menu.Set("root", root)
                .Set("closeButton", close.button)
                .Set("defaultsButton", defaults.button)
                .Set("graphicsTab", graphicsTab)
                .Set("soundTab", soundTab)
                .Set("gameTab", gameTab)
                .Set("graphicsPage", gPage.gameObject)
                .Set("soundPage", sPage.gameObject)
                .Set("gamePage", gmPage.gameObject)
                .Set("displayMode", displayMode)
                .Set("aspect", aspect)
                .Set("resolution", resolution)
                .Set("applyButton", apply.button)
                .Set("graphicsNote", note)
                .Set("masterSlider", master.slider).Set("masterValue", master.value)
                .Set("bgmSlider", bgm.slider).Set("bgmValue", bgm.value)
                .Set("sfxSlider", sfx.slider).Set("sfxValue", sfx.value)
                .Set("voiceSlider", voice.slider).Set("voiceValue", voice.value)
                .Set("mute", mute)
                .Set("textSpeedSlider", textSpeed.slider).Set("textSpeedValue", textSpeed.value)
                .Set("autoAdvanceSlider", autoSpeed.slider).Set("autoAdvanceValue", autoSpeed.value)
                .Set("guideMode", guide);

            sPage.gameObject.SetActive(false);
            gmPage.gameObject.SetActive(false);
            root.SetActive(false);
            return menu;
        }

        // ------------------------------------------------------------ 일시 정지

        public static PauseMenu BuildPause(RectTransform canvasRoot, GameObject host, SettingsMenu settings)
        {
            var root = K.UI("PauseMenu", canvasRoot);
            K.Stretch(root.GetComponent<RectTransform>());

            var scrim = K.Img("Scrim", root.transform, new Color(0f, 0f, 0f, 0.66f));
            K.Stretch(scrim.rectTransform);

            var panel = K.Img("Panel", root.transform, new Color32(0x16, 0x19, 0x1D, 0xFF));
            var panelRt = panel.rectTransform;
            K.Anchor(panelRt, K.V(0.5f, 0.5f), K.V(0.5f, 0.5f), K.V(-240f, -192f), K.V(240f, 192f));

            var title = K.Text("Title", panelRt, "일시 정지", T.Label, P.Brass, TextAlignmentOptions.Center);
            K.Spaced(title);
            K.Anchor(title.rectTransform, K.V(0, 1), K.V(1, 1), K.V(L.Pad, -L.Unit * 9), K.V(-L.Pad, -L.Unit * 4));

            var list = K.UI("List", panelRt);
            var listRt = list.GetComponent<RectTransform>();
            K.Anchor(listRt, K.V(0, 0), K.V(1, 1), K.V(L.Pad * 2, L.Pad * 2), K.V(-L.Pad * 2, -L.Unit * 12));
            var vlg = list.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = L.Unit * 2;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var resume = MenuButton(listRt, "Resume", "계속하기");
            var open = MenuButton(listRt, "Settings", "설정");
            var toTitle = MenuButton(listRt, "Title", "타이틀로");

            var hint = K.Text("Hint", panelRt, "ESC 로 닫기", T.Caption, P.InkSoft, TextAlignmentOptions.Center);
            K.Anchor(hint.rectTransform, K.V(0, 0), K.V(1, 0), K.V(0, L.Unit), K.V(0, L.Unit * 4));

            var pause = host.AddComponent<PauseMenu>();
            pause.Set("root", root)
                 .Set("resumeButton", resume)
                 .Set("settingsButton", open)
                 .Set("titleButton", toTitle)
                 .Set("settings", settings);

            root.SetActive(false);
            return pause;
        }

        // ============================================================== 헬퍼

        private static Button Tab(Transform parent, string name, string label)
        {
            var made = K.Button(name, parent, label, T.Choice);
            made.rect.sizeDelta = new Vector2(0f, 56f);
            made.label.alignment = TextAlignmentOptions.Left;
            made.label.margin = new Vector4(L.Pad, 0f, 0f, 0f);
            var strip = K.Img("Strip", made.rect, P.Brass);
            K.Anchor(strip.rectTransform, K.V(0, 0), K.V(0, 1), Vector2.zero, K.V(L.Unit * 0.5f, 0));
            strip.raycastTarget = false;
            return made.button;
        }

        private static Button MenuButton(Transform parent, string name, string label)
        {
            var made = K.Button(name, parent, label, T.Body);
            made.rect.sizeDelta = new Vector2(0f, 64f);
            var strip = K.Img("Strip", made.rect, P.Brass);
            K.Anchor(strip.rectTransform, K.V(0, 0), K.V(0, 1), Vector2.zero, K.V(L.Unit * 0.5f, 0));
            strip.raycastTarget = false;
            return made.button;
        }

        private static RectTransform Page(Transform parent, string name)
        {
            var page = K.UI(name, parent);
            var rt = page.GetComponent<RectTransform>();
            K.Stretch(rt);
            var vlg = page.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = L.Unit;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            return rt;
        }

        private static RectTransform Row(Transform parent, string name, string label)
        {
            var row = K.UI(name, parent);
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, RowH);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = RowH;

            var text = K.Text("Label", rt, label, T.Choice, P.Ink, TextAlignmentOptions.Left);
            K.Anchor(text.rectTransform, K.V(0, 0), K.V(0, 1), K.V(0, 0), K.V(LabelW, 0));
            return rt;
        }

        private static OptionCycler CyclerRow(Transform parent, string label)
        {
            var row = Row(parent, "Row_" + label, label);

            var box = K.UI("Cycler", row);
            var boxRt = box.GetComponent<RectTransform>();
            K.Anchor(boxRt, K.V(0, 0.5f), K.V(0, 0.5f), K.V(LabelW, -28f), K.V(LabelW + ControlW, 28f));

            var prev = K.Button("Prev", boxRt, "‹", T.Body);
            K.Anchor(prev.rect, K.V(0, 0), K.V(0, 1), K.V(0, 0), K.V(56f, 0));

            var next = K.Button("Next", boxRt, "›", T.Body);
            K.Anchor(next.rect, K.V(1, 0), K.V(1, 1), K.V(-56f, 0), K.V(0, 0));

            var valueBg = K.Img("ValueBg", boxRt, P.PanelSunk);
            K.Anchor(valueBg.rectTransform, K.V(0, 0), K.V(1, 1), K.V(56f + L.Unit, 0), K.V(-56f - L.Unit, 0));
            valueBg.raycastTarget = false;

            var value = K.Text("Value", valueBg.rectTransform, "", T.Choice, P.Ink, TextAlignmentOptions.Center);
            K.Stretch(value.rectTransform);

            var cycler = box.AddComponent<OptionCycler>();
            cycler.Set("prevButton", prev.button)
                  .Set("nextButton", next.button)
                  .Set("valueLabel", value);
            return cycler;
        }

        private static (Slider slider, TMP_Text value) SliderRow(Transform parent, string label, float min, float max)
        {
            var row = Row(parent, "Row_" + label, label);

            var sliderGo = K.UI("Slider", row);
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            K.Anchor(sliderRt, K.V(0, 0.5f), K.V(0, 0.5f), K.V(LabelW, -14f), K.V(LabelW + ControlW - 96f, 14f));

            var bg = K.Img("Background", sliderRt, P.PanelSunk);
            K.Anchor(bg.rectTransform, K.V(0, 0.5f), K.V(1, 0.5f), K.V(0, -4f), K.V(0, 4f));
            bg.sprite = K.UiSprite;
            bg.raycastTarget = true;

            var fillArea = K.UI("Fill Area", sliderRt);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            K.Anchor(fillAreaRt, K.V(0, 0.5f), K.V(1, 0.5f), K.V(0, -4f), K.V(0, 4f));
            var fill = K.Img("Fill", fillAreaRt, P.Brass);
            K.Stretch(fill.rectTransform);
            fill.sprite = K.UiSprite;
            fill.raycastTarget = false;

            var handleArea = K.UI("Handle Slide Area", sliderRt);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            K.Stretch(handleAreaRt);
            var handle = K.Img("Handle", handleAreaRt, P.Ink);
            handle.rectTransform.sizeDelta = new Vector2(24f, 24f);
            handle.sprite = K.UiSprite;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = max;

            var value = K.Text("Value", row, "", T.Choice, P.Brass, TextAlignmentOptions.Right);
            K.Anchor(value.rectTransform, K.V(0, 0), K.V(0, 1), K.V(LabelW + ControlW - 80f, 0), K.V(LabelW + ControlW, 0));
            return (slider, value);
        }
    }
}
