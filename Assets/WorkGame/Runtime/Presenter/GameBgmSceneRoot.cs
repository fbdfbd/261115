using System;
using UnityEngine;

namespace WorkGame
{
    public enum GameBgmScene { Title, Game }

    [DisallowMultipleComponent]
    public sealed class GameBgmSceneRoot : MonoBehaviour
    {
        [SerializeField] private GameBgmScene scene = GameBgmScene.Game;
        [SerializeField] private GameBgmSO music;
        [SerializeField] private GameAudioRoot audioPrefab;
        [SerializeField] private GameOptionsRoot optionsRoot;
        [SerializeField] private WorkRoot workRoot;
        private GameAudioRoot audioRoot;
        private GameBgmSession session;
        private WorkRoot boundRoot;
        private GameCollectionRoot boundCollection;
        private WorkSystem work;
        private CareFlowSystem careFlow;
        private GameOptionsSystem options;
        private IDisposable eventMusicHandle;
        private string eventDialogueId;
        private GameBgmTrack eventTrack;
        private bool eventOverrideActive;
        private WorkSfxPresenter workSounds;
        private CharacterSfxPresenter characterSounds;
        private bool started, connected, dirty, tutorial;

        private void Start() { started = true; Connect(); }
        private void OnEnable() { if (started) Connect(); }
        private void OnDisable() => Disconnect();

        private void Connect()
        {
            if (connected) return;
            if (music == null) { Debug.LogError("GameBgmSceneRoot에 BGM SO를 연결하세요.", this); return; }
            audioRoot = GameAudioRoot.GetOrCreate(audioPrefab);
            if (audioRoot == null) { Debug.LogError("GameBgmSceneRoot에 활성 GameAudioRoot 프리팹을 연결하세요.", this); return; }
            connected = true;
            session = audioRoot.BeginSession(music.FadeSeconds);
            // All scene Awakes have completed by Start; options cannot be missed due to script order.
            options = optionsRoot != null ? optionsRoot.Options : null;
            if (options != null)
            {
                options.MusicChanged += SendMusic;
                options.SoundChanged += SendSound;
                SendMusic(options.MusicVolume);
                SendSound(options.SoundVolume);
            }
            else Debug.LogWarning("GameBgmSceneRoot에 GameOptionsRoot를 연결해야 저장 음량이 적용됩니다.", this);
            BindCollection();
            if (scene == GameBgmScene.Game && workRoot != null)
            {
                boundRoot = workRoot;
                boundRoot.RunStarted += BindRun;
                BindRun(boundRoot.System);
            }
            dirty = true;
        }

        private void BindCollection()
        {
            UnbindCollection();
            if (scene != GameBgmScene.Title) return;
            // Existing scenes need no extra Inspector wiring. Search this scene once, including hidden UI.
            foreach (var owner in gameObject.scene.GetRootGameObjects())
            {
                var collection = owner.GetComponentInChildren<GameCollectionRoot>(true);
                if (collection == null) continue;
                boundCollection = collection;
                boundCollection.Changed += MarkDirty;
                dirty = true;
                return;
            }
        }

        private void UnbindCollection()
        {
            if (!ReferenceEquals(boundCollection, null)) boundCollection.Changed -= MarkDirty;
            boundCollection = null;
        }

        private void BindRun(WorkSystem system)
        {
            bool replacingRun = work != null || careFlow != null;
            UnbindRun();
            // A load/new run also invalidates event handles belonging to the previous run.
            if (replacingRun && audioRoot != null && session != null && !session.IsDisposed)
            {
                session.Dispose();
                session = audioRoot.BeginSession(music.FadeSeconds);
            }
            work = system;
            careFlow = boundRoot != null ? boundRoot.CareFlow : null;
            if (work != null) work.Changed += MarkDirty;
            if (careFlow != null) careFlow.Changed += MarkDirty;
            if (work != null && music.WorkSounds != null && audioRoot != null)
                workSounds = new WorkSfxPresenter(work, music.WorkSounds, audioRoot.Effects,
                    FindSceneView<WorkDeskView>(), FindSceneView<WorkPaperView>(), FindSceneView<WorkFeedbackView>(),
                    FindSceneView<WorkHudView>(), FindSceneView<WorkFaxView>(), FindSceneView<WorkMonthReportView>());
            if (work != null && music.CharacterSounds != null && audioRoot != null)
                characterSounds = new CharacterSfxPresenter(FindSceneView<CharacterPettingView>(), music.CharacterSounds, audioRoot.Effects);
            dirty = true;
        }

