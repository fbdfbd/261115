using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareStatusView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private List<Button> closeButtons = new List<Button>();
        [SerializeField] private Button furnitureButton;
        [SerializeField] private List<CareStatRowView> mainStats = new List<CareStatRowView>();
        [SerializeField] private List<CareStatRowView> detailStats = new List<CareStatRowView>();
        [SerializeField] private List<CareEquippedItemView> equipped = new List<CareEquippedItemView>();
        public IReadOnlyList<CareStatRowView> MainStats => mainStats;
        public IReadOnlyList<CareStatRowView> DetailStats => detailStats;
        public IReadOnlyList<CareEquippedItemView> Equipped => equipped;
        public event Action CloseClicked;
        public event Action FurnitureClicked;
        private void Awake() => SetVisible(false);
        private void OnEnable()
        {
            foreach (var button in closeButtons) if (button != null) button.onClick.AddListener(Close);
            if (furnitureButton != null) furnitureButton.onClick.AddListener(OpenFurniture);
            foreach (var slot in equipped) if (slot != null) slot.Clicked += OpenFurniture;
        }
        private void OnDisable()
        {
            foreach (var button in closeButtons) if (button != null) button.onClick.RemoveListener(Close);
            if (furnitureButton != null) furnitureButton.onClick.RemoveListener(OpenFurniture);
            foreach (var slot in equipped) if (slot != null) slot.Clicked -= OpenFurniture;
        }
        public void SetVisible(bool visible) => WorkUi.Show(panelGroup, visible);
        private void Close() => CloseClicked?.Invoke();
        private void OpenFurniture() => FurnitureClicked?.Invoke();
    }
}
