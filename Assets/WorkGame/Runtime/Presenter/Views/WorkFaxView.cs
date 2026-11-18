using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WorkFaxView : MonoBehaviour, IDropHandler
    {
        [Tooltip("플레이 중 해금·표시 상태를 무시하고 팩스 표시와 드롭 접수를 테스트합니다.")]
        [SerializeField] private bool forceEnabled;
        [SerializeField] private CanvasGroup group;
        [Tooltip("이 Image의 Raycast Target을 켜두세요. 팩스 View의 자식 또는 같은 오브젝트에 둡니다.")]
        [SerializeField] private Image dropArea;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Slider progressSlider;
        [Tooltip("실제 진행률이 0일 때 슬라이더에 표시할 시작 비율입니다.")]
        [SerializeField, Range(0, 1)] private float progressStart = 0.1f;
        [Header("작동 중 외형 진동 — 판정 영역과 UI는 고정")]
        [SerializeField] private RectTransform faxVisual;
        [SerializeField, Min(0)] private float vibrationPixels = 1.5f;
        [SerializeField, Range(0, 2)] private float vibrationAngle = 0.3f;
        [SerializeField, Min(0.04f)] private float vibrationSeconds = 0.12f;
        public event Action<int> PaperDropped;
        public bool ForceEnabled => forceEnabled;
        private int lastCount = -1;
        private int lastSlots = -1;
        private Tween vibration;
        private float vibrationPhase;
        private Vector2 visualHome;
        private Quaternion visualRotation;
        private bool operating;

        private void OnDisable() => SetOperating(false);
        private void OnDestroy() => vibration?.Kill();

        public void OnDrop(PointerEventData data)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (!group.interactable || !group.blocksRaycasts || data.pointerDrag == null) return;
            var paper = data.pointerDrag.GetComponent<WorkPaperItemView>();
            if (paper != null && paper.DraggedPaperId >= 0) PaperDropped?.Invoke(paper.DraggedPaperId);
        }

        public void ShowState(bool visible, bool canDrop, int count, int slots, float progress, bool processing = true)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            WorkUi.Show(group, visible);
            group.interactable = visible && canDrop;
            SetOperating(visible && count > 0 && processing && isActiveAndEnabled);
            if (dropArea != null) dropArea.raycastTarget = true;
            if (lastCount != count || lastSlots != slots)
            {
                lastCount = count;
                lastSlots = slots;
                if (countText != null) countText.text = count + "/" + slots;
            }
            if (progressSlider == null) return;
            WorkUi.Show(progressSlider.gameObject, count > 0);
            if (count == 0) return;
            progressSlider.minValue = 0;
            progressSlider.maxValue = 1;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(Mathf.Lerp(progressStart, 1f, Mathf.Clamp01(progress)));
        }

        private void SetOperating(bool value)
        {
            if (faxVisual == null || operating == value) return;
            operating = value;
            if (operating)
            {
                if (vibration == null)
                {
                    visualHome = faxVisual.anchoredPosition;
                    visualRotation = faxVisual.localRotation;
                    vibration = DOTween.To(() => vibrationPhase, ApplyVibration, 1f, vibrationSeconds)
                        .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart)
                        .SetAutoKill(false).SetUpdate(true).Pause();
                }
                vibration.Restart();
            }
            else
            {
                vibration?.Pause();
                faxVisual.anchoredPosition = visualHome;
                faxVisual.localRotation = visualRotation;
            }
        }

        private void ApplyVibration(float phase)
        {
            vibrationPhase = phase;
            float wave = Mathf.Sin(phase * Mathf.PI * 2);
            faxVisual.anchoredPosition = visualHome + new Vector2(wave * vibrationPixels, 0);
            faxVisual.localRotation = visualRotation * Quaternion.Euler(0, 0, wave * vibrationAngle);
        }
    }
}
