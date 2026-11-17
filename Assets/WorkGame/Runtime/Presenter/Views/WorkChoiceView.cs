using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorkGame
{
    [RequireComponent(typeof(Button))]
    public sealed class WorkChoiceView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image fill;
        [Header("정답 강조 — 비우면 버튼 배경 사용")]
        [SerializeField] private Image hintImage;
        [SerializeField] private Color hintColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField, Min(0.05f)] private float hintSeconds = 0.45f;
        private Tween hintTween;
        private Color hintRestColor;
        private int index;
        private bool holding;

        public event Action<int> HoldStarted;
        public event Action HoldStopped;

        public void Show(int choiceIndex, string name)
        {
            CancelHold();
            SetHint(false);
            index = choiceIndex;
            if (button == null) button = GetComponent<Button>();
            if (nameText != null) nameText.text = name;
            if (fill != null)
            {
                fill.raycastTarget = false;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            SetProgress(0);
        }

        public void SetProgress(float progress)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(progress);
        }

        public void SetInput(bool enabled)
        {
            if (button == null) button = GetComponent<Button>();
            button.interactable = enabled;
            if (!enabled) CancelHold();
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || holding) return;
            if (button == null) button = GetComponent<Button>();
            if (!button.IsInteractable()) return;
            holding = true;
            HoldStarted?.Invoke(index);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) CancelHold();
        }

        public void OnPointerExit(PointerEventData data) => CancelHold();
        private void OnDisable() { CancelHold(); SetHint(false); }

        public void SetHint(bool visible)
        {
            if (!visible)
            {
                if (hintTween == null) return;
                hintTween.Kill();
                hintTween = null;
                if (hintImage != null) hintImage.color = hintRestColor;
                return;
            }
            if (hintTween != null) return;
            if (hintImage == null) hintImage = GetComponent<Image>();
            if (hintImage == null) return;
            hintRestColor = hintImage.color;
            hintTween = DOTween.To(() => hintImage.color, color => hintImage.color = color, hintColor,
                Mathf.Max(0.05f, hintSeconds)).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        public void SetHintPaused(bool paused)
        {
            if (paused) hintTween?.Pause();
            else hintTween?.Play();
        }

        public void CancelHold()
        {
            SetProgress(0);
            if (!holding) return;
            holding = false;
            HoldStopped?.Invoke();
        }
    }
}
