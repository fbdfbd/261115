using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareHudView : MonoBehaviour
    {
        [SerializeField] private List<CareStatRowView> mainStats = new List<CareStatRowView>();
        [SerializeField] private Button detailsButton;
        public IReadOnlyList<CareStatRowView> MainStats => mainStats;
        public event Action DetailsClicked;
        private void Awake() => SetCanOpen(false);
        private void OnEnable() { if (detailsButton != null) detailsButton.onClick.AddListener(OpenDetails); }
        private void OnDisable() { if (detailsButton != null) detailsButton.onClick.RemoveListener(OpenDetails); }
        private void OpenDetails() => DetailsClicked?.Invoke();
        public void SetCanOpen(bool value) { if (detailsButton != null) detailsButton.interactable = value; }
    }
}
