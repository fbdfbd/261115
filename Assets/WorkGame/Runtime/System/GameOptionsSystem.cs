using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    public readonly struct GameResolution
    {
        public int Width { get; }
        public int Height { get; }
        public GameResolution(int width, int height) { Width = width; Height = height; }
        public override string ToString() => Width + " x " + Height;
    }

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
        private readonly GameResolution[] resolutions;
        private bool dirty;
        public IReadOnlyList<GameResolution> Resolutions { get; }
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
            int width = Math.Max(1, Screen.width), height = Math.Max(1, Screen.height);
            settings = ReadSettings(width, height);
            resolutions = Screen.resolutions.Select(size => new GameResolution(size.width, size.height))
                .Append(new GameResolution(width, height))
                .Where(size => size.Width > 0 && size.Height > 0)
                .GroupBy(size => (size.Width, size.Height)).Select(group => group.First())
                .OrderBy(size => size.Width).ThenBy(size => size.Height).ToArray();
            Resolutions = Array.AsReadOnly(resolutions);
            ResolutionIndex = Array.FindIndex(resolutions, size => size.Width == settings.Width && size.Height == settings.Height);
            if (ResolutionIndex < 0)
            {
                ResolutionIndex = Array.FindIndex(resolutions, size => size.Width == width && size.Height == height);
                settings.Width = width; settings.Height = height;
            }
            ApplyDisplay();
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
