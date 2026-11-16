using System;
using System.Collections.Generic;

namespace WorkGame
{
    public readonly struct PaperSpot
    {
        public float X { get; }
        public float Y { get; }
        public float Angle { get; }

        public PaperSpot(float x, float y, float angle) { X = x; Y = y; Angle = angle; }
    }

    // A small, fixed number of candidate checks. Runs only when the layout changes.
    public sealed class WorkDeskLayout
    {
        private const int CandidatesPerPaper = 12;
        private const int ImprovePasses = 2;
        public int Capacity { get; }
        public IReadOnlyList<PaperSpot> Spots { get; }

        private struct Candidate
        {
            public PaperSpot Spot;
            public double HalfWidth;
            public double HalfHeight;
        }

        private WorkDeskLayout(int capacity, PaperSpot[] spots) { Capacity = capacity; Spots = spots; }

        public static WorkDeskLayout Create(float width, float height, float paperWidth, float paperHeight,
            int limit, int seed, float gap = 12, float jitter = 8, float maxAngle = 8, float extraAngle = 0,
            float overlap = 0.2f)
        {
            if (width <= 0 || height <= 0 || paperWidth <= 0 || paperHeight <= 0 || limit <= 0)
                return new WorkDeskLayout(0, Array.Empty<PaperSpot>());
            gap = Math.Max(0, gap);
            jitter = Math.Max(0, jitter);
            overlap = Math.Max(0, Math.Min(0.8f, overlap));
            var random = new Random(seed);
            var placed = new Candidate[limit];
            for (int i = 0; i < limit; i++)
                placed[i] = ChooseSpot(i, i, placed, random, width, height, paperWidth, paperHeight,
                    gap, jitter, maxAngle, extraAngle, overlap, false);

            // Let early papers move too, so a bad first position does not force a pile around it.
            for (int pass = 0; pass < ImprovePasses; pass++)
                for (int i = 0; i < limit; i++)
                    placed[i] = ChooseSpot(i, limit, placed, random, width, height, paperWidth, paperHeight,
                        gap, jitter, maxAngle, extraAngle, overlap, true);

            var spots = new PaperSpot[limit];
            for (int i = 0; i < limit; i++) spots[i] = placed[i].Spot;
            return new WorkDeskLayout(limit, spots);
        }

        private static Candidate ChooseSpot(int index, int count, Candidate[] placed, Random random,
            float width, float height, float paperWidth, float paperHeight,
            float gap, float jitter, float maxAngle, float extraAngle, float overlap, bool keepCurrent)
        {
            Candidate best = placed[index];
            double bestScore = keepCurrent ? Score(best, placed, count, index, gap, overlap) : double.MaxValue;
            for (int trial = 0; trial < CandidatesPerPaper; trial++)
            {
                var candidate = MakeCandidate(random, width, height, paperWidth, paperHeight, jitter, maxAngle, extraAngle);
                double score = Score(candidate, placed, count, index, gap, overlap);
                if (score >= bestScore) continue;
                best = candidate;
                bestScore = score;
            }
            return best;
        }

        private static Candidate MakeCandidate(Random random, float width, float height,
            float paperWidth, float paperHeight, float jitter, float maxAngle, float extraAngle)
        {
            float angle = (float)(random.NextDouble() * 2 - 1) * Math.Abs(maxAngle);
            double radians = angle * Math.PI / 180;
            double halfWidth = (Math.Abs(Math.Cos(radians)) * paperWidth
                + Math.Abs(Math.Sin(radians)) * paperHeight) / 2;
            double halfHeight = (Math.Abs(Math.Sin(radians)) * paperWidth
                + Math.Abs(Math.Cos(radians)) * paperHeight) / 2;
            // Let rotated corners use the desk padding instead of squeezing all centers inward.
            double rangeX = Math.Max(0, (width - paperWidth) / 2);
            double rangeY = Math.Max(0, (height - paperHeight) / 2);
            double x = (random.NextDouble() * 2 - 1) * rangeX + (random.NextDouble() * 2 - 1) * jitter;
            double y = (random.NextDouble() * 2 - 1) * rangeY + (random.NextDouble() * 2 - 1) * jitter;
            return new Candidate {
                Spot = new PaperSpot((float)Math.Max(-rangeX, Math.Min(rangeX, x)),
                    (float)Math.Max(-rangeY, Math.Min(rangeY, y)), angle),
                HalfWidth = halfWidth, HalfHeight = halfHeight
            };
        }

        private static double Score(Candidate candidate, Candidate[] placed, int count, int skip,
            float gap, float allowedOverlap)
        {
            double score = 0;
            double worstOverlap = 0;
            double nearest = 4;
            for (int i = 0; i < count; i++)
            {
                if (i == skip) continue;
                var other = placed[i];
                double dx = Math.Abs(candidate.Spot.X - other.Spot.X);
                double dy = Math.Abs(candidate.Spot.Y - other.Spot.Y);
                double width = candidate.HalfWidth + other.HalfWidth;
                double height = candidate.HalfHeight + other.HalfHeight;
                double intersectX = Math.Max(0, width + gap - dx);
                double intersectY = Math.Max(0, height + gap - dy);
                double area = Math.Max(1, Math.Min(candidate.HalfWidth * candidate.HalfHeight,
                    other.HalfWidth * other.HalfHeight) * 4);
                double overlap = intersectX * intersectY / area;
                double excess = Math.Max(0, overlap - allowedOverlap);
                score += overlap * overlap * 20 + excess * excess * 200;
                worstOverlap = Math.Max(worstOverlap, overlap);
                nearest = Math.Min(nearest, dx * dx / Math.Max(1, width * width)
                    + dy * dy / Math.Max(1, height * height));
            }
            // Strongly avoid hiding any one paper; use distance only as a small tie-breaker.
            return score + worstOverlap * worstOverlap * 500 - nearest * 0.01;
        }
    }
}
