using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WorkGame.Tests
{
    public sealed class GameBgmSelectionTests
    {
        [Test]
        public void IntroUsesDedicatedTrackAndReturnsToWorkWithoutAnEventOverride()
        {
            var music = ScriptableObject.CreateInstance<GameBgmSO>();
            var config = ScriptableObject.CreateInstance<CareConfigSO>();
            var clip = AudioClip.Create("Intro BGM test", 64, 1, 8000, false);
            var owner = new GameObject("Intro BGM test");
            owner.SetActive(false);
            using var work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            using var care = config.CreateSystem(work);
            using var flow = new CareFlowSystem(work, care, new CareDialogueFile {
                IntroDialogue = "intro", Dialogues = new[] { new CareDialogueData {
                    Id = "intro", Lines = new[] { new CareDialogueLine { Text = "한 달 전" } } } } });
            using var session = new GameBgmSession();
            try
            {
                var root = owner.AddComponent<GameBgmSceneRoot>();
                Set(root, "music", music); Set(root, "work", work);
                Set(root, "careFlow", flow); Set(root, "session", session);
                music.Months[0].Care.Clip = clip; music.DefaultWork.Clip = clip;
                Assert.That(flow.StartIntro(), Is.True);
                Assert.That(work.Run.Phase, Is.EqualTo(WorkPhase.Ready));
                Assert.That(Resolve(root), Is.SameAs(music.Months[0].Care));
                music.Intro.Clip = clip;
                music.Events = new[] { new GameEventBgm { DialogueId = "intro", Mode = GameEventBgmMode.Silence } };
                typeof(GameBgmSceneRoot).GetMethod("RefreshMusic", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, null);
                Assert.That(session.CurrentTrack, Is.SameAs(music.Intro));
                Assert.That(flow.Dialogue.Next(), Is.True);
                Assert.That(flow.IsIntro, Is.False);
                typeof(GameBgmSceneRoot).GetMethod("RefreshMusic", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, null);
                Assert.That(session.CurrentTrack, Is.SameAs(music.DefaultWork));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner); UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(config); UnityEngine.Object.DestroyImmediate(music);
            }
        }

        [Test]
        public void AllTwelveMonthsKeepWorkThroughReportAndCareThroughResultBeforeEnding()
        {
            var music = ScriptableObject.CreateInstance<GameBgmSO>();
            var careConfig = ScriptableObject.CreateInstance<CareConfigSO>();
            var clip = AudioClip.Create("Bgm selection test", 64, 1, 8000, false);
            var owner = new GameObject("Bgm selection test");
            owner.SetActive(false); // Selection test must not connect to the live scene's audio player.
            using var work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            using var care = careConfig.CreateSystem(work);
            using var flow = new CareFlowSystem(work, care, new CareDialogueFile());
            try
            {
                var root = owner.AddComponent<GameBgmSceneRoot>();
                Set(root, "music", music);
                Set(root, "work", work);
                Set(root, "careFlow", flow);
                foreach (var month in music.Months) { month.Work.Clip = clip; month.Care.Clip = clip; }
                Assert.That(work.StartRun(), Is.True);
                for (int month = 1; month <= 12; month++)
                {
                    Assert.That(work.Run.Month, Is.EqualTo(month));
                    Assert.That(Resolve(root), Is.SameAs(music.Months[month - 1].Work));
                    Assert.That(work.LeaveWork(), Is.True);
                    Assert.That(flow.Phase, Is.EqualTo(CareFlowPhase.Report));
                    Assert.That(Resolve(root), Is.SameAs(music.Months[month - 1].Work), "급여 정산 중에는 Work 유지");
                    flow.EnterCare();
                    Assert.That(Resolve(root), Is.SameAs(music.Months[month - 1].Care));
                    Assert.That(Muffled(root), Is.False);
                    for (int slot = 0; slot < CareSystem.SlotCount; slot++) care.SetLesson(slot, CareSystem.RestId);
                    Assert.That(flow.GoNext(), Is.True);
                    Assert.That(Resolve(root), Is.SameAs(music.Months[month - 1].Care), "육성 결과 중에는 Care 유지");
                    Assert.That(Muffled(root), Is.True, "성장 결과에서만 BGM 필터 적용");
                    flow.ConfirmResult();
                    Assert.That(Muffled(root), Is.False, "이벤트·다음 월로 넘어가면 필터 해제");
                }
                Assert.That(flow.Phase, Is.EqualTo(CareFlowPhase.Ended));
                Assert.That(Resolve(root), Is.SameAs(music.Ending));
                root.BeginTutorial();
                Assert.That(Resolve(root), Is.SameAs(music.Tutorial));
                root.EndTutorial();
                Assert.That(Resolve(root), Is.SameAs(music.Ending));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(careConfig);
                UnityEngine.Object.DestroyImmediate(music);
            }
        }

        [Test]
        public void MissingMonthClipsUseSeparateDefaultsAndNeverKeepAnEarlierMonth()
        {
            var music = ScriptableObject.CreateInstance<GameBgmSO>();
            var clip = AudioClip.Create("Bgm fallback test", 64, 1, 8000, false);
            try
            {
                music.Months[0].Work.Clip = clip;
                Assert.That(music.GetMonth(1, false), Is.SameAs(music.Months[0].Work));
                Assert.That(music.GetMonth(2, false), Is.SameAs(music.DefaultWork));
                Assert.That(music.GetMonth(1, true), Is.SameAs(music.DefaultCare));
                music.Months = null;
                Assert.That(music.GetMonth(12, true), Is.SameAs(music.DefaultCare));
                Assert.That(music.GetMonth(0, false), Is.SameAs(music.DefaultWork));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(music);
            }
        }

        [TestCase(0)]
        [TestCase(15)]
        public void OvertimeFadesToSilenceAtWorkLimitAndRestoresMusicInCareAndNextMonth(int extraSeconds)
        {
            var music = ScriptableObject.CreateInstance<GameBgmSO>();
            var careConfig = ScriptableObject.CreateInstance<CareConfigSO>();
            var owner = new GameObject("Overtime BGM selection test");
            owner.SetActive(false);
            using var work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            using var care = careConfig.CreateSystem(work);
            using var flow = new CareFlowSystem(work, care, new CareDialogueFile());
            try
            {
                var root = owner.AddComponent<GameBgmSceneRoot>();
                Set(root, "music", music);
                Set(root, "work", work);
                Set(root, "careFlow", flow);
                Assert.That(work.StartRun(), Is.True);
                var upgrades = new WorkUpgradeSystem(work, new[]
                {
                    new WorkUpgradeNode("more-time", 0, Array.Empty<string>(), new[]
                    {
                        new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.ExtraSeconds, Value = extraSeconds }
                    })
                });
                Assert.That(upgrades.Buy("more-time"), Is.True);
                work.AddTime(work.MonthSeconds - extraSeconds - 1);
                Assert.That(Resolve(root), Is.SameAs(music.DefaultWork));
                if (extraSeconds > 0)
                {
                    work.AddTime(2);
                    Assert.That(Resolve(root), Is.SameAs(music.DefaultWork), "강화로 늘어난 시간도 정규 업무입니다.");
                }
                work.AddTime(work.MonthSeconds - work.Run.Seconds);
                Assert.That(Resolve(root), Is.SameAs(music.DefaultWork), "정확히 0초일 때 초과 시간은 아직 없습니다.");
                work.AddTime(0.01);
                Assert.That(Resolve(root), Is.Null, "Care의 10초 유예와 무관하게 Work 초과부터 무음입니다.");
                Assert.That(work.LeaveWork(), Is.True);
                Assert.That(work.Run.LastResult.OvertimeSeconds, Is.GreaterThan(0));
                Assert.That(Resolve(root), Is.Null, "급여 정산에서 Work 곡을 다시 켜지 않습니다.");
                flow.EnterCare();
                Assert.That(Resolve(root), Is.SameAs(music.DefaultCare));
                for (int slot = 0; slot < CareSystem.SlotCount; slot++) care.SetLesson(slot, CareSystem.RestId);
                Assert.That(flow.GoNext(), Is.True);
                flow.ConfirmResult();
                Assert.That(work.Run.Month, Is.EqualTo(2));
                Assert.That(Resolve(root), Is.SameAs(music.DefaultWork));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(careConfig);
                UnityEngine.Object.DestroyImmediate(music);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TitleCollectionAutomaticallyBindsAndRestoresTitleOnCloseOrDisable(bool alreadyOpen)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var music = ScriptableObject.CreateInstance<GameBgmSO>();
            var careConfig = ScriptableObject.CreateInstance<CareConfigSO>();
            var clip = AudioClip.Create("Collection BGM test", 64, 1, 8000, false);
            var owner = new GameObject("Collection BGM test");
            owner.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
            var root = owner.AddComponent<GameBgmSceneRoot>();
            var collection = owner.AddComponent<GameCollectionRoot>();
            var view = owner.AddComponent<GameCollectionView>();
            Set(root, "music", music);
            Set(root, "scene", GameBgmScene.Title);
            Set(collection, "view", view);
            Set(collection, "presenter", new GameCollectionPresenter(careConfig,
                new WorkSaveSystem(new PlayerPrefsSaveStore()), view));
            music.Collection.Clip = clip;
            try
            {
                if (alreadyOpen) collection.Open();
                Invoke(root, "BindCollection");
                Assert.That(Resolve(root), Is.SameAs(alreadyOpen ? music.Collection : music.Title));
                Set(root, "dirty", false);
                if (alreadyOpen) collection.Close(); else collection.Open();
                Assert.That(IsDirty(root), Is.True, "컬렉션 표시 변경이 BGM 갱신을 요청합니다.");
                Assert.That(Resolve(root), Is.SameAs(alreadyOpen ? music.Title : music.Collection));
                collection.Open();
                music.Collection.Clip = null;
                Assert.That(Resolve(root), Is.SameAs(music.Title), "컬렉션 곡 미지정 시 타이틀을 유지합니다.");
                music.Collection.Clip = clip;
                Set(root, "dirty", false);
                view.ShowList();
                Assert.That(IsDirty(root), Is.False, "컬렉션 내부 페이지 이동은 곡을 바꾸지 않습니다.");
                Invoke(collection, "OnDisable");
                Assert.That(IsDirty(root), Is.True);
                Assert.That(Resolve(root), Is.SameAs(music.Title));
                Invoke(root, "UnbindCollection");
                Set(root, "dirty", false);
                Invoke(collection, "OnDisable");
                Assert.That(IsDirty(root), Is.False, "씬 해제 후에는 이전 컬렉션 이벤트를 받지 않습니다.");
            }
            finally
            {
                Invoke(root, "UnbindCollection");
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(careConfig);
                UnityEngine.Object.DestroyImmediate(music);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static bool IsDirty(GameBgmSceneRoot root)
            => (bool)typeof(GameBgmSceneRoot).GetField("dirty", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);

        private static void Invoke(object target, string method)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static bool Muffled(GameBgmSceneRoot root)
            => (bool)typeof(GameBgmSceneRoot).GetProperty("ShouldMuffleMusic", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);

        private static GameBgmTrack Resolve(GameBgmSceneRoot root)
            => (GameBgmTrack)typeof(GameBgmSceneRoot).GetMethod("ResolveTrack", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, null);
    }
}
