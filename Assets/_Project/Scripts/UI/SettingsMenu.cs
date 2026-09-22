using System;
using System.Collections.Generic;
using ItIsYou.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 설정 창. 그래픽 · 사운드 · 게임 세 탭.
    ///
    /// 그래픽은 '적용'을 눌러야 화면에 반영된다 — 해상도가 바뀌는 순간 창이 흔들리므로
    /// 슬라이더처럼 즉시 반영하면 사고가 난다. 사운드와 게임은 만지는 즉시 반영되고,
    /// 창을 닫을 때 저장된다.
    /// </summary>
    [AddComponentMenu("It is You/Settings Menu")]
    public class SettingsMenu : MonoBehaviour
    {
        [Header("창")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [Tooltip("창 밖에서 설정을 여는 버튼(우측 패널의 톱니). 비워 둬도 된다.")]
        [SerializeField] private List<Button> openButtons = new List<Button>();
        [SerializeField] private Button defaultsButton;

        [Header("탭")]
        [SerializeField] private Button graphicsTab;
        [SerializeField] private Button soundTab;
        [SerializeField] private Button gameTab;
        [SerializeField] private GameObject graphicsPage;
        [SerializeField] private GameObject soundPage;
        [SerializeField] private GameObject gamePage;

        [Header("그래픽")]
        [SerializeField] private OptionCycler displayMode;
        [SerializeField] private OptionCycler aspect;
        [SerializeField] private OptionCycler resolution;
        [SerializeField] private Button applyButton;
        [SerializeField] private TMP_Text graphicsNote;

        [Header("사운드")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider voiceSlider;
        [SerializeField] private TMP_Text masterValue;
        [SerializeField] private TMP_Text bgmValue;
        [SerializeField] private TMP_Text sfxValue;
        [SerializeField] private TMP_Text voiceValue;
        [SerializeField] private OptionCycler mute;

        [Header("게임")]
        [SerializeField] private Slider textSpeedSlider;
        [SerializeField] private TMP_Text textSpeedValue;
        [SerializeField] private Slider autoAdvanceSlider;
        [SerializeField] private TMP_Text autoAdvanceValue;
        [SerializeField] private OptionCycler guideMode;

        [Header("탭 강조색")]
        [SerializeField] private Color tabActive = new Color32(0xC4, 0xA7, 0x5C, 0xFF);
        [SerializeField] private Color tabIdle = new Color32(0x9A, 0x96, 0x8D, 0xFF);

        private static readonly string[] DisplayModeNames = { "전체 화면", "창 모드" };
        private static readonly string[] AspectNames = { "16 : 9", "4 : 3 (임시)" };
        private static readonly string[] OnOff = { "꺼짐", "켜짐" };

        private List<Vector2Int> _resolutions = new List<Vector2Int>();
        private bool _wired;
        private bool _open;
        private bool _suppress;

        /// <summary>창이 열렸다 / 닫혔다.</summary>
        public event Action<bool> OpenChanged;

        public bool IsOpen => _open;

        private void Awake()
        {
            Wire();
            if (!_open && root != null && root != gameObject) root.SetActive(false);
        }

        private void Update()
        {
            if (!_open) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            for (int i = 0; i < openButtons.Count; i++) if (openButtons[i] != null) openButtons[i].onClick.AddListener(Open);
            if (defaultsButton != null) defaultsButton.onClick.AddListener(OnDefaults);
            if (graphicsTab != null) graphicsTab.onClick.AddListener(() => ShowTab(0));
            if (soundTab != null) soundTab.onClick.AddListener(() => ShowTab(1));
            if (gameTab != null) gameTab.onClick.AddListener(() => ShowTab(2));
            if (applyButton != null) applyButton.onClick.AddListener(OnApplyGraphics);

            if (displayMode != null) displayMode.Changed += _ => RefreshGraphicsNote();
            if (aspect != null) aspect.Changed += OnAspectChanged;

            Hook(masterSlider, v => { GameSettings.MasterVolume = v; GameSettings.ApplyAudio(); Percent(masterValue, v); });
            Hook(bgmSlider, v => { GameSettings.BgmVolume = v; GameSettings.ApplyAudio(); Percent(bgmValue, v); });
            Hook(sfxSlider, v => { GameSettings.SfxVolume = v; GameSettings.ApplyAudio(); Percent(sfxValue, v); });
            Hook(voiceSlider, v => { GameSettings.VoiceVolume = v; GameSettings.ApplyAudio(); Percent(voiceValue, v); });
            if (mute != null) mute.Changed += i => { if (_suppress) return; GameSettings.Muted = i == 1; GameSettings.ApplyAudio(); };

            Hook(textSpeedSlider, v => { GameSettings.TextSpeed = v; if (textSpeedValue != null) textSpeedValue.text = $"{v:0.0}×"; });
            Hook(autoAdvanceSlider, v => { GameSettings.AutoAdvanceSeconds = v; if (autoAdvanceValue != null) autoAdvanceValue.text = $"{v:0.0}초"; });
            if (guideMode != null) guideMode.Changed += i => { if (_suppress) return; GameSettings.GuideMode = i == 1; };
        }

        private void Hook(Slider slider, Action<float> onChange)
        {
            if (slider == null) return;
            slider.onValueChanged.AddListener(v => { if (!_suppress) onChange(v); });
        }

        // ---------------------------------------------------------------- 열고 닫기

        public void Open()
        {
            Wire();
            if (!GameSettings.Loaded) GameSettings.Load();

            _open = true;
            if (root != null) root.SetActive(true);
            Populate();
            ShowTab(0);
            OpenChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            GameSettings.Save();
            if (root != null) root.SetActive(false);
            OpenChanged?.Invoke(false);
        }

        private void ShowTab(int index)
        {
            if (graphicsPage != null) graphicsPage.SetActive(index == 0);
            if (soundPage != null) soundPage.SetActive(index == 1);
            if (gamePage != null) gamePage.SetActive(index == 2);
            Tint(graphicsTab, index == 0);
            Tint(soundTab, index == 1);
            Tint(gameTab, index == 2);
        }

        private void Tint(Button tab, bool active)
        {
            if (tab == null) return;
            var label = tab.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = active ? tabActive : tabIdle;
        }

        // ---------------------------------------------------------------- 값 채우기

        private void Populate()
        {
            _suppress = true;

            if (displayMode != null) displayMode.SetOptions(DisplayModeNames, (int)GameSettings.DisplayMode);
            if (aspect != null) aspect.SetOptions(AspectNames, (int)GameSettings.Aspect);
            PopulateResolutions(GameSettings.Aspect, new Vector2Int(GameSettings.Width, GameSettings.Height));

            Set(masterSlider, GameSettings.MasterVolume); Percent(masterValue, GameSettings.MasterVolume);
            Set(bgmSlider, GameSettings.BgmVolume); Percent(bgmValue, GameSettings.BgmVolume);
            Set(sfxSlider, GameSettings.SfxVolume); Percent(sfxValue, GameSettings.SfxVolume);
            Set(voiceSlider, GameSettings.VoiceVolume); Percent(voiceValue, GameSettings.VoiceVolume);
            if (mute != null) mute.SetOptions(OnOff, GameSettings.Muted ? 1 : 0);

            Set(textSpeedSlider, GameSettings.TextSpeed);
            Set(autoAdvanceSlider, GameSettings.AutoAdvanceSeconds);
            if (autoAdvanceValue != null) autoAdvanceValue.text = $"{GameSettings.AutoAdvanceSeconds:0.0}초";
            if (textSpeedValue != null) textSpeedValue.text = $"{GameSettings.TextSpeed:0.0}×";
            if (guideMode != null) guideMode.SetOptions(OnOff, GameSettings.GuideMode ? 1 : 0);

            _suppress = false;
            RefreshGraphicsNote();
        }

        private void PopulateResolutions(AspectOption which, Vector2Int prefer)
        {
            var saved = GameSettings.Aspect;
            GameSettings.Aspect = which;
            _resolutions = GameSettings.AvailableResolutions();
            GameSettings.Aspect = saved;

            var labels = new List<string>();
            var index = -1;
            for (int i = 0; i < _resolutions.Count; i++)
            {
                labels.Add(GameSettings.ResolutionLabel(_resolutions[i]));
                if (_resolutions[i] == prefer) index = i;
            }
            if (index < 0)
            {
                var closest = GameSettings.ClosestPreset(_resolutions.ToArray(), prefer);
                index = Mathf.Max(0, _resolutions.IndexOf(closest));
            }
            if (resolution != null) resolution.SetOptions(labels, index);
        }

        private void OnAspectChanged(int index)
        {
            if (_suppress) return;
            var which = (AspectOption)index;
            var prefer = which == AspectOption.Classic4x3 ? new Vector2Int(1280, 960) : new Vector2Int(1920, 1080);
            PopulateResolutions(which, prefer);
            RefreshGraphicsNote();
        }

        private void RefreshGraphicsNote()
        {
            if (graphicsNote == null) return;
            var fullscreen = displayMode != null && displayMode.Index == (int)DisplayModeOption.Fullscreen;
            var wide = aspect == null || aspect.Index == (int)AspectOption.Wide16x9;
            graphicsNote.text = fullscreen && wide
                ? "전체 화면(16:9)은 모니터 해상도로 그려집니다. 해상도 선택은 창 모드에서 적용됩니다."
                : "적용을 누르면 화면이 잠시 깜빡일 수 있습니다.";
        }

        private void OnApplyGraphics()
        {
            if (displayMode != null) GameSettings.DisplayMode = (DisplayModeOption)displayMode.Index;
            if (aspect != null) GameSettings.Aspect = (AspectOption)aspect.Index;
            if (resolution != null && _resolutions.Count > 0)
            {
                var r = _resolutions[Mathf.Clamp(resolution.Index, 0, _resolutions.Count - 1)];
                GameSettings.Width = r.x;
                GameSettings.Height = r.y;
            }
            GameSettings.Save();
            GameSettings.ApplyDisplay();
        }

        private void OnDefaults()
        {
            GameSettings.ResetToDefaults();
            GameSettings.ApplyAudio();
            Populate();
        }

        private static void Set(Slider s, float v)
        {
            if (s != null) s.value = v;
        }

        private static void Percent(TMP_Text t, float v)
        {
            if (t != null) t.text = $"{Mathf.RoundToInt(v * 100f)}%";
        }
    }
}
