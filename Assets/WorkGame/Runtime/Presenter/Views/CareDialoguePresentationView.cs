using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WorkGame
{
    // 대화 데이터에는 키만, 씬에는 실제 이미지·소리·연출 연결만 둡니다.
    public sealed class CareDialoguePresentationView : MonoBehaviour
    {
        [Serializable] public sealed class Layer
        {
            public string Key = "event";
            public UnityEngine.UI.Image Image;
        }
        [Serializable] public sealed class Picture { public string Key; public Sprite Sprite; }
        [Serializable] public sealed class Sound { public string Key; public AudioClip Clip; }
        [SerializeField] private List<Layer> layers = new List<Layer>();
        [SerializeField] private List<Picture> pictures = new List<Picture>();
        [SerializeField] private List<Sound> sounds = new List<Sound>();
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private List<CareDialogueCueExtension> extensions = new List<CareDialogueCueExtension>();
        [SerializeField] private CareDialogueCutsceneView cutscene;
        [Tooltip("Custom 명령의 시작 알림. 비동기 완료를 기다리는 연출은 Extensions에 연결합니다.")]
        [SerializeField] private UnityEvent<string> customCue = new UnityEvent<string>();
        private Coroutine routine;
        private bool paused;
        public bool IsPlaying { get; private set; }
        public bool HasCutscene => cutscene != null && cutscene.IsVisible;
        public event Action<bool> BlockingChanged;

        private void Awake() => Clear();
        private void OnDisable() => Clear(true);
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (audioSource != null) { if (value) audioSource.Pause(); else audioSource.UnPause(); }
            foreach (var extension in extensions) if (extension != null) extension.SetPaused(value);
        }

        public void Play(CareDialogueCue[] cues, bool immediate)
        {
            if (cues == null || cues.Length == 0) return;
            if (immediate || !isActiveAndEnabled)
            {
                foreach (var cue in cues) ApplyImmediate(cue);
                return;
            }
            SetPlaying(true);
            // 첫 yield 전 완료되는 명령도 코루틴 핸들을 남기지 않도록 마지막에 정리합니다.
            routine = StartCoroutine(Run(cues));
            if (!IsPlaying) routine = null;
        }

        private IEnumerator Run(CareDialogueCue[] cues)
        {
            try
            {
                foreach (var cue in cues)
                {
                    while (paused) yield return null;
                    if (cue.Kind == CareDialogueCueKind.ShowImage || cue.Kind == CareDialogueCueKind.HideImage)
                    {
                        var image = FindImage(cue.Layer);
                        if (image == null) continue;
                        bool show = cue.Kind == CareDialogueCueKind.ShowImage;
                        if (show && !SetPicture(image, cue.Key)) continue;
                        image.enabled = true;
                        float start = show ? 0 : image.color.a;
                        float end = show ? 1 : 0;
                        float elapsed = 0;
                        SetAlpha(image, start);
                        while (elapsed < cue.Seconds)
                        {
                            if (!paused) elapsed += Time.unscaledDeltaTime;
                            SetAlpha(image, Mathf.Lerp(start, end, cue.Seconds <= 0 ? 1 : elapsed / cue.Seconds));
                            yield return null;
                        }
                        SetAlpha(image, end); image.enabled = show;
                    }
                    else if (cue.Kind == CareDialogueCueKind.Custom)
                    {
                        var extension = extensions.Find(x => x != null && x.CanPlay(cue.Key));
                        customCue.Invoke(cue.Key);
                        if (extension != null) yield return extension.Play(cue);
                        yield return Delay(cue.Seconds);
                    }
                    else
                    {
                        if (cue.Kind == CareDialogueCueKind.Sound)
                        {
                            var sound = sounds.Find(x => x.Key == cue.Key);
                            if (sound?.Clip != null && audioSource != null) audioSource.PlayOneShot(sound.Clip);
                        }
                        yield return Delay(cue.Seconds);
                    }
                }
            }
            finally
            {
                routine = null;
                SetPlaying(false);
            }
        }

        private IEnumerator Delay(float seconds)
        {
            float elapsed = 0;
            while (elapsed < seconds)
            {
                if (!paused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void ApplyImmediate(CareDialogueCue cue)
        {
            if (cue.Kind == CareDialogueCueKind.Custom)
            {
                var extension = extensions.Find(x => x != null && x.CanPlay(cue.Key));
                if (extension != null) extension.ApplyFinal(cue);
                return;
            }
            var image = FindImage(cue.Layer);
            if (image == null) return;
            if (cue.Kind == CareDialogueCueKind.ShowImage && SetPicture(image, cue.Key))
            { SetAlpha(image, 1); image.enabled = true; }
            else if (cue.Kind == CareDialogueCueKind.HideImage) image.enabled = false;
        }

        private UnityEngine.UI.Image FindImage(string key) => layers.Find(x => x.Key == key)?.Image;
        private bool SetPicture(UnityEngine.UI.Image image, string key)
        {
            var picture = pictures.Find(x => x.Key == key);
            if (picture?.Sprite == null) return false;
            image.sprite = picture.Sprite;
            return true;
        }
        private static void SetAlpha(UnityEngine.UI.Image image, float alpha)
        { var color = image.color; color.a = alpha; image.color = color; }
        private void SetPlaying(bool value)
        {
            if (IsPlaying == value) return;
            IsPlaying = value; BlockingChanged?.Invoke(value);
        }
        public void FinishDialogue(Action finish)
        {
            if (!HasCutscene) { finish?.Invoke(); return; }
            SetPlaying(true);
            cutscene.FinishDialogue(finish, () => Clear());
        }

        public void Clear(bool force = false)
        {
            // The dialogue closes under full cover; keep the transition alive until reveal completes.
            if (!force && cutscene != null && cutscene.IsClosing) return;
            if (routine != null) StopCoroutine(routine);
            routine = null;
            foreach (var extension in extensions) if (extension != null) extension.Cancel();
            foreach (var layer in layers) if (layer.Image != null) layer.Image.enabled = false;
            if (audioSource != null) audioSource.Stop();
            SetPlaying(false);
        }
    }
}
