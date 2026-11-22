using System;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Audio/Character SFX")]
    public sealed class CharacterSfxSO : ScriptableObject
    {
        [Header("쓰다듬기 — 정상 접수된 클릭에서 목록 중 하나를 무작위 재생")]
        [Tooltip("빈 항목은 제외합니다. 각 항목의 Volume / Pitch를 적용합니다.")]
        public GameSfxClip[] Petting = Array.Empty<GameSfxClip>();
        [Tooltip("짧은 소리의 연타 방지 간격(초). 소리가 재생 중이면 새 소리를 겹치거나 중간에 끊지 않습니다.")]
        [Min(0f)] public float PettingMinInterval = 0.15f;

        public int PettingCount
        {
            get
            {
                int count = 0;
                if (Petting != null)
                    foreach (var sound in Petting) if (sound?.Clip != null) count++;
                return count;
            }
        }

        // Index into the valid entries, rather than the raw array with empty Inspector slots.
        public GameSfxClip GetPetting(int index)
        {
            if (index < 0 || Petting == null) return null;
            foreach (var sound in Petting)
                if (sound?.Clip != null && index-- == 0) return sound;
            return null;
        }
    }
}
