using System;
using UnityEngine;

namespace WorkGame
{
    [Serializable]
    public sealed class GameBgmTrack
    {
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
    }

    [Serializable]
    public sealed class GameMonthBgm
    {
        public GameBgmTrack Work = new GameBgmTrack();
        public GameBgmTrack Care = new GameBgmTrack();
    }

    public enum GameEventBgmMode { Inherit, Play, Silence }

    [Serializable]
    public sealed class GameEventBgm
    {
        public string DialogueId;
        public GameEventBgmMode Mode;
        public GameBgmTrack Track = new GameBgmTrack();
    }

    [CreateAssetMenu(menuName = "Work Game/Audio/BGM")]
    public sealed class GameBgmSO : ScriptableObject
    {
        public GameBgmTrack Title = new GameBgmTrack();
        [Tooltip("타이틀 컬렉션 전용 곡. Clip 미지정 시 Title을 유지합니다.")]
        public GameBgmTrack Collection = new GameBgmTrack();
        public GameBgmTrack Tutorial = new GameBgmTrack();
        public GameBgmTrack Ending = new GameBgmTrack();
        public GameBgmTrack DefaultWork = new GameBgmTrack();
        public GameBgmTrack DefaultCare = new GameBgmTrack();
        [Tooltip("페이드아웃/페이드인 각각의 시간(초). 기존 곡을 완전히 끈 뒤 새 곡을 올립니다. 0은 즉시 전환입니다.")]
        [Min(0f)] public float FadeSeconds = 1.5f;
        [Tooltip("Element 0 = MONTH 1, Element 11 = MONTH 12. 빈 월별 곡은 기본 곡을 사용합니다.")]
        public GameMonthBgm[] Months = CreateMonths();
        [Header("Work 효과음 — Work SFX SO를 한 번 연결하면 자동 적용")]
        public WorkSfxSO WorkSounds;
        [Header("Care 효과음 — Care SFX SO를 한 번 연결하면 자동 적용")]
        public CareSfxSO CareSounds;
        [Header("캐릭터 효과음 — Character SFX SO를 한 번 연결하면 자동 적용")]
        public CharacterSfxSO CharacterSounds;
        [Tooltip("대화 시작 ID로 지정합니다. 선택지 후속 대화에도 같은 설정을 유지하며, 미등록 이벤트는 기본 BGM을 사용합니다.")]
        public GameEventBgm[] Events = Array.Empty<GameEventBgm>();

        public bool TryGetEvent(string dialogueId, out GameBgmTrack track)
        {
            track = null;
            if (string.IsNullOrEmpty(dialogueId) || Events == null) return false;
            var entry = Array.Find(Events, item => item != null && item.DialogueId == dialogueId);
            if (entry == null) return false;
            if (entry.Mode == GameEventBgmMode.Silence) return true;
            if (entry.Mode != GameEventBgmMode.Play || entry.Track?.Clip == null) return false;
            track = entry.Track;
            return true;
        }

        public GameBgmTrack GetMonth(int month, bool care)
        {
            var entry = month >= 1 && month <= 12 && Months != null && month <= Months.Length
                ? Months[month - 1] : null;
            var track = care ? entry?.Care : entry?.Work;
            return track?.Clip != null ? track : care ? DefaultCare : DefaultWork;
        }

        private static GameMonthBgm[] CreateMonths()
        {
            var months = new GameMonthBgm[12];
            for (int i = 0; i < months.Length; i++) months[i] = new GameMonthBgm();
            return months;
        }
    }
}
