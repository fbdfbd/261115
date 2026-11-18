using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkBarGraphView : WorkGraphView
    {
        [Serializable]
        public sealed class BarItem
        {
            public GameObject Root;
            public TMP_Text NameText;
            public TMP_Text ValueText;
            public Slider Bar;
        }

        [SerializeField] private List<BarItem> items = new List<BarItem>();
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
                ShowName(item.ValueText, paper.GetChoiceText(i));
                if (item.Bar == null) continue;
                item.Bar.minValue = 0;
                item.Bar.maxValue = paper.DisplayMax;
                item.Bar.wholeNumbers = true;
                item.Bar.interactable = false;
                item.Bar.SetValueWithoutNotify(paper.Choices[i].Value);
                if (documentStyle != null && item.Bar.fillRect != null)
                {
                    var fill = item.Bar.fillRect.GetComponent<Graphic>();
                    if (fill != null) fill.color = documentStyle.GetBarColor(paper, i);
                }
            }
        }
    }
}
