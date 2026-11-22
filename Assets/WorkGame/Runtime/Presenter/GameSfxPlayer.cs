using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace WorkGame
{
    public interface IGameSfxHandle : IDisposable
    {
        bool IsPlaying { get; }
        void SetPaused(bool paused);
    }

    public interface IGameSfxPlayer
    {
        IGameSfxHandle Play(GameSfxClip sound, bool loop = false);
    }

    // Shared by Work now and other SFX/UI later. Every voice routes through the SFX mixer.
    public sealed class GameSfxPlayer : IGameSfxPlayer, IDisposable
    {
        private sealed class Voice
        {
            public AudioSource Source;
            public float Gain;
            public int Version;
            public bool Paused;
            public bool Loop;
        }

        private sealed class Handle : IGameSfxHandle
        {
            private Voice voice;
            private readonly int version;
            public Handle(Voice voice) { this.voice = voice; version = voice.Version; }
            private bool Valid => voice != null && voice.Source != null && voice.Version == version;
            public bool IsPlaying => Valid && (voice.Paused || voice.Source.isPlaying);
            public void SetPaused(bool paused)
            {
                if (!Valid || voice.Paused == paused) return;
                voice.Paused = paused;
                if (paused) voice.Source.Pause(); else voice.Source.UnPause();
            }
            public void Dispose()
            {
                if (Valid)
                {
                    voice.Source.Stop(); voice.Source.clip = null; voice.Paused = false;
                    voice.Version++;
                }
                voice = null;
            }
        }

        private readonly Transform parent;
        private readonly AudioMixerGroup group;
        private readonly List<Voice> voices = new List<Voice>();
        private readonly Dictionary<GameSfxClip, float> lastPlayed = new Dictionary<GameSfxClip, float>();
        private float volume = 1f;
        private bool onMixer, disposed;
        private const int MaxOneShots = 16;
        private const int MaxLoops = 8;

        public GameSfxPlayer(Transform parent, AudioMixerGroup group) { this.parent = parent; this.group = group; }

        public IGameSfxHandle Play(GameSfxClip sound, bool loop = false)
        {
            if (disposed || parent == null || sound?.Clip == null) return null;
            float now = Time.unscaledTime;
            if (!loop && sound.MinInterval > 0f && lastPlayed.TryGetValue(sound, out float previous)
                && now - previous < sound.MinInterval) return null;
            Voice voice = null;
            int count = 0;
            foreach (var candidate in voices)
            {
                if (candidate.Loop != loop) continue;
                count++;
                if (!candidate.Paused && !candidate.Source.isPlaying) { voice = candidate; break; }
            }
            if (voice == null)
            {
                if (count >= (loop ? MaxLoops : MaxOneShots)) return null;
                var owner = new GameObject(loop ? "SFX Loop" : "SFX One Shot");
                owner.transform.SetParent(parent, false);
                var source = owner.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 0f; source.outputAudioMixerGroup = group;
                voice = new Voice { Source = source, Loop = loop };
                voices.Add(voice);
            }
            voice.Version++;
            voice.Gain = Safe(sound.Volume, 0f, 1f);
            voice.Paused = false;
            voice.Source.Stop();
            voice.Source.clip = sound.Clip;
            voice.Source.loop = loop;
            voice.Source.pitch = Safe(sound.Pitch, 0.5f, 2f);
            ApplyVolume(voice);
            voice.Source.Play();
            if (!loop) lastPlayed[sound] = now;
            return new Handle(voice);
        }

        public void SetVolume(float value, bool appliedOnMixer)
        {
            volume = Safe(value, 0f, 1f); onMixer = appliedOnMixer;
            foreach (var voice in voices) if (voice.Source != null) ApplyVolume(voice);
        }

        private void ApplyVolume(Voice voice) => voice.Source.volume = voice.Gain * (onMixer ? (volume > 0f ? 1f : 0f) : volume);
        private static float Safe(float value, float min, float max)
            => float.IsNaN(value) || float.IsInfinity(value) ? min : Mathf.Clamp(value, min, max);

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var voice in voices)
            {
                voice.Version++;
                if (voice.Source == null) continue;
                voice.Source.Stop();
                UnityEngine.Object.Destroy(voice.Source.gameObject);
            }
            voices.Clear(); lastPlayed.Clear();
        }
    }
}
