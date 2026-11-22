using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareMonthReportView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup, pictureGroup, lineGroup;
        [SerializeField] private CareDialogueClickArea clickArea;
        [SerializeField] private Image picture;
        [SerializeField] private Sprite defaultPicture;
        [SerializeField] private TMP_Text titleText, lineText, hintText;
        [SerializeField] private List<CareMonthStatView> stats = new List<CareMonthStatView>();
        [Header("마지막 주차 확인 후 기존 대각선 전환")]
        [SerializeField] private DiagonalTransitionView closeTransition;
        [Header("등장 순서 — 이미지 → 글자 → 스텟")]
        [SerializeField, Min(0.01f)] private float openSeconds = 0.2f;
        [SerializeField, Min(0.01f)] private float pictureSeconds = 0.3f;
        [SerializeField, Min(0.001f)] private float letterSeconds = 0.035f;
        [SerializeField, Min(0)] private float statGap = 0.12f;
        [SerializeField, Min(0.01f)] private float statSeconds = 0.35f;
        [Header("수업 아이콘 — 가볍게 좌우로 살랑임")]
        [SerializeField, Range(0, 5)] private float iconSwayDegrees = 1.5f;
        [SerializeField, Min(0.1f)] private float iconSwaySeconds = 1.6f;
        [SerializeField] private UnityEvent onOpen = new UnityEvent();
        [SerializeField] private UnityEvent onReady = new UnityEvent();
        public IReadOnlyList<CareMonthStatView> Stats => stats;
        public event Action Confirmed;
        private Sequence sequence;
        private Tween iconSway;
        private Quaternion pictureRotation;
        private bool rotationSaved;
        private bool visible, ready;
        private Coroutine closeRoutine;
        private UnityAction coveredHandler;
        private bool ownsTransition;
        public bool IsClosing { get; private set; }

        private void Awake()
        {
            if (defaultPicture == null && picture != null) defaultPicture = picture.sprite;
            Hide();
        }
        private void OnEnable() { if (clickArea != null) clickArea.Clicked += Click; }
        private void OnDisable()
        {
            CancelCloseTransition();
            if (clickArea != null) clickArea.Clicked -= Click;
            Hide();
        }
        public void Open(int month, string line, Sprite sprite, int week = 0, bool animateIcon = false)
        {
            bool firstOpen = !visible;
            sequence?.Kill();
            StopIconMotion();
            visible = true; ready = false;
            WorkUi.Show(panelGroup, true);
            if (firstOpen) SetAlpha(panelGroup, 0);
            SetAlpha(pictureGroup, 0); SetAlpha(lineGroup, 0);
            if (titleText != null) titleText.text = week > 0 ? month + "월 · " + week + "주차" : month + "월 · 달이의 한 달";
            int letters = 0;
            if (lineText != null)
            {
                lineText.text = line ?? "";
                lineText.maxVisibleCharacters = int.MaxValue;
                lineText.ForceMeshUpdate();
                letters = lineText.textInfo.characterCount;
                lineText.maxVisibleCharacters = 0;
            }
            if (picture != null)
            {
                picture.sprite = sprite != null ? sprite : defaultPicture;
                picture.enabled = picture.sprite != null;
                picture.preserveAspect = true;
                if (animateIcon && picture.enabled) StartIconMotion();
            }
            if (hintText != null) WorkUi.Show(hintText.gameObject, false);
            sequence = DOTween.Sequence().SetUpdate(true);
            if (firstOpen)
            {
                sequence.AppendCallback(() => onOpen.Invoke());
                sequence.Append(Fade(panelGroup, openSeconds));
            }
            sequence.Append(Fade(pictureGroup, pictureSeconds));
            sequence.AppendCallback(() => SetAlpha(lineGroup, 1));
            if (letters > 0)
            {
                int count = 0;
                sequence.Append(DOTween.To(() => count, value =>
                {
                    count = value;
                    if (lineText != null) lineText.maxVisibleCharacters = value;
                }, letters, letters * letterSeconds).SetEase(Ease.Linear));
            }
            float start = sequence.Duration();
            int index = 0;
            foreach (var row in stats)
                if (row != null) sequence.Insert(start + index++ * statGap, row.CreateAnimation(statSeconds));
            sequence.OnComplete(MarkReady);
        }
        private void Click()
        {
            if (!visible || IsClosing) return;
            if (!ready) { FinishAnimation(); return; }
            // 같은 클릭으로 연출 생략과 다음 단계 진행이 함께 일어나지 않습니다.
            ready = false;
            Confirmed?.Invoke();
        }
        private void FinishAnimation()
        {
            sequence?.Kill(); sequence = null;
            SetAlpha(panelGroup, 1); SetAlpha(pictureGroup, 1); SetAlpha(lineGroup, 1);
            if (lineText != null) lineText.maxVisibleCharacters = int.MaxValue;
            foreach (var row in stats) if (row != null) row.ShowFinal();
            MarkReady();
        }
        private void MarkReady()
        {
            sequence = null; ready = true;
            if (lineText != null) lineText.maxVisibleCharacters = int.MaxValue;
            if (hintText != null)
            {
                hintText.text = "화면을 눌러 계속";
                WorkUi.Show(hintText.gameObject, true);
            }
            onReady.Invoke();
        }
        public void CloseWithTransition(Action covered)
        {
            if (IsClosing) return;
            if (closeTransition == null || !closeTransition.isActiveAndEnabled)
            {
                covered?.Invoke();
                return;
            }
            IsClosing = true;
            closeRoutine = StartCoroutine(CloseRoutine(covered));
        }

        private IEnumerator CloseRoutine(Action covered)
        {
            yield return null;
            while (closeTransition != null && closeTransition.isActiveAndEnabled && closeTransition.IsPlaying)
                yield return null;
            bool committed = false;
            Action commit = () =>
            {
                if (committed) return;
                committed = true;
                covered?.Invoke();
            };
            if (closeTransition != null && closeTransition.isActiveAndEnabled)
            {
                coveredHandler = () => commit();
                closeTransition.Covered.AddListener(coveredHandler);
                ownsTransition = true;
                closeTransition.Play();
                yield return null;
                while (closeTransition != null && closeTransition.isActiveAndEnabled && closeTransition.IsPlaying)
                    yield return null;
            }
            RemoveCoveredHandler();
            ownsTransition = false;
            commit(); // Recover if the transition was interrupted before full cover.
            closeRoutine = null;
            IsClosing = false;
            if (visible) ready = true; // A pause or cancelled result commit can be confirmed again.
        }

        private void RemoveCoveredHandler()
        {
            if (closeTransition != null && coveredHandler != null)
                closeTransition.Covered.RemoveListener(coveredHandler);
            coveredHandler = null;
        }

        public void CancelCloseTransition()
        {
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = null;
            RemoveCoveredHandler();
            if (ownsTransition && closeTransition != null) closeTransition.HideImmediate();
            ownsTransition = false;
            IsClosing = false;
        }

        private void StartIconMotion()
        {
            if (!rotationSaved)
            {
                pictureRotation = picture.rectTransform.localRotation;
                rotationSaved = true;
            }
            var target = picture.rectTransform;
            target.localRotation = pictureRotation * Quaternion.Euler(0, 0, -iconSwayDegrees);
            iconSway = target.DOLocalRotateQuaternion(pictureRotation * Quaternion.Euler(0, 0, iconSwayDegrees), iconSwaySeconds)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void StopIconMotion()
        {
            iconSway?.Kill(); iconSway = null;
            if (picture != null && rotationSaved) picture.rectTransform.localRotation = pictureRotation;
        }

        public void Hide()
        {
            StopIconMotion();
            sequence?.Kill(); sequence = null;
            visible = false; ready = false;
            WorkUi.Show(panelGroup, false);
            foreach (var row in stats) if (row != null) row.ResetAnimation();
        }
        private static void SetAlpha(CanvasGroup group, float value) { if (group != null) group.alpha = value; }
        private static Tween Fade(CanvasGroup group, float seconds) => group != null
            ? DOTween.To(() => group.alpha, value => group.alpha = value, 1, seconds)
            : DOVirtual.DelayedCall(seconds, () => { });
    }
}
