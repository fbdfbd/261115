using System;
using UnityEngine;

namespace WorkGame
{
    // A run owns this subscription/handle; actual voices and option volume use the shared SFX player.
    public sealed class CharacterSfxPresenter : IDisposable
    {
        private readonly CharacterPettingView view;
        private readonly CharacterSfxSO sounds;
        private readonly IGameSfxPlayer player;
        private readonly Func<float> clock;
        private readonly Func<int, int> choose;
        private IGameSfxHandle current;
        private float lastPlayed = float.NegativeInfinity;
        private bool disposed;

        public CharacterSfxPresenter(CharacterPettingView view, CharacterSfxSO sounds, IGameSfxPlayer player,
            Func<float> clock = null, Func<int, int> choose = null)
        {
            this.view = view; this.sounds = sounds; this.player = player;
            this.clock = clock ?? (() => Time.unscaledTime);
            this.choose = choose ?? (count => UnityEngine.Random.Range(0, count));
            if (view != null) view.Petted += OnPetted;
        }

        private void OnPetted()
        {
            if (disposed || view == null || !view.IsPettable || sounds == null) return;
            float now = clock();
            if ((current != null && current.IsPlaying) || now - lastPlayed < sounds.PettingMinInterval) return;
            int count = sounds.PettingCount;
            if (count == 0) return;
            var sound = sounds.GetPetting(choose(count));
            if (sound == null) return;
            current?.Dispose();
            current = player.Play(sound);
            if (current != null) lastPlayed = now;
        }

        public void Tick()
        {
            if (disposed || current == null) return;
            if (view == null || !view.IsPettable || !current.IsPlaying)
            {
                current.Dispose(); current = null;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (!ReferenceEquals(view, null)) view.Petted -= OnPetted;
            current?.Dispose(); current = null;
        }
    }
}
