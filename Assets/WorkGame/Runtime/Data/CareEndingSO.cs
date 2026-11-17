using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [Serializable]
    public sealed class CareEndingPage
    {
        [Tooltip("첫 페이지 외에는 비워 두면 앞 이미지를 유지합니다.")]
        public Sprite Image;
        [TextArea(4, 16)] public string Text;
    }

    [CreateAssetMenu(menuName = "Work Game/Care/Ending")]
    public sealed class CareEndingSO : ScriptableObject
    {
        public string Id;
        public string Name;
        [Tooltip("복수 조건 충족 시 큰 값 우선. 같으면 ID 순으로 결정합니다.")]
        public int Priority;
        [Tooltip("대표 스탯의 최소 등급. 모든 조건을 충족해야 합니다.")]
        public List<CareMainNeed> MainNeeds = new List<CareMainNeed>();
        [Tooltip("세부 스탯의 최소 수치. 모든 조건을 충족해야 합니다.")]
        public List<CareGain> Needs = new List<CareGain>();
        [TextArea(3, 8)] public string Text;
        [TextArea(2, 6)] public string LowText;
        [TextArea(2, 6)] public string NormalText;
        [TextArea(2, 6)] public string HighText;
        [Header("엔딩 연출 — 비워 두면 기존 본문 사용")]
        public List<CareEndingPage> Pages = new List<CareEndingPage>();
        [Tooltip("선택된 친밀도 후일담을 마지막 편지 페이지로 덧붙입니다.")]
        public bool AppendFriendshipText = true;
        public CareEnding CreateEnding() => new CareEnding(Id, Name, Priority, Needs,
            Text, LowText, NormalText, HighText, MainNeeds);
    }
}
