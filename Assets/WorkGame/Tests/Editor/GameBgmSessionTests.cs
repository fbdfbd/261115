using NUnit.Framework;

namespace WorkGame.Tests
{
    public sealed class GameBgmSessionTests
    {
        [Test]
        public void OverrideReturnsToLatestBaseRatherThanOriginalBase()
        {
            using var session = new GameBgmSession();
            var monthOne = new GameBgmTrack();
            var monthTwo = new GameBgmTrack();
            var eventTrack = new GameBgmTrack();
            session.SetBaseTrack(monthOne);
            var handle = session.RequestOverride(eventTrack);
            session.SetBaseTrack(monthTwo);
            Assert.That(session.CurrentTrack, Is.SameAs(eventTrack));
            handle.Dispose();
            Assert.That(session.CurrentTrack, Is.SameAs(monthTwo));
        }

        [Test]
        public void ReleasingOlderOverrideDoesNotCancelNewerEvent()
        {
            using var session = new GameBgmSession();
            var basis = new GameBgmTrack();
            var latest = new GameBgmTrack();
            session.SetBaseTrack(basis);
            var first = session.RequestOverride(new GameBgmTrack());
            var second = session.RequestOverride(latest);
            first.Dispose();
            Assert.That(session.CurrentTrack, Is.SameAs(latest));
            second.Dispose();
            Assert.That(session.CurrentTrack, Is.SameAs(basis));
        }

        [Test]
        public void ReleasingLatestOverrideRestoresStillActiveOlderEvent()
        {
            using var session = new GameBgmSession();
            var earlier = new GameBgmTrack();
            using var first = session.RequestOverride(earlier);
            var second = session.RequestOverride(new GameBgmTrack());
            second.Dispose();
            Assert.That(session.CurrentTrack, Is.SameAs(earlier));
        }

        [Test]
        public void DisposedSceneCannotSendRequestsOrReleaseIntoNewScene()
        {
            var oldScene = new GameBgmSession();
            var oldEvent = oldScene.RequestOverride(new GameBgmTrack());
            oldScene.Dispose();
            using var newScene = new GameBgmSession();
            var title = new GameBgmTrack();
            newScene.SetBaseTrack(title);
            int staleNotifications = 0;
            oldScene.Changed += _ => staleNotifications++;
            oldEvent.Dispose();
            oldScene.SetBaseTrack(new GameBgmTrack());
            oldScene.RequestOverride(new GameBgmTrack()).Dispose();
            oldScene.Dispose();
            Assert.That(oldScene.CurrentTrack, Is.Null);
            Assert.That(newScene.CurrentTrack, Is.SameAs(title));
            Assert.That(staleNotifications, Is.Zero);
        }

        [Test]
        public void OverrideHandleCanBeReleasedMoreThanOnce()
        {
            using var session = new GameBgmSession();
            var handle = session.RequestOverride(new GameBgmTrack());
            int notifications = 0;
            session.Changed += _ => notifications++;
            handle.Dispose();
            handle.Dispose();
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void NullOverrideTemporarilySilencesBaseTrack()
        {
            using var session = new GameBgmSession();
            var basis = new GameBgmTrack();
            session.SetBaseTrack(basis);
            var handle = session.RequestOverride(null);
            Assert.That(session.CurrentTrack, Is.Null);
            handle.Dispose();
            Assert.That(session.CurrentTrack, Is.SameAs(basis));
        }

        [Test]
        public void SameBaseTrackDoesNotRepeatedlyNotifyPlayback()
        {
            using var session = new GameBgmSession();
            var basis = new GameBgmTrack();
            int notifications = 0;
            session.Changed += _ => notifications++;
            session.SetBaseTrack(basis);
            session.SetBaseTrack(basis);
            Assert.That(notifications, Is.EqualTo(1));
        }
    }
}
