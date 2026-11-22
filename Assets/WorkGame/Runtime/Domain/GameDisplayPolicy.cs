using System;
using System.Linq;

namespace WorkGame
{
    public readonly struct GameResolution
    {
        public int Width { get; }
        public int Height { get; }
        public GameResolution(int width, int height) { Width = width; Height = height; }
        public override string ToString() => Width + " x " + Height;
    }

    public readonly struct GameViewport
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public GameViewport(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
    }

    public static class GameDisplayPolicy
    {
        public static GameResolution[] GetResolutions(int displayWidth, int displayHeight)
        {
            var sizes = new[]
            {
                new GameResolution(1280, 720), new GameResolution(1600, 900),
                new GameResolution(1920, 1080), new GameResolution(2560, 1440)
            };
            if (displayWidth <= 0 || displayHeight <= 0) return sizes.Take(3).ToArray();
            var supported = sizes.Where(size => size.Width <= displayWidth && size.Height <= displayHeight).ToArray();
            if (supported.Length > 0) return supported;
            int unit = Math.Max(1, Math.Min(displayWidth / 16, displayHeight / 9));
            return new[] { new GameResolution(unit * 16, unit * 9) };
        }

        public static int GetResolutionIndex(GameResolution[] sizes, int width, int height)
        {
            int exact = Array.FindIndex(sizes, size => size.Width == width && size.Height == height);
            if (exact >= 0) return exact;
            int preferred = Array.FindIndex(sizes, size => size.Width == 1920 && size.Height == 1080);
            return preferred >= 0 ? preferred : sizes.Length - 1;
        }

        public static GameViewport GetViewport(int width, int height)
        {
            const double target = 16d / 9;
            if (width <= 0 || height <= 0) return new GameViewport(0, 0, 1, 1);
            double aspect = (double)width / height;
            if (Math.Abs(aspect - target) < 0.000001) return new GameViewport(0, 0, 1, 1);
            if (aspect > target)
            {
                float fit = (float)(target / aspect);
                return new GameViewport((1 - fit) / 2, 0, fit, 1);
            }
            else
            {
                float fit = (float)(aspect / target);
                return new GameViewport(0, (1 - fit) / 2, 1, fit);
            }
        }
    }
}
