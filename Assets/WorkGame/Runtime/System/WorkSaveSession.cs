using System;

namespace WorkGame
{
    // 자동 저장 시점만 연결합니다. 저장 형식과 슬롯 관리는 WorkSaveSystem이 맡습니다.
    public sealed class WorkSaveSession : IDisposable
    {
        private readonly WorkSaveSystem saves;
        private readonly WorkSystem work;
        private readonly CareSystem care;
        private readonly CareFlowSystem flow;
        private readonly int seed;
        private int savedWorkMonth;
        public event Action<Exception> Failed;

        public WorkSaveSession(WorkSaveSystem saves, int seed, WorkSystem work, CareSystem care = null,
            CareFlowSystem flow = null, bool restored = false)
        {
            if (saves == null || work == null || (care == null) != (flow == null))
                throw new ArgumentException("저장·업무 시스템과 육성 연결을 확인하세요.");
            this.saves = saves; this.seed = seed; this.work = work; this.care = care; this.flow = flow;
            savedWorkMonth = restored ? work.Run.Month : -1;
            work.Changed += SaveWorkStart;
            if (flow != null)
            {
                flow.CareStarting += SaveCareStart;
                flow.EndingStarting += SaveEndingChoice;
            }
        }

        private void SaveWorkStart()
        {
            if (work.Run.Phase != WorkPhase.Working || savedWorkMonth == work.Run.Month) return;
            savedWorkMonth = work.Run.Month;
            Save(SavePoint.WorkStart);
        }
        private void SaveCareStart() => Save(SavePoint.CareStart);
        private void SaveEndingChoice() => Save(SavePoint.EndingChoice);
        private void Save(SavePoint point)
        {
            try { saves.Capture(seed, point, work, care, flow); }
            catch (Exception e) { Failed?.Invoke(e); }
        }
        public void Dispose()
        {
            work.Changed -= SaveWorkStart;
            if (flow != null)
            {
                flow.CareStarting -= SaveCareStart;
                flow.EndingStarting -= SaveEndingChoice;
            }
            Failed = null;
        }
    }
}
