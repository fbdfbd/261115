using System;
using System.Collections.Generic;
using System.Text;

namespace WorkGame.Editor
{
    // Handles quoted commas, escaped quotes, CRLF and quoted multiline cells.
    internal static class WorkCsv
    {
        public static bool HasColumn(string text, string name)
        {
            var rows = Split(text.TrimStart('\uFEFF'));
            return rows.Count > 0 && rows[0].Exists(x => x.Trim() == name);
        }

        public static List<Dictionary<string, string>> Read(string text, params string[] headers)
        {
            var rows = Split(text.TrimStart('\uFEFF'));
            if (rows.Count == 0) throw new FormatException("CSV is empty.");
            if (rows[0].Count != headers.Length) throw new FormatException("CSV header count is wrong.");
            for (int i = 0; i < headers.Length; i++)
                if (rows[0][i].Trim() != headers[i]) throw new FormatException($"Expected header: {headers[i]}");

            var result = new List<Dictionary<string, string>>();
            for (int row = 1; row < rows.Count; row++)
            {
                if (rows[row].Count != headers.Length) throw new FormatException($"CSV row {row + 1}: wrong column count.");
                var values = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length; i++) values.Add(headers[i], rows[row][i].Trim());
                result.Add(values);
            }
            if (result.Count == 0) throw new FormatException("CSV has no data rows.");
            return result;
        }

        private static List<List<string>> Split(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            bool closed = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c != '"') cell.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else { quoted = false; closed = true; }
                    continue;
                }
                if (c == ',' || c == '\r' || c == '\n')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    closed = false;
                    if (c == ',') continue;
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    if (row.Count != 1 || row[0].Length != 0) rows.Add(row);
                    row = new List<string>();
                }
                else if (c == '"' && cell.Length == 0 && !closed) quoted = true;
                else if (closed || c == '"') throw new FormatException("Invalid CSV quoting.");
                else cell.Append(c);
            }
            if (quoted) throw new FormatException("CSV quote was not closed.");
            if (cell.Length > 0 || row.Count > 0 || closed)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }
            return rows;
        }
    }
}
