using System.Linq;
using NUnit.Framework;

namespace WorkGame.Tests
{
    public sealed class GameDisplayPolicyTests
    {
        [TestCase(1920, 1080, 3)]
        [TestCase(2560, 1440, 4)]
        [TestCase(3840, 2160, 4)]
        [TestCase(2560, 1080, 3)]
        [TestCase(1366, 768, 1)]
        public void ResolutionChoicesFitTheMonitorAndStopAt1440p(int width, int height, int count)
        {
            var choices = GameDisplayPolicy.GetResolutions(width, height);
            Assert.That(choices.Length, Is.EqualTo(count));
            Assert.That(choices.All(size => size.Width <= width && size.Height <= height
                && size.Width * 9 == size.Height * 16 && size.Width <= 2560), Is.True);
        }
        [Test]
        public void UnsupportedSavedResolutionReturnsTo1080p()
        {
            var choices = GameDisplayPolicy.GetResolutions(1920, 1080);
            int index = GameDisplayPolicy.GetResolutionIndex(choices, 2560, 1440);
            Assert.That(choices[index].Width, Is.EqualTo(1920));
            Assert.That(choices[index].Height, Is.EqualTo(1080));
            var small = GameDisplayPolicy.GetResolutions(1024, 768);
            Assert.That(small.Single().Width, Is.LessThanOrEqualTo(1024));
            Assert.That(small.Single().Width * 9, Is.EqualTo(small.Single().Height * 16));
        }
        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(3440, 1440)]
        [TestCase(1920, 1200)]
        [TestCase(1080, 1920)]
        public void ViewportKeepsSixteenByNineCenteredWithoutCropping(int width, int height)
        {
            var fit = GameDisplayPolicy.GetViewport(width, height);
            Assert.That(width * fit.Width / (height * fit.Height), Is.EqualTo(16d / 9).Within(0.00001));
            Assert.That(fit.X * 2 + fit.Width, Is.EqualTo(1).Within(0.00001));
            Assert.That(fit.Y * 2 + fit.Height, Is.EqualTo(1).Within(0.00001));
            Assert.That(fit.X, Is.GreaterThanOrEqualTo(0));
            Assert.That(fit.Y, Is.GreaterThanOrEqualTo(0));
        }
    }
}
