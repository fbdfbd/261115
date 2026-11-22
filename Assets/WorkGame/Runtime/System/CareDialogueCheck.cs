using System;
using System.Linq;
using System.Collections.Generic;

namespace WorkGame
{
    internal static class CareDialogueCheck
    {
        internal static void Check(CareDialogueFile data)
        {
            if (data?.Leave == null || data.Dialogues == null || data.MonthTalks == null || data.Promises == null)
                throw new ArgumentException("대화 JSON의 Leave / Dialogues / MonthTalks / Promises를 확인하세요.");
            var settings = data.Leave;
            if (!WorkSettings.IsPositive(settings.EarlySeconds) || !WorkSettings.IsPositive(settings.OnTimeSeconds)
                || settings.EarlySeconds <= settings.OnTimeSeconds)
                throw new ArgumentException("일찍 퇴근 기준은 정시 허용 시간보다 커야 합니다.");
            if (data.Dialogues.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id))
                || data.Dialogues.Select(x => x.Id).Distinct().Count() != data.Dialogues.Length)
                throw new ArgumentException("대화 ID는 비어 있거나 중복될 수 없습니다.");
            var ids = data.Dialogues.Select(x => x.Id).ToHashSet();
            if (!string.IsNullOrEmpty(data.IntroDialogue) && !ids.Contains(data.IntroDialogue))
                throw new ArgumentException("인트로 대화 ID를 찾을 수 없습니다: " + data.IntroDialogue);
            foreach (var dialogue in data.Dialogues)
            {
                if (dialogue.Lines == null || dialogue.Lines.Length == 0 || dialogue.Choices == null
                    || dialogue.Lines.Any(x => x == null || string.IsNullOrWhiteSpace(x.Text)
                        || (x.Speaker == "나르미" && dialogue.Id != data.IntroDialogue)
                        || !Enum.IsDefined(typeof(CareDialogueEmotion), x.Emotion))
                    || dialogue.Choices.Any(x => x == null || string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Text))
                    || dialogue.Choices.Select(x => x.Key).Distinct().Count() != dialogue.Choices.Length)
                    throw new ArgumentException("대사·선택지 내용을 확인하세요: " + dialogue.Id);
                foreach (var choice in dialogue.Choices)
                    if (choice.HighestStatGain < 0 || (!string.IsNullOrEmpty(choice.NextDialogue) && !ids.Contains(choice.NextDialogue)))
                        throw new ArgumentException("선택지의 다음 대화·능력치 보상을 확인하세요: " + dialogue.Id);
                foreach (var cue in dialogue.Lines.SelectMany(line => line.Cues ?? Array.Empty<CareDialogueCue>()))
                    if (cue == null || !Enum.IsDefined(typeof(CareDialogueCueKind), cue.Kind)
                        || float.IsNaN(cue.Seconds) || float.IsInfinity(cue.Seconds) || cue.Seconds < 0
                        || ((cue.Kind == CareDialogueCueKind.ShowImage || cue.Kind == CareDialogueCueKind.HideImage) && string.IsNullOrWhiteSpace(cue.Layer))
                        || ((cue.Kind == CareDialogueCueKind.ShowImage || cue.Kind == CareDialogueCueKind.Sound || cue.Kind == CareDialogueCueKind.Custom)
                            && string.IsNullOrWhiteSpace(cue.Key)))
                        throw new ArgumentException("대화 연출 명령을 확인하세요: " + dialogue.Id);
            }
            var checkedIds = new HashSet<string>();
            var visiting = new HashSet<string>();
            var byId = data.Dialogues.ToDictionary(x => x.Id);
            void Visit(string id)
            {
                if (checkedIds.Contains(id)) return;
                if (!visiting.Add(id)) throw new ArgumentException("다음 대화가 순환합니다: " + id);
                foreach (var choice in byId[id].Choices)
                    if (!string.IsNullOrEmpty(choice.NextDialogue)) Visit(choice.NextDialogue);
                visiting.Remove(id); checkedIds.Add(id);
            }
            foreach (var id in ids) Visit(id);
            var stories = data.Stories ?? Array.Empty<CareStoryData>();
            if (stories.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id) || x.Month < 1 || x.Month > 12
                || (!string.IsNullOrEmpty(x.Unlock) && string.IsNullOrWhiteSpace(x.Unlock))
                || !ids.Contains(x.Dialogue) || (!string.IsNullOrEmpty(x.PromiseSuccessDialogue)
                    && (!ids.Contains(x.PromiseSuccessDialogue) || !data.Promises.Any(p => p != null && p.Id == x.PromiseId && p.TargetMonth <= x.Month))))
                || stories.Select(x => x.Id).Distinct().Count() != stories.Length)
                throw new ArgumentException("스토리 ID·월·대화·약속 참조를 확인하세요.");
            foreach (string id in new[] { settings.EarlyDialogue, settings.SlightlyEarlyDialogue, settings.OnTimeDialogue, settings.OvertimeDialogue })
                if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) throw new ArgumentException("퇴근 대화 ID를 찾을 수 없습니다: " + id);
            var leaveMonths = settings.Months ?? Array.Empty<CareMonthLeave>();
            if (leaveMonths.Any(x => x == null || x.Month < 1 || x.Month > 12)
                || leaveMonths.Select(x => x.Month).Distinct().Count() != leaveMonths.Length)
                throw new ArgumentException("월별 퇴근 반응의 월은 1~12이며 중복될 수 없습니다.");
            foreach (var monthly in leaveMonths)
                foreach (string id in new[] { monthly.EarlyDialogue, monthly.SlightlyEarlyDialogue,
                    monthly.OnTimeDialogue, monthly.OvertimeDialogue })
                    if (string.IsNullOrWhiteSpace(id) || !ids.Contains(id))
                        throw new ArgumentException("월별 퇴근 반응에는 네 종류의 대화가 필요합니다: " + monthly.Month);
            if (data.MonthTalks.Any(x => x == null || x.Month < 1 || x.Month > 12 || !ids.Contains(x.Dialogue))
                || data.MonthTalks.Select(x => x.Month).Distinct().Count() != data.MonthTalks.Length)
                throw new ArgumentException("월별 대화는 월 1~12에 하나씩 등록하세요.");
            if (data.Promises.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id))
                || data.Promises.Select(x => x.Id).Distinct().Count() != data.Promises.Length
                || data.Promises.Select(x => x.OfferMonth).Distinct().Count() != data.Promises.Length)
                throw new ArgumentException("약속 ID와 제안 월은 중복될 수 없습니다.");
            foreach (var promise in data.Promises)
            {
                if ((promise.OfferMonth != 3 && promise.OfferMonth != 6 && promise.OfferMonth != 9)
                    || promise.TargetMonth != promise.OfferMonth + 1
                    || (promise.Condition != "Early" && promise.Condition != "NoOvertime")
                    || !ids.Contains(promise.OfferDialogue) || !ids.Contains(promise.SuccessDialogue)
                    || !ids.Contains(promise.FailureDialogue)
                    || (!string.IsNullOrEmpty(promise.EarlySuccessDialogue) && !ids.Contains(promise.EarlySuccessDialogue)))
                    throw new ArgumentException("약속 월·조건·대화 참조를 확인하세요: " + promise.Id);
                var offer = data.Dialogues.First(x => x.Id == promise.OfferDialogue);
                if (offer.Friendship != 0 || offer.Choices.Length != 2 || offer.Choices.Any(x => x.Friendship != 0
                    || x.HighestStatGain != 0 || !string.IsNullOrEmpty(x.NextDialogue))
                    || !offer.Choices.Any(x => x.Key == "accept") || !offer.Choices.Any(x => x.Key == "decline"))
                    throw new ArgumentException("약속 제안에는 친밀도 변화 없는 accept / decline 선택지가 필요합니다: " + promise.Id);
            }
        }
    }
}
