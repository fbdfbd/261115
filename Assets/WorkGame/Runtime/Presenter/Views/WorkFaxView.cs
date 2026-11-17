using System;
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
        public event Action<int> PaperDropped;
        public bool ForceEnabled => forceEnabled;
        private int lastCount = -1;
        private int lastSlots = -1;

        public void OnDrop(PointerEventData data)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (!group.interactable || !group.blocksRaycasts || data.pointerDrag == null) return;
            var paper = data.pointerDrag.GetComponent<WorkPaperItemView>();
            if (paper != null && paper.DraggedPaperId >= 0) PaperDropped?.Invoke(paper.DraggedPaperId);
        }

        public void ShowState(bool visible, bool canDrop, int count, int slots, float progress)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            WorkUi.Show(group, visible);
            group.interactable = visible && canDrop;
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
            progressSlider.SetValueWithoutNotify(Mathf.Clamp01(progress));
        }
    }
}
