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
                ShowName(item.ValueText, paper.GetChoiceText(i));
            }
        }
    }
}