        private T FindSceneView<T>() where T : Component
        {
            foreach (var owner in gameObject.scene.GetRootGameObjects())
            {
                var view = owner.GetComponentInChildren<T>(true);
                if (view != null) return view;
            }
            return null;
        }

        private void UnbindRun()
        {
            characterSounds?.Dispose();
            characterSounds = null;
            workSounds?.Dispose();
            workSounds = null;
            ClearEventMusic();
            // Unsubscribe from the exact instances we subscribed to, not WorkRoot's replacements.
            if (work != null) work.Changed -= MarkDirty;
            if (careFlow != null) careFlow.Changed -= MarkDirty;
            work = null;
            careFlow = null;
        }

        private void LateUpdate()
        {
            workSounds?.Tick();
            characterSounds?.Tick();
            if (!connected || !dirty || session == null || session.IsDisposed) return;
            dirty = false;
            RefreshMusic();
        }

        private void RefreshMusic()
        {
            session.SetBaseTrack(ResolveTrack());
            SyncEventMusic();
        }

        private void SyncEventMusic()
        {
            string id = scene == GameBgmScene.Game && !tutorial && careFlow?.Phase == CareFlowPhase.Dialogue
                ? careFlow.Dialogue.RootDialogueId : null;
            bool active = music.TryGetEvent(id, out var track);
            if (id == eventDialogueId && active == eventOverrideActive && ReferenceEquals(track, eventTrack)) return;
            // The base was refreshed first. Release/acquire in the same tick, so adjacent events
            // never briefly restore Care music or restore last month's song.
            ClearEventMusic();
            eventDialogueId = id;
            eventTrack = track;
            eventOverrideActive = active;
            if (active) eventMusicHandle = session.RequestOverride(track);
        }

        private void ClearEventMusic()
        {
            eventMusicHandle?.Dispose();
            eventMusicHandle = null;
            eventDialogueId = null;
            eventTrack = null;
            eventOverrideActive = false;
        }

        private GameBgmTrack ResolveTrack()
        {
            if (scene == GameBgmScene.Title)
                return boundCollection != null && boundCollection.IsOpen && music.Collection?.Clip != null
                    ? music.Collection : music.Title;
            if (tutorial) return music.Tutorial;
            if (work == null) return null;
            if (careFlow != null && (careFlow.Phase == CareFlowPhase.EndingChoice || careFlow.Phase == CareFlowPhase.Ended))
                return music.Ending;
            if (careFlow == null && work.Run.Phase == WorkPhase.Finished) return music.Ending;
            if (work.Run.Phase == WorkPhase.Ready) return null;
            bool care = careFlow != null
                ? careFlow.Phase != CareFlowPhase.Working && careFlow.Phase != CareFlowPhase.Report
                : work.Run.Phase == WorkPhase.AfterWork;
            // Use Work's actual time limit (including upgrades), not Care's leave-response grace period.
            // Keep overtime silent through the salary report until Care really starts.
            if (!care && work.Run.Seconds > work.MonthSeconds) return null;
            return music.GetMonth(work.Run.Month, care);
        }

        // Future tutorial flow calls these explicitly; this does not delay WorkRoot.StartRun.
        public void BeginTutorial() { tutorial = true; MarkDirty(); }
        public void EndTutorial() { tutorial = false; MarkDirty(); }

        // Keep and Dispose the returned handle on event end/skip/cancel.
        // Refresh the base first, even if called before this component's LateUpdate.
        public IDisposable RequestOverride(GameBgmTrack track)
        {
            if (session == null || session.IsDisposed) return null;
            RefreshMusic();
            return session.RequestOverride(track);
        }

        private void SendMusic(float value) { if (session != null && !session.IsDisposed) audioRoot.SetMusicVolume(value); }
        private void SendSound(float value) { if (session != null && !session.IsDisposed) audioRoot.SetSoundVolume(value); }
        private void MarkDirty() => dirty = true;

        private void Disconnect()
        {
            if (!connected) return;
            connected = false;
            // Unity may already have destroyed the native WorkRoot during scene unload;
            // its managed event still belongs to this exact subscribed instance.
            if (!ReferenceEquals(boundRoot, null)) boundRoot.RunStarted -= BindRun;
            boundRoot = null;
            UnbindCollection();
            UnbindRun();
            if (options != null)
            {
                options.MusicChanged -= SendMusic;
                options.SoundChanged -= SendSound;
            }
            options = null;
            session?.Dispose();
            session = null;
            audioRoot = null;
        }
    }
}
