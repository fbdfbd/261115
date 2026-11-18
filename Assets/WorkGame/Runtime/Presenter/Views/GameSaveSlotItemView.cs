using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameSaveSlotItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text slotText;
        [SerializeField] private TMP_Text monthText;
        [SerializeField] private TMP_Text[] statTexts = new TMP_Text[4];
        private SaveSlot slot;
        public event Action<SaveSlot> Selected;
        private void OnEnable() { if (button != null) button.onClick.AddListener(Select); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Select); }
        public void Show(SaveSlotInfo info, bool selectable)
        {
            slot = info.Slot;
            bool ready = info.State == SaveSlotState.Ready;
            if (button != null) button.interactable = selectable;
            if (slotText != null) slotText.text = slot == SaveSlot.Auto ? "자동 저장" : "슬롯 " + (int)slot;
            if (monthText != null) monthText.text = ready ? info.Month + "월"
                : info.State == SaveSlotState.Empty ? "비어 있음" : "읽기 실패";
            for (int i = 0; i < statTexts.Length && i < 4; i++)
                if (statTexts[i] != null) statTexts[i].text = ready
                    ? CareStatNames.GetName((CareStat)i) + "\n" + info.GetStat((CareStat)i) : "";
        }
        private void Select() => Selected?.Invoke(slot);
    }
}
