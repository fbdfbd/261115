using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WorkGame
{
    public static class WorkHighlightText
    {
        public static string Mark(string text, IReadOnlyList<string> words, Color color)
        {
            if (string.IsNullOrEmpty(text) || words == null || words.Count == 0) return text;
            var found = new bool[text.Length];
            foreach (string word in words.Where(x => !string.IsNullOrEmpty(x)))
                for (int start = text.IndexOf(word, StringComparison.Ordinal); start >= 0;
                    start = text.IndexOf(word, start + 1, StringComparison.Ordinal))
                    for (int i = start; i < start + word.Length; i++) found[i] = true;
            var result = new StringBuilder();
            string tag = "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">";
            for (int i = 0; i < text.Length; i++)
            {
                if (found[i] && (i == 0 || !found[i - 1])) result.Append(tag);
                result.Append(text[i]);
                if (found[i] && (i == text.Length - 1 || !found[i + 1])) result.Append("</color>");
            }
            return result.ToString();
        }
    }
}
