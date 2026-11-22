using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WorkGame.Tests
{
    public sealed class CareStoryTests
    {
        private static CareDialogueData Dialogue(string id, params string[] lines) => new CareDialogueData
        { Id = id, Lines = lines.Select(text => new CareDialogueLine { Text = text }).ToArray() };

        private static CareDialogueFile Data(int month = 1)
        {
            var story = Dialogue("story", "첫 줄", "선택해 주세요");
            story.Choices = new[]
            {
                new CareDialogueChoice { Key = "good", Text = "응원", HighestStatGain = 10, Friendship = 2, NextDialogue = "reply" },
                new CareDialogueChoice { Key = "neutral", Text = "다른 대답", NextDialogue = "reply" }
            };
            return new CareDialogueFile
            {
                Dialogues = new[] { story, Dialogue("reply", "대답", "끝") },
                Stories = new[] { new CareStoryData { Id = "test", Month = month, Dialogue = "story", Unlock = "nameplate" } }
            };
        }

        private sealed class Run : IDisposable
        {
            public readonly WorkSystem Work;
            public readonly CareSystem Care;
            public readonly CareFlowSystem Flow;
            public Run(CareDialogueFile data, int logic = 20, int research = 20, GameSaveData saved = null,
                bool startWork = true, CareEnding fallback = null)
            {
                var papers = Enumerable.Range(1, 12).Select(month => new WorkPaper(month, month, "01", "Test", "Test", "Score", "",
                    PickRule.Highest, PaperDisplay.Number, new[] { new WorkChoice("A", 1), new WorkChoice("B", 0) }));
                Work = new WorkSystem(new WorkSettings(), papers, 1);
                if (saved != null) Work.RestoreSave(saved.Work);
                Care = new CareSystem(Work, new[]
                {
                    new CareLesson("home", "Home", 1, 0, new[] { new CareGain(CareStat.Logic, 3) }),
                    new CareLesson("rest", "Rest", 1, 0, Array.Empty<CareGain>())
                }, Array.Empty<CareSynergy>(), Array.Empty<CareGift>(), Array.Empty<CareEnding>(),
                    fallback ?? new CareEnding("fallback", "Fallback", 0, Array.Empty<CareGain>(), "", "", "", ""),
                    new[] { new CareGain(CareStat.Logic, logic), new CareGain(CareStat.Research, research) });
                Flow = new CareFlowSystem(Work, Care, data);
                if (saved == null) { if (startWork) Work.StartRun(); }
                else { Care.RestoreSave(saved.Care); Flow.RestoreSave(saved.Flow, saved.Point); }
            }
            public void BeginResult(string lesson = "rest")
            {
                Work.AddTime(Work.MonthSeconds);
                Assert.That(Work.LeaveWork(), Is.True);
                Flow.EnterCare(); Drain();
                for (int i = 0; i < 4; i++) Assert.That(Care.SetLesson(i, lesson), Is.True);
                Assert.That(Flow.GoNext(), Is.True);
            }
            public void Drain(int choice = 0)
            {
                int remaining = 200;
                while (Flow.Dialogue.IsOpen && remaining-- > 0)
                    Assert.That(Flow.Dialogue.HasChoices ? Flow.Dialogue.Choose(choice) : Flow.Dialogue.Next(), Is.True);
                Assert.That(remaining, Is.GreaterThan(0), "대화가 끝나지 않았습니다.");
            }
            public void ReachResult(int month)
            {
                while (Work.Run.Month < month) { BeginResult(); Flow.ConfirmResult(); Drain(); }
                BeginResult();
            }
            public GameSaveData Save(SavePoint point) => new GameSaveData
            { Point = point, Work = Work.CaptureSave(), Care = Care.CaptureSave(point), Flow = Flow.CaptureSave() };
            public void Dispose() { Flow.Dispose(); Care.Dispose(); Work.Dispose(); }
        }

        [Test]
        public void MonthsWithoutStoriesAdvanceNormallyAndMultipleStoriesKeepRegistrationOrder()
        {
            var data = Data(2);
            data.Stories = data.Stories.Concat(new[] { new CareStoryData { Id = "second", Month = 2, Dialogue = "reply" } }).ToArray();
            using var run = new Run(data);
            run.BeginResult(); run.Flow.ConfirmResult();
            Assert.That(run.Work.Run.Month, Is.EqualTo(2));
            Assert.That(run.Flow.HasStoryUnlock("nameplate"), Is.False);
            run.BeginResult(); run.Flow.ConfirmResult();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("story"));
            Assert.That(run.Flow.WillAdvanceAfterDialogue, Is.False);
            run.Flow.Dialogue.Next(); run.Flow.Dialogue.Choose(1);
            run.Flow.Dialogue.Next(); run.Flow.Dialogue.Next();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("reply"));
            Assert.That(run.Flow.WillAdvanceAfterDialogue, Is.True);
            Assert.That(run.Flow.Dialogue.WasCompleted("story/test"), Is.True);
            Assert.That(run.Flow.Dialogue.WasCompleted("story/second"), Is.False);
            Assert.That(run.Work.Run.Month, Is.EqualTo(2));
            run.Drain();
            Assert.That(run.Work.Run.Month, Is.EqualTo(3));
            Assert.That(run.Flow.Dialogue.WasCompleted("story/second"), Is.True);
        }

        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(6, false)]
        [TestCase(8, true)]
        public void EventExitTransitionWaitsUntilTheMonthActuallyEnds(int month, bool expected)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            using var run = new Run(data);
            run.ReachResult(month); run.Flow.ConfirmResult();
            Assert.That(run.Flow.WillAdvanceAfterDialogue, Is.EqualTo(expected));
            if (month != 6) return;
            while (run.Flow.IsStoryPlaying)
            {
                if (run.Flow.Dialogue.HasChoices) run.Flow.Dialogue.Choose(0);
                else run.Flow.Dialogue.Next();
            }
            Assert.That(run.Flow.Dialogue.RootDialogueId, Is.EqualTo("promise_album_offer"));
            Assert.That(run.Flow.WillAdvanceAfterDialogue, Is.True);
            run.Flow.Dialogue.Next();
            run.Flow.Dialogue.SetPresentationBlocked(true);
            Assert.That(run.Flow.Dialogue.Choose(0), Is.False);
            Assert.That(run.Work.Run.Month, Is.EqualTo(6));
            run.Flow.Dialogue.SetPresentationBlocked(false);
            Assert.That(run.Flow.Dialogue.Choose(0), Is.True);
            Assert.That(run.Work.Run.Month, Is.EqualTo(7));
        }

        [Test]
        public void StoryWaitsForResultConfirmationAndChoiceReplyBeforeWork()
        {
            using var run = new Run(Data());
            run.BeginResult();
            Assert.That(run.Flow.Dialogue.IsOpen, Is.False);
            run.Flow.ConfirmResult();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("story"));
            run.Flow.Dialogue.Next(); run.Flow.Dialogue.Choose(0);
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("reply"));
            Assert.That(run.Work.Run.Month, Is.EqualTo(1));
            Assert.That(run.Flow.HasStoryUnlock("nameplate"), Is.False);
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(20));
            run.Drain();
            Assert.That(run.Work.Run.Month, Is.EqualTo(2));
            Assert.That(run.Flow.HasStoryUnlock("nameplate"), Is.True);
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(30));
            Assert.That(run.Care.Run.Friendship, Is.EqualTo(2));
            run.Flow.ConfirmResult();
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(30));
        }

        [TestCase(20, 20, 30, 20)]
        [TestCase(20, 21, 20, 31)]
        [TestCase(296, 296, 300, 296)]
        [TestCase(300, 299, 300, 299)]
        public void HighestDetailStatUsesDefinitionOrderAndExistingCap(int logic, int research, int expectedLogic, int expectedResearch)
        {
            using var run = new Run(Data(), logic, research);
            run.BeginResult(); run.Flow.ConfirmResult(); run.Drain();
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(expectedLogic));
            Assert.That(run.Care.Run.GetStat(CareStat.Research), Is.EqualTo(expectedResearch));
        }

        [Test]
        public void RewardUsesStatsAfterTheScheduleAndNeutralChoiceHasNoReward()
        {
            using var run = new Run(Data(), 10, 20);
            run.BeginResult("home"); run.Flow.ConfirmResult(); run.Drain();
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(32));
            Assert.That(run.Care.Run.GetStat(CareStat.Research), Is.EqualTo(20));
            using var neutral = new Run(Data());
            neutral.BeginResult(); neutral.Flow.ConfirmResult(); neutral.Drain(1);
            Assert.That(neutral.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(20));
            Assert.That(neutral.Flow.Dialogue.History.Count(x => x.IsChoice), Is.EqualTo(1));
        }

        [Test]
        public void StoryPrecedesPromiseOfferInTheSameMonth()
        {
            var data = Data(6);
            var offer = Dialogue("offer", "약속");
            offer.Choices = new[] { new CareDialogueChoice { Key = "accept", Text = "Yes" }, new CareDialogueChoice { Key = "decline", Text = "No" } };
            data.Dialogues = data.Dialogues.Concat(new[] { offer, Dialogue("success", "성공"), Dialogue("failure", "실패") }).ToArray();
            data.Promises = new[] { new CarePromiseData { Id = "promise", OfferMonth = 6, TargetMonth = 7, Condition = "NoOvertime",
                OfferDialogue = "offer", SuccessDialogue = "success", FailureDialogue = "failure" } };
            using var run = new Run(data);
            run.ReachResult(6); run.Flow.ConfirmResult();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("story"));
            run.Flow.Dialogue.Next(); run.Flow.Dialogue.Choose(0);
            run.Flow.Dialogue.Next(); run.Flow.Dialogue.Next();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("offer"));
            Assert.That(run.Work.Run.Month, Is.EqualTo(6));
            run.Flow.Dialogue.Choose(0);
            Assert.That(run.Work.Run.Month, Is.EqualTo(7));
            Assert.That(run.Flow.Promises.Records.Single().State, Is.EqualTo(CarePromiseState.Accepted));
        }

        [Test]
        public void PresentationBlockingAndReadSkipDoNotBypassChoicesOrRepeatEffects()
        {
            using var run = new Run(Data());
            run.Flow.Dialogue.SetReadCheck(_ => true);
            int immediateLines = 0;
            run.Flow.Dialogue.LineEntered += (_, immediate) => { if (immediate) immediateLines++; };
            run.BeginResult(); run.Flow.ConfirmResult();
            run.Flow.Dialogue.SetPresentationBlocked(true);
            Assert.That(run.Flow.Dialogue.Next(), Is.False);
            Assert.That(run.Flow.Dialogue.Skip(), Is.False);
            run.Flow.Dialogue.SetPresentationBlocked(false);
            Assert.That(run.Flow.Dialogue.Skip(), Is.True);
            Assert.That(run.Flow.Dialogue.HasChoices, Is.True);
            run.Flow.Dialogue.SetPresentationBlocked(true);
            Assert.That(run.Flow.Dialogue.Choose(0), Is.False);
            run.Flow.Dialogue.SetPresentationBlocked(false);
            run.Flow.Dialogue.Choose(0);
            run.Flow.Dialogue.Skip();
            Assert.That(immediateLines, Is.EqualTo(2));
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(30));
        }

        [Test]
        public void SaveRestoresUnlocksChoicesAndRewardWithoutReapplyingThem()
        {
            using var run = new Run(Data());
            run.BeginResult(); run.Flow.ConfirmResult(); run.Drain();
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(run.Save(SavePoint.WorkStart)));
            using var loaded = new Run(Data(), saved: saved);
            Assert.That(loaded.Flow.HasStoryUnlock("nameplate"), Is.True);
            Assert.That(loaded.Flow.Dialogue.WasCompleted("story/test"), Is.True);
            Assert.That(loaded.Flow.Dialogue.History.Single(x => x.IsChoice).Text, Is.EqualTo("응원"));
            loaded.Flow.ConfirmResult();
            Assert.That(loaded.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(30));
            using var fresh = new Run(Data());
            Assert.That(fresh.Flow.HasStoryUnlock("nameplate"), Is.False);
        }

        [Test]
        public void CareCheckpointRollsBackPendingRewardAndOldSavesNeedNoUnlockField()
        {
            using var run = new Run(Data());
            run.Work.AddTime(run.Work.MonthSeconds); run.Work.LeaveWork();
            var checkpoint = run.Save(SavePoint.CareStart);
            checkpoint.Flow.StoryUnlocks = null;
            using var loaded = new Run(Data(), saved: checkpoint);
            loaded.Flow.EnterCare();
            for (int i = 0; i < 4; i++) loaded.Care.SetLesson(i, "rest");
            loaded.Flow.GoNext(); loaded.Flow.ConfirmResult();
            loaded.Flow.Dialogue.Next(); loaded.Flow.Dialogue.Choose(0);
            Assert.That(loaded.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(20));
            Assert.That(loaded.Flow.HasStoryUnlock("nameplate"), Is.False);
        }

        [Test]
        public void InvalidDialogueLinksCyclesAndCueDurationsFailAtLoad()
        {
            var data = Data(); data.Dialogues[0].Choices[0].NextDialogue = "missing";
            Assert.Throws<ArgumentException>(() => { using var run = new Run(data); });
            data = Data(); data.Dialogues[0].Choices[0].NextDialogue = "story";
            Assert.Throws<ArgumentException>(() => { using var run = new Run(data); });
            data = Data(); data.Dialogues[0].Lines[0].Cues = new[] { new CareDialogueCue { Kind = CareDialogueCueKind.Wait, Seconds = float.NaN } };
            Assert.Throws<ArgumentException>(() => { using var run = new Run(data); });
        }

        [Test]
        public void EndingMemoriesUseOnlyReadStoryAndSelectedChoiceAndSurviveSave()
        {
            var data = Data();
            data.EndingIntroText = "Intro";
            data.Dialogues[0].EndingText = "Read story";
            data.Dialogues[0].Choices[0].EndingText = "Selected encouragement";
            data.Dialogues[0].Choices[1].EndingText = "Unselected reply";
            using var run = new Run(data);
            Assert.That(run.Flow.BuildEndingMemories(), Is.Empty);
            run.BeginResult(); run.Flow.ConfirmResult(); run.Drain();
            var memories = run.Flow.BuildEndingMemories();
            Assert.That(memories, Is.EqualTo(new[] { "Intro", "Selected encouragement" }));
            using var loaded = new Run(data);
            loaded.Flow.RestoreSave(run.Flow.CaptureSave(), SavePoint.EndingChoice);
            Assert.That(loaded.Flow.BuildEndingMemories(), Is.EqualTo(memories));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AlbumPhotoWaitsForTheWholeSuccessDialogueAndSurvivesReload(bool early)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            using var run = new Run(data);
            run.ReachResult(6); run.Flow.ConfirmResult(); run.Drain();
            Assert.That(run.Work.Run.Month, Is.EqualTo(7));
            Assert.That(run.Flow.HasReadSuccessfulPromise("album"), Is.False);
            while (run.Work.PendingCount > 0)
            {
                Assert.That(run.Work.StartHold(run.Work.CurrentPaper.Id, 0), Is.True);
                run.Work.AddTime(1);
            }
            run.Work.AddTime(Math.Max(0, run.Work.MonthSeconds - (early ? 35 : 0) - run.Work.Run.Seconds));
            Assert.That(run.Work.LeaveWork(), Is.True);
            run.Flow.EnterCare();
            Assert.That(run.Flow.Promises.Records.Single(record => record.Promise.Id == "album").State,
                Is.EqualTo(CarePromiseState.Succeeded));
            while (run.Flow.Dialogue.LineIndex < run.Flow.Dialogue.Current.Lines.Length - 1)
            {
                Assert.That(run.Flow.HasReadSuccessfulPromise("album"), Is.False);
                run.Flow.Dialogue.Next();
            }
            Assert.That(run.Flow.HasReadSuccessfulPromise("album"), Is.False);
            run.Flow.Dialogue.Next();
            Assert.That(run.Flow.HasReadSuccessfulPromise("album"), Is.True);
            run.Flow.RestoreSave(run.Flow.CaptureSave(), SavePoint.CareStart);
            Assert.That(run.Flow.HasReadSuccessfulPromise("album"), Is.True);
        }

        [TestCase(-1, CareFriendship.Low, "Separate")]
        [TestCase(0, CareFriendship.Normal, "Visits")]
        [TestCase(50, CareFriendship.High, "Together")]
        public void EndingScriptKeepsCareerBetweenMemoriesAndActualFriendship(int friendship,
            CareFriendship tier, string expected)
        {
            var data = Data(); data.Stories = Array.Empty<CareStoryData>();
            var ending = new CareEnding("fallback", "Fallback", 0, Array.Empty<CareGain>(),
                "Career one\nCareer two", "Separate", "Visits", "Together");
            using var run = new Run(data, fallback: ending);
            run.Care.AddFriendship(friendship);
            run.ReachResult(12); run.Flow.ConfirmResult(); run.Drain();
            var result = run.Care.Run.Ending;
            Assert.That(result.Friendship, Is.EqualTo(tier));
            var lines = CareEndingScript.Build(result, new[] { "Five years later\nA real memory" });
            Assert.That(lines.Select(line => line.Scene), Is.EqualTo(new[]
            {
                CareEndingScene.Memories, CareEndingScene.Memories,
                CareEndingScene.Career, CareEndingScene.Career, CareEndingScene.Friendship
            }));
            Assert.That(lines.Last().Text, Is.EqualTo(expected));
            Assert.That(CareEndingScript.Build(result, Array.Empty<string>()).First().Text, Does.Contain("5년"));
            Assert.That(CareEndingScript.Build(result, null, appendFriendship: false)
                .All(line => line.Scene != CareEndingScene.Friendship), Is.True);
        }

        [TestCase("baking", CarePromiseState.Succeeded, false)]
        [TestCase("baking", CarePromiseState.Failed, false)]
        [TestCase("baking", CarePromiseState.Declined, false)]
        [TestCase("album", CarePromiseState.Succeeded, false)]
        [TestCase("album", CarePromiseState.Succeeded, true)]
        [TestCase("album", CarePromiseState.Failed, false)]
        [TestCase("album", CarePromiseState.Declined, false)]
        [TestCase("show", CarePromiseState.Succeeded, false)]
        [TestCase("show", CarePromiseState.Failed, false)]
        [TestCase("show", CarePromiseState.Declined, false)]
        public void EndingMemoriesRecallExactPromiseOutcome(string id, CarePromiseState state, bool early)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            // JsonUtility leaves omitted arrays null on some Editor versions.
            foreach (var dialogue in data.Dialogues)
                if (dialogue.Choices == null) dialogue.Choices = Array.Empty<CareDialogueChoice>();
            using var run = new Run(data);
            var promise = data.Promises.Single(x => x.Id == id);
            var saved = run.Flow.CaptureSave();
            saved.Promises = new[] { new SavedPromise
            {
                Id = id, State = state, ResultDialogue = state == CarePromiseState.Succeeded
                    ? early ? promise.EarlySuccessDialogue : promise.SuccessDialogue
                    : state == CarePromiseState.Failed ? promise.FailureDialogue : null
            } };
            run.Flow.RestoreSave(saved, SavePoint.EndingChoice);
            string expected = state == CarePromiseState.Succeeded
                ? early ? promise.EarlySuccessEndingText : promise.SuccessEndingText
                : state == CarePromiseState.Failed ? promise.FailureEndingText : promise.DeclinedEndingText;
            Assert.That(expected, Is.Not.Null.And.Not.Empty);
            Assert.That(run.Flow.BuildEndingMemories(), Is.EqualTo(new[] { data.EndingIntroText, expected }));
        }

        [Test]
        public void ConfiguredNarrationHasNoAutomaticPlayerSpeechAndNewChoicesAreEquallyKind()
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues)
            {
                Assert.That(dialogue.Id == data.IntroDialogue || dialogue.Lines.All(line => line.Speaker != "나르미"), Is.True, dialogue.Id);
                var choices = dialogue.Choices ?? Array.Empty<CareDialogueChoice>();
                if (!choices.Any(choice => choice.Key == "practical")) continue;
                Assert.That(choices.Select(choice => choice.Key),
                    Is.EqualTo(new[] { "practical", "empathy", "together" }), dialogue.Id);
                Assert.That(choices.All(choice => choice.Friendship == 0 && choice.HighestStatGain == 0
                    && !string.IsNullOrEmpty(choice.EndingText)
                    && (dialogue.Id == "story_02_nameplate" ? string.IsNullOrEmpty(choice.NextDialogue)
                        : data.Dialogues.Any(reply => reply.Id == choice.NextDialogue))), Is.True, dialogue.Id);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IntroKeepsWorkStoppedThenStartsJanuaryAndRestoresHistory(bool skip)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            using var run = new Run(data, startWork: false);
            run.Flow.Dialogue.SetReadCheck(_ => skip);
            Assert.That(run.Flow.StartIntro(), Is.True);
            Assert.That(run.Flow.StartIntro(), Is.False);
            Assert.That(run.Flow.IsIntro, Is.True);
            Assert.That(run.Flow.Dialogue.Line.Text, Is.EqualTo("한 달 전…"));
            Assert.That(run.Flow.Dialogue.Line.HideCharacter, Is.True);
            run.Work.AddTime(1000);
            Assert.That(run.Work.Run.Seconds, Is.Zero);
            Assert.That(run.Work.Run.Month, Is.Zero);
            Assert.That(run.Work.Run.Phase, Is.EqualTo(WorkPhase.Ready));
            Assert.That(run.Flow.CanGoNext, Is.False);
            Assert.That(run.Flow.OpenPanel(CarePanel.Schedule), Is.False);
            run.Flow.Dialogue.SetPresentationBlocked(true);
            Assert.That(run.Flow.Dialogue.Next(), Is.False);
            run.Flow.Dialogue.SetPresentationBlocked(false);
            if (skip)
            {
                Assert.That(run.Flow.Dialogue.Skip(false), Is.True);
                Assert.That(run.Flow.IsIntro, Is.True);
            }
            else
            {
                while (run.Flow.Dialogue.Line.HideCharacter) run.Flow.Dialogue.Next();
                Assert.That(run.Flow.Dialogue.Line.Emotion, Is.EqualTo(CareDialogueEmotion.Sad));
            }
            run.Drain();
            Assert.That(run.Flow.IsIntro, Is.False);
            Assert.That(run.Work.Run.Month, Is.EqualTo(1));
            Assert.That(run.Work.Run.Phase, Is.EqualTo(WorkPhase.Working));
            Assert.That(run.Work.Run.Seconds, Is.Zero);
            Assert.That(run.Work.Run.Money, Is.Zero);
            Assert.That(run.Care.Run.FinishedMonth, Is.Zero);
            Assert.That(run.Flow.Dialogue.WasCompleted("intro"), Is.True);
            var saved = run.Flow.CaptureSave();
            using var restored = new Run(data);
            Assert.DoesNotThrow(() => restored.Flow.RestoreSave(saved, SavePoint.WorkStart));
            Assert.That(restored.Flow.IsIntro, Is.False);
            Assert.That(restored.Flow.Dialogue.History.Count, Is.EqualTo(run.Flow.Dialogue.History.Count));
            Assert.That(restored.Flow.StartIntro(), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void NameplateCutsceneStartsFirstAndFinalChoiceAdvancesStraightToWork(int choice)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            using var run = new Run(data);
            run.ReachResult(2); run.Flow.ConfirmResult();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo("story_02_nameplate"));
            Assert.That(run.Flow.Dialogue.LineIndex, Is.Zero);
            Assert.That(run.Flow.Dialogue.Line.Cues.Any(cue => cue.Kind == CareDialogueCueKind.Custom
                && cue.Key == "story_nameplate"), Is.True);
            while (!run.Flow.Dialogue.HasChoices) run.Flow.Dialogue.Next();
            Assert.That(run.Flow.Dialogue.Current.Choices.All(item => string.IsNullOrEmpty(item.NextDialogue)), Is.True);
            var stats = run.Care.Run.GetStat(CareStat.Logic);
            var money = run.Work.Run.Money;
            run.Flow.Dialogue.SetPresentationBlocked(true);
            Assert.That(run.Flow.Dialogue.Next(), Is.False);
            Assert.That(run.Flow.Dialogue.Choose(choice), Is.False);
            Assert.That(run.Flow.HasStoryUnlock("dali_nameplate"), Is.False);
            Assert.That(run.Work.Run.Month, Is.EqualTo(2));
            run.Flow.Dialogue.SetPresentationBlocked(false); // Choice is committed under full diagonal cover.
            Assert.That(run.Flow.Dialogue.Choose(choice), Is.True);
            Assert.That(run.Flow.HasStoryUnlock("dali_nameplate"), Is.True);
            Assert.That(run.Work.Run.Month, Is.EqualTo(3));
            Assert.That(run.Flow.Phase, Is.EqualTo(CareFlowPhase.Working));
            Assert.That(run.Flow.Dialogue.IsOpen, Is.False);
            Assert.That(run.Flow.Dialogue.History.Last().IsChoice, Is.True);
            Assert.That(run.Flow.Dialogue.Choose(choice), Is.False);
            Assert.That(run.Flow.GoNext(), Is.False);
            Assert.That(run.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(stats));
            Assert.That(run.Work.Run.Money, Is.EqualTo(money));
        }

        [Test]
        public void SkipCanStopAtTheLastLineForCutsceneExitWithoutCompletingTheStory()
        {
            using var run = new Run(Data());
            run.Flow.Dialogue.SetReadCheck(_ => true);
            run.BeginResult(); run.Flow.ConfirmResult();
            run.Flow.Dialogue.Skip(); run.Flow.Dialogue.Choose(0);
            Assert.That(run.Flow.Dialogue.Skip(false), Is.True);
            Assert.That(run.Flow.Dialogue.IsOpen, Is.True);
            Assert.That(run.Flow.Dialogue.LineIndex, Is.EqualTo(run.Flow.Dialogue.Current.Lines.Length - 1));
            Assert.That(run.Flow.HasStoryUnlock("nameplate"), Is.False);
            run.Flow.Dialogue.Next();
            Assert.That(run.Flow.HasStoryUnlock("nameplate"), Is.True);
        }

        [TestCase(CareLeaveKind.Early, -30)]
        [TestCase(CareLeaveKind.SlightlyEarly, -20)]
        [TestCase(CareLeaveKind.OnTime, 0)]
        [TestCase(CareLeaveKind.Overtime, 11)]
        public void MonthlyArrivalSelectsItsOwnDialogueForAllTwelveMonths(CareLeaveKind kind, int difference)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            data.Stories = Array.Empty<CareStoryData>();
            data.Promises = Array.Empty<CarePromiseData>();
            using var run = new Run(data);
            Assert.That(data.Leave.Months.Length, Is.EqualTo(12));
            var shown = new System.Collections.Generic.HashSet<string>();
            for (int month = 1; month <= 12; month++)
            {
                while (run.Work.CurrentPaper != null)
                {
                    Assert.That(run.Work.StartHold(run.Work.CurrentPaper.Id, 0), Is.True);
                    run.Work.AddTime(0.5);
                }
                run.Work.AddTime(run.Work.MonthSeconds + difference - run.Work.Run.Seconds);
                Assert.That(run.Work.LeaveWork(), Is.True);
                run.Flow.EnterCare();
                Assert.That(run.Flow.LeaveKind, Is.EqualTo(kind));
                Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo(data.Leave.GetDialogue(kind, month)));
                Assert.That(shown.Add(run.Flow.Dialogue.Current.Id), Is.True, "Each month needs a different reaction.");
                run.Drain();
                for (int week = 0; week < 4; week++) run.Care.SetLesson(week, "rest");
                Assert.That(run.Flow.GoNext(), Is.True);
                run.Flow.ConfirmResult(); run.Drain();
            }
        }

        [Test]
        public void PreviousAutomaticSpeechHistoryCanBeLoadedAndSavedAgain()
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(
                Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            using var run = new Run(data);
            var old = new CareFlowSaveData
            {
                Completed = Array.Empty<string>(), Promises = Array.Empty<SavedPromise>(),
                History = new[] { new SavedDialogueLog { Month = 2, Dialogue = "story_02_nameplate", Index = 3 } }
            };
            run.Flow.RestoreSave(old, SavePoint.EndingChoice);
            Assert.That(run.Flow.Dialogue.History.Single().Text, Is.EqualTo("일할 때도 잘 보이는 곳에 둘게."));
            var saved = run.Flow.CaptureSave();
            Assert.That(saved.HistoryVersion, Is.EqualTo(1));
            Assert.That(saved.History.Single().Legacy, Is.True);
            using var restored = new Run(data);
            restored.Flow.RestoreSave(saved, SavePoint.EndingChoice);
            Assert.That(restored.Flow.Dialogue.History.Single().Text, Is.EqualTo(run.Flow.Dialogue.History.Single().Text));
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void ConfiguredCookieStoryMatchesPromiseOutcome(bool accept, bool early)
        {
            var data = JsonUtility.FromJson<CareDialogueFile>(File.ReadAllText(Path.Combine(Application.dataPath, "WorkGame/Samples/CareDialogue.json")));
            foreach (var dialogue in data.Dialogues) dialogue.Choices ??= Array.Empty<CareDialogueChoice>();
            using var run = new Run(data);
            run.ReachResult(3); run.Flow.ConfirmResult();
            while (!run.Flow.Dialogue.HasChoices) run.Flow.Dialogue.Next();
            run.Flow.Dialogue.Choose(accept ? 0 : 1);
            if (early)
            {
                while (run.Work.CurrentPaper != null)
                {
                    Assert.That(run.Work.StartHold(run.Work.CurrentPaper.Id, 0), Is.True);
                    run.Work.AddTime(0.5);
                }
            }
            else run.Work.AddTime(run.Work.MonthSeconds);
            Assert.That(run.Work.LeaveWork(), Is.True);
            run.Flow.EnterCare(); run.Drain();
            for (int i = 0; i < 4; i++) run.Care.SetLesson(i, "rest");
            run.Flow.GoNext(); run.Flow.ConfirmResult();
            Assert.That(run.Flow.Dialogue.Current.Id, Is.EqualTo(accept && early ? "story_04_cookie_together" : "story_04_cookie"));
        }
    }
}
