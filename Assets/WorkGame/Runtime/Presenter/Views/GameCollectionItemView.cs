using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class GameCollectionItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image picture;
        [SerializeField] private GameObject lockedDim;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Color nameColor = Color.white;
        [SerializeField] private Color noImageNameColor = new Color(0.17f, 0.21f, 0.19f);
        private string endingId;
        public event Action<string> Selected;
        private void OnEnable() { if (button != null) button.onClick.AddListener(Select); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Select); }
        public void Show(string id, string name, Sprite sprite, bool unlocked)
        {
            endingId = id;
            if (button != null) button.interactable = true; // 미획득 항목도 조건을 열람할 수 있습니다.
            if (nameText != null)
            {
                nameText.text = unlocked ? name : "???";
                nameText.color = unlocked && sprite == null ? noImageNameColor : nameColor;
            }
            if (picture != null)
            {
                picture.sprite = unlocked ? sprite : null;
                picture.preserveAspect = true; picture.enabled = picture.sprite != null;
            }
            WorkUi.Show(lockedDim, !unlocked);
        }
        private void Select() { if (!string.IsNullOrEmpty(endingId)) Selected?.Invoke(endingId); }
    }
}
