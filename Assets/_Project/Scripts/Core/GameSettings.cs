using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>화면 모드. 게임에서 흔히 쓰는 두 가지만 둔다.</summary>
    public enum DisplayModeOption
    {
        /// <summary>전체 화면. 해상도 전환이 없는 테두리 없는 전체 창 방식이라 알트탭이 가볍다.</summary>
        Fullscreen = 0,
        Windowed = 1,
    }

    /// <summary>해상도 목록을 거르는 화면 비율.</summary>
    public enum AspectOption
    {
        Wide16x9 = 0,
        Classic4x3 = 1,   // 임시
    }

    /// <summary>
    /// 플레이어가 바꿀 수 있는 설정 전부. PlayerPrefs 에 저장되고 실행 시 먼저 적용된다.
    ///
    /// 값을 바꾸는 것과 화면·소리에 적용하는 것을 나눠 두었다.
    /// 설정 창에서는 값만 만지다가 '적용'을 눌렀을 때 한 번에 반영하기 위해서다.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyDisplayMode = "settings.displayMode";
        private const string KeyWidth = "settings.width";
        private const string KeyHeight = "settings.height";
        private const string KeyAspect = "settings.aspect";
        private const string KeyMaster = "settings.volume.master";
        private const string KeyBgm = "settings.volume.bgm";
        private const string KeySfx = "settings.volume.sfx";
        private const string KeyVoice = "settings.volume.voice";
        private const string KeyMuted = "settings.muted";
        private const string KeyTextSpeed = "settings.textSpeed";
        private const string KeyGuideMode = "settings.guideMode";

        /// <summary>16:9 후보. 실제 모니터보다 큰 것은 목록에서 빠진다.</summary>
        public static readonly Vector2Int[] Wide = new[]
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160),
        };

        /// <summary>4:3 후보 (임시).</summary>
        public static readonly Vector2Int[] Classic = new[]
        {
            new Vector2Int(1024, 768),
            new Vector2Int(1280, 960),
            new Vector2Int(1600, 1200),
        };

        public static DisplayModeOption DisplayMode { get; set; } = DisplayModeOption.Fullscreen;
        public static AspectOption Aspect { get; set; } = AspectOption.Wide16x9;
        public static int Width { get; set; } = 1920;
        public static int Height { get; set; } = 1080;

        public static float MasterVolume { get; set; } = 0.8f;
        public static float BgmVolume { get; set; } = 0.8f;
        public static float SfxVolume { get; set; } = 0.8f;
        public static float VoiceVolume { get; set; } = 1f;
        public static bool Muted { get; set; }

        /// <summary>평범한 대화의 글자 속도 배율. 심문의 '말하는 속도'에는 적용하지 않는다.</summary>
        public static float TextSpeed { get; set; } = 1f;

        /// <summary>가이드 모드 (기획 중). 지금은 값만 저장해 둔다.</summary>
        public static bool GuideMode { get; set; }

        public static bool Loaded { get; private set; }

        /// <summary>소리 값이 바뀌었다. AudioSource 들이 이걸 듣고 볼륨을 다시 계산한다.</summary>
        public static event Action AudioChanged;

        /// <summary>표시 값이 바뀌어 적용됐다.</summary>
        public static event Action DisplayChanged;

        // ---------------------------------------------------------------- 저장

        public static void Load()
        {
            DisplayMode = (DisplayModeOption)PlayerPrefs.GetInt(KeyDisplayMode, (int)DisplayModeOption.Fullscreen);
            Aspect = (AspectOption)PlayerPrefs.GetInt(KeyAspect, (int)AspectOption.Wide16x9);

            var native = NativeResolution();
            var defaultRes = ClosestPreset(Wide, native);
            Width = PlayerPrefs.GetInt(KeyWidth, defaultRes.x);
            Height = PlayerPrefs.GetInt(KeyHeight, defaultRes.y);

            MasterVolume = PlayerPrefs.GetFloat(KeyMaster, 0.8f);
            BgmVolume = PlayerPrefs.GetFloat(KeyBgm, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.8f);
            VoiceVolume = PlayerPrefs.GetFloat(KeyVoice, 1f);
            Muted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;
            TextSpeed = PlayerPrefs.GetFloat(KeyTextSpeed, 1f);
            GuideMode = PlayerPrefs.GetInt(KeyGuideMode, 0) == 1;
            Loaded = true;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(KeyDisplayMode, (int)DisplayMode);
            PlayerPrefs.SetInt(KeyAspect, (int)Aspect);
            PlayerPrefs.SetInt(KeyWidth, Width);
            PlayerPrefs.SetInt(KeyHeight, Height);
            PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
            PlayerPrefs.SetFloat(KeyBgm, BgmVolume);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            PlayerPrefs.SetFloat(KeyVoice, VoiceVolume);
            PlayerPrefs.SetInt(KeyMuted, Muted ? 1 : 0);
            PlayerPrefs.SetFloat(KeyTextSpeed, TextSpeed);
            PlayerPrefs.SetInt(KeyGuideMode, GuideMode ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            DisplayMode = DisplayModeOption.Fullscreen;
            Aspect = AspectOption.Wide16x9;
            var def = ClosestPreset(Wide, NativeResolution());
            Width = def.x;
            Height = def.y;
            MasterVolume = 0.8f;
            BgmVolume = 0.8f;
            SfxVolume = 0.8f;
            VoiceVolume = 1f;
            Muted = false;
            TextSpeed = 1f;
            GuideMode = false;
        }

        // ---------------------------------------------------------------- 적용

        /// <summary>창 모드와 해상도를 실제 화면에 적용한다.</summary>
        public static void ApplyDisplay()
        {
            var mode = DisplayMode == DisplayModeOption.Windowed
                ? FullScreenMode.Windowed
                : FullScreenMode.FullScreenWindow;

            var native = NativeResolution();
            var w = Mathf.Clamp(Width, 640, Mathf.Max(640, native.x));
            var h = Mathf.Clamp(Height, 480, Mathf.Max(480, native.y));

            // 전체 화면은 모니터 크기로 그린다 — 다른 크기를 넣으면 드라이버가 스케일링을 붙여 흐려진다.
            if (mode == FullScreenMode.FullScreenWindow && Aspect == AspectOption.Wide16x9)
            {
                w = native.x;
                h = native.y;
            }

            Screen.SetResolution(w, h, mode);
            DisplayChanged?.Invoke();
        }

        /// <summary>전체 볼륨을 적용한다. 채널별 볼륨은 <see cref="AudioChanged"/> 를 듣는 쪽이 계산한다.</summary>
        public static void ApplyAudio()
        {
            AudioListener.volume = Muted ? 0f : Mathf.Clamp01(MasterVolume);
            AudioChanged?.Invoke();
        }

        /// <summary>채널 볼륨. AudioSource 의 기본 볼륨에 곱해 쓴다.</summary>
        public static float ChannelVolume(SoundChannelKind kind) => kind switch
        {
            SoundChannelKind.Bgm => Mathf.Clamp01(BgmVolume),
            SoundChannelKind.Sfx => Mathf.Clamp01(SfxVolume),
            SoundChannelKind.Voice => Mathf.Clamp01(VoiceVolume),
            _ => 1f
        };

        // ---------------------------------------------------------------- 해상도

        /// <summary>지금 비율에서 고를 수 있는 해상도. 모니터보다 큰 것은 뺀다.</summary>
        public static List<Vector2Int> AvailableResolutions()
        {
            var source = Aspect == AspectOption.Classic4x3 ? Classic : Wide;
            var native = NativeResolution();
            var list = new List<Vector2Int>();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].x <= native.x && source[i].y <= native.y) list.Add(source[i]);
            }
            // 모니터가 아주 작아도 최소 하나는 남긴다.
            if (list.Count == 0) list.Add(source[0]);
            return list;
        }

        public static Vector2Int NativeResolution()
        {
#if UNITY_EDITOR
            return new Vector2Int(3840, 2160);
#else
            var d = Display.main;
            if (d != null && d.systemWidth > 0 && d.systemHeight > 0)
                return new Vector2Int(d.systemWidth, d.systemHeight);
            var r = Screen.currentResolution;
            return new Vector2Int(r.width, r.height);
#endif
        }

        public static Vector2Int ClosestPreset(Vector2Int[] presets, Vector2Int target)
        {
            var best = presets[0];
            var bestDiff = int.MaxValue;
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i].x > target.x || presets[i].y > target.y) continue;
                var diff = Mathf.Abs(presets[i].x - target.x) + Mathf.Abs(presets[i].y - target.y);
                if (diff < bestDiff) { bestDiff = diff; best = presets[i]; }
            }
            return best;
        }

        public static string ResolutionLabel(Vector2Int r) => $"{r.x} × {r.y}";
    }

    public enum SoundChannelKind
    {
        Master = 0,
        Bgm = 1,
        Sfx = 2,
        Voice = 3,
    }
}
