using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public enum CareEndingScene { Memories, Career, Friendship }
    public sealed class CareEndingLine
    {
        public CareEndingScene Scene { get; }
        public string Text { get; }
        public bool CharacterVisible { get; }
        public CareEndingLine(CareEndingScene scene, string text, bool characterVisible = false)
        { Scene = scene; Text = text; CharacterVisible = characterVisible; }
    }
    public static class CareEndingScript
    {
        public static CareEndingLine[] Build(CareEndingResult result, IEnumerable<string> memories,
            string careerText = null, bool appendFriendship = true)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var lines = new List<CareEndingLine>();
            void Add(CareEndingScene scene, string text, bool character = false, int appearFrom = 0)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                lines.AddRange(text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line)).Select((line, index) =>
                        new CareEndingLine(scene, line.Trim(), character && index >= appearFrom)));
            }
            foreach (string memory in memories ?? Array.Empty<string>()) Add(CareEndingScene.Memories, memory);
            if (lines.Count == 0) Add(CareEndingScene.Memories, "그로부터 5년이 지났다.");
            Add(CareEndingScene.Career, string.IsNullOrWhiteSpace(careerText) ? result.Ending.Text : careerText);
            // Normal starts with an empty room. Dali enters when the present-day visit begins (third line).
            if (appendFriendship) Add(CareEndingScene.Friendship, result.FriendshipText,
                result.Friendship != CareFriendship.Low, result.Friendship == CareFriendship.Normal ? 2 : 0);
            return lines.ToArray();
        }
    }
}
