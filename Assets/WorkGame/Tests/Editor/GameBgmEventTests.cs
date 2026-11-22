using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using WorkGame.Editor;

namespace WorkGame.Tests
{
    public sealed class GameBgmEventTests
    {
        private static CareDialogueData Dialogue(string id, string next = null) => new CareDialogueData
        {
            Id = id, Lines = new[] { new CareDialogueLine { Text = id + " 첫 대사" } },
            Choices = next == null ? Array.Empty<CareDialogueChoice>()
                : new[] { new CareDialogueChoice { Key = "continue", Text = "계속", NextDialogue = next } }
        };

        private static CareDialogueFile Data(bool secondStory = false, int storyMonth = 1) => new CareDialogueFile
        {
            Leave = new CareLeaveSettings
            {
                Months = new[] { new CareMonthLeave { Month = 1, EarlyDialogue = "entry", SlightlyEarlyDialogue = "entry",
                    OnTimeDialogue = "entry", OvertimeDialogue = "entry" } }
            },
            Dialogues = new[] { Dialogue("entry", "entry_reply"), Dialogue("entry_reply"),
                Dialogue("story", "story_reply"), Dialogue("story_reply"), Dialogue("second_story") },
            Stories = secondStory
                ? new[] { new CareStoryData { Id = "first", Month = storyMonth, Dialogue = "story" },
                    new CareStoryData { Id = "second", Month = storyMonth, Dialogue = "second_story" } }
                : new[] { new CareStoryData { Id = "first", Month = storyMonth, Dialogue = "story" } }
        };

        private sealed class Run : IDisposable
        {
            public readonly GameBgmSO Music = ScriptableObject.CreateInstance<GameBgmSO>();
            private readonly CareConfigSO config = ScriptableObject.CreateInstance<CareConfigSO>();
            private readonly AudioClip clip = AudioClip.Create("Event BGM test", 64, 1, 8000, false);
            private readonly GameObject owner = new GameObject("Event BGM test");
            private readonly UnityEngine.SceneManagement.Scene preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            public readonly WorkSystem Work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            public readonly CareSystem Care;
            public readonly CareFlowSystem Flow;
            public readonly GameBgmSceneRoot Root;
            public GameBgmSession Session = new GameBgmSession();

