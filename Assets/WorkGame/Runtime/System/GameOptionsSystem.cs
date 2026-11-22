using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    // 옵션은 플레이 슬롯과 별도로 보관합니다. 소리는 이벤트만 제공하고 재생기는 연결하지 않습니다.
    public sealed class GameOptionsSystem
    {
        private const string SaveKey = "WorkGame.Options";
        [Serializable]
        private sealed class Settings
        {
            public int Version = 1;
            public int Width;
            public int Height;
            public bool Windowed;
            public float Music = 1;
            public float Sound = 1;
        }

        private readonly Settings settings;
        private GameResolution[] resolutions;
        private bool dirty;
        public IReadOnlyList<GameResolution> Resolutions { get; private set; }
        public int ResolutionIndex { get; private set; }
        public bool Windowed => settings.Windowed;
        public float MusicVolume => settings.Music;
        public float SoundVolume => settings.Sound;
        public bool CanChangeDisplay { get; }
        public event Action Changed;
        public event Action<float> MusicChanged;
        public event Action<float> SoundChanged;

        public GameOptionsSystem()
        {
            CanChangeDisplay = Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer
                || Application.isEditor;
            settings = ReadSettings(1920, 1080);
            RefreshDisplay(false);
            ApplyDisplay();
        }

        public void RefreshDisplay() => RefreshDisplay(true);
        private void RefreshDisplay(bool apply)
        {
            var display = Screen.mainWindowDisplayInfo;
            var current = Screen.currentResolution;
            int width = display.width > 0 ? display.width : current.width;
            int height = display.height > 0 ? display.height : current.height;
            var supported = GameDisplayPolicy.GetResolutions(width, height);
            bool listChanged = resolutions == null || !resolutions.SequenceEqual(supported);
            resolutions = supported;
            Resolutions = Array.AsReadOnly(resolutions);
            ResolutionIndex = GameDisplayPolicy.GetResolutionIndex(resolutions, settings.Width, settings.Height);
            var selected = resolutions[ResolutionIndex];
            bool changed = settings.Width != selected.Width || settings.Height != selected.Height;
            settings.Width = selected.Width; settings.Height = selected.Height;
            if (changed) dirty = true;
            if (apply && changed) ApplyDisplay();
            if (apply && (changed || listChanged)) Changed?.Invoke();
        }

        public void SetResolution(int index)
        {
            if (!CanChangeDisplay || index < 0 || index >= resolutions.Length || ResolutionIndex == index) return;
            ResolutionIndex = index;
            settings.Width = resolutions[index].Width; settings.Height = resolutions[index].Height;
            dirty = true; ApplyDisplay(); Changed?.Invoke();
        }
        public void SetWindowed(bool windowed)
        {
            if (!CanChangeDisplay || settings.Windowed == windowed) return;
            settings.Windowed = windowed;
            dirty = true; ApplyDisplay(); Changed?.Invoke();
        }
        public void SetMusic(float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume)) return;
            volume = Mathf.Clamp01(volume);
            if (settings.Music == volume) return;
            settings.Music = volume; dirty = true;
            MusicChanged?.Invoke(volume); Changed?.Invoke();
        }
        public void SetSound(float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume)) return;
            volume = Mathf.Clamp01(volume);
            if (settings.Sound == volume) return;
            settings.Sound = volume; dirty = true;
            SoundChanged?.Invoke(volume); Changed?.Invoke();
        }

        // 슬라이더를 움직이는 매 프레임에는 쓰지 않고, 창을 닫거나 앱이 중단될 때 저장합니다.
        public void Save()
        {
            if (!dirty) return;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(settings));
            PlayerPrefs.Save(); dirty = false;
        }
        private void ApplyDisplay()
        {
            if (CanChangeDisplay) Screen.SetResolution(settings.Width, settings.Height,
                settings.Windowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow);
        }
        private static Settings ReadSettings(int width, int height)
        {
            try
            {
                string json = PlayerPrefs.GetString(SaveKey, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<Settings>(json);
                    if (data != null && data.Version == 1 && data.Width > 0 && data.Height > 0
                        && !float.IsNaN(data.Music) && !float.IsInfinity(data.Music)
                        && !float.IsNaN(data.Sound) && !float.IsInfinity(data.Sound))
                    {
                        data.Music = Mathf.Clamp01(data.Music); data.Sound = Mathf.Clamp01(data.Sound);
                        return data;
                    }
                }
            }
            catch (ArgumentException) { }
            return new Settings { Width = width, Height = height, Windowed = Screen.fullScreenMode == FullScreenMode.Windowed };
        }
    }
}
