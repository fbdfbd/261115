using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkUpgradeTreeView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private bool hideOnStart = true;
        [Header("강화 모달 — 바깥 배경 포함, 선택 사항")]
        [SerializeField] private CanvasGroup modalGroup;
        [SerializeField] private Button outsideButton;
        [SerializeField] private List<WorkUpgradeNodeView> nodes = new List<WorkUpgradeNodeView>();
        [SerializeField] private TMP_Text pointsText;
        [Header("선택한 강화 — 왼쪽 상세")]
        [SerializeField] private Image selectionIcon;
        [SerializeField] private TMP_Text selectionTitle;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text selectionCost;
        [SerializeField] private TMP_Text selectionStatus;
        [SerializeField] private WorkHoldButton featureSwitch;
        [SerializeField] private TMP_Text featureSwitchText;
        private WorkUpgradeSO selectedNode;
        private bool selectedBought;
        private bool sortUnlocked, sortEnabled, faxUnlocked, faxVisible;
        private bool doNotDisturbUnlocked, doNotDisturbEnabled;
        private bool isOpen;
        public event Action Closed;
        public IReadOnlyList<WorkUpgradeNodeView> Nodes => nodes;
        public string SelectedNodeId => selectedNode != null ? selectedNode.Id : "";
        public event Action<string> BuyClicked;
        public event Action<bool> SortChanged;
        public event Action<bool> FaxChanged;
        public event Action<bool> DoNotDisturbChanged;
        public event Action SelectionChanged;

        private void Awake() { if (hideOnStart) Close(); }
        private void OnEnable()
        {
            if (outsideButton != null) outsideButton.onClick.AddListener(Close);
            foreach (var node in nodes)
            {
                if (node == null) continue;
                node.BuyClicked += Buy;
                node.Selected += SelectNode;
            }
            if (featureSwitch != null)
            {
                featureSwitch.SetAllowed(false);
                featureSwitch.GetComponent<Button>().onClick.AddListener(ChangeFeature);
            }
        }
        private void OnDisable()
        {
            if (outsideButton != null) outsideButton.onClick.RemoveListener(Close);
            foreach (var node in nodes)
            {
                if (node == null) continue;
                node.BuyClicked -= Buy;
                node.Selected -= SelectNode;
            }
            if (featureSwitch != null) featureSwitch.GetComponent<Button>().onClick.RemoveListener(ChangeFeature);
            CancelHolds();
        }

        public void Open()
        {
            isOpen = true;
            WorkUi.Show(modalGroup, true);
            WorkUi.Show(panelGroup, true);
        }
        public void Close()
        {
            bool wasOpen = isOpen;
            isOpen = false;
            CancelHolds();
            WorkUi.Show(panelGroup, false);
            WorkUi.Show(modalGroup, false);
            if (wasOpen) Closed?.Invoke();
        }
        // Consume page-area clicks if the outside Button is also an ancestor.
        public void OnPointerDown(PointerEventData data) { }
        public void OnPointerUp(PointerEventData data) { }
        public void OnPointerClick(PointerEventData data) { }
        private void Buy(string id) => BuyClicked?.Invoke(id);

        private void SelectNode(WorkUpgradeSO node)
        {
            selectedNode = node;
            selectedBought = false;
            foreach (var item in nodes) if (item != null) item.SetSelected(item.NodeId == node.Id);
            if (selectionIcon != null) { selectionIcon.sprite = node.Icon; selectionIcon.enabled = node.Icon != null; }
            if (selectionTitle != null) selectionTitle.text = node.Name;
            if (descriptionText != null) descriptionText.text = node.Description;
            UpdateSwitch();
            SelectionChanged?.Invoke();
        }

        public void ShowState(int points, bool sortUnlocked, bool sortEnabled, bool faxUnlocked, bool faxVisible,
            bool doNotDisturbUnlocked = false, bool doNotDisturbEnabled = false)
        {
            if (pointsText != null) pointsText.text = points.ToString();
            this.sortUnlocked = sortUnlocked; this.sortEnabled = sortEnabled;
            this.faxUnlocked = faxUnlocked; this.faxVisible = faxVisible;
            this.doNotDisturbUnlocked = doNotDisturbUnlocked; this.doNotDisturbEnabled = doNotDisturbEnabled;
            UpdateSwitch();
        }

        public void ShowSelectionState(WorkUpgradeState state, bool canBuy)
        {
            if (selectedNode == null) return;
            selectedBought = state == WorkUpgradeState.Bought;
            if (selectionCost != null) selectionCost.text = selectedBought ? "구매 완료" : "필요 숙련 " + selectedNode.Cost;
            if (selectionStatus != null) selectionStatus.text = selectedBought ? "구매한 스킬입니다."
                : state == WorkUpgradeState.Locked ? "선행 강화가 필요합니다."
                : canBuy ? "스킬을 꾹 눌러 구매" : "숙련 포인트가 부족합니다.";
            UpdateSwitch();
        }

        private void UpdateSwitch()
        {
            if (featureSwitch == null) return;
            var kind = selectedNode != null ? selectedNode.SwitchKind : WorkUpgradeSwitch.None;
            WorkUi.Show(featureSwitch.gameObject, kind != WorkUpgradeSwitch.None);
            bool unlocked = selectedBought && (kind == WorkUpgradeSwitch.Sort ? sortUnlocked
                : kind == WorkUpgradeSwitch.Fax ? faxUnlocked : kind == WorkUpgradeSwitch.DoNotDisturb && doNotDisturbUnlocked);
            bool enabled = kind == WorkUpgradeSwitch.Sort ? sortEnabled
                : kind == WorkUpgradeSwitch.Fax ? faxVisible : kind == WorkUpgradeSwitch.DoNotDisturb && doNotDisturbEnabled;
            // 기존 프리팹 참조는 유지하고, 기능 전환은 클릭으로만 처리합니다.
            featureSwitch.SetAllowed(false);
            featureSwitch.GetComponent<Button>().interactable = unlocked;
            if (featureSwitchText != null) featureSwitchText.text = !unlocked ? "미해금"
                : enabled ? "ON · 끄기" : "OFF · 켜기";
        }

        private void ChangeFeature()
        {
            if (!selectedBought || selectedNode == null) return;
            if (selectedNode.SwitchKind == WorkUpgradeSwitch.Sort && sortUnlocked) SortChanged?.Invoke(!sortEnabled);
            if (selectedNode.SwitchKind == WorkUpgradeSwitch.Fax && faxUnlocked) FaxChanged?.Invoke(!faxVisible);
            if (selectedNode.SwitchKind == WorkUpgradeSwitch.DoNotDisturb && doNotDisturbUnlocked)
                DoNotDisturbChanged?.Invoke(!doNotDisturbEnabled);
        }

        private void CancelHolds()
        {
            foreach (var node in nodes) if (node != null) node.CancelHold();
            if (featureSwitch != null) featureSwitch.CancelHold();
        }
    }
}
