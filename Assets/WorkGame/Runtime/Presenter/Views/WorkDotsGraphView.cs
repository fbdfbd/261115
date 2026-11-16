using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkDotsGraphView : WorkGraphView
    {
        [Serializable]
        public sealed class DotsItem
        {
            public GameObject Root;
            public TMP_Text NameText;
            public List<Image> Dots = new List<Image>();
        }

        [SerializeField] private List<DotsItem> items = new List<DotsItem>();
        [SerializeField] private Color filledColor = new Color(0.27f, 0.38f, 0.46f);
        [SerializeField] private Color emptyColor = new Color(0.86f, 0.88f, 0.88f);

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
                for (int dot = 0; dot < item.Dots.Count; dot++)
                    if (item.Dots[dot] != null)
                        item.Dots[dot].color = dot < paper.Choices[i].Value ? filledColor : emptyColor;
            }
        }
    }
}
