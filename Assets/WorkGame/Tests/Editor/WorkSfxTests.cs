using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WorkGame.Tests
{
    public sealed class WorkSfxTests
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
            public readonly List<(GameSfxClip clip, bool loop, Handle handle)> Calls = new List<(GameSfxClip, bool, Handle)>();
            public IGameSfxHandle Play(GameSfxClip sound, bool loop = false)
            {
                var handle = new Handle(); Calls.Add((sound, loop, handle)); return handle;
            }
        }
        private static WorkPaper Paper(int id, int month) => new WorkPaper(id, month, "01", "Test", "Test", "Score", "",
            PickRule.Highest, PaperDisplay.Number, new[] { new WorkChoice("A", 1), new WorkChoice("B", 0) });
        private static WorkSystem Work()
        {
            var work = new WorkSystem(new WorkSettings(), new[] { Paper(1, 1), Paper(2, 2) });
            work.StartRun(); return work;
        }

        [Test]
        public void HoldLoopDoesNotDuplicateAndStopsOnReleasePauseCompletionAndDisposal()
        {
            var sounds = ScriptableObject.CreateInstance<WorkSfxSO>();
            using var work = Work();
            var player = new Player();
            using var presenter = new WorkSfxPresenter(work, sounds, player);
            try
            {
                Assert.That(work.StartHold(1, 0), Is.True);
                work.AddTime(0.1); work.AddTime(0.1);
                Assert.That(player.Calls.Count, Is.EqualTo(1));
                Assert.That(player.Calls[0].loop, Is.True);
                Assert.That(player.Calls[0].clip, Is.SameAs(sounds.HoldLoop));
                work.StopHold();
                Assert.That(player.Calls[0].handle.IsPlaying, Is.False);
                work.StartHold(1, 0); work.SetPaused(true);
                Assert.That(player.Calls[1].handle.IsPlaying, Is.False);
                work.SetPaused(false);
                Assert.That(player.Calls.Count, Is.EqualTo(2), "일시정지 해제만으로 누르기 소리를 다시 켜지 않습니다.");
                work.StartHold(1, 0);
                presenter.OnCue(WorkSfxCue.HoldComplete);
                Assert.That(player.Calls[2].handle.IsPlaying, Is.False, "완료 소리 전에 반복 소리를 정지합니다.");
                Assert.That(player.Calls[3].clip, Is.SameAs(sounds.HoldComplete));
                presenter.Dispose();
                Assert.That(player.Calls.All(x => !x.handle.IsPlaying), Is.True);
                int count = player.Calls.Count;
                work.StopHold(); work.StartHold(1, 0); presenter.OnCue(WorkSfxCue.PaperHover);
                Assert.That(player.Calls.Count, Is.EqualTo(count));
            }
            finally { UnityEngine.Object.DestroyImmediate(sounds); }
        }

        [Test]
        public void LowTimeWarningCrossesThresholdOnlyOncePerMonthAndNotOnBind()
        {
            var sounds = ScriptableObject.CreateInstance<WorkSfxSO>();
            using var work = new WorkSystem(new WorkSettings { MonthSeconds = 90 },
                new[] { Paper(1, 1), Paper(2, 2) });
            work.StartRun();
            var player = new Player();
            using var presenter = new WorkSfxPresenter(work, sounds, player);
            try
            {
                Assert.That(player.Calls.Count, Is.Zero);
                work.AddTime(79); Assert.That(player.Calls.Count, Is.Zero);
                work.AddTime(1.1); work.AddTime(1); work.SetPaused(true); work.SetPaused(false);
                Assert.That(player.Calls.Count(x => x.clip == sounds.LowTime), Is.EqualTo(1));
                work.AddTime(10); work.LeaveWork(); work.StartNextMonth(); work.AddTime(80.1);
                Assert.That(player.Calls.Count(x => x.clip == sounds.LowTime), Is.EqualTo(2));
                var resumed = new Player();
                using var reconnected = new WorkSfxPresenter(work, sounds, resumed);
                Assert.That(resumed.Calls.Count, Is.Zero, "진행 중 연결/로드만으로 경고를 재생하지 않습니다.");
            }
            finally { UnityEngine.Object.DestroyImmediate(sounds); }
        }

        [TestCase(false, false, false, WorkSfxCue.BubbleNormal)]
        [TestCase(true, false, false, WorkSfxCue.BubbleWrong)]
        [TestCase(false, false, true, WorkSfxCue.BubbleDoubleReward)]
        [TestCase(false, true, false, WorkSfxCue.BubbleInstant)]
        [TestCase(false, true, true, WorkSfxCue.BubbleInstant)]
        public void BubbleSoundUsesOnlyItsActualVariant(bool wrong, bool instant, bool boosted, WorkSfxCue expected)
            => Assert.That(WorkSfxSO.BubbleCue(wrong, instant, boosted), Is.EqualTo(expected));

        [Test]
        public void WorkAndReportSoundsRespectPauseAndTheCurrentPhase()
        {
            var sounds = ScriptableObject.CreateInstance<WorkSfxSO>();
            using var work = Work();
            var player = new Player();
            using var presenter = new WorkSfxPresenter(work, sounds, player);
            try
            {
                presenter.OnCue(WorkSfxCue.ReportRow);
                Assert.That(player.Calls.Count, Is.Zero);
                presenter.OnCue(WorkSfxCue.PaperHover);
                work.SetPaused(true);
                Assert.That(player.Calls[0].handle.Paused, Is.True);
                presenter.OnCue(WorkSfxCue.PaperPick);
                Assert.That(player.Calls.Count, Is.EqualTo(1));
                work.SetPaused(false);
                Assert.That(player.Calls[0].handle.Paused, Is.False);
                work.AddTime(90); work.LeaveWork();
                presenter.OnCue(WorkSfxCue.PaperHover);
                presenter.OnCue(WorkSfxCue.ReportRow);
                presenter.OnCue(WorkSfxCue.ReportComplete);
                Assert.That(player.Calls.Select(x => x.clip), Is.EqualTo(new[] { sounds.PaperHover, sounds.ReportRow, sounds.ReportComplete }));
            }
            finally { UnityEngine.Object.DestroyImmediate(sounds); }
        }

        [Test]
        public void FaxLoopFollowsActualMachineStateAndStopsOnDisableWithoutAVisualAssigned()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX fax test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var fax = owner.AddComponent<WorkFaxView>();
            var sounds = ScriptableObject.CreateInstance<WorkSfxSO>();
            using var work = Work();
            var player = new Player();
            using var presenter = new WorkSfxPresenter(work, sounds, player, fax: fax);
            try
            {
                fax.ShowState(true, true, 1, 2, 0);
                fax.ShowState(true, true, 1, 2, 0.5f);
                Assert.That(player.Calls.Count, Is.EqualTo(1));
                Assert.That(player.Calls[0].clip, Is.SameAs(sounds.FaxLoop));
                owner.SetActive(false);
                // Non-ExecuteAlways behaviours do not receive normal play lifecycle in EditMode.
                typeof(WorkFaxView).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(fax, null);
                Assert.That(player.Calls[0].handle.IsPlaying, Is.False);
            }
            finally
            {
                presenter.Dispose(); UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sounds); EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [Test]
        public void ProgressDoesNotSoundOnInitialDisplayResetOrRepeatedRefresh()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX progress test");
            owner.SetActive(false); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var hud = owner.AddComponent<WorkHudView>();
            int count = 0;
            hud.SoundRequested += cue => { if (cue == WorkSfxCue.Progress) count++; };
            try
            {
                hud.ShowProgress(0, 10); hud.ShowProgress(0, 10);
                Assert.That(count, Is.Zero);
                hud.ShowProgress(1, 10); hud.ShowProgress(1, 10);
                Assert.That(count, Is.EqualTo(1));
                hud.ShowProgress(0, 10); hud.ShowProgress(0, 20);
                Assert.That(count, Is.EqualTo(1));
                hud.ShowProgress(1, 20);
                Assert.That(count, Is.EqualTo(2));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(preview); }
        }

        [Test]
        public void SkillPointsSoundOncePerIncreaseAcrossRefreshAndResult()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX points test");
            owner.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var hud = owner.AddComponent<WorkHudView>();
            int count = 0;
            hud.SoundRequested += cue => { if (cue == WorkSfxCue.SkillPointsGain) count++; };
            try
            {
                hud.ShowSkillPoints(100); hud.ShowSkillPoints(100);
                Assert.That(count, Is.Zero);
                hud.ShowSkillPoints(120); hud.ShowSkillPointResult(120, true);
                Assert.That(count, Is.EqualTo(1));
                hud.ShowSkillPointResult(150, true); hud.ShowSkillPoints(150);
                Assert.That(count, Is.EqualTo(2));
                hud.ShowSkillPointResult(130, false); hud.ShowSkillPoints(130);
                Assert.That(count, Is.EqualTo(2));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(preview); }
        }

        [Test]
        public void BubbleQueueSoundsOnlyWhenVisibleAndSuppressedNotificationsStaySilent()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX bubble test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var bubbleOwner = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasGroup));
            bubbleOwner.transform.SetParent(owner.transform);
            var bubble = bubbleOwner.AddComponent<WorkFeedbackBubbleView>();
            var view = owner.AddComponent<WorkFeedbackView>();
            Set(view, "bubbles", new List<WorkFeedbackBubbleView> { bubble });
            Invoke(view, "OnEnable");
            var cues = new List<WorkSfxCue>(); view.SoundRequested += cues.Add;
            try
            {
                view.Show(new WorkFeedbackMessage(FeedbackResult.Success, "일반"));
                view.Show(new WorkFeedbackMessage(FeedbackResult.Failure, "오답"));
                Assert.That(cues, Is.EqualTo(new[] { WorkSfxCue.BubbleNormal }));
                Invoke(bubble, "Finish");
                Assert.That(cues, Is.EqualTo(new[] { WorkSfxCue.BubbleNormal, WorkSfxCue.BubbleDisappear, WorkSfxCue.BubbleWrong }));
                view.SetSuppressed(true);
                view.Show(new WorkFeedbackMessage(FeedbackResult.Success, "방해 금지"), true);
                Assert.That(cues.Count, Is.EqualTo(3));
            }
            finally { Invoke(view, "OnDisable"); UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(preview); }
        }

        [Test]
        public void BubbleExitSoundsOnceOnNaturalOrEarlyFinishButNotOnClear()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX bubble exit test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var bubbleOwner = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasGroup));
            bubbleOwner.transform.SetParent(owner.transform);
            var bubble = bubbleOwner.AddComponent<WorkFeedbackBubbleView>();
            var view = owner.AddComponent<WorkFeedbackView>();
            Set(view, "bubbles", new List<WorkFeedbackBubbleView> { bubble });
            Invoke(view, "OnEnable");
            int exits = 0;
            view.SoundRequested += cue => { if (cue == WorkSfxCue.BubbleDisappear) exits++; };
            var message = new WorkFeedbackMessage(FeedbackResult.Success, "Test");
            try
            {
                view.Show(message);
                DG.Tweening.TweenExtensions.Complete((DG.Tweening.Tween)Get(bubble, "lifeTween"), true);
                Assert.That(exits, Is.Zero);
                DG.Tweening.TweenExtensions.Complete((DG.Tweening.Tween)Get(bubble, "exitTween"), true);
                Assert.That(exits, Is.EqualTo(1));
                Invoke(bubble, "Finish");
                Assert.That(exits, Is.EqualTo(1));
                view.Show(message);
                bubble.FinishSoon(0.1f); bubble.FinishSoon(0.1f);
                DG.Tweening.TweenExtensions.Complete((DG.Tweening.Tween)Get(bubble, "exitTween"), true);
                Assert.That(exits, Is.EqualTo(2));
                view.Show(message); view.Clear();
                view.Show(message); view.SetSuppressed(true);
                Assert.That(exits, Is.EqualTo(2));
            }
            finally { Invoke(view, "OnDisable"); UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(preview); }
        }

        [Test]
        public void ReportSoundsMatchVisibleRowsThenOneFinalResultAndCancelWithTheSequence()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX report test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var report = owner.AddComponent<WorkMonthReportView>();
            var rows = new List<CanvasGroup>();
            for (int i = 0; i < 4; i++)
            {
                var row = new GameObject("Row " + i, typeof(CanvasGroup)); row.transform.SetParent(owner.transform);
                rows.Add(row.GetComponent<CanvasGroup>());
            }
            rows[2].gameObject.SetActive(false);
            Set(report, "pointRows", rows.Take(3).ToList());
            Set(report, "salaryRows", rows.Skip(3).ToList());
            var cues = new List<WorkSfxCue>(); report.SoundRequested += cues.Add;
            try
            {
                report.Open(new MonthResult(1, 0, 1, 0, 0, 100, null));
                var sequence = (DG.Tweening.Sequence)Get(report, "sequence");
                DG.Tweening.TweenExtensions.Complete(sequence, true);
                Assert.That(cues, Is.EqualTo(new[] { WorkSfxCue.ReportRow, WorkSfxCue.ReportRow, WorkSfxCue.ReportRow, WorkSfxCue.ReportComplete }));
                report.Hide(); cues.Clear();
                report.Open(new MonthResult(2, 0, 1, 0, 0, 100, null));
                sequence = (DG.Tweening.Sequence)Get(report, "sequence");
                DG.Tweening.TweenSettingsExtensions.SetUpdate(sequence, DG.Tweening.UpdateType.Manual);
                report.Hide();
                DG.Tweening.DOTween.ManualUpdate(20f, 20f);
                Assert.That(cues, Is.Empty);
            }
            finally { report.Hide(); UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(preview); }
        }

        [Test]
        public void DropSoundDoesNotDoubleAfterFaxAcceptanceOrSoundOnProgrammaticCancellation()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("SFX drop test", typeof(RectTransform));
            owner.SetActive(false); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            var paper = owner.AddComponent<WorkPaperItemView>();
            int count = 0; paper.SoundRequested += cue => { if (cue == WorkSfxCue.PaperDrop) count++; };
            try
            {
                Set(paper, "dragging", true);
                paper.NotifyDrop(); paper.OnEndDrag(null);
                Assert.That(count, Is.EqualTo(1));
                Set(paper, "dragging", true);
                paper.CancelDrag();
                Assert.That(count, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static readonly System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    }
}
