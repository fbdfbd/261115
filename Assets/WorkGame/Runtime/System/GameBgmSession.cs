using System;
using System.Collections.Generic;

namespace WorkGame
{
    // A scene owns a session. Disposing it invalidates every outstanding override handle.
    // No scene objects or audio playback live here; the latest active override wins.
    public sealed class GameBgmSession : IDisposable
    {
        private sealed class Override : IDisposable
        {
            private GameBgmSession owner;
            public readonly GameBgmTrack Track;
            public Override(GameBgmSession owner, GameBgmTrack track) { this.owner = owner; Track = track; }
            public void Dispose()
            {
                var previous = owner;
                owner = null;
                previous?.Release(this);
            }
        }

        private readonly List<Override> overrides = new List<Override>();
        private GameBgmTrack baseTrack;
        public bool IsDisposed { get; private set; }
        public GameBgmTrack CurrentTrack => IsDisposed ? null
            : overrides.Count > 0 ? overrides[overrides.Count - 1].Track : baseTrack;
        public event Action<GameBgmSession> Changed;

        public void SetBaseTrack(GameBgmTrack track)
        {
            if (IsDisposed || ReferenceEquals(baseTrack, track)) return;
            baseTrack = track;
            Changed?.Invoke(this);
        }

        // Null is an intentional temporary silence. Disposed sessions return an inert handle.
        public IDisposable RequestOverride(GameBgmTrack track)
        {
            var handle = new Override(IsDisposed ? null : this, track);
            if (IsDisposed) return handle;
            overrides.Add(handle);
            Changed?.Invoke(this);
            return handle;
        }

        private void Release(Override handle)
        {
            if (IsDisposed || !overrides.Remove(handle)) return;
            Changed?.Invoke(this);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            foreach (var handle in overrides.ToArray()) handle.Dispose();
            overrides.Clear();
            baseTrack = null;
            Changed?.Invoke(this);
            Changed = null;
        }
    }
}
