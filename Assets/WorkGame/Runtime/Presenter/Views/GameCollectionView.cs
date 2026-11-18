using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameCollectionView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject listPage;
        [SerializeField] private GameObject detailPage;
        [SerializeField] private List<GameCollectionItemView> items = new List<GameCollectionItemView>();
        [Header("상세 — 이미지는 고정 프레임 안에서 비율 유지")]
        [SerializeField] private Image detailImage;
        [SerializeField] private GameObject detailDim;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text mainConditionsText;
        [SerializeField] private TMP_Text detailConditionsTitle;
        [SerializeField] private RectTransform detailConditionsRow;
        [SerializeField] private TMP_Text[] detailConditions = new TMP_Text[4];
        [SerializeField] private ScrollRect detailScroll;
        [SerializeField] private RectTransform detailContent;
        public bool IsOpen { get; private set; }
        public int Capacity => items.Count;
        public event Action CloseRequested;
        public event Action BackRequested;
        public event Action<string> Selected;

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backgroundButton != null) backgroundButton.onClick.AddListener(Close);
            if (backButton != null) backButton.onClick.AddListener(Back);
            foreach (var item in items) if (item != null) item.Selected += Select;
        }
        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (backgroundButton != null) backgroundButton.onClick.RemoveListener(Close);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            foreach (var item in items) if (item != null) item.Selected -= Select;
            Hide();
        }
        public void Open(int unlocked, int total)
        {
            IsOpen = true; WorkUi.Show(panelGroup, true);
            if (countText != null) countText.SetText("발견 {0} / {1}", unlocked, total);
            for (int i = 0; i < items.Count; i++) if (items[i] != null) WorkUi.Show(items[i].gameObject, i < total);
            ShowList();
        }
        public void ShowItem(int index, string id, string name, Sprite image, bool unlocked)
        {
            if (index >= 0 && index < items.Count && items[index] != null) items[index].Show(id, name, image, unlocked);
        }
        public void ShowList()
        {
            WorkUi.Show(listPage, true); WorkUi.Show(detailPage, false);
            if (backButton != null) WorkUi.Show(backButton.gameObject, false);
            if (countText != null) WorkUi.Show(countText.gameObject, true);
            if (titleText != null) titleText.text = "달이의 성장 기록";
        }
        public void ShowDetail(string name, Sprite image, string description, string mainConditions,
            string[] columns, int maxLines, bool unlocked)
        {
            WorkUi.Show(listPage, false); WorkUi.Show(detailPage, true);
            if (backButton != null) WorkUi.Show(backButton.gameObject, true);
            if (countText != null) WorkUi.Show(countText.gameObject, false);
            if (titleText != null) titleText.text = unlocked ? name : "???";
            if (detailImage != null)
            {
                detailImage.sprite = unlocked ? image : null;
                detailImage.preserveAspect = true; detailImage.enabled = detailImage.sprite != null;
            }
            WorkUi.Show(detailDim, !unlocked);
            if (descriptionText != null)
            {
                descriptionText.text = unlocked ? description : "";
                WorkUi.Show(descriptionText.gameObject, unlocked && !string.IsNullOrWhiteSpace(description));
            }
            if (mainConditionsText != null) mainConditionsText.text = mainConditions;
            bool hasDetails = maxLines > 0;
            if (detailConditionsTitle != null) WorkUi.Show(detailConditionsTitle.gameObject, hasDetails);
            if (detailConditionsRow != null)
            {
                WorkUi.Show(detailConditionsRow.gameObject, hasDetails);
            }
            float columnHeight = 0;
            for (int i = 0; i < detailConditions.Length && i < columns.Length; i++)
                if (detailConditions[i] != null)
                {
                    detailConditions[i].text = columns[i];
                    columnHeight = Mathf.Max(columnHeight, detailConditions[i].GetPreferredValues(columns[i]).y);
                }
            if (detailConditionsRow != null && hasDetails)
                detailConditionsRow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(60, columnHeight + 16));
            if (detailContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
            if (detailScroll != null) detailScroll.verticalNormalizedPosition = 1;
        }
        public void Hide() { IsOpen = false; WorkUi.Show(panelGroup, false); }
        private void Close() => CloseRequested?.Invoke();
        private void Back() => BackRequested?.Invoke();
        private void Select(string id) => Selected?.Invoke(id);
    }
}
