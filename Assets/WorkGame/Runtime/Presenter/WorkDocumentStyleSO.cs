using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Document Style", fileName = "WorkDocumentStyle")]
    public sealed class WorkDocumentStyleSO : ScriptableObject
    {
        [Header("막대그래프 공통 색상 풀 — 서로 다른 5색")]
        [SerializeField] private Color barColor1 = new Color32(87, 132, 144, 255);
        [SerializeField] private Color barColor2 = new Color32(201, 146, 91, 255);
        [SerializeField] private Color barColor3 = new Color32(139, 122, 166, 255);
        [SerializeField] private Color barColor4 = new Color32(111, 148, 116, 255);
        [SerializeField] private Color barColor5 = new Color32(188, 115, 120, 255);
        [Header("숫자 뒤 단위")]
        [SerializeField] private Color unitColor = new Color32(138, 148, 129, 255);

        // Weak keys retain assignments while a paper exists without keeping old runs alive.
        private readonly ConditionalWeakTable<WorkPaper, int[]> paperColors = new ConditionalWeakTable<WorkPaper, int[]>();
        private readonly System.Random colorRandom = new System.Random();

        public Color UnitColor => unitColor;

        public Color GetBarColor(WorkPaper paper, int choiceIndex)
        {
            if (paper == null) throw new ArgumentNullException(nameof(paper));
            if (choiceIndex < 0 || choiceIndex >= 5) throw new ArgumentOutOfRangeException(nameof(choiceIndex));
            var indices = paperColors.GetValue(paper, CreateColorOrder);
            switch (indices[choiceIndex])
            {
                case 0: return barColor1;
                case 1: return barColor2;
                case 2: return barColor3;
                case 3: return barColor4;
                default: return barColor5;
            }
        }

        private int[] CreateColorOrder(WorkPaper paper)
        {
            var indices = new[] { 0, 1, 2, 3, 4 };
            // Independent from UnityEngine.Random so cosmetic draws do not change gameplay.
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int other = colorRandom.Next(i + 1);
                int saved = indices[i];
                indices[i] = indices[other];
                indices[other] = saved;
            }
            return indices;
        }
    }
}
