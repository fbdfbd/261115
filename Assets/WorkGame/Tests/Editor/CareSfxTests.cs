using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WorkGame.Tests
{
    public sealed class CareSfxTests
    {
        private sealed class Handle : IGameSfxHandle
        {
            public bool IsPlaying { get; private set; } = true;
            public bool Paused;
            public void SetPaused(bool paused) => Paused = paused;
            public void Dispose() => IsPlaying = false;
        }
        private sealed class Player : IGameSfxPlayer
        {
            public readonly List<(GameSfxClip sound, Handle handle)> Calls = new List<(GameSfxClip, Handle)>();
            public IGameSfxHandle Play(GameSfxClip sound, bool loop = false)
            { var h = new Handle(); Calls.Add((sound, h)); return h; }
        }
        private sealed class Source : ICareSfxSource
        {
            public event Action<CareSfxCue> SoundRequested;
            public void Emit(CareSfxCue cue) => SoundRequested?.Invoke(cue);
        }
        private sealed class Run : IDisposable
        {
            public WorkSystem Work;
            public CareSystem Care;
            public CareFlowSystem Flow;
            public CareSfxSO Sounds = ScriptableObject.CreateInstance<CareSfxSO>();
            public Player Player = new Player();
            public Source Source = new Source();
            public CareSfxPresenter Audio;
            public Run()
            {
                var paper = new WorkPaper(1, 1, "01", "Test", "Test", "Score", "", PickRule.Highest,
                    PaperDisplay.Number, new[] { new WorkChoice("A", 1), new WorkChoice("B", 0) });
                Work = new WorkSystem(new WorkSettings(), new[] { paper });
                Care = new CareSystem(Work, new[] {
                    new CareLesson("home", "Home", 1, 0, Array.Empty<CareGain>()),
                    new CareLesson("rest", "Rest", 1, 0, Array.Empty<CareGain>())
                }, new[] { new CareSynergy("pair", "Pair", new[] { "home", "home" }, Array.Empty<CareGain>()) },
                    new[] { new CareGift("desk", "Desk", CareGiftKind.Desk, 0, 0, Array.Empty<CareGiftBonus>()) },
                    Array.Empty<CareEnding>(), new CareEnding("fallback", "Fallback", 0, Array.Empty<CareGain>(), "", "", "", ""),
                    Array.Empty<CareGain>());
                Flow = new CareFlowSystem(Work, Care, new CareDialogueFile());
                Work.StartRun(); Work.AddTime(Work.MonthSeconds); Work.LeaveWork(); Flow.EnterCare();
                Audio = new CareSfxPresenter(Work, Care, Flow, Sounds, Player, Source);
            }
            public void Dispose()
            { Audio.Dispose(); Flow.Dispose(); Care.Dispose(); Work.Dispose(); UnityEngine.Object.DestroyImmediate(Sounds); }
        }

        [Test]
        public void ScheduleSoundsOnlyForChangesAndNewSynergies()
        {
            using var run = new Run();
            Assert.That(run.Player.Calls, Is.Empty);
            run.Care.SetLesson(0, "home"); run.Care.SetLesson(0, "home");
            Assert.That(run.Player.Calls.Select(x => x.sound), Is.EqualTo(new[] { run.Sounds.ScheduleAdd }));
            run.Care.SetLesson(1, "home");
            Assert.That(run.Player.Calls.Select(x => x.sound), Is.EqualTo(new[] {
                run.Sounds.ScheduleAdd, run.Sounds.ScheduleAdd, run.Sounds.Synergy }));
            run.Care.ClearLesson(0); run.Care.ClearLesson(0);
            Assert.That(run.Player.Calls.Last().sound, Is.SameAs(run.Sounds.ScheduleRemove));
            Assert.That(run.Player.Calls.Count, Is.EqualTo(4));
        }

        [Test]
        public void PurchaseAutoEquipDoesNotDoubleAndOwnedEquipHasItsOwnSound()
        {
            using var run = new Run();
            Assert.That(run.Care.BuyGift("desk"), Is.True);
            Assert.That(run.Player.Calls.Select(x => x.sound), Is.EqualTo(new[] { run.Sounds.Purchase }));
            run.Care.BuyGift("desk"); run.Care.SetActiveGift("desk");
            Assert.That(run.Player.Calls.Count, Is.EqualTo(1));
            run.Care.ClearActiveGift(CareGiftKind.Desk); run.Care.ClearActiveGift(CareGiftKind.Desk);
            run.Care.SetActiveGift("desk");
            Assert.That(run.Player.Calls.Select(x => x.sound), Is.EqualTo(new[] {
                run.Sounds.Purchase, run.Sounds.Unequip, run.Sounds.Equip }));
        }

        [Test]
        public void SkillPurchasePauseAndDisposeRespectOwnership()
        {
            using var run = new Run();
            var upgrades = new WorkUpgradeSystem(run.Work, new[] {
                new WorkUpgradeNode("skill", 0, Array.Empty<string>(), Array.Empty<WorkUpgradeEffect>()) });
            Assert.That(upgrades.Buy("skill"), Is.True);
            upgrades.Buy("skill");
            Assert.That(run.Player.Calls.Select(x => x.sound), Is.EqualTo(new[] { run.Sounds.Purchase }));
            run.Work.SetPaused(true);
            Assert.That(run.Player.Calls[0].handle.Paused, Is.True);
            run.Source.Emit(CareSfxCue.DialogueBlip);
            Assert.That(run.Player.Calls.Count, Is.EqualTo(1));
            run.Work.SetPaused(false);
            Assert.That(run.Player.Calls[0].handle.Paused, Is.False);
            run.Audio.Dispose(); run.Source.Emit(CareSfxCue.DialogueBlip);
            Assert.That(run.Player.Calls[0].handle.IsPlaying, Is.False);
            Assert.That(run.Player.Calls.Count, Is.EqualTo(1));
        }

        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string name) => target.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [Test]
        public void DialogueClickRevealsBeforeAdvancingAndRefreshDoesNotRestartTyping()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("Dialogue SFX test", typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
            var textOwner = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textOwner.transform.SetParent(owner.transform);
            var text = textOwner.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.rectTransform.sizeDelta = new Vector2(600, 200);
            var view = owner.AddComponent<CareDialogueView>(); Set(view, "bodyText", text);
            int next = 0; view.NextRequested += () => next++;
            var data = new CareDialogueData { Id = "test", Lines = new[] {
                new CareDialogueLine { Speaker = "달이", Text = "Hello world" } } };
            try
            {
                view.Show(data, 0, false);
                Assert.That(view.IsWriting, Is.True);
                view.SetPaused(true); Invoke(view, "Next");
                Assert.That(view.IsWriting, Is.True);
                view.SetPaused(false); Invoke(view, "Next");
                Assert.That(view.IsWriting, Is.False); Assert.That(next, Is.Zero);
                view.Show(data, 0, false);
                Assert.That(view.IsWriting, Is.False);
                Invoke(view, "Next"); Assert.That(next, Is.EqualTo(1));
                view.Hide(); Assert.That(view.IsWriting, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void WeekReportSoundsFollowRowsAndSkippingDoesNotBurstOrPlayCompletion()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("Care report SFX test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
            var report = owner.AddComponent<CareMonthReportView>();
            var rows = new List<CareMonthStatView>();
            for (int i = 0; i < 3; i++)
            {
                var rowOwner = new GameObject("Row"); rowOwner.transform.SetParent(owner.transform);
                var row = rowOwner.AddComponent<CareMonthStatView>();
                row.Prepare(0, 20, 20, new CareGradeSettings()); rows.Add(row);
            }
            Set(report, "stats", rows);
            var cues = new List<CareSfxCue>(); report.SoundRequested += cues.Add;
            try
            {
                report.Open(1, "", null, 1);
                ((Sequence)Get(report, "sequence")).Complete(true);
                Assert.That(cues, Is.EqualTo(new[] { CareSfxCue.WeekOpen, CareSfxCue.StatRise,
                    CareSfxCue.StatRise, CareSfxCue.StatRise, CareSfxCue.WeekComplete }));
                cues.Clear(); report.Open(1, "", null, 2); Invoke(report, "Click");
                Assert.That(cues, Is.EqualTo(new[] { CareSfxCue.WeekOpen }));
                report.Hide(); DOTween.ManualUpdate(10, 10);
                Assert.That(cues.Count, Is.EqualTo(1));
            }
            finally { report.Hide(); UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
