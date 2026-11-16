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

        public WorkSubmitResult(int paperId, bool isRight, int rightCount, int wrongCount)
        {
            PaperId = paperId;
            IsRight = isRight;
            RightCount = rightCount;
            WrongCount = wrongCount;
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
