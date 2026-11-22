using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed partial class CareFlowSystem
    {
        private CareStoryData[] stories;
        private CareStoryData activeStory;
        private readonly HashSet<string> storyUnlocks = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<CareGain> storyGains = new List<CareGain>();
        public bool HasStoryUnlock(string key) => key != null && storyUnlocks.Contains(key);
        public string StoryRewardText { get; private set; } = "";
        public bool IsStoryPlaying => activeStory != null;

        private void InitializeStories(CareDialogueFile data)
        {
            stories = data.Stories ?? Array.Empty<CareStoryData>();
            Dialogue.ChoiceSelected += SelectStoryChoice;
        }

        private void SelectStoryChoice(CareDialogueChoice choice)
        {
            if (activeStory == null || choice.HighestStatGain <= 0) return;
            var gain = care.GetHighestStoryGain(choice.HighestStatGain);
            storyGains.Add(gain);
            StoryRewardText = CareStatNames.GetName(gain.Stat) + " +" + gain.Amount;
        }

        private void ContinueMonthEnd()
        {
            var story = stories.FirstOrDefault(x => x.Month == care.Month && !Dialogue.WasCompleted("story/" + x.Id));
            if (story != null)
            {
                activeStory = story; storyGains.Clear(); StoryRewardText = "";
                var promise = Promises.Records.FirstOrDefault(x => x.Promise.Id == story.PromiseId);
                string id = promise?.State == CarePromiseState.Succeeded && !string.IsNullOrEmpty(story.PromiseSuccessDialogue)
                    ? story.PromiseSuccessDialogue : story.Dialogue;
                if (Play(id, "story/" + story.Id)) return;
                throw new InvalidOperationException("스토리 대화를 시작하지 못했습니다: " + story.Id);
            }
            var offer = Promises.Offer(care.Month);
            if (offer != null)
            {
                offeredPromise = offer.Promise.Id;
                if (Play(offer.Promise.OfferDialogue, "offer/" + care.Month)) return;
                offeredPromise = null;
            }
            AdvanceMonth();
        }

        private void CompleteStory()
        {
            if (!string.IsNullOrEmpty(activeStory.Unlock)) storyUnlocks.Add(activeStory.Unlock);
            activeStory = null;
            care.ApplyStoryGains(storyGains);
            storyGains.Clear();
            ContinueMonthEnd();
        }
    }

    public sealed partial class CareSystem
    {
        internal CareGain GetHighestStoryGain(int amount)
        {
            var highest = CareStat.Logic;
            for (int i = (int)CareStat.Logic + 1; i <= (int)CareStat.Leadership; i++)
                if (Run.GetStat((CareStat)i) > Run.GetStat(highest)) highest = (CareStat)i;
            return new CareGain(highest, Math.Min(amount, StatMax - Run.GetStat(highest)));
        }

        internal void ApplyStoryGains(IEnumerable<CareGain> gains)
        {
            Run.AddStats(gains);
            Changed?.Invoke();
        }
    }
}
