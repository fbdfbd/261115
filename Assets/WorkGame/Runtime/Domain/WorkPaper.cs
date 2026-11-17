using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class WorkPaper
    {
        public int Id { get; }
        public int Month { get; }
        public string CategoryId { get; }
        public string CategoryName { get; }
        public string CategoryShortName { get; }
        public string Target { get; }
        public string Title { get; }
        public string Agency { get; }
        public string Metric { get; }
        public IReadOnlyList<string> HighlightWords { get; }
        public string Unit { get; }
        public PickRule Rule { get; }
        public PaperDisplay Display { get; }
        public int DisplayMax { get; }
        public IReadOnlyList<WorkChoice> Choices { get; }
        public PaperState State { get; private set; }
        public int PickedIndex { get; private set; } = -1;
        public bool IsInFax { get; internal set; }

        public WorkPaper(int id, int month, string categoryId, string categoryName,
            string target, string metric, string unit, PickRule rule,
            PaperDisplay display, IEnumerable<WorkChoice> choices, string categoryShortName = null, int displayMax = 0,
            string title = null, IEnumerable<string> highlightWords = null, string agency = null)
        {
            var copy = choices?.ToArray() ?? throw new ArgumentNullException(nameof(choices));
            if (copy.Length < 2 || copy.Any(x => x == null))
                throw new ArgumentException("A paper needs at least two choices.");
            if (copy.Select(x => x.Value).Distinct().Count() != copy.Length)
                throw new ArgumentException("Choice values must be different.");
            if ((display == PaperDisplay.Stars || display == PaperDisplay.Grade)
                && copy.Any(x => x.Value < 1 || x.Value > 5))
                throw new ArgumentException("Stars and grades use values 1 to 5.");

            Id = id;
            Month = month;
            CategoryId = categoryId;
            CategoryName = categoryName;
            CategoryShortName = string.IsNullOrWhiteSpace(categoryShortName) ? categoryName : categoryShortName;
            Target = target;
            Title = title;
            Agency = agency;
            Metric = metric;
            HighlightWords = Array.AsReadOnly((highlightWords ?? Array.Empty<string>()).ToArray());
            Unit = unit;
            Rule = rule;
            Display = display;
            DisplayMax = displayMax > 0 ? displayMax : copy.Max(x => x.Value);
            Choices = Array.AsReadOnly(copy);
        }

        internal bool Submit(int choiceIndex)
        {
            if (State != PaperState.Pending || choiceIndex < 0 || choiceIndex >= Choices.Count)
                return false;

            PickedIndex = choiceIndex;
            State = WorkRules.IsRight(this, choiceIndex) ? PaperState.Right : PaperState.Wrong;
            return true;
        }

        internal WorkPaper Copy()
        {
            return new WorkPaper(Id, Month, CategoryId, CategoryName, Target, Metric, Unit, Rule, Display, Choices,
                CategoryShortName, DisplayMax, Title, HighlightWords, Agency);
        }

        public string GetChoiceText(int index)
        {
            int value = Choices[index].Value;
            if (Display == PaperDisplay.Stars)
                return new string('★', value) + new string('☆', 5 - value);
            if (Display == PaperDisplay.Grade)
                return new[] { "D", "C", "B", "A", "S" }[value - 1];
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture) + Unit;
        }

        public float GetBarFill(int index)
        {
            if (DisplayMax <= 0) return 0;
            return (float)Math.Max(0, Math.Min(1, Choices[index].Value / (double)DisplayMax));
        }
    }
}
