using TMPro;
using UnityEngine;

namespace WorkGame
{
    public abstract class WorkGraphView : MonoBehaviour
    {
        [SerializeField] private TMP_Text metricTitle;
        [SerializeField] private TMP_Text unitText;

        public void Show(WorkPaper paper)
        {
            if (metricTitle != null)
            {
                string unit = string.IsNullOrWhiteSpace(paper.Unit) ? "" : " (" + paper.Unit + ")";
                string label = "비교 항목 · " + paper.Metric + unit;
                int titleSize = Mathf.RoundToInt(metricTitle.fontSize) + 1;
                int labelSize = Mathf.RoundToInt(metricTitle.fontSize * 0.9f);
                metricTitle.richText = true;
                metricTitle.text = string.IsNullOrWhiteSpace(paper.Title)
                    ? $"<size={labelSize}>{label}</size>"
                    : $"<size={titleSize}>{paper.Title}</size>\n<size={labelSize}>{label}</size>";
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
