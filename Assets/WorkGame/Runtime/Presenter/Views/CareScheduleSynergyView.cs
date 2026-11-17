using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareScheduleSynergyView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text text;
        private string id;
        public event Action<string> Clicked;
        private void OnEnable() { if (button != null) button.onClick.AddListener(Click); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Click); }
        private void Click() => Clicked?.Invoke(id);
        public void Show(string synergyId, string label, Color color)
        {
            id = synergyId;
            if (text != null) { text.richText = true; text.text = label; text.color = color; }
        }
    }
}
