using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup), typeof(Button))]
    public sealed class WorkPaperItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Button clickButton;
        [SerializeField] private CanvasGroup paperGroup;
        [SerializeField] private Outline hoverOutline;
        [Tooltip("호버하지 않을 때의 아웃라인 거리. (0, 0)이면 숨깁니다. 호버/드래그 중에는 Outline 컴포넌트의 기존 거리를 사용합니다.")]
        [SerializeField] private Vector2 idleOutlineDistance = new Vector2(1, -1);
        [Tooltip("정렬 해금 후에만 표시할 카테고리 이름. 선택 사항입니다.")]
        [SerializeField] private TMP_Text sortedCategoryText;
        [Header("호버 꿈틀")]
        [SerializeField] private bool hoverWiggle = true;
        [SerializeField, Range(0, 10)] private float wiggleAngle = 4;
        [SerializeField, Range(0.1f, 0.5f)] private float wiggleSeconds = 0.22f;
        [Header("연출 — 선택 사항")]
        [SerializeField] private UnityEvent onShow;
        [SerializeField] private UnityEvent onComplete;
        [Tooltip("체크하면 완료 연출 끝에 FinishRemove()를 호출해야 종이가 재사용됩니다.")]
        [SerializeField] private bool waitForRemoveAnimation;
        [Header("즉시완료 — 들어 올린 뒤 오른쪽으로 휘리릭")]
        [SerializeField, Min(0.01f)] private float instantLiftSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float instantFlySeconds = 0.58f;
        [SerializeField, Min(0)] private float instantLiftPixels = 75f;
        [Tooltip("오른쪽으로 날아가는 동안 시계 방향으로 회전할 각도입니다.")]
        [SerializeField, Min(0)] private float instantSpinDegrees = 420f;
        private Tween instantTween;
        private RectTransform flightParent;
        private int flightSibling;
        private Vector2 flightAnchorMin, flightAnchorMax, flightPivot, flightPosition, flightSize;
        private Vector3 flightScale;
        private Color flightOutlineColor;
        private bool flying;
        private Action<int> clicked;
        private Tween wiggle;
        private Quaternion restRotation = Quaternion.identity;
        private RectTransform dragRoot;
        private RectTransform oldParent;
        private int oldSibling;
        private Vector2 oldAnchorMin, oldAnchorMax, oldPivot, oldPosition, oldSize;
        private Vector3 oldScale;
        private Vector3 dragOffset;
        private bool dragging;
        private bool dropSoundPlayed;
        private bool hoverAllowed = true;
        private Vector2? hoverOutlineDistance;
        private bool dragEnabled;
        private bool suppressClick;
        private int dragPaperId = -1;

        public WorkPaper Paper { get; private set; }
        public int PaperId => Paper?.Id ?? -1;
        public bool IsBound => Paper != null;
        public bool IsLeaving { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public float HoverAngleSpace => hoverWiggle ? wiggleAngle * 2 : 0;
        public event Action Returned;
        public event Action DragEnded;
        public event Action DragStarted;
        public event Action<WorkSfxCue> SoundRequested;
        public bool IsDragging => dragging;
        public int DraggedPaperId => dragging ? dragPaperId : -1;

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
            if (hoverOutline != null && !hoverOutlineDistance.HasValue)
                hoverOutlineDistance = hoverOutline.effectDistance;
        }

        private void OnEnable()
        {
            if (clickButton == null) clickButton = GetComponent<Button>();
            clickButton.onClick.AddListener(Click);
        }

        private void OnDisable()
        {
            StopInstantFlight();
            RestoreDrag();
            if (clickButton != null) clickButton.onClick.RemoveListener(Click);
            StopWiggle();
            SetHover(false);
        }

        public void Bind(WorkPaper paper, Action<int> onClick)
        {
            GetParts();
            StopInstantFlight();
            Paper = paper;
            clicked = onClick;
            IsLeaving = false;
            StopWiggle();
            Rect.localScale = Vector3.one;
            gameObject.SetActive(true);
            WorkUi.Show(paperGroup, true);
            clickButton.interactable = true;
            SetHover(false);
        }

        public void PlayShow() => onShow?.Invoke();

        public void PlayInstantComplete()
        {
            if (!IsBound || IsLeaving) return;
            GetParts();
            RestoreDrag();
            StopWiggle();
            IsLeaving = true;
            clickButton.interactable = false;
            paperGroup.interactable = paperGroup.blocksRaycasts = false;
            flightParent = Rect.parent as RectTransform;
            flightSibling = Rect.GetSiblingIndex();
            flightAnchorMin = Rect.anchorMin; flightAnchorMax = Rect.anchorMax;
            flightPivot = Rect.pivot; flightPosition = Rect.anchoredPosition;
            flightSize = Rect.sizeDelta; flightScale = Rect.localScale;
            flying = true;
            SetHover(false);
            if (hoverOutline != null) flightOutlineColor = hoverOutline.effectColor;

            // Canvas 최상단에서 날려 책상 마스크/정렬 갱신에 잘리지 않게 합니다.
            var canvas = GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas.transform as RectTransform : flightParent;
            if (root != null && root != Rect)
            {
                Vector3 worldPosition = Rect.position;
                Vector2 size = Rect.rect.size;
                Rect.SetParent(root, true);
                Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
                Rect.sizeDelta = size;
                Rect.position = worldPosition;
            }
            Rect.SetAsLastSibling();
            Vector2 start = Rect.anchoredPosition;
            Vector2 lifted = start + new Vector2(-18f, instantLiftPixels);
            Vector3 scale = Rect.localScale;
            float angle = Rect.localEulerAngles.z;
            float distance = root != null ? root.rect.width + Rect.rect.width : 1800f;
            float lift = Mathf.Max(0.01f, instantLiftSeconds);
            float fly = Mathf.Max(0.01f, instantFlySeconds);
            instantTween = DOTween.Sequence().SetUpdate(true)
                .Append(DOTween.To(() => Rect.anchoredPosition, value => Rect.anchoredPosition = value,
                    lifted, lift).SetEase(Ease.OutCubic))
                .Join(Rect.DOScale(scale * 1.18f, lift).SetEase(Ease.OutBack))
                .Join(Rect.DOLocalRotate(new Vector3(0, 0, angle - 16f), lift))
                .AppendCallback(() => SoundRequested?.Invoke(WorkSfxCue.PaperFly))
                .Append(DOTween.To(() => 0f, progress =>
                {
                    float travel = Mathf.SmoothStep(0f, 1f, progress);
                    Rect.anchoredPosition = Vector2.Lerp(lifted, lifted + new Vector2(distance, 100f), travel)
                        + Vector2.up * (Mathf.Sin(travel * Mathf.PI) * 55f);
                    Rect.localRotation = Quaternion.Euler(0, 0,
                        Mathf.Lerp(angle - 16f, angle - 16f - instantSpinDegrees, travel));
                    Rect.localScale = scale * Mathf.Lerp(1.18f, 0.85f, progress);
                    // Outline은 종이 뒤에 짙은 사본을 겹쳐 그리므로, 본체가 반투명해지기 전에 알파를 줄입니다.
                    if (hoverOutline != null)
                    {
                        Color outlineColor = flightOutlineColor;
                        outlineColor.a *= 1f - Mathf.InverseLerp(0.45f, 0.65f, progress);
                        hoverOutline.effectColor = outlineColor;
                    }
                    paperGroup.alpha = 1f - Mathf.InverseLerp(0.65f, 1f, progress);
                }, 1f, fly).SetEase(Ease.Linear))
                .OnComplete(FinishRemove);
        }

        private void StopInstantFlight()
        {
            instantTween?.Kill();
            instantTween = null;
            if (!flying) return;
            flying = false;
            if (hoverOutline != null) hoverOutline.effectColor = flightOutlineColor;
            if (flightParent != null)
            {
                Rect.SetParent(flightParent, false);
                Rect.SetSiblingIndex(flightSibling);
                Rect.anchorMin = flightAnchorMin; Rect.anchorMax = flightAnchorMax;
                Rect.pivot = flightPivot; Rect.sizeDelta = flightSize;
                Rect.anchoredPosition = flightPosition;
                Rect.localScale = flightScale;
                Rect.localRotation = restRotation;
            }
            flightParent = null;
        }

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
            // 접수된 종이는 먼저 숨겨 원래 자리로 돌아가는 모습이 보이지 않게 합니다.
            WorkUi.Show(paperGroup, false);
            StopInstantFlight();
            RestoreDrag();
            StopWiggle();
            Paper = null;
            clicked = null;
            IsLeaving = false;
            if (clickButton != null) clickButton.interactable = false;
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
            if (!hoverAllowed || !IsBound || IsLeaving || dragging) return;
            SoundRequested?.Invoke(WorkSfxCue.PaperHover);
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
            if (hoverOutline == null) return;
            bool highlighted = !IsLeaving && ((hover && hoverAllowed) || dragging);
            hoverOutline.effectDistance = highlighted ? hoverOutlineDistance.GetValueOrDefault() : idleOutlineDistance;
            hoverOutline.enabled = (highlighted || idleOutlineDistance != Vector2.zero) && IsBound && (!IsLeaving || flying);
        }

        public void SetHoverAllowed(bool allowed)
        {
            hoverAllowed = allowed;
            if (allowed) return;
            SetHover(false);
            if (!dragging) StopWiggle();
        }

        private void Click()
        {
            if (IsBound && !IsLeaving && !dragging && !suppressClick) clicked?.Invoke(PaperId);
        }

        public void SetDragRoot(RectTransform root) => dragRoot = root;
        public void CancelDrag() => RestoreDrag();
        public void SetDragEnabled(bool enabled)
        {
            dragEnabled = enabled;
            if (!enabled) RestoreDrag();
        }

        public void ShowCategory(bool visible)
        {
            if (sortedCategoryText == null) return;
            sortedCategoryText.text = Paper?.CategoryShortName ?? "";
            WorkUi.Show(sortedCategoryText.gameObject, visible && IsBound);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) suppressClick = false;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!dragEnabled || data.button != PointerEventData.InputButton.Left || !IsBound || IsLeaving || Paper.IsInFax) return;
            StopWiggle();
            SetHover(false);
            oldParent = Rect.parent as RectTransform;
            if (oldParent == null) return;
            oldSibling = Rect.GetSiblingIndex();
            oldAnchorMin = Rect.anchorMin; oldAnchorMax = Rect.anchorMax; oldPivot = Rect.pivot;
            oldPosition = Rect.anchoredPosition; oldSize = Rect.sizeDelta;
            oldScale = Rect.localScale;
            dragging = true;
            dropSoundPlayed = false;
            SetHover(true);
            suppressClick = true;
            dragPaperId = PaperId;
            if (dragRoot != null)
            {
                Vector2 size = Rect.rect.size;
                Vector3 position = Rect.position;
                Rect.SetParent(dragRoot, true);
                // Stretch 종이도 다른 크기의 부모로 옮길 때 외형이 변하지 않게 합니다.
                Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
                Rect.sizeDelta = size;
                Rect.position = position;
            }
            Rect.SetAsLastSibling();
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)Rect.parent,
                data.position, data.pressEventCamera, out var pointer)) dragOffset = Rect.position - pointer;
            else dragOffset = Vector3.zero;
            paperGroup.blocksRaycasts = false;
            SoundRequested?.Invoke(WorkSfxCue.PaperPick);
            DragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData data)
        {
            if (!dragging) return;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)Rect.parent,
                data.position, data.pressEventCamera, out var pointer)) Rect.position = pointer + dragOffset;
        }

        public void OnEndDrag(PointerEventData data)
        {
            NotifyDrop();
            RestoreDrag();
        }

        // The fax accepts its OnDrop before OnEndDrag; emit once before the model removes the paper.
        public void NotifyDrop()
        {
            if (!dragging || dropSoundPlayed) return;
            dropSoundPlayed = true;
            SoundRequested?.Invoke(WorkSfxCue.PaperDrop);
        }

        private void RestoreDrag()
        {
            if (!dragging) return;
            dragging = false;
            SetHover(false);
            dragPaperId = -1;
            if (oldParent != null)
            {
                Rect.SetParent(oldParent, false);
                Rect.SetSiblingIndex(oldSibling);
                Rect.anchorMin = oldAnchorMin; Rect.anchorMax = oldAnchorMax; Rect.pivot = oldPivot;
                Rect.sizeDelta = oldSize;
                Rect.anchoredPosition = oldPosition;
                Rect.localRotation = restRotation;
                Rect.localScale = oldScale;
            }
            if (paperGroup != null) paperGroup.blocksRaycasts = IsBound && !IsLeaving;
            oldParent = null;
            DragEnded?.Invoke();
        }

        private void StopWiggle()
        {
            wiggle?.Kill();
            wiggle = null;
            Rect.localRotation = restRotation;
        }
    }
}
