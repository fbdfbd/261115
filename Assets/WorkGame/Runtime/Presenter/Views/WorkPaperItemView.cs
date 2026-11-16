using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup), typeof(Button))]
    public sealed class WorkPaperItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button clickButton;
        [SerializeField] private CanvasGroup paperGroup;
        [SerializeField] private Outline hoverOutline;
        [Header("호버 꿈틀")]
        [SerializeField] private bool hoverWiggle = true;
        [SerializeField, Range(0, 10)] private float wiggleAngle = 4;
        [SerializeField, Range(0.1f, 0.5f)] private float wiggleSeconds = 0.22f;
        [Header("연출 — 선택 사항")]
        [SerializeField] private UnityEvent onShow;
        [SerializeField] private UnityEvent onComplete;
        [Tooltip("체크하면 완료 연출 끝에 FinishRemove()를 호출해야 종이가 재사용됩니다.")]
        [SerializeField] private bool waitForRemoveAnimation;
        private Action<int> clicked;
        private Tween wiggle;
        private Quaternion restRotation = Quaternion.identity;

        public WorkPaper Paper { get; private set; }
        public int PaperId => Paper?.Id ?? -1;
        public bool IsBound => Paper != null;
        public bool IsLeaving { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public float HoverAngleSpace => hoverWiggle ? wiggleAngle * 2 : 0;
        public event Action Returned;

        private void Awake()
        {
            GetParts();
            SetHover(false);
        }

        private void GetParts()
        {
            if (clickButton == null) clickButton = GetComponent<Button>();
            if (paperGroup == null) paperGroup = GetComponent<CanvasGroup>();
            if (hoverOutline == null) hoverOutline = GetComponent<Outline>();
        }

        private void OnEnable()
        {
            if (clickButton == null) clickButton = GetComponent<Button>();
            clickButton.onClick.AddListener(Click);
        }

        private void OnDisable()
        {
            if (clickButton != null) clickButton.onClick.RemoveListener(Click);
            StopWiggle();
            SetHover(false);
        }

        public void Bind(WorkPaper paper, Action<int> onClick)
        {
            GetParts();
            Paper = paper;
            clicked = onClick;
            IsLeaving = false;
            StopWiggle();
            Rect.localScale = Vector3.one;
            gameObject.SetActive(true);
            WorkUi.Show(paperGroup, true);
            clickButton.interactable = true;
        }

        public void PlayShow() => onShow?.Invoke();

        public void PlayComplete()
        {
            if (IsLeaving) return;
            IsLeaving = true;
            StopWiggle();
            clickButton.interactable = false;
            if (paperGroup != null)
            {
                paperGroup.interactable = false;
                paperGroup.blocksRaycasts = false;
            }
            SetHover(false);
            onComplete?.Invoke();
            if (!waitForRemoveAnimation) FinishRemove();
        }

        // Animation events keep this name, but now return the scene object instead of destroying it.
        public void FinishRemove()
        {
            if (!IsBound || !IsLeaving) return;
            ResetPaper();
            Returned?.Invoke();
        }

        public void ResetPaper()
        {
            GetParts();
            StopWiggle();
            Paper = null;
            clicked = null;
            IsLeaving = false;
            clickButton.interactable = false;
            WorkUi.Show(paperGroup, false);
            gameObject.SetActive(false);
        }

        public void SetPose(Vector2 position, float angle)
        {
            StopWiggle();
            restRotation = Quaternion.Euler(0, 0, angle);
            Rect.anchoredPosition = position;
            Rect.localRotation = restRotation;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (!IsBound || IsLeaving) return;
            SetHover(true);
            if (!hoverWiggle || wiggleAngle <= 0 || (wiggle != null && wiggle.IsPlaying())) return;
            if (wiggle == null)
                wiggle = Rect.DOPunchRotation(new Vector3(0, 0, wiggleAngle), wiggleSeconds, 5, 0.4f)
                    .SetAutoKill(false).SetRecyclable(true).SetUpdate(true).Pause();
            wiggle.Restart();
        }
        public void OnPointerExit(PointerEventData data) => SetHover(false);

        private void SetHover(bool hover)
        {
            if (hoverOutline != null) hoverOutline.enabled = hover && IsBound && !IsLeaving;
        }

        private void Click()
        {
            if (IsBound && !IsLeaving) clicked?.Invoke(PaperId);
        }

        private void StopWiggle()
        {
            wiggle?.Kill();
            wiggle = null;
            Rect.localRotation = restRotation;
        }
    }
}
