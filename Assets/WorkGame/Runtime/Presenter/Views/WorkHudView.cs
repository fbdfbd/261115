using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public class WorkHudView : MonoBehaviour
    {
        [Header("기능 표시")]
        [SerializeField] private bool showMonth = true;
        [SerializeField] private bool showCategories = true;
        [SerializeField] private bool showProgressText = true;
        [SerializeField] private bool showProgressSlider = true;
        [Header("월 / 카테고리")]
        [SerializeField] private TMP_Text monthText;
        [SerializeField] private List<WorkCategoryView> categorySlots = new List<WorkCategoryView>();
        [Header("처리 진행도")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;
        private int categoryCount;
        private bool categoryLayoutDirty = true;
        private readonly HashSet<RectTransform> categoryParents = new HashSet<RectTransform>();
        private int lastDone = -1;
        private int lastTotal = -1;

        private void OnEnable() => categoryLayoutDirty = true;

        private void LateUpdate()
        {
            if (!categoryLayoutDirty) return;
            categoryLayoutDirty = false;
            categoryParents.Clear();
            // Fit each label/card before the parent reads its width to arrange the row.
            foreach (var slot in categorySlots)
            {
                if (slot == null) continue;
                if (slot.gameObject.activeInHierarchy) slot.RefreshLayout();
                if (slot.transform.parent is RectTransform parent) categoryParents.Add(parent);
            }
            foreach (var parent in categoryParents) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        public void ShowMonth(int month, IEnumerable<WorkCategorySO> monthCategories)
        {
            if (monthText != null) monthText.text = month + "월";
            WorkUi.Show(monthText != null ? monthText.gameObject : null, showMonth);
            categoryCount = 0;
            foreach (var category in monthCategories)
            {
                while (categoryCount < categorySlots.Count && categorySlots[categoryCount] == null) categoryCount++;
                if (categoryCount >= categorySlots.Count)
                {
                    Debug.LogWarning("HUD 카테고리 표시 칸이 부족합니다. Category Slots에 배치한 객체를 더 연결하세요.", this);
                    break;
                }
                categorySlots[categoryCount].Show(category);
                categoryCount++;
            }
            ShowCategorySlots();
            categoryLayoutDirty = true;
        }

        public void ShowProgress(int done, int total)
        {
            WorkUi.Show(monthText != null ? monthText.gameObject : null, showMonth);
            ShowCategorySlots();
            WorkUi.Show(progressText != null ? progressText.gameObject : null, showProgressText);
            WorkUi.Show(progressSlider != null ? progressSlider.gameObject : null, showProgressSlider);
            if (lastDone == done && lastTotal == total) return;
            lastDone = done;
            lastTotal = total;
            if (progressText != null) progressText.text = done + "/" + total;
            SetProgress(total == 0 ? 0 : (float)done / total);
        }

        // Replace/override this method when adding progress animation.
        public virtual void SetProgress(float progress)
        {
            if (progressSlider == null) return;
            progressSlider.minValue = 0;
            progressSlider.maxValue = 1;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(Mathf.Clamp01(progress));
        }

        private void ShowCategorySlots()
        {
            for (int i = 0; i < categorySlots.Count; i++)
            {
                var slot = categorySlots[i];
                if (slot == null) continue;
                bool visible = showCategories && i < categoryCount;
                if (slot.gameObject.activeSelf == visible) continue;
                WorkUi.Show(slot.gameObject, visible);
                categoryLayoutDirty = true;
            }
        }
    }
}
