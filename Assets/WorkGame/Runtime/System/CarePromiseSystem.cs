using System.Collections.Generic;
using System.Linq;

namespace WorkGame
{
    public sealed class CarePromiseSystem
    {
        private readonly CarePromiseData[] definitions;
        private readonly List<CarePromiseRecord> records = new List<CarePromiseRecord>();
        public IReadOnlyList<CarePromiseRecord> Records { get; }
        internal CarePromiseSystem(CarePromiseData[] definitions)
        {
            this.definitions = definitions; Records = records.AsReadOnly();
        }

        internal CarePromiseRecord Offer(int month)
        {
            var definition = definitions.FirstOrDefault(x => x.OfferMonth == month);
            if (definition == null) return null;
            var record = records.FirstOrDefault(x => x.Promise.Id == definition.Id);
            if (record == null) { record = new CarePromiseRecord(definition); records.Add(record); }
            return record.State == CarePromiseState.Offered ? record : null;
        }

        internal void Reply(string id, bool accept)
        {
            var record = records.FirstOrDefault(x => x.Promise.Id == id);
            if (record?.State == CarePromiseState.Offered)
                record.State = accept ? CarePromiseState.Accepted : CarePromiseState.Declined;
        }

        internal CarePromiseRecord Resolve(int month, CareLeaveKind kind)
        {
            var record = records.FirstOrDefault(x => x.Promise.TargetMonth == month && x.State != CarePromiseState.Declined);
            if (record == null || record.State == CarePromiseState.Offered) return null;
            if (record.State != CarePromiseState.Accepted) return record;
            bool success = record.Promise.Condition == "Early" ? kind == CareLeaveKind.Early : kind != CareLeaveKind.Overtime;
            record.State = success ? CarePromiseState.Succeeded : CarePromiseState.Failed;
            record.ResultDialogue = !success ? record.Promise.FailureDialogue
                : kind == CareLeaveKind.Early && !string.IsNullOrEmpty(record.Promise.EarlySuccessDialogue)
                    ? record.Promise.EarlySuccessDialogue : record.Promise.SuccessDialogue;
            return record;
        }
    }
}
