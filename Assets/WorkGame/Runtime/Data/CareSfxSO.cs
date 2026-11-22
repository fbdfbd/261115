using System;
using UnityEngine;

namespace WorkGame
{
    public enum CareSfxCue
    {
        DialogueBlip, DialogueNext, DialogueChoice, PanelOpen, PanelClose,
        ScheduleAdd, ScheduleRemove, Synergy, Purchase, Equip, Unequip, Denied,
        WeekOpen, StatRise, GradeUp, WeekComplete, EndingSelect, EndingPage, EndingComplete
    }

    public interface ICareSfxSource
    {
        event Action<CareSfxCue> SoundRequested;
    }

    [CreateAssetMenu(menuName = "Work Game/Audio/Care SFX")]
    public sealed class CareSfxSO : ScriptableObject
    {
        [Header("대사 — 글자 출력 중 / 넘기기 / 선택지 확정")]
        public GameSfxClip DialogueBlip = new GameSfxClip { MinInterval = 0.065f };
        public GameSfxClip DialogueNext = new GameSfxClip();
        public GameSfxClip DialogueChoice = new GameSfxClip();
        [Header("창·로그 열기 / 닫기 — 일반 UI 클릭과 별개")]
        public GameSfxClip PanelOpen = new GameSfxClip();
        public GameSfxClip PanelClose = new GameSfxClip();
        [Header("스케줄 — N주차 편성 / 해제 / 새로운 시너지 성립")]
        public GameSfxClip ScheduleAdd = new GameSfxClip();
        public GameSfxClip ScheduleRemove = new GameSfxClip();
        public GameSfxClip Synergy = new GameSfxClip();
        [Header("구매 성공 — Work 스킬·Care 가구 공통. 구매 시 자동 장착음은 중복 재생 안 함")]
        public GameSfxClip Purchase = new GameSfxClip();
        [Header("보유 가구 장착 / 해제")]
        public GameSfxClip Equip = new GameSfxClip();
        public GameSfxClip Unequip = new GameSfxClip();
        [Header("진행 거절 — 수업료 부족 등")]
        public GameSfxClip Denied = new GameSfxClip();
        [Header("주차 결과 — 페이지 등장 / 스탯 행 등장 / 등급 상승 / 연출 완료")]
        public GameSfxClip WeekOpen = new GameSfxClip();
        public GameSfxClip StatRise = new GameSfxClip { MinInterval = 0.09f };
        public GameSfxClip GradeUp = new GameSfxClip { MinInterval = 0.12f };
        public GameSfxClip WeekComplete = new GameSfxClip();
        [Header("엔딩 — 진로 확정 / 페이지 / 마지막 완료")]
        public GameSfxClip EndingSelect = new GameSfxClip();
        public GameSfxClip EndingPage = new GameSfxClip();
        public GameSfxClip EndingComplete = new GameSfxClip();

        public GameSfxClip Get(CareSfxCue cue) => cue switch
        {
            CareSfxCue.DialogueBlip => DialogueBlip, CareSfxCue.DialogueNext => DialogueNext,
            CareSfxCue.DialogueChoice => DialogueChoice, CareSfxCue.PanelOpen => PanelOpen,
            CareSfxCue.PanelClose => PanelClose, CareSfxCue.ScheduleAdd => ScheduleAdd,
            CareSfxCue.ScheduleRemove => ScheduleRemove, CareSfxCue.Synergy => Synergy,
            CareSfxCue.Purchase => Purchase, CareSfxCue.Equip => Equip, CareSfxCue.Unequip => Unequip,
            CareSfxCue.Denied => Denied, CareSfxCue.WeekOpen => WeekOpen,
            CareSfxCue.StatRise => StatRise, CareSfxCue.GradeUp => GradeUp,
            CareSfxCue.WeekComplete => WeekComplete, CareSfxCue.EndingSelect => EndingSelect,
            CareSfxCue.EndingPage => EndingPage, CareSfxCue.EndingComplete => EndingComplete,
            _ => null
        };
    }
}
