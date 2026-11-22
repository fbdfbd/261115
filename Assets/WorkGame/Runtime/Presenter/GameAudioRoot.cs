using UnityEngine;
using UnityEngine.Audio;

namespace WorkGame
{
    // Playback only. Scene roots own all subscriptions to gameplay and options.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class GameAudioRoot : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private string musicParameter = "MusicVol";
        [SerializeField] private string sfxParameter = "SFXVol";
        [Tooltip("SFX 전체 출력 보정(dB). 옵션 슬라이더의 비율과 음소거는 유지합니다.")]
        [SerializeField, Range(0f, 12f)] private float sfxBoostDb = 3f;
        private static GameAudioRoot instance;
        private AudioSource source;
        private float gain, from, to;
        private GameBgmSession session;
        private AudioClip desiredClip;
        private float desiredGain;
        private float fadeSeconds = 1.5f;
        private float elapsed, duration;
        private float musicVolume = 1f, soundVolume = 1f;
        private bool requestDirty, volumesDirty = true, musicOnMixer, fadingOut;
        private bool warnedMusic, warnedSound;
        private GameSfxPlayer effects;
        public IGameSfxPlayer Effects => effects;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => instance = null;

        public static GameAudioRoot GetOrCreate(GameAudioRoot prefab)
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<GameAudioRoot>();
            if (instance != null) return instance;
            if (prefab == null || !prefab.gameObject.activeSelf || !prefab.enabled) return null;
            return Instantiate(prefab);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            // This component belongs on a dedicated root prefab, never on a Canvas or WorkRoot.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            // One voice guarantees that songs never overlap, including interrupted transitions.
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.outputAudioMixerGroup = musicGroup;
            effects = new GameSfxPlayer(transform, sfxGroup);
        }

        public GameBgmSession BeginSession(float transitionSeconds)
        {
            if (session != null)
            {
                session.Changed -= OnSessionChanged;
                session.Dispose();
            }
            fadeSeconds = SafeGain(transitionSeconds, 0f, float.MaxValue);
            session = new GameBgmSession();
            session.Changed += OnSessionChanged;
            requestDirty = true;
            return session;
        }

        public void SetMusicVolume(float value) { musicVolume = SafeGain(value); volumesDirty = true; }
        public void SetSoundVolume(float value) { soundVolume = SafeGain(value); volumesDirty = true; }

        private void OnSessionChanged(GameBgmSession sender)
        {
            if (ReferenceEquals(session, sender)) requestDirty = true;
        }

        private void OnValidate() => volumesDirty = true;

        private void LateUpdate()
        {
            // Start/OnEnable ordering cannot apply mixer parameters in Awake accidentally.
            if (volumesDirty)
            {
                volumesDirty = false;
                musicOnMixer = ApplyMixer(musicGroup, musicParameter, musicVolume, ref warnedMusic);
                bool sfxOnMixer = ApplyMixer(sfxGroup, sfxParameter, soundVolume, ref warnedSound, sfxBoostDb);
                effects.SetVolume(soundVolume, sfxOnMixer);
            }
            if (requestDirty)
            {
                requestDirty = false;
                var track = session?.CurrentTrack;
                var clip = track?.Clip;
                float gain = clip != null ? SafeGain(track.Volume) : 0f;
                if (clip != desiredClip || gain != desiredGain)
                {
                    desiredClip = clip;
                    desiredGain = gain;
                    Retarget();
                }
            }
            elapsed += Time.unscaledDeltaTime;
            float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            gain = Mathf.Lerp(from, to, progress);
            if (progress >= 1f)
            {
                if (fadingOut)
                {
                    StopCurrent();
                    StartDesiredClip();
                }
                else if (to == 0f && source.clip != null) StopCurrent();
            }
            source.volume = gain * (musicOnMixer ? (musicVolume > 0f ? 1f : 0f) : musicVolume);
        }

        private void Retarget()
        {
            if (fadeSeconds <= 0f)
            {
                if (source.isPlaying && source.clip == desiredClip) StartFade(desiredGain, false);
                else { StopCurrent(); StartDesiredClip(); }
                return;
            }
            // While fading out, only replace the destination. Do not prolong the fade or start it early.
            if (fadingOut) return;
            if (source.isPlaying && source.clip == desiredClip) StartFade(desiredGain, false);
            else if (source.isPlaying && gain > 0f) StartFade(0f, true);
            else { StopCurrent(); StartDesiredClip(); }
        }

        private void StartDesiredClip()
        {
            if (desiredClip == null || desiredGain == 0f) { StartFade(0f, false); return; }
            // The old clip has already stopped at zero gain before this assignment and Play.
            source.clip = desiredClip;
            source.volume = 0f;
            source.Play();
            StartFade(desiredGain, false);
        }

        private void StartFade(float target, bool fadeOut)
        {
            from = gain;
            to = target;
            elapsed = 0f;
            duration = fadeSeconds;
            fadingOut = fadeOut;
            if (duration <= 0f) gain = target;
        }

        private void StopCurrent()
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            gain = 0f;
            fadingOut = false;
        }

        private bool ApplyMixer(AudioMixerGroup group, string parameter, float value, ref bool warned, float boostDb = 0f)
        {
            if (group != null && !string.IsNullOrEmpty(parameter)
                && group.audioMixer.SetFloat(parameter, value > 0f
                    ? Mathf.Max(-80f, 20f * Mathf.Log10(value) + SafeGain(boostDb, 0f, 12f)) : -80f))
                return true;
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("AudioMixer 그룹/노출 파라미터를 확인하세요: " + parameter
                    + " (해당 음량은 AudioSource에서 대신 적용합니다.)", this);
            }
            return false;
        }

        private static float SafeGain(float value, float min = 0f, float max = 1f)
            => float.IsNaN(value) || float.IsInfinity(value) ? min : Mathf.Clamp(value, min, max);

        private void OnDestroy()
        {
            if (instance != this) return;
            if (session != null) { session.Changed -= OnSessionChanged; session.Dispose(); }
            effects?.Dispose();
            instance = null;
        }
    }
}
