using System;
using UnityEngine;

namespace WorkGame
{
    public enum WorkSfxCue
    {
        PaperHover, PaperPick, PaperDrop, HoldLoop, HoldComplete, PaperFly, LowTime,
        BubbleNormal, BubbleWrong, BubbleDoubleReward, BubbleInstant, FaxLoop,
        Progress, ReportRow, ReportComplete, SkillPointsGain, BubbleDisappear
    }

    [Serializable]
    public sealed class GameSfxClip
    {
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0.5f, 2f)] public float Pitch = 1f;
        [Tooltip("같은 효과음의 최소 재생 간격(초). 0은 제한 없음. 반복 재생에는 적용하지 않습니다.")]
        [Min(0f)] public float MinInterval;
    }

    [CreateAssetMenu(menuName = "Work Game/Audio/Work SFX")]
    public sealed class WorkSfxSO : ScriptableObject
    {
        [Header("1. 책상 위 서류에 마우스를 올렸을 때")]
        public GameSfxClip PaperHover = new GameSfxClip { MinInterval = 0.06f };
        [Header("2. 서류 집기 / 내려놓기 — 상세 열기·닫기와 드래그")]
        public GameSfxClip PaperPick = new GameSfxClip();
        public GameSfxClip PaperDrop = new GameSfxClip();
        [Header("3. 서류 내부 버튼 누르는 동안 반복 — 손 떼면 정지")]
        public GameSfxClip HoldLoop = new GameSfxClip();
        [Header("4. 꾸욱 누르기 완료 — 정답/오답 판정 순간")]
        public GameSfxClip HoldComplete = new GameSfxClip();
        [Header("5. 서류가 실제로 날아가기 시작할 때")]
        public GameSfxClip PaperFly = new GameSfxClip();
        [Header("6. 시간 부족 경고 — 월마다 기준 진입 시 한 번")]
        [Min(0f)] public float LowTimeSeconds = 10f;
        public GameSfxClip LowTime = new GameSfxClip();
        [Header("7~10. 버블이 실제로 표시될 때 — 종류별 하나만 재생")]
        [Tooltip("7. 일반 정답 버블")]
        public GameSfxClip BubbleNormal = new GameSfxClip();
        [Tooltip("8. 오답 버블")]
        public GameSfxClip BubbleWrong = new GameSfxClip();
        [Tooltip("9. 2배 보상 버블")]
        public GameSfxClip BubbleDoubleReward = new GameSfxClip();
        [Tooltip("10. 즉시완료 버블. 2배 보상도 함께 발생하면 이 소리를 우선합니다.")]
        public GameSfxClip BubbleInstant = new GameSfxClip();
        [Header("11. 팩스가 서류를 처리하는 동안 반복")]
        public GameSfxClip FaxLoop = new GameSfxClip();
        [Header("12. 업무 진행도 숫자가 증가할 때 — 초기 표시·월 초기화 제외")]
        public GameSfxClip Progress = new GameSfxClip();
        [Header("13. 월 성과표의 각 줄이 등장하기 시작할 때")]
        public GameSfxClip ReportRow = new GameSfxClip();
        [Header("14. 월 성과표의 전체 결과 연출이 끝났을 때")]
        public GameSfxClip ReportComplete = new GameSfxClip();
        [Header("15. 숙련 포인트 증가 — 증가 1회당 재생, 초기 표시·감소 제외")]
        public GameSfxClip SkillPointsGain = new GameSfxClip();
        [Header("16. 버블 퇴장 완료 — 자동·클릭·밀려나기 공통, 강제 정리·방해금지 제외")]
        public GameSfxClip BubbleDisappear = new GameSfxClip();

        public GameSfxClip Get(WorkSfxCue cue)
        {
            switch (cue)
            {
                case WorkSfxCue.PaperHover: return PaperHover;
                case WorkSfxCue.PaperPick: return PaperPick;
                case WorkSfxCue.PaperDrop: return PaperDrop;
                case WorkSfxCue.HoldLoop: return HoldLoop;
                case WorkSfxCue.HoldComplete: return HoldComplete;
                case WorkSfxCue.PaperFly: return PaperFly;
                case WorkSfxCue.LowTime: return LowTime;
                case WorkSfxCue.BubbleNormal: return BubbleNormal;
                case WorkSfxCue.BubbleWrong: return BubbleWrong;
                case WorkSfxCue.BubbleDoubleReward: return BubbleDoubleReward;
                case WorkSfxCue.BubbleInstant: return BubbleInstant;
                case WorkSfxCue.FaxLoop: return FaxLoop;
                case WorkSfxCue.Progress: return Progress;
                case WorkSfxCue.ReportRow: return ReportRow;
                case WorkSfxCue.ReportComplete: return ReportComplete;
                case WorkSfxCue.SkillPointsGain: return SkillPointsGain;
                case WorkSfxCue.BubbleDisappear: return BubbleDisappear;
                default: return null;
            }
        }

        public static WorkSfxCue BubbleCue(bool wrong, bool instant, bool boosted)
            => wrong ? WorkSfxCue.BubbleWrong : instant ? WorkSfxCue.BubbleInstant
                : boosted ? WorkSfxCue.BubbleDoubleReward : WorkSfxCue.BubbleNormal;
    }
}
