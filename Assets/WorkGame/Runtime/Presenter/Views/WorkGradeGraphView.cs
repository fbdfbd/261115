using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WorkGame
{
    public sealed class WorkGradeGraphView : WorkGraphView
    {
        [Serializable]
        public sealed class TextItem
        {
            public GameObject Root;
            public TMP_Text NameText;
            public TMP_Text ValueText;
        }

        [SerializeField] private List<TextItem> items = new List<TextItem>();
        [SerializeField] private WorkDocumentStyleSO documentStyle;

        protected override void ShowValues(WorkPaper paper)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                bool used = i < paper.Choices.Count;
                WorkUi.Show(item.Root, used);
                if (!used) continue;
                ShowName(item.NameText, paper.Choices[i].Name);
                if (item.ValueText != null)
                {
                    item.ValueText.richText = true;
                    string value = paper.GetChoiceText(i);
                    if (paper.Display == PaperDisplay.Number && !string.IsNullOrWhiteSpace(paper.Unit))
                    {
                        Color unitColor = documentStyle != null ? documentStyle.UnitColor : new Color32(138, 148, 129, 255);
                        string number = paper.Choices[i].Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        value = number + "<color=#" + ColorUtility.ToHtmlStringRGBA(unitColor) + ">" + paper.Unit + "</color>";
                    }
                    item.ValueText.text = value;
                }
            }
        }
    }
}
