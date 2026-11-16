using System;
using NUnit.Framework;

namespace WorkGame.Tests
{
    public sealed class WorkDeskLayoutTests
    {
        [Test]
        public void CapacityIsLimitedByAreaAndConnectedViews()
        {
            var layout = WorkDeskLayout.Create(500, 300, 100, 100, 8, 1, gap: 0, jitter: 0, maxAngle: 0);
            Assert.That(layout.Capacity, Is.EqualTo(15));
            Assert.That(layout.Spots.Count, Is.EqualTo(8));
        }

        [TestCase(90, 300)]
        [TestCase(300, 90)]
        [TestCase(0, 300)]
        public void PaperLargerThanTheDeskIsNotPlaced(float width, float height)
        {
            var layout = WorkDeskLayout.Create(width, height, 100, 100, 10, 1, gap: 0, jitter: 0, maxAngle: 0);
            Assert.That(layout.Spots.Count, Is.Zero);
        }

        [Test]
        public void LayoutLeavesRoomForRotationJitterAndHover()
        {
            const float width = 1200, height = 800, paperWidth = 120, paperHeight = 160;
            double sine = Math.Sin(16 * Math.PI / 180);
            double halfWidth = (paperWidth + paperHeight * sine) / 2;
            double halfHeight = (paperHeight + paperWidth * sine) / 2;
            for (int seed = 0; seed < 20; seed++)
            {
                var layout = WorkDeskLayout.Create(width, height, paperWidth, paperHeight, 50, seed,
                    gap: 12, jitter: 8, maxAngle: 8, extraAngle: 8);
                Assert.That(layout.Spots.Count, Is.GreaterThan(0));
                foreach (var spot in layout.Spots)
                {
                    Assert.That(Math.Abs(spot.X) + halfWidth, Is.LessThanOrEqualTo(width / 2 + 0.001));
                    Assert.That(Math.Abs(spot.Y) + halfHeight, Is.LessThanOrEqualTo(height / 2 + 0.001));
                    Assert.That(Math.Abs(spot.Angle), Is.LessThanOrEqualTo(8));
                }
                for (int i = 0; i < layout.Spots.Count; i++)
                    for (int j = i + 1; j < layout.Spots.Count; j++)
                    {
                        var a = layout.Spots[i];
                        var b = layout.Spots[j];
                        bool separate = Math.Abs(a.X - b.X) >= halfWidth * 2 - 0.001
                            || Math.Abs(a.Y - b.Y) >= halfHeight * 2 - 0.001;
                        Assert.That(separate, Is.True, $"Overlap at seed {seed}: {i}, {j}");
                    }
            }
        }

        [Test]
        public void SameSeedKeepsPositionsRepeatable()
        {
            var first = WorkDeskLayout.Create(1000, 700, 100, 140, 10, 123);
            var second = WorkDeskLayout.Create(1000, 700, 100, 140, 10, 123);
            for (int i = 0; i < first.Spots.Count; i++)
            {
                Assert.That(first.Spots[i].X, Is.EqualTo(second.Spots[i].X));
                Assert.That(first.Spots[i].Y, Is.EqualTo(second.Spots[i].Y));
                Assert.That(first.Spots[i].Angle, Is.EqualTo(second.Spots[i].Angle));
            }
        }
    }
}
