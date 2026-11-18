using System;

namespace WorkGame
{
    // 타이틀에서 요청한 뒤 게임 씬을 엽니다. 요청은 WorkRoot.Start에서 한 번만 소비합니다.
    public static class WorkStartRequest
    {
        private static SaveSlot? slot;
        public static void NewRun() => slot = null;
        public static void ContinueAuto() => slot = SaveSlot.Auto;
        public static void LoadManual(int number)
        {
            if (number < 1 || number > 4) throw new ArgumentOutOfRangeException(nameof(number));
            slot = (SaveSlot)number;
        }
        public static void Load(SaveSlot savedSlot)
        {
            if (!Enum.IsDefined(typeof(SaveSlot), savedSlot)) throw new ArgumentOutOfRangeException(nameof(savedSlot));
            slot = savedSlot;
        }
        public static SaveSlot? Take() { var result = slot; slot = null; return result; }
    }
}
