using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkGame.Tests
{
    public sealed class CharacterSfxTests
    {
        private sealed class Handle : IGameSfxHandle
        {
            public bool IsPlaying { get; set; } = true;
            public void SetPaused(bool paused) { }
            public void Dispose() => IsPlaying = false;
        }
        private sealed class Player : IGameSfxPlayer
        {
            public readonly List<GameSfxClip> Calls = new List<GameSfxClip>();
            public Handle Last;
            public IGameSfxHandle Play(GameSfxClip sound, bool loop = false)
            {
                Assert.That(loop, Is.False);
                Calls.Add(sound); Last = new Handle(); return Last;
            }
        }
        private sealed class Run : IDisposable
        {
            private readonly UnityEngine.SceneManagement.Scene preview = EditorSceneManager.NewPreviewScene();
            private readonly GameObject owner = new GameObject("Petting SFX test");
            private readonly CareConfigSO config = ScriptableObject.CreateInstance<CareConfigSO>();
            private readonly UnityEngine.Random.State randomState = UnityEngine.Random.state;
            private readonly AudioClip first = AudioClip.Create("Petting test A", 64, 1, 8000, false);
            private readonly AudioClip second = AudioClip.Create("Petting test B", 64, 1, 8000, false);
            public readonly CharacterSfxSO Sounds = ScriptableObject.CreateInstance<CharacterSfxSO>();
            public readonly WorkSystem Work = new WorkSystem(new WorkSettings(), Array.Empty<WorkPaper>());
            public readonly CareSystem Care;
            public readonly CareFlowSystem Flow;
            public readonly CharacterPettingView View;
            public readonly CharacterSfxPresenter Presenter;
            public readonly Player Audio = new Player();
            public float Time;
            public int NextIndex, Chooses, ValidChoices;

            public Run()
            {
                owner.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
                var root = owner.AddComponent<WorkRoot>();
                var character = owner.AddComponent<CharacterExpressionAnimator>();
                View = owner.AddComponent<CharacterPettingView>();
                Set(View, "hearts", Array.Empty<SpriteRenderer>());
                Set(View, "hitAreas", Array.Empty<SpriteRenderer>());
                Set(View, "workRoot", root); Set(View, "character", character);
                Care = config.CreateSystem(Work);
                Flow = new CareFlowSystem(Work, Care, new CareDialogueFile
                {
                    Dialogues = new[] { new CareDialogueData { Id = "talk", Lines = new[] { new CareDialogueLine { Text = "대화" } } } },
                    MonthTalks = new[] { new CareMonthTalk { Month = 1, Dialogue = "talk" } }
                });
                typeof(WorkRoot).GetProperty("CareFlow").SetValue(root, Flow);
                Work.StartRun(); Work.AddTime(Work.MonthSeconds); Work.LeaveWork(); Flow.EnterCare();
                owner.SetActive(true);
                Sounds.Petting = new[] { new GameSfxClip { Clip = first }, new GameSfxClip { Clip = second } };
                Presenter = new CharacterSfxPresenter(View, Sounds, Audio, () => Time,
                    count => { Chooses++; ValidChoices = count; return NextIndex; });
            }
            public void Click(PointerEventData.InputButton button = PointerEventData.InputButton.Left)
            {
                var data = new PointerEventData(null) { button = button, pointerId = 1 };
                View.OnPointerDown(data); View.OnPointerClick(data);
            }
            public void Dispose()
            {
                Presenter.Dispose(); Flow.Dispose(); Care.Dispose(); Work.Dispose();
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(Sounds); UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second);
                EditorSceneManager.ClosePreviewScene(preview);
                UnityEngine.Random.state = randomState;
            }
        }

        [Test]
        public void AcceptedPetPicksOneValidSoundAndDoesNotOverlapRapidClicks()
        {
            using var run = new Run();
            var a = run.Sounds.Petting[0]; var b = run.Sounds.Petting[1];
            run.Sounds.Petting = new[] { null, a, new GameSfxClip(), b };
            run.NextIndex = 1; run.Click();
            Assert.That(run.Audio.Calls, Is.EqualTo(new[] { b }));
            Assert.That(run.ValidChoices, Is.EqualTo(2));
            run.Time = 0.5f; run.Click();
            Assert.That(run.Audio.Calls.Count, Is.EqualTo(1));
            run.Audio.Last.IsPlaying = false;
            run.NextIndex = 0; run.Click();
            Assert.That(run.Audio.Calls, Is.EqualTo(new[] { b, a }));
        }

        [Test]
        public void EmptyOrMissingEntriesAreSilentAndNeverRandomlySelected()
        {
            using var run = new Run();
            run.Sounds.Petting = new[] { null, new GameSfxClip() };
            run.Click();
            Assert.That(run.Audio.Calls, Is.Empty);
            Assert.That(run.Chooses, Is.Zero);
            run.Sounds.Petting = null;
            Assert.That(run.Sounds.PettingCount, Is.Zero);
            Assert.That(run.Sounds.GetPetting(0), Is.Null);
            Assert.That(run.Sounds.GetPetting(-1), Is.Null);
        }

        [Test]
        public void PointerDownAloneRightClickAndClickWithoutPressDoNotSound()
        {
            using var run = new Run();
            var data = new PointerEventData(null) { button = PointerEventData.InputButton.Left, pointerId = 1 };
            run.View.OnPointerClick(data);
            Assert.That(run.Audio.Calls, Is.Empty);
            run.View.OnPointerDown(data);
            Assert.That(run.Audio.Calls, Is.Empty);
            run.Click(PointerEventData.InputButton.Right);
            Assert.That(run.Audio.Calls, Is.Empty);
            run.Click(); Assert.That(run.Audio.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void MinimumIntervalAlsoLimitsVeryShortSounds()
        {
            using var run = new Run();
            run.Sounds.PettingMinInterval = 1f;
            run.Click(); run.Audio.Last.IsPlaying = false;
            run.Time = 0.1f; run.Click();
            Assert.That(run.Audio.Calls.Count, Is.EqualTo(1));
            run.Time = 1f; run.Click();
            Assert.That(run.Audio.Calls.Count, Is.EqualTo(2));
        }

        [TestCase("pause")]
        [TestCase("panel")]
        [TestCase("dialogue")]
        [TestCase("disable")]
        public void LosingPettingAvailabilityStopsTheSoundAndBlocksNewOnes(string cause)
        {
            using var run = new Run();
            run.Click();
            switch (cause)
            {
                case "pause": run.Work.SetPaused(true); break;
                case "panel": run.Flow.OpenPanel(CarePanel.Stats); break;
                case "dialogue": Assert.That(run.Flow.Talk(), Is.True); break;
                case "disable": run.View.gameObject.SetActive(false); break;
            }
            run.Presenter.Tick();
            Assert.That(run.Audio.Last.IsPlaying, Is.False);
            run.Click(); Assert.That(run.Audio.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisposalStopsPlaybackAndUnsubscribesTheOldRun()
        {
            using var run = new Run();
            run.Click(); run.Presenter.Dispose();
            Assert.That(run.Audio.Last.IsPlaying, Is.False);
            run.Time = 1f; run.Click();
            Assert.That(run.Audio.Calls.Count, Is.EqualTo(1));
        }

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
