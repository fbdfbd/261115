using System;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkPaperView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup paperGroup;
        [Header("상세 서류 4종")]
        [SerializeField] private GameObject dotsPanel;
        [SerializeField] private GameObject gradePanel;
        [SerializeField] private GameObject horizontalPanel;
        [SerializeField] private GameObject verticalPanel;
        [Header("닫기")]
        [SerializeField] private Button outsideButton;
        private WorkPaperPanelView activePanel;

        public event Action CloseClicked;
        public event Action<int> ChoiceStarted;
        public event Action ChoiceStopped;

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (outsideButton != null) outsideButton.onClick.AddListener(ClickClose);
        }
        private void OnDisable()
        {
            if (outsideButton != null) outsideButton.onClick.RemoveListener(ClickClose);
        }

        public void Show(WorkPaper paper)
        {
            Hide();
            GameObject panel;
            switch (paper.Display)
            {
                case PaperDisplay.Stars: panel = dotsPanel; break;
                case PaperDisplay.HorizontalBar: panel = horizontalPanel; break;
                case PaperDisplay.VerticalBar: panel = verticalPanel; break;
                default: panel = gradePanel; break;
            }
            if (panel == null) Debug.LogWarning("이 서류의 상세 패널을 연결해 주세요: " + paper.Display, this);
            // The paper itself must block clicks from reaching the outside close button.
            var background = panel != null ? panel.GetComponent<Graphic>() : null;
            if (background != null) background.raycastTarget = true;
            if (panel != null && panel.GetComponent<WorkPaperClickBlocker>() == null)
                panel.AddComponent<WorkPaperClickBlocker>();
            activePanel = panel != null ? panel.GetComponent<WorkPaperPanelView>() : null;
            if (activePanel != null)
            {
                activePanel.HoldStarted += StartChoice;
                activePanel.HoldStopped += StopChoice;
            }
            else if (panel != null) Debug.LogWarning("문서 루트에 WorkPaperPanelView를 연결해 주세요.", panel);
            WorkUi.Show(panel, true);
            if (activePanel != null) activePanel.Show(paper);
            WorkUi.Show(paperGroup, panel != null);
        }

        public void Hide()
        {
            if (activePanel != null)
            {
                activePanel.ClearHold();
                activePanel.HoldStarted -= StartChoice;
                activePanel.HoldStopped -= StopChoice;
                activePanel = null;
            }
            WorkUi.Show(paperGroup, false);
            HidePanels();
        }

        private void HidePanels()
        {
            WorkUi.Show(dotsPanel, false);
            WorkUi.Show(gradePanel, false);
            WorkUi.Show(horizontalPanel, false);
            WorkUi.Show(verticalPanel, false);
        }
        public void ShowHold(int index, float progress, bool allowInput)
        {
            if (activePanel != null) activePanel.ShowHold(index, progress, allowInput);
        }

        private void StartChoice(int index) => ChoiceStarted?.Invoke(index);
        private void StopChoice() => ChoiceStopped?.Invoke();
        private void ClickClose() => CloseClicked?.Invoke();
    }
}
