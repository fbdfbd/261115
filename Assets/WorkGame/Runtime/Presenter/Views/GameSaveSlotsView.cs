using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameSaveSlotsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private List<GameSaveSlotItemView> slots = new List<GameSaveSlotItemView>();
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Color successColor = new Color(0.2f, 0.45f, 0.3f);
        [SerializeField] private Color errorColor = new Color(0.7f, 0.2f, 0.2f);
        public bool IsOpen { get; private set; }
        public event Action CloseRequested;
        public event Action<SaveSlot> Selected;
        private void Awake() => Hide();
        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
            if (backgroundButton != null) backgroundButton.onClick.AddListener(RequestClose);
            foreach (var slot in slots) if (slot != null) slot.Selected += Select;
        }
        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
            if (backgroundButton != null) backgroundButton.onClick.RemoveListener(RequestClose);
            foreach (var slot in slots) if (slot != null) slot.Selected -= Select;
            Hide();
        }
        public void ShowSlot(int index, SaveSlotInfo info, bool selectable)
        {
            if (index >= 0 && index < slots.Count && slots[index] != null) slots[index].Show(info, selectable);
        }
        public void Open(SavePanelMode mode)
        {
            IsOpen = true; ShowError(null);
            if (titleText != null) titleText.text = mode == SavePanelMode.Save ? "저장하기" : "불러오기";
            WorkUi.Show(panelGroup, true);
        }
        public void Hide() { IsOpen = false; WorkUi.Show(panelGroup, false); }
        public void ShowError(string message) => ShowMessage(message, true);
        public void ShowMessage(string message, bool error = false)
        {
            if (errorText == null) return;
            errorText.color = error ? errorColor : successColor;
            errorText.text = message ?? ""; WorkUi.Show(errorText.gameObject, !string.IsNullOrEmpty(message));
        }
        private void RequestClose() => CloseRequested?.Invoke();
        private void Select(SaveSlot slot) => Selected?.Invoke(slot);
    }
}
