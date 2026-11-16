namespace WorkGame
{
    public enum PickRule { Highest, Lowest }
    public enum PaperDisplay { Number, VerticalBar, HorizontalBar, Stars, Grade }
    public enum PaperState { Pending, Right, Wrong }
    public enum WorkPhase { Ready, Working, AfterWork, Finished }
    public enum WorkGrade { B, A, S }

    public sealed class WorkChoice
    {
        public string Name { get; }
        public int Value { get; }

        public WorkChoice(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}
