using System;

namespace WorkGame
{
    public enum FeedbackResult { Success, Failure }

    public sealed class WorkSubmitResult
    {
        public int PaperId { get; }
        public bool IsRight { get; }
        public int RightCount { get; }
        public int WrongCount { get; }
        // Actual balance change: negative on a wrong answer, never below a zero balance.
        public int SkillPointsGained { get; }
        public bool IsProtected { get; }
        public bool IsPointBoosted { get; }

        public WorkSubmitResult(int paperId, bool isRight, int rightCount, int wrongCount, int skillPointsGained = 0,
            bool isProtected = false, bool isPointBoosted = false)
        {
            PaperId = paperId;
            IsRight = isRight;
            RightCount = rightCount;
            WrongCount = wrongCount;
            SkillPointsGained = skillPointsGained;
            IsProtected = isProtected;
            IsPointBoosted = isPointBoosted;
        }
    }

    [Serializable]
    public sealed class WorkFeedbackLine
    {
        public string Id;
        public FeedbackResult Result;
        public int Tier = 1;
        public string Text;
    }

    public sealed class WorkFeedbackMessage
    {
        public FeedbackResult Result { get; }
        public string Text { get; }

        public WorkFeedbackMessage(FeedbackResult result, string text)
        {
            Result = result;
            Text = text;
        }
    }
}
