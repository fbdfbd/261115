using TMPro;
using UnityEngine;

namespace WorkGame
{
    public abstract class WorkGraphView : MonoBehaviour
    {
        [SerializeField] private TMP_Text metricTitle;
        [SerializeField] private TMP_Text unitText;
        [Header("검토 강조 1단계 — 정답 방향")]
        [SerializeField] private Color highValueColor = new Color(0.15f, 0.4f, 0.95f);
        [SerializeField] private Color lowValueColor = new Color(0.9f, 0.2f, 0.2f);

        public void Show(WorkPaper paper, bool highlight = false)
        {
            if (metricTitle != null)
            {
                string unit = string.IsNullOrWhiteSpace(paper.Unit) ? "" : " (" + paper.Unit + ")";
                Color highlightColor = paper.Rule == PickRule.Highest ? highValueColor : lowValueColor;
                string metric = highlight ? WorkHighlightText.Mark(paper.Metric, paper.HighlightWords, highlightColor) : paper.Metric;
                string label = "비교 항목 · " + metric + unit;
                int titleSize = Mathf.RoundToInt(metricTitle.fontSize) + 1;
                int labelSize = Mathf.RoundToInt(metricTitle.fontSize * 0.9f);
                metricTitle.richText = true;
                string title = string.IsNullOrWhiteSpace(paper.Agency) ? paper.Title : paper.Agency + " · " + paper.Title;
                metricTitle.text = string.IsNullOrWhiteSpace(paper.Title)
                    ? $"<size={labelSize}>{label}</size>"
                    : $"<size={titleSize}>{title}</size>\n<size={labelSize}>{label}</size>";
            }
            if (unitText != null) WorkUi.Show(unitText.gameObject, false);
            ShowValues(paper);
        }

        protected abstract void ShowValues(WorkPaper paper);

        protected static void ShowName(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
