using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WorkGame.Tests
{
    public sealed class GameMusicFilterTests
    {
        private sealed class Run : IDisposable
        {
            private readonly UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewPreviewScene();
            private readonly GameObject owner = new GameObject("Music filter test");
            public readonly GameAudioRoot Root;
            public readonly AudioLowPassFilter Filter;
            public readonly AudioSource Music, Sfx;
            public Run()
            {
                owner.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
                Music = owner.AddComponent<AudioSource>(); Music.volume = 0.7f;
                Filter = owner.AddComponent<AudioLowPassFilter>(); Filter.enabled = false;
                Root = owner.AddComponent<GameAudioRoot>();
                typeof(GameAudioRoot).GetField("musicFilter", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Root, Filter);
                var child = new GameObject("SFX"); child.transform.SetParent(owner.transform);
                Sfx = child.AddComponent<AudioSource>(); Sfx.volume = 0.8f;
            }
            public void Step(float seconds) => typeof(GameAudioRoot)
                .GetMethod("UpdateMusicFilter", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(Root, new object[] { seconds });
            public void Dispose() { UnityEngine.Object.DestroyImmediate(owner); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void FilterTransitionsBothWaysWithoutChangingVolumesOrFilteringSfx()
        {
            using var run = new Run();
            using var session = run.Root.BeginSession(0.5f);
            run.Root.SetMusicMuffled(session, true); run.Step(0.125f);
            Assert.That(run.Filter.enabled, Is.True);
            Assert.That(run.Filter.cutoffFrequency, Is.InRange(1001f, 21999f));
            run.Root.SetMusicMuffled(session, true); run.Step(0.125f);
            Assert.That(run.Filter.cutoffFrequency, Is.EqualTo(1000f).Within(0.1f));
            Assert.That(run.Music.volume, Is.EqualTo(0.7f));
            Assert.That(run.Sfx.volume, Is.EqualTo(0.8f));
            Assert.That(run.Sfx.GetComponent<AudioLowPassFilter>(), Is.Null);
            run.Root.SetMusicMuffled(session, false); run.Step(0.25f);
            Assert.That(run.Filter.enabled, Is.False);
            Assert.That(run.Filter.cutoffFrequency, Is.EqualTo(22000f).Within(0.1f));
        }

        [Test]
        public void ReplacingSessionClearsMuffleAndRejectsStaleSceneRequests()
        {
            using var run = new Run();
            using var old = run.Root.BeginSession(0.5f);
            run.Root.SetMusicMuffled(old, true); run.Step(0.25f);
            using var current = run.Root.BeginSession(0.5f);
            run.Root.SetMusicMuffled(old, true); run.Step(0.25f);
            Assert.That(run.Filter.enabled, Is.False);
            run.Root.SetMusicMuffled(current, true); run.Step(0.25f);
            run.Root.SetMusicMuffled(old, false);
            run.Step(0.25f);
            Assert.That(run.Filter.enabled, Is.True);
        }

        [Test]
        public void DisposingCurrentSessionRestoresNormalSound()
        {
            using var run = new Run();
            var session = run.Root.BeginSession(0.5f);
            run.Root.SetMusicMuffled(session, true); run.Step(0.25f);
            session.Dispose(); run.Step(0.25f);
            Assert.That(run.Filter.enabled, Is.False);
        }
    }
}
