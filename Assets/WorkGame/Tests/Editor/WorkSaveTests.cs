using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WorkGame.Tests
{
    // 실제 PlayerPrefs에는 쓰지 않습니다. 직렬화와 게임 시스템은 실제 구현을 사용합니다.
    public sealed class WorkSaveTests
    {
        private sealed class MemoryStore : IGameSaveStore
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
            public int Writes;
            public bool FailWrites;
            public string Read(string key) => Data.TryGetValue(key, out var value) ? value : "";
            public void Write(string key, string json)
            {
                if (FailWrites) throw new InvalidOperationException("저장 공간 부족");
                Data[key] = json; Writes++;
            }
            public void Delete(string key) => Data.Remove(key);
        }

        private sealed class TestRun : IDisposable
        {
            public readonly WorkSystem Work;
            public readonly CareSystem Care;
            public readonly CareFlowSystem Flow;
            public readonly WorkUpgradeSystem Upgrades;
            public readonly WorkSaveSession Session;

            public TestRun(WorkSaveSystem saves, GameSaveData saved = null, int endingCount = 6)
            {
                var papers = Enumerable.Range(1, 12).SelectMany(month => new[] { Paper(month * 10, month), Paper(month * 10 + 1, month) });
                Work = new WorkSystem(new WorkSettings(), papers, 123);
                if (saved != null) Work.RestoreSave(saved.Work);
                else Work.InheritUpgrades(saves.GetInheritedUpgrades().Where(id => id == "hands"));
                Upgrades = new WorkUpgradeSystem(Work, new[]
                {
                    new WorkUpgradeNode("hands", 50, Array.Empty<string>(), new[]
                    {
                        new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.ExtraSeconds, Value = 10 },
                        new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointChancePercent, Value = 50 },
                        new WorkUpgradeEffect { Kind = WorkUpgradeEffectKind.PointMultiplier, Value = 3 }
                    })
                });
                Upgrades.Changed += () => saves.SaveUpgrades(Work.Run.BoughtUpgrades);
                Care = new CareSystem(Work, new[]
                {
                    new CareLesson("home", "홈스쿨링", 1, 0, new[] { new CareGain(CareStat.Logic, 1) }),
                    new CareLesson("rest", "휴식", 1, 0, Array.Empty<CareGain>()),
                    new CareLesson("math", "도토리 수학", 1, 20, new[] { new CareGain(CareStat.Research, 5) })
                }, Array.Empty<CareSynergy>(), new[]
                {
                    new CareGift("desk", "책상", CareGiftKind.Desk, 100, 3, Array.Empty<CareGiftBonus>()),
                    new CareGift("desk2", "새 책상", CareGiftKind.Desk, 150, 4, Array.Empty<CareGiftBonus>())
                }, Enumerable.Range(1, endingCount).Select(index => Ending("job" + index, 100 - index)),
                    Ending("unemployed", 0), new[] { new CareGain(CareStat.Logic, 7) });
                Flow = new CareFlowSystem(Work, Care, DialogueData());
                if (saved != null)
                {
                    Care.RestoreSave(saved.Care);
                    Flow.RestoreSave(saved.Flow, saved.Point);
                    saves.UseCheckpoint(saved);
                }
                Session = new WorkSaveSession(saves, 123, Work, Care, Flow, saved != null);
                Session.Failed += error => Assert.Fail(error.ToString());
                if (saved == null) Work.StartRun();
            }

            public void Submit(int choice = 0)
            {
                int id = Work.CurrentPaper.Id;
                Assert.That(Work.SelectPaper(id), Is.True);
                Assert.That(Work.StartHold(id, choice), Is.True);
                Work.AddTime(0.5);
            }

            public void EnterCare()
            {
                Work.AddTime(Work.MonthSeconds);
                Assert.That(Work.LeaveWork(), Is.True);
                Flow.EnterCare();
                FinishDialogue();
            }

            public void FinishDialogue()
            {
                while (Flow.Dialogue.IsOpen)
                {
                    if (Flow.Dialogue.HasChoices) Flow.Dialogue.Choose(0);
                    else Flow.Dialogue.Next();
                }
            }

            public void FinishCare(string lesson = "rest")
            {
                for (int i = 0; i < 4; i++) Assert.That(Care.SetLesson(i, lesson), Is.True);
                Assert.That(Flow.GoNext(), Is.True);
                Flow.ConfirmResult();
                FinishDialogue();
            }

            public void ReachEnding()
            {
                for (int month = Work.Run.Month; month <= 12; month++) { EnterCare(); FinishCare(); }
            }

            public void Dispose() { Session.Dispose(); Flow.Dispose(); Care.Dispose(); Work.Dispose(); }
        }

        private static WorkPaper Paper(int id, int month) => new WorkPaper(id, month, "01", "AI 산업 육성", "제품",
            "AI 기술 활용도", "%", PickRule.Highest, PaperDisplay.Number,
            new[] { new WorkChoice("A", 90), new WorkChoice("B", 10), new WorkChoice("C", 50) });

        private static CareEnding Ending(string id, int priority) => new CareEnding(id, id, priority,
            Array.Empty<CareGain>(), "후일담", "낮음", "보통", "높음");

        private static CareDialogueFile DialogueData() => new CareDialogueFile
        {
            Leave = new CareLeaveSettings { EarlyDialogue = "entry", SlightlyEarlyDialogue = "entry", OnTimeDialogue = "entry", OvertimeDialogue = "entry" },
            Dialogues = new[]
            {
                new CareDialogueData { Id = "entry", Friendship = -2, Lines = new[] { new CareDialogueLine { Text = "왔어요?", ImageKey = "dali" } } },
                new CareDialogueData { Id = "talk", Friendship = 3, Lines = new[] { new CareDialogueLine { Text = "도토리가 좋아요." } } },
                new CareDialogueData
                {
                    Id = "offer", Lines = new[] { new CareDialogueLine { Text = "다음 달에는 야근하지 마요." } },
                    Choices = new[] { new CareDialogueChoice { Key = "accept", Text = "좋아" }, new CareDialogueChoice { Key = "decline", Text = "미안" } }
                },
                new CareDialogueData { Id = "success", Friendship = 5, Lines = new[] { new CareDialogueLine { Text = "약속 지켰네요!" } } },
                new CareDialogueData { Id = "failure", Friendship = -5, Lines = new[] { new CareDialogueLine { Text = "기다렸어요." } } }
            },
            MonthTalks = new[] { new CareMonthTalk { Month = 1, Dialogue = "talk" } },
            Promises = new[] { new CarePromiseData { Id = "promise", OfferMonth = 3, TargetMonth = 4, Condition = "NoOvertime",
                OfferDialogue = "offer", SuccessDialogue = "success", FailureDialogue = "failure" } }
        };

        private static GameSaveData Load(WorkSaveSystem saves, SaveSlot slot = SaveSlot.Auto)
        {
            Assert.That(saves.TryLoad(slot, out var data, out var error), Is.True, error);
            return data;
        }

        [Test]
        public void WorkResumeRestartsTimerAndRollsBackSubmissionsAndFax()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.Submit();
            Assert.That(run.Work.Fax.TrySend(11, true), Is.True);
            run.Work.AddTime(2);
            Assert.That(saves.SaveManual(SaveSlot.Manual1), Is.True);
            using var loaded = new TestRun(saves, Load(saves, SaveSlot.Manual1));
            Assert.That(loaded.Work.Run.Seconds, Is.Zero);
            Assert.That(loaded.Work.Run.SkillPoints, Is.Zero);
            Assert.That(loaded.Work.MonthDone, Is.Zero);
            Assert.That(loaded.Work.Fax.Jobs, Is.Empty);
            Assert.That(loaded.Work.Run.Papers.All(paper => paper.State == PaperState.Pending), Is.True);
        }

        [Test]
        public void CareResumeRollsBackTuitionFurnitureStatsAndCompletedTalk()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.EnterCare();
            long money = run.Work.Run.Money;
            Assert.That(run.Care.BuyGift("desk"), Is.True);
            Assert.That(run.Flow.Talk(), Is.True);
            run.FinishDialogue();
            for (int i = 0; i < 4; i++) run.Care.SetLesson(i, "math");
            Assert.That(run.Flow.GoNext(), Is.True); // 결과창을 확인하기 전에 종료한 상황
            Assert.That(run.Care.Run.GetStat(CareStat.Research), Is.EqualTo(20));
            saves.SaveManual(SaveSlot.Manual2);
            using var loaded = new TestRun(saves, Load(saves, SaveSlot.Manual2));
            Assert.That(loaded.Flow.Phase, Is.EqualTo(CareFlowPhase.Report));
            Assert.That(loaded.Work.Run.Money, Is.EqualTo(money));
            Assert.That(loaded.Care.Run.Gifts, Is.Empty);
            Assert.That(loaded.Care.Run.GetStat(CareStat.Research), Is.Zero);
            Assert.That(loaded.Care.Run.GetStat(CareStat.Logic), Is.EqualTo(7));
            Assert.That(loaded.Care.Plan.All(string.IsNullOrEmpty), Is.True);
            Assert.That(loaded.Flow.Dialogue.Completed, Is.Empty);
            Assert.That(loaded.Care.CanPlan, Is.True);
            loaded.Flow.EnterCare();
            loaded.FinishDialogue();
            loaded.Flow.EnterCare(); // 중복 진입은 친밀도를 또 깎지 않습니다.
            Assert.That(loaded.Care.Run.Friendship, Is.EqualTo(-2));
            Assert.That(loaded.Flow.CanTalk, Is.True);
            Assert.That(loaded.Work.Run.Money, Is.EqualTo(money), "급여는 다시 지급하지 않습니다.");
        }

        [Test]
        public void NextWorkStartKeepsConfirmedCareUpgradesAndFurniture()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.Submit();
            run.EnterCare();
            Assert.That(run.Upgrades.Buy("hands"), Is.True);
            run.Work.SetSortEnabled(false); run.Work.SetFaxVisible(false);
            Assert.That(run.Care.BuyGift("desk"), Is.True);
            Assert.That(run.Care.BuyGift("desk2"), Is.True);
            run.FinishCare("math");
            using var loaded = new TestRun(saves, Load(saves));
            Assert.That(loaded.Work.Run.Month, Is.EqualTo(2));
            Assert.That(loaded.Work.Run.Seconds, Is.Zero);
            Assert.That(loaded.Work.Run.Money, Is.EqualTo(670));
            Assert.That(loaded.Work.Run.BoughtUpgrades, Is.EqualTo(new[] { "hands" }));
            Assert.That(loaded.Work.MonthSeconds, Is.EqualTo(100));
            Assert.That(loaded.Work.Run.SortEnabled, Is.False);
            Assert.That(loaded.Work.Run.FaxVisible, Is.False);
            Assert.That(loaded.Care.Run.GetActiveGift(CareGiftKind.Desk), Is.EqualTo("desk2"));
            Assert.That(loaded.Care.Run.GetStat(CareStat.Research), Is.EqualTo(20));
            Assert.That(loaded.Work.PendingCount, Is.EqualTo(3)); // 지난달 미처리 1장 + 새 업무 2장
            Assert.That(loaded.Flow.Dialogue.History.Select(log => log.Text), Is.EqualTo(run.Flow.Dialogue.History.Select(log => log.Text)));
        }

        [Test]
        public void WorkClockDoesNotWriteEveryFrame()
        {
            var store = new MemoryStore();
            var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            int writes = store.Writes;
            for (int frame = 0; frame < 600; frame++) run.Work.AddTime(1.0 / 60);
            Assert.That(store.Writes, Is.EqualTo(writes));
        }

        [Test]
        public void SlotsAreIndependentAndManualSaveCopiesEntrySnapshot()
        {
            var store = new MemoryStore();
            var saves = new WorkSaveSystem(store);
            Assert.That(saves.SaveManual(SaveSlot.Manual1), Is.False);
            using var run = new TestRun(saves);
            saves.SaveManual(SaveSlot.Manual1);
            run.EnterCare();
            saves.SaveManual(SaveSlot.Manual2);
            run.FinishCare();
            saves.SaveManual(SaveSlot.Manual3);
            saves.SaveManual(SaveSlot.Manual4);
            Assert.That(Load(saves, SaveSlot.Manual1).Point, Is.EqualTo(SavePoint.WorkStart));
            Assert.That(Load(saves, SaveSlot.Manual1).Work.Month, Is.EqualTo(1));
            Assert.That(Load(saves, SaveSlot.Manual2).Point, Is.EqualTo(SavePoint.CareStart));
            Assert.That(Load(saves, SaveSlot.Manual3).Work.Month, Is.EqualTo(2));
            Assert.That(Load(saves, SaveSlot.Manual4).Work.Month, Is.EqualTo(2));
            Assert.That(store.Data.Count, Is.EqualTo(6)); // 자동 1 + 수동 4 + 공용 프로필
            using var loaded = new TestRun(saves, Load(saves, SaveSlot.Manual2));
            Assert.That(Load(saves).Point, Is.EqualTo(SavePoint.CareStart));
            Assert.That(Load(saves, SaveSlot.Manual3).Work.Month, Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(6)]
        public void EndingResumeKeepsUpToFiveOrderedCandidatesAndClearsSelectedEnding(int endingCount)
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves, endingCount: endingCount);
            run.ReachEnding();
            var data = Load(saves);
            Assert.That(data.Point, Is.EqualTo(SavePoint.EndingChoice));
            Assert.That(data.Care.EndingChoices, Is.EqualTo(Enumerable.Range(1, Math.Min(5, endingCount)).Select(index => "job" + index)));
            if (endingCount > 1) Assert.That(run.Flow.ChooseEnding("job3"), Is.True);
            using var loaded = new TestRun(saves, data, endingCount);
            Assert.That(loaded.Care.Run.Ending, Is.Null);
            Assert.That(loaded.Flow.Phase, Is.EqualTo(CareFlowPhase.EndingChoice));
            Assert.That(loaded.Care.GetEndingChoices().Select(ending => ending.Id), Is.EqualTo(data.Care.EndingChoices));
            loaded.Flow.ResumeEndingChoice();
            if (endingCount == 1) Assert.That(loaded.Care.Run.Ending.Ending.Id, Is.EqualTo("job1"));
            else
            {
                Assert.That(loaded.Care.Run.Ending, Is.Null);
                Assert.That(loaded.Flow.ChooseEnding("job6"), Is.False);
                Assert.That(loaded.Flow.ChooseEnding("job2"), Is.True);
            }
        }

        [Test]
        public void ReadRecordsSurviveRollbackSlotDeletionAndNewRun()
        {
            var store = new MemoryStore();
            var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            saves.SaveManual(SaveSlot.Manual1);
            saves.MarkEndingRead("job1"); saves.MarkDialogueRead("entry");
            using var loaded = new TestRun(saves, Load(saves, SaveSlot.Manual1));
            saves.Delete(SaveSlot.Manual1);
            var reopened = new WorkSaveSystem(store);
            using var newRun = new TestRun(reopened);
            Assert.That(reopened.WasEndingRead("job1"), Is.True);
            Assert.That(reopened.WasDialogueRead("entry"), Is.True);
            newRun.Flow.Dialogue.SetReadCheck(reopened.WasDialogueRead);
            newRun.Work.AddTime(90); newRun.Work.LeaveWork(); newRun.Flow.EnterCare();
            Assert.That(newRun.Flow.Dialogue.CanSkip, Is.True);
        }

        [Test]
        public void CollectionUnlockRequiresAnEndingReadAndSurvivesReload()
        {
            var store = new MemoryStore();
            var saves = new WorkSaveSystem(store);
            Assert.That(saves.HasReadEnding, Is.False);
            saves.MarkDialogueRead("entry");
            Assert.That(saves.HasReadEnding, Is.False);
            saves.MarkEndingRead("job1");
            Assert.That(saves.HasReadEnding, Is.True);
            var reloaded = new WorkSaveSystem(store);
            Assert.That(reloaded.HasReadEnding, Is.True);
            Assert.That(reloaded.WasEndingRead("job1"), Is.True);
            Assert.That(new WorkSaveSystem(new MemoryStore()).HasReadEnding, Is.False);
        }

        [Test]
        public void PromiseRestoresBeforeResolutionAndAppliesResultOnce()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            for (int month = 1; month <= 3; month++) { run.EnterCare(); run.FinishCare(); }
            Assert.That(run.Flow.Promises.Records.Single().State, Is.EqualTo(CarePromiseState.Accepted));
            run.EnterCare();
            int friendship = run.Care.Run.Friendship;
            var data = Load(saves);
            Assert.That(data.Flow.Promises.Single().State, Is.EqualTo(CarePromiseState.Accepted));
            using var loaded = new TestRun(saves, data);
            loaded.Flow.EnterCare(); loaded.FinishDialogue(); loaded.Flow.EnterCare();
            Assert.That(loaded.Care.Run.Friendship, Is.EqualTo(friendship));
            Assert.That(loaded.Flow.Promises.Records.Single().State, Is.EqualTo(CarePromiseState.Succeeded));
        }

        [Test]
        public void QuarterlyReviewMatchesAfterRestoringBacklogAndWrongAnswers()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.Submit(1); run.EnterCare(); run.FinishCare();
            var start = Load(saves);
            run.Submit(); run.EnterCare(); run.FinishCare();
            run.Submit(); run.EnterCare();
            var expected = run.Work.Run.LastResult.Review;
            using var loaded = new TestRun(saves, start);
            loaded.Submit(); loaded.EnterCare(); loaded.FinishCare();
            loaded.Submit(); loaded.EnterCare();
            var actual = loaded.Work.Run.LastResult.Review;
            Assert.That(actual.Total, Is.EqualTo(expected.Total));
            Assert.That(actual.Right, Is.EqualTo(expected.Right));
            Assert.That(actual.Grade, Is.EqualTo(expected.Grade));
            Assert.That(actual.Bonus, Is.EqualTo(expected.Bonus));
            Assert.That(loaded.Work.Run.Attendance, Is.EqualTo(run.Work.Run.Attendance));
            Assert.That(loaded.Work.Run.Money, Is.EqualTo(run.Work.Run.Money));
        }

        [Test]
        public void RandomPointRewardsContinueFromTheSavedSequence()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.Submit(); run.EnterCare(); run.Upgrades.Buy("hands"); run.FinishCare();
            // 월 2에서 난수를 사용하고 월 3 시작 상태를 저장합니다.
            run.Submit(); run.EnterCare(); run.FinishCare();
            var start = Load(saves);
            var rewards = new List<int>();
            run.Work.PaperSubmitted += result => rewards.Add(result.SkillPointsGained);
            while (run.Work.CurrentPaper != null) run.Submit();
            using var loaded = new TestRun(saves, start);
            var restoredRewards = new List<int>();
            loaded.Work.PaperSubmitted += result => restoredRewards.Add(result.SkillPointsGained);
            while (loaded.Work.CurrentPaper != null) loaded.Submit();
            Assert.That(restoredRewards, Is.EqualTo(rewards));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("{}")]
        [TestCase("null")]
        [TestCase("{\"Version\":999}")]
        public void InvalidOrEmptySlotDoesNotReturnPlayableData(string json)
        {
            var store = new MemoryStore(); store.Data["WorkGame.Save.Auto"] = json;
            var saves = new WorkSaveSystem(store);
            Assert.That(saves.TryLoad(SaveSlot.Auto, out var data, out var error), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void ChangedPlanAndMissingFurnitureFailWithoutMutatingOriginalRun()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.EnterCare();
            var data = Load(saves);
            using var changedWork = new WorkSystem(new WorkSettings(), new[] { Paper(999, 1) });
            Assert.Throws<ArgumentException>(() => changedWork.RestoreSave(data.Work));
            data.Care.Gifts = new[] { "missing" };
            Assert.Throws<ArgumentException>(() => run.Care.RestoreSave(data.Care));
            Assert.That(run.Work.Run.Month, Is.EqualTo(1));
            Assert.That(run.Care.Run.Gifts, Is.Empty);
        }

        [Test]
        public void FailedWriteDoesNotReplaceTheCurrentCheckpoint()
        {
            var store = new MemoryStore(); var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            string before = store.Data["WorkGame.Save.Auto"];
            var replacement = Load(saves); replacement.Work.Money = 9999;
            store.FailWrites = true;
            Assert.Throws<InvalidOperationException>(() => saves.UseCheckpoint(replacement));
            store.FailWrites = false;
            saves.SaveManual(SaveSlot.Manual1);
            Assert.That(store.Data["WorkGame.Save.Manual1"], Is.EqualTo(before));
        }

        [Test]
        public void CaptureIsDetachedAndKeepsLongCurrencyWithoutDocumentText()
        {
            var store = new MemoryStore(); var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            var data = Load(saves); data.Work.Money = 5000000000L;
            saves.UseCheckpoint(data);
            data.Work.Money = 0; data.Work.Papers[0].Choice = 0;
            var loaded = Load(saves);
            Assert.That(loaded.Work.Money, Is.EqualTo(5000000000L));
            Assert.That(loaded.Work.Papers[0].Choice, Is.EqualTo(-1));
            Assert.That(store.Data["WorkGame.Save.Auto"], Does.Not.Contain("AI 기술 활용도"));
            Assert.That(JsonUtility.ToJson(loaded).Length, Is.LessThan(100000));
        }

        [Test]
        public void TitleLoadRequestIsConsumedOnceAndNewRunClearsIt()
        {
            WorkStartRequest.LoadManual(3);
            Assert.That(WorkStartRequest.Take(), Is.EqualTo(SaveSlot.Manual3));
            Assert.That(WorkStartRequest.Take(), Is.Null);
            WorkStartRequest.ContinueAuto();
            WorkStartRequest.NewRun();
            Assert.That(WorkStartRequest.Take(), Is.Null);
        }

        [Test]
        public void ReturningToTitleSavesTheCheckpointInsteadOfLiveChanges()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            Assert.That(saves.SaveAuto(), Is.False);
            using var run = new TestRun(saves);
            run.Submit();
            Assert.That(saves.SaveAuto(), Is.True);
            Assert.That(Load(saves).Work.SkillPoints, Is.Zero);
            run.EnterCare();
            long startingMoney = run.Work.Run.Money;
            run.Care.BuyGift("desk");
            Assert.That(saves.SaveAuto(), Is.True);
            var data = Load(saves);
            Assert.That(data.Point, Is.EqualTo(SavePoint.CareStart));
            Assert.That(data.Care.Gifts, Is.Empty);
            Assert.That(data.Work.Money, Is.EqualTo(startingMoney));
        }

        [Test]
        public void RestoredGradesComeFromDetailStatsAndFriendshipCanBeNegative()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            var data = Load(saves);
            data.Care.Stats[0] = 70; data.Care.Stats[1] = 60; data.Care.Stats[2] = 90;
            data.Care.Friendship = -123;
            using var loaded = new TestRun(saves, data);
            Assert.That(loaded.Care.Run.GetStat(CareStat.Knowledge), Is.EqualTo(220));
            Assert.That(loaded.Care.GetGrade(CareStat.Knowledge), Is.EqualTo(CareGrade.A));
            Assert.That(loaded.Care.Run.Friendship, Is.EqualTo(-123));
        }

        [Test]
        public void MissingNestedDataAndInProgressWorkCannotReplaceTheCheckpoint()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            var missing = Load(saves); missing.Care.Stats = null;
            Assert.Throws<ArgumentException>(() => saves.UseCheckpoint(missing));
            run.Submit();
            Assert.Throws<ArgumentException>(() => saves.Capture(123, SavePoint.WorkStart, run.Work, run.Care, run.Flow));
            Assert.That(Load(saves).Work.SkillPoints, Is.Zero);
        }

        [Test]
        public void UnemployedFallbackAlsoResumesBeforeTheSingleEnding()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves, endingCount: 0);
            run.ReachEnding();
            Assert.That(run.Care.Run.Ending.Ending.Id, Is.EqualTo("unemployed"));
            var data = Load(saves);
            using var loaded = new TestRun(saves, data, 0);
            Assert.That(loaded.Care.Run.Ending, Is.Null);
            loaded.Flow.ResumeEndingChoice();
            Assert.That(loaded.Care.Run.Ending.Ending.Id, Is.EqualTo("unemployed"));
        }

        [Test]
        public void SlotPreviewShowsTheSavedMainStatsInsteadOfCurrentCareProgress()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.EnterCare();
            for (int i = 0; i < 4; i++) run.Care.SetLesson(i, "math");
            Assert.That(run.Flow.GoNext(), Is.True);
            var info = saves.GetSlotInfo(SaveSlot.Auto);
            Assert.That(info.State, Is.EqualTo(SaveSlotState.Ready));
            Assert.That(info.Month, Is.EqualTo(1));
            Assert.That(info.GetStat(CareStat.Knowledge), Is.EqualTo(7));
            Assert.That(run.Care.Run.GetStat(CareStat.Knowledge), Is.EqualTo(27));
        }

        [Test]
        public void SlotPreviewDistinguishesEmptyAndInvalidFiles()
        {
            var store = new MemoryStore();
            store.Data["WorkGame.Save.Manual1"] = "{}";
            var saves = new WorkSaveSystem(store);
            Assert.That(saves.GetSlotInfo(SaveSlot.Auto).State, Is.EqualTo(SaveSlotState.Empty));
            Assert.That(saves.GetSlotInfo(SaveSlot.Manual1).State, Is.EqualTo(SaveSlotState.Invalid));
        }

        [Test]
        public void FifthSlotCanBeSavedRestoredAndRequestedFromTitle()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.EnterCare(); run.FinishCare("math");
            Assert.That(saves.SaveManual(SaveSlot.Manual4), Is.True);
            using var loaded = new TestRun(saves, Load(saves, SaveSlot.Manual4));
            Assert.That(loaded.Work.Run.Month, Is.EqualTo(2));
            Assert.That(loaded.Care.Run.GetStat(CareStat.Research), Is.EqualTo(20));
            WorkStartRequest.LoadManual(4);
            Assert.That(WorkStartRequest.Take(), Is.EqualTo(SaveSlot.Manual4));
            WorkStartRequest.Load(SaveSlot.Auto);
            Assert.That(WorkStartRequest.Take(), Is.EqualTo(SaveSlot.Auto));
            Assert.That(Enum.GetValues(typeof(SaveSlot)).Length, Is.EqualTo(5));
        }

        [TestCase(SavePoint.WorkStart)]
        [TestCase(SavePoint.CareStart)]
        public void SavingFromPauseKeepsTheEntryCheckpointAndLoadsUnpaused(SavePoint point)
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            run.Submit();
            if (point == SavePoint.CareStart)
            {
                run.EnterCare();
                Assert.That(run.Care.BuyGift("desk"), Is.True);
                for (int i = 0; i < 4; i++) run.Care.SetLesson(i, "math");
                Assert.That(run.Flow.GoNext(), Is.True);
            }
            run.Work.SetPaused(true);
            Assert.That(run.Work.IsPaused, Is.True);
            Assert.That(saves.SaveManual(SaveSlot.Manual4), Is.True);
            var saved = Load(saves, SaveSlot.Manual4);
            Assert.That(saved.Point, Is.EqualTo(point));
            using var loaded = new TestRun(saves, saved);
            Assert.That(loaded.Work.IsPaused, Is.False);
            Assert.That(loaded.Care.Run.Gifts, Is.Empty);
            Assert.That(loaded.Care.Run.GetStat(CareStat.Research), Is.Zero);
            Assert.That(loaded.Work.Run.SkillPoints, Is.EqualTo(point == SavePoint.WorkStart ? 0 : 100));
            if (point == SavePoint.CareStart) Assert.That(loaded.Care.CanPlan, Is.True);
            else Assert.That(loaded.Work.Run.Seconds, Is.Zero);
        }

        [Test]
        public void NewRunInheritsSkillsButResetsFurnitureMoneyPointsAndCare()
        {
            var store = new MemoryStore(); var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            run.Submit(); run.EnterCare();
            Assert.That(run.Upgrades.Buy("hands"), Is.True);
            Assert.That(run.Care.BuyGift("desk"), Is.True);
            run.FinishCare("math");
            saves.MarkEndingRead("job1"); saves.MarkDialogueRead("entry");
            var reopened = new WorkSaveSystem(store);
            using var next = new TestRun(reopened);
            Assert.That(next.Work.Run.Month, Is.EqualTo(1));
            Assert.That(next.Upgrades.GetState("hands"), Is.EqualTo(WorkUpgradeState.Bought));
            Assert.That(next.Work.MonthSeconds, Is.EqualTo(100));
            Assert.That(next.Work.Run.SkillPoints, Is.Zero);
            Assert.That(next.Work.Run.Money, Is.Zero);
            Assert.That(next.Care.Run.Gifts, Is.Empty);
            Assert.That(next.Care.Run.ActiveGifts.All(string.IsNullOrEmpty), Is.True);
            Assert.That(next.Care.Run.GetStat(CareStat.Research), Is.Zero);
            Assert.That(next.Care.Run.Friendship, Is.Zero);
            Assert.That(reopened.WasEndingRead("job1"), Is.True);
            Assert.That(reopened.WasDialogueRead("entry"), Is.True);
        }

        [Test]
        public void SkillsRemainInheritedAfterDeletingAllSlots()
        {
            var store = new MemoryStore(); var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            run.Submit(); run.EnterCare(); run.Upgrades.Buy("hands"); run.FinishCare();
            foreach (SaveSlot slot in Enum.GetValues(typeof(SaveSlot))) saves.Delete(slot);
            using var next = new TestRun(new WorkSaveSystem(store));
            Assert.That(next.Work.Run.BoughtUpgrades, Is.EqualTo(new[] { "hands" }));
        }

        [Test]
        public void LegacySaveFilesSupplyInheritedSkillsWithoutChangingReadRecords()
        {
            var store = new MemoryStore(); var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            run.Submit(); run.EnterCare(); run.Upgrades.Buy("hands"); run.FinishCare();
            // 계승 필드가 없던 이전 버전의 공용 기록입니다.
            store.Data["WorkGame.Save.Profile"] = "{\"Version\":1,\"Endings\":[\"job1\"],\"Dialogues\":[]}";
            var reopened = new WorkSaveSystem(store);
            using var next = new TestRun(reopened);
            Assert.That(next.Work.Run.BoughtUpgrades, Is.EqualTo(new[] { "hands" }));
            Assert.That(reopened.WasEndingRead("job1"), Is.True);
        }

        [Test]
        public void LoadingAnOlderSlotKeepsLatestSkillsAndRestoresSlotPoints()
        {
            var saves = new WorkSaveSystem(new MemoryStore());
            using var run = new TestRun(saves);
            saves.SaveManual(SaveSlot.Manual1);
            run.Submit(); run.EnterCare(); run.Upgrades.Buy("hands"); run.FinishCare();
            using var oldRun = new TestRun(saves, Load(saves, SaveSlot.Manual1));
            Assert.That(oldRun.Work.Run.BoughtUpgrades, Is.EqualTo(new[] { "hands" }));
            Assert.That(oldRun.Work.MonthSeconds, Is.EqualTo(100));
            Assert.That(oldRun.Work.Run.SkillPoints, Is.Zero);
            Assert.That(saves.GetInheritedUpgrades(), Is.EqualTo(new[] { "hands" }));
            using var next = new TestRun(saves);
            Assert.That(next.Work.Run.BoughtUpgrades, Is.EqualTo(new[] { "hands" }));
        }

        [Test]
        public void PurchasedSkillsPersistImmediatelyWithoutChangingSlotPoints()
        {
            var store = new MemoryStore();
            var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            run.Submit(); run.EnterCare();
            int points = run.Work.Run.SkillPoints;
            saves.SaveManual(SaveSlot.Manual1);
            Assert.That(run.Upgrades.Buy("hands"), Is.True);
            var reopened = new WorkSaveSystem(store);
            using var loaded = new TestRun(reopened, Load(reopened, SaveSlot.Manual1));
            Assert.That(loaded.Work.Run.BoughtUpgrades, Is.EqualTo(new[] { "hands" }));
            Assert.That(loaded.Work.Run.SkillPoints, Is.EqualTo(points));
            Assert.That(loaded.Upgrades.CanBuy("hands"), Is.False);
        }

        [Test]
        public void MigratedSlotsCannotOverwriteTheLatestSharedSkills()
        {
            var store = new MemoryStore(); var saves = new WorkSaveSystem(store);
            using var run = new TestRun(saves);
            run.Submit(); run.EnterCare(); run.Upgrades.Buy("hands"); run.FinishCare();
            saves.SaveManual(SaveSlot.Manual1); // 과거 슬롯에 구매 기록이 남아 있습니다.
            saves.SaveUpgrades(Array.Empty<string>()); // 공용 기록을 덮어씁니다.
            var reopened = new WorkSaveSystem(store);
            Assert.That(Load(reopened, SaveSlot.Manual1).Work.Upgrades, Is.Empty);
            Assert.That(reopened.GetInheritedUpgrades(), Is.Empty);
        }
    }
}
