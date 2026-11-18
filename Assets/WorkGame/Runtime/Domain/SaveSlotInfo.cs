using System;

namespace WorkGame
{
    public enum SaveSlotState { Empty, Ready, Invalid }
    public enum SavePanelMode { Load, Save }

    // 화면에 필요한 정보만 전달합니다. 대표 스탯은 저장된 세부 스탯 3개씩의 합입니다.
    public sealed class SaveSlotInfo
    {
        private readonly int[] stats = new int[4];
        public SaveSlot Slot { get; }
        public SaveSlotState State { get; }
        public int Month { get; }
        public string Error { get; }
        public SaveSlotInfo(SaveSlot slot, SaveSlotState state, GameSaveData data = null, string error = null)
        {
            Slot = slot; State = state; Error = error;
            if (data == null) return;
            Month = data.Work.Month;
            if (data.Care == null) return;
            for (int main = 0; main < 4; main++)
            {
                int first = main * 3;
                stats[main] = (int)Math.Min(int.MaxValue, (long)data.Care.Stats[first]
                    + data.Care.Stats[first + 1] + data.Care.Stats[first + 2]);
            }
        }
        public int GetStat(CareStat main)
        {
            if (!CareStats.IsMain(main)) throw new ArgumentException("대표 스탯만 표시합니다.", nameof(main));
            return stats[(int)main];
        }
    }
}
