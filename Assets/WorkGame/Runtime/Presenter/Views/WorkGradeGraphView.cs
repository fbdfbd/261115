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
                        string number = paper.Choices[i].Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        value = number + "<size=90%>" + paper.Unit + "</size>";
                    }
                    item.ValueText.text = value;
                }
            }
        }
    }
}