            public Run(bool secondStory = false, int storyMonth = 1)
            {
                owner.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
                Root = owner.AddComponent<GameBgmSceneRoot>();
                Care = config.CreateSystem(Work);
                Flow = new CareFlowSystem(Work, Care, Data(secondStory, storyMonth));
                Flow.Dialogue.SetReadCheck(_ => true);
                Music.DefaultWork.Clip = clip;
                Music.DefaultCare.Clip = clip;
                Music.Events = new[] { Event("entry"), Event("story"), Event("second_story") };
                Set(Root, "music", Music);
                Set(Root, "work", Work);
                Set(Root, "careFlow", Flow);
                Set(Root, "session", Session);
                Work.StartRun();
            }
            private GameEventBgm Event(string id) => new GameEventBgm
            { DialogueId = id, Mode = GameEventBgmMode.Play, Track = new GameBgmTrack { Clip = clip } };
            public void Refresh() => Invoke(Root, "RefreshMusic");
            public void EnterCare()
            {
                Work.AddTime(Work.MonthSeconds);
                Assert.That(Work.LeaveWork(), Is.True);
                Flow.EnterCare(); Refresh();
            }
            public void FinishEntry()
            {
                Flow.Dialogue.Choose(0);
                Flow.Dialogue.Skip();
                Assert.That(Flow.Phase, Is.EqualTo(CareFlowPhase.Free));
                Refresh();
            }
            public void EnterStory()
            {
                EnterCare(); FinishEntry();
                for (int slot = 0; slot < CareSystem.SlotCount; slot++) Care.SetLesson(slot, CareSystem.RestId);
                Assert.That(Flow.GoNext(), Is.True);
                Flow.ConfirmResult(); Refresh();
            }
            public void Dispose()
            {
                Invoke(Root, "UnbindRun"); Session.Dispose();
                Flow.Dispose(); Care.Dispose(); Work.Dispose();
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(Music);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [Test]
        public void EntryMusicSurvivesChoicesAndRepeatedRefreshThenReturnsToCare()
        {
            using var run = new Run();
            run.EnterCare();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[0].Track));
            int changes = 0;
            run.Session.Changed += _ => changes++;
            Assert.That(run.Flow.Dialogue.Choose(0), Is.True);
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("entry_reply"));
            Assert.That(run.Flow.Dialogue.RootDialogueId, Is.EqualTo("entry"));
            run.Refresh(); run.Refresh();
            Assert.That(changes, Is.Zero, "대화 분기나 반복 갱신에서 핸들을 재발급하지 않습니다.");
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[0].Track));
            Assert.That(run.Flow.Dialogue.Skip(), Is.True);
            Assert.That(run.Flow.Dialogue.RootDialogueId, Is.Null);
            run.Refresh();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.DefaultCare));
        }

        [Test]
        public void MonthEndEventReturnsDirectlyToNextMonthsWork()
        {
            using var run = new Run();
            run.EnterStory();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[1].Track));
            run.Flow.Dialogue.Choose(0); run.Refresh();
            Assert.That(run.Flow.Dialogue.RootDialogueId, Is.EqualTo("story"));
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[1].Track));
            run.Flow.Dialogue.Skip(); run.Refresh();
            Assert.That(run.Work.Run.Month, Is.EqualTo(2));
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.DefaultWork));
        }

        [Test]
        public void AdjacentEventsSwitchToNextEventWithoutLosingItsRootIdentity()
        {
            using var run = new Run(true);
            run.EnterStory();
            run.Flow.Dialogue.Choose(0); run.Flow.Dialogue.Skip();
            Assert.That(run.Flow.Dialogue.RootDialogueId, Is.EqualTo("second_story"));
            run.Refresh();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[2].Track));
            run.Flow.Dialogue.Skip(); run.Refresh();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.DefaultWork));
        }

        [Test]
        public void LastMonthEventReturnsDirectlyToEndingRatherThanCareMusic()
        {
            using var run = new Run(storyMonth: 12);
            for (int month = 1; month <= 12; month++)
            {
                run.Work.AddTime(run.Work.MonthSeconds);
                Assert.That(run.Work.LeaveWork(), Is.True);
                run.Flow.EnterCare();
                while (run.Flow.Dialogue.IsOpen)
                    if (run.Flow.Dialogue.HasChoices) run.Flow.Dialogue.Choose(0); else run.Flow.Dialogue.Skip();
                for (int slot = 0; slot < CareSystem.SlotCount; slot++) run.Care.SetLesson(slot, CareSystem.RestId);
                Assert.That(run.Flow.GoNext(), Is.True);
                run.Flow.ConfirmResult();
            }
            run.Refresh();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[1].Track));
            run.Flow.Dialogue.Choose(0); run.Flow.Dialogue.Skip(); run.Refresh();
            Assert.That(run.Flow.Phase, Is.EqualTo(CareFlowPhase.Ended));
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Ending));
        }

        [Test]
        public void SilenceIsExplicitAndAnEmptyPlayClipKeepsTheBaseMusic()
        {
            using var run = new Run();
            run.Music.Events[0].Mode = GameEventBgmMode.Silence;
            run.EnterCare();
            Assert.That(run.Session.CurrentTrack, Is.Null);
            run.Music.Events[0].Mode = GameEventBgmMode.Play;
            run.Music.Events[0].Track.Clip = null;
            run.Refresh();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.DefaultCare));
            Assert.That(run.Music.TryGetEvent("missing", out _), Is.False);
            run.Music.Events[0].Mode = GameEventBgmMode.Inherit;
            Assert.That(run.Music.TryGetEvent("entry", out _), Is.False);
        }

        [Test]
        public void RunReplacementReappliesSameEventAndInvalidatesTheOldHandle()
        {
            using var run = new Run();
            run.EnterCare();
            var oldHandle = (IDisposable)Get(run.Root, "eventMusicHandle");
            Invoke(run.Root, "UnbindRun");
            run.Session.Dispose();
            run.Session = new GameBgmSession();
            Set(run.Root, "session", run.Session);
            Set(run.Root, "work", run.Work);
            Set(run.Root, "careFlow", run.Flow);
            run.Refresh();
            oldHandle.Dispose();
            Assert.That(run.Session.CurrentTrack, Is.SameAs(run.Music.Events[0].Track));
        }

        [Test]
        public void CatalogLabelsActualRootsByMonthAndContextAndDoesNotListChoiceReplies()
        {
            var data = Data();
            data.Promises = new[] { new CarePromiseData { Id = "test", OfferMonth = 3, TargetMonth = 4,
                OfferDialogue = "offer", SuccessDialogue = "success", FailureDialogue = "failure" } };
            data.Stories[0].PromiseId = "test";
            data.Stories[0].PromiseSuccessDialogue = "story_success";
            data.Dialogues = data.Dialogues.Concat(new[] { Dialogue("offer"), Dialogue("success"),
                Dialogue("failure"), Dialogue("story_success") }).ToArray();
            var catalog = GameBgmEventCatalog.Build(data);
            var entry = catalog.Single(x => x.DialogueId == "entry");
            Assert.That(entry.Month, Is.EqualTo(1));
            Assert.That(entry.Contexts.Any(x => x.Contains("Care 진입") && x.Contains("정시 퇴근")), Is.True);
            Assert.That(entry.Preview, Does.Contain("첫 대사"));
            Assert.That(catalog.Single(x => x.DialogueId == "offer").Month, Is.EqualTo(3));
            Assert.That(catalog.Single(x => x.DialogueId == "success").Month, Is.EqualTo(4));
            Assert.That(catalog.Single(x => x.DialogueId == "story_success").Contexts[0], Does.Contain("약속 성공 버전"));
            Assert.That(catalog.Any(x => x.DialogueId == "entry_reply" || x.DialogueId == "story_reply"), Is.False);
        }

        private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    }
}
