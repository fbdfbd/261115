using System;
using DG.Tweening;
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
        [Header("종이 연출")]
        [SerializeField, Min(0.01f)] private float openSeconds = 0.28f;
        [SerializeField, Min(0.01f)] private float closeSeconds = 0.18f;
        [SerializeField, Min(0)] private float completePause = 0.07f;
        [SerializeField, Min(0.01f)] private float flySeconds = 0.20f;
        [SerializeField, Range(0, 0.7f)] private float dimAlpha = 0.35f;
        private WorkPaperPanelView activePanel;
        private RectTransform animatedPaper;
        private CanvasGroup panelGroup;
        private Graphic dim;
        private Sequence transition;
        private Vector3 restPosition, restScale;
        private Quaternion restRotation;
        private WorkPaperItemView sourcePaper;
        private CanvasGroup sourceGroup;
        private int shownPaperId = -1;
        private int heldChoice = -1;
        private bool inputAllowed;

        public bool IsTransitioning => transition != null && transition.IsActive();
        public float CompletionShineDelay => completePause + 0.08f;

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
            Hide();
        }

        public void Show(WorkPaper paper, int highlightLevel = 0, double workSeconds = 0,
            WorkPaperItemView source = null)
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
            if (activePanel != null) activePanel.Show(paper, highlightLevel, workSeconds);
            WorkUi.Show(paperGroup, panel != null);
            if (panel == null) return;

            animatedPaper = panel.transform as RectTransform;
            if (animatedPaper == null) return;
            // 레이아웃은 열 때 한 번만 확정합니다. 매 프레임 레이아웃/메시를 만들지 않습니다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(animatedPaper);
            restPosition = animatedPaper.localPosition;
            restRotation = animatedPaper.localRotation;
            restScale = animatedPaper.localScale;
            panelGroup = panel.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = panel.AddComponent<CanvasGroup>();
            dim = outsideButton != null ? outsideButton.targetGraphic : null;
            if (dim != null) dim.color = new Color(0, 0, 0, 0);
            if (background != null && panel.GetComponent<UnityEngine.UI.Shadow>() == null)
            {
                var shadow = panel.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.18f);
                shadow.effectDistance = new Vector2(8, -10);
            }

            sourcePaper = source;
            shownPaperId = paper.Id;
            sourceGroup = source != null ? source.GetComponent<CanvasGroup>() : null;
            GetSourcePose(out var position, out var scale, out var rotation);
            if (sourceGroup != null) sourceGroup.alpha = 0;
            animatedPaper.localPosition = position;
            animatedPaper.localScale = Vector3.Scale(scale, new Vector3(0.82f, 1, 1));
            animatedPaper.localRotation = rotation * Quaternion.Euler(0, -18, -3);
            panelGroup.alpha = 0;
            inputAllowed = true;
            SetPanelInput(false);
            transition = NewSequence();
            transition.Append(animatedPaper.DOLocalMove(restPosition, openSeconds).SetEase(Ease.OutCubic));
            transition.Join(animatedPaper.DOScale(restScale, openSeconds).SetEase(Ease.OutCubic));
            transition.Join(animatedPaper.DOLocalRotateQuaternion(restRotation, openSeconds).SetEase(Ease.OutCubic));
            transition.Insert(0.03f, FadePanel(1, openSeconds * 0.6f));
            if (dim != null) transition.Insert(0, FadeDim(dimAlpha, openSeconds));
            transition.OnComplete(() => { transition = null; SetPanelInput(inputAllowed); });
        }

        public void Hide()
        {
            transition?.Kill();
            transition = null;
            // 먼저 입력/홀드를 해제합니다. 부모 전환 중의 콜백으로 제출이 다시 일어나지 않습니다.
            if (activePanel != null)
            {
                activePanel.ClearHold();
                activePanel.HoldStarted -= StartChoice;
                activePanel.HoldStopped -= StopChoice;
                activePanel = null;
            }
            if (animatedPaper != null)
            {
                animatedPaper.localPosition = restPosition;
                animatedPaper.localScale = restScale;
                animatedPaper.localRotation = restRotation;
            }
            if (panelGroup != null) panelGroup.alpha = 1;
            // 완료된 슬롯은 이미 다른 종이에 재사용될 수 있으므로 같은 서류일 때만 복원합니다.
            if (sourceGroup != null && sourcePaper != null && sourcePaper.PaperId == shownPaperId
                && sourcePaper.Paper.State == PaperState.Pending && !sourcePaper.IsLeaving) sourceGroup.alpha = 1;
            animatedPaper = null;
            panelGroup = null;
            sourcePaper = null;
            sourceGroup = null;
            shownPaperId = -1;
            heldChoice = -1;
            inputAllowed = false;
            if (dim != null) dim.color = new Color(0, 0, 0, 0);
            WorkUi.Show(paperGroup, false);
            HidePanels();
        }

        public void Close()
        {
            if (IsTransitioning) return;
            if (animatedPaper == null) { Hide(); return; }
            SetPanelInput(false);
            GetSourcePose(out var position, out var scale, out var rotation);
            transition = NewSequence();
            transition.Append(animatedPaper.DOLocalMove(position, closeSeconds).SetEase(Ease.InCubic));
            transition.Join(animatedPaper.DOScale(scale, closeSeconds).SetEase(Ease.InCubic));
            transition.Join(animatedPaper.DOLocalRotateQuaternion(rotation, closeSeconds));
            transition.Join(FadePanel(0, closeSeconds));
            if (dim != null) transition.Join(FadeDim(0, closeSeconds));
            transition.OnComplete(Hide);
        }

        public void PlayComplete()
        {
            if (animatedPaper == null) { Hide(); return; }
            transition?.Kill();
            transition = null;
            SetPanelInput(false);
            activePanel?.ShowHold(heldChoice, 1, false);
            animatedPaper.localPosition = restPosition;
            animatedPaper.localScale = restScale;
            animatedPaper.localRotation = restRotation;
            panelGroup.alpha = 1;
            var parent = (RectTransform)animatedPaper.parent;
            // 화면 비율과 무관하게 회전한 종이 전체가 오른쪽 화면 밖으로 나갑니다.
            float diagonal = Vector2.Scale(animatedPaper.rect.size,
                new Vector2(Mathf.Abs(restScale.x), Mathf.Abs(restScale.y))).magnitude;
            var destination = new Vector3(parent.rect.xMax + diagonal,
                restPosition.y + animatedPaper.rect.height * 0.12f, restPosition.z);
            transition = NewSequence();
            transition.AppendInterval(completePause);
            transition.Append(animatedPaper.DOLocalMove(destination, flySeconds).SetEase(Ease.InCubic));
            transition.Join(animatedPaper.DOLocalRotateQuaternion(restRotation * Quaternion.Euler(0, 0, -14),
                flySeconds).SetEase(Ease.InQuad));
            if (dim != null) transition.Join(FadeDim(0, flySeconds));
            transition.OnComplete(Hide);
        }

        private Sequence NewSequence() => DOTween.Sequence().SetUpdate(true).SetTarget(this);

        private Tween FadePanel(float alpha, float seconds) =>
            DOTween.To(() => panelGroup.alpha, value => panelGroup.alpha = value, alpha, seconds);

        private Tween FadeDim(float alpha, float seconds) =>
            DOTween.To(() => dim.color.a, value => {
                var color = dim.color;
                color.a = value;
                dim.color = color;
            }, alpha, seconds);

        private void SetPanelInput(bool allowed)
        {
            if (panelGroup != null) panelGroup.interactable = allowed;
            if (outsideButton != null) outsideButton.interactable = allowed;
            activePanel?.ShowHold(-1, 0, allowed);
        }

        private void GetSourcePose(out Vector3 position, out Vector3 scale, out Quaternion rotation)
        {
            position = restPosition + new Vector3(0, -24, 0);
            scale = restScale * 0.85f;
            rotation = restRotation;
            if (sourcePaper == null || sourcePaper.PaperId != shownPaperId || !sourcePaper.gameObject.activeInHierarchy) return;
            var source = sourcePaper.Rect;
            var parent = animatedPaper.parent;
            var parentScale = parent.lossyScale;
            scale = new Vector3(
                source.rect.width * Mathf.Abs(source.lossyScale.x) / Mathf.Max(0.001f, animatedPaper.rect.width * Mathf.Abs(parentScale.x)),
                source.rect.height * Mathf.Abs(source.lossyScale.y) / Mathf.Max(0.001f, animatedPaper.rect.height * Mathf.Abs(parentScale.y)),
                restScale.z);
            rotation = Quaternion.Inverse(parent.rotation) * source.rotation;
            position = parent.InverseTransformPoint(source.TransformPoint(source.rect.center))
                - rotation * Vector3.Scale(animatedPaper.rect.center, scale);
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
            inputAllowed = allowInput;
            heldChoice = index;
            if (activePanel != null) activePanel.ShowHold(index, progress, allowInput && !IsTransitioning);
            if (panelGroup != null) panelGroup.interactable = allowInput && !IsTransitioning;
            if (outsideButton != null) outsideButton.interactable = allowInput && !IsTransitioning;
        }

        public void ShowAssist(int level, double workSeconds) => activePanel?.ShowAssist(level, workSeconds);

        private void StartChoice(int index) { if (!IsTransitioning && inputAllowed) ChoiceStarted?.Invoke(index); }
        private void StopChoice() => ChoiceStopped?.Invoke();
        private void ClickClose() { if (!IsTransitioning && inputAllowed) CloseClicked?.Invoke(); }
    }
}
