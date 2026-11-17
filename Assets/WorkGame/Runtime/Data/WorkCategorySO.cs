using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [Serializable]
    public sealed class WorkMetric
    {
        [Tooltip("상황 제목. 예: 서류 한 장 넘기려고 예약하는 전동 책상")]
        public string Title;
        [Tooltip("요청 기관. 비워두면 기존 공통 업무입니다.")]
        public string Agency;
        [Tooltip("비교 항목. 예: 에너지 소비량")]
        public string Name;
        [Tooltip("비교 항목 Name에 포함된 단어. 검토 강조 1단계에서 색상을 적용합니다.")]
        public List<string> HighlightWords = new List<string>();
        public string Unit;
        public PickRule Rule;
        public PaperDisplay Display;
        public int Min = 1;
        public int Max = 100;
        // 0 uses Max. Bars start at zero; this is the full-length value.
        public int ScaleMax;
    }

    [CreateAssetMenu(menuName = "Work Game/Category")]
    public sealed class WorkCategorySO : ScriptableObject
    {
        public string Id;
        public string Title;
        public string ShortName;
        public Color HudColor = new Color(0.88f, 0.91f, 0.93f);
        public string Target;
        public List<WorkMetric> Metrics = new List<WorkMetric>();
    }
}
