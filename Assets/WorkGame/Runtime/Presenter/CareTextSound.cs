using TMPro;

namespace WorkGame
{
    internal static class CareTextSound
    {
        // TMP character indices ignore rich-text tags. At most one request per frame, even after a frame stall.
        public static bool HasLetter(TMP_Text text, int from, int to)
        {
            if (text == null) return false;
            int end = System.Math.Min(to, text.textInfo.characterCount);
            for (int i = System.Math.Max(0, from); i < end; i++)
                if (char.IsLetterOrDigit(text.textInfo.characterInfo[i].character)) return true;
            return false;
        }
    }
}
