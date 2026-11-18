using System;
using System.Linq;

namespace WorkGame
{
    public sealed partial class CareFlowSystem
    {
        public CareFlowSaveData CaptureSave() => new CareFlowSaveData
        {
            Completed = Dialogue.Completed.ToArray(), History = Dialogue.CaptureHistory(), Promises = Promises.CaptureSave()
        };

        public void RestoreSave(CareFlowSaveData data, SavePoint point)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CareFlowSystem));
            if (data?.Completed == null || data.History == null || data.Promises == null)
                throw new ArgumentException("대화 및 약속 저장 데이터가 없습니다.");
            Dialogue.RestoreSave(data);
            Promises.RestoreSave(data.Promises);
            shownMonth = care.Month; shownPhase = work.Run.Phase;
            offeredPromise = null; Panel = CarePanel.None;
            Phase = point == SavePoint.WorkStart ? CareFlowPhase.Working
                : point == SavePoint.CareStart ? CareFlowPhase.Report : CareFlowPhase.EndingChoice;
        }

        // View 연결이 끝난 뒤 호출합니다. 후보 한 개는 기존처럼 바로 엔딩을 엽니다.
        public void ResumeEndingChoice()
        {
            if (disposed || work.Run.Phase != WorkPhase.Finished || !care.HasResult) return;
            if (care.GetEndingChoices().Count == 1)
            {
                SetPhase(CareFlowPhase.Ended);
                if (care.ResolveEnding() != null) Finished?.Invoke();
            }
            else SetPhase(CareFlowPhase.EndingChoice);
        }
    }

    public sealed partial class CareDialogueSystem
    {
        internal SavedDialogueLog[] CaptureHistory() => history.Select(log =>
        {
            var data = dialogues[log.DialogueId];
            int index = log.IsChoice ? Array.FindIndex(data.Choices, choice => choice.Text == log.Text)
                : Array.FindIndex(data.Lines, line => line.Text == log.Text && line.ImageKey == log.ImageKey
                    && (string.IsNullOrEmpty(line.Speaker) ? "달이" : line.Speaker) == log.Speaker);
            return new SavedDialogueLog { Month = log.Month, Dialogue = log.DialogueId, Index = index, IsChoice = log.IsChoice };
        }).ToArray();

        internal void RestoreSave(CareFlowSaveData data)
        {
            if (data.Completed.Any(string.IsNullOrWhiteSpace) || data.Completed.Distinct().Count() != data.Completed.Length)
                throw new ArgumentException("대화 완료 기록을 확인하세요.");
            var logs = data.History.Select(log =>
            {
                if (log == null || log.Month < 1 || log.Month > 12 || log.Dialogue == null
                    || !dialogues.TryGetValue(log.Dialogue, out var dialogue) || log.Index < 0
                    || log.Index >= (log.IsChoice ? dialogue.Choices.Length : dialogue.Lines.Length))
                    throw new ArgumentException("대화 로그의 원본을 찾을 수 없습니다.");
                if (log.IsChoice) return new CareDialogueLog(log.Month, log.Dialogue, "나르미", dialogue.Choices[log.Index].Text, null, true);
                var line = dialogue.Lines[log.Index];
                return new CareDialogueLog(log.Month, log.Dialogue, string.IsNullOrEmpty(line.Speaker) ? "달이" : line.Speaker, line.Text, line.ImageKey);
            }).ToArray();
            Clear(); completed.Clear(); completed.AddRange(data.Completed);
            history.Clear(); history.AddRange(logs);
        }
    }

    public sealed partial class CarePromiseSystem
    {
        internal SavedPromise[] CaptureSave() => records.Select(record => new SavedPromise
        {
            Id = record.Promise.Id, State = record.State, ResultDialogue = record.ResultDialogue
        }).ToArray();

        internal void RestoreSave(SavedPromise[] data)
        {
            if (data.Any(record => record == null || record.Id == null || !definitions.Any(definition => definition.Id == record.Id)
                || !Enum.IsDefined(typeof(CarePromiseState), record.State)) || data.Select(record => record.Id).Distinct().Count() != data.Length)
                throw new ArgumentException("약속 저장 기록을 확인하세요.");
            records.Clear();
            foreach (var saved in data)
            {
                var definition = definitions.First(item => item.Id == saved.Id);
                if (!string.IsNullOrEmpty(saved.ResultDialogue) && saved.ResultDialogue != definition.SuccessDialogue
                    && saved.ResultDialogue != definition.EarlySuccessDialogue && saved.ResultDialogue != definition.FailureDialogue)
                    throw new ArgumentException("약속 결과 대화를 찾을 수 없습니다.");
                records.Add(new CarePromiseRecord(definition) { State = saved.State, ResultDialogue = saved.ResultDialogue });
            }
        }
    }
}
