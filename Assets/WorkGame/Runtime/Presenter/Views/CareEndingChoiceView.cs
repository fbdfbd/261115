using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class CareEndingChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform cardsRoot;
        [SerializeField] private List<CareEndingCardView> cards = new List<CareEndingCardView>();
        [SerializeField] private Vector2 cardSize = new Vector2(280, 380);
        [SerializeField, Min(0.01f)] private float openSeconds = 0.2f;
        [SerializeField, Min(0.01f)] private float selectSeconds = 0.15f;
        [Header("추가 연출 연결")]
        [SerializeField] private UnityEvent onOpen = new UnityEvent();
        [SerializeField] private UnityEvent<string> onSelect = new UnityEvent<string>();
        public IReadOnlyList<CareEndingCardView> Cards => cards;
        public event Action<string> Selected;
        private Tween tween;
        private bool visible, busy;
        private int count;

        private void Awake() => Hide();
        private void OnEnable() { foreach (var card in cards) if (card != null) card.Clicked += Select; }
        private void OnDisable()
        {
            foreach (var card in cards) if (card != null) card.Clicked -= Select;
            Hide();
        }
        public void Show(int count)
        {
            if (count > cards.Count) throw new InvalidOperationException("엔딩 선택 카드 5개를 View 목록에 연결하세요.");
            this.count = count;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) WorkUi.Show(cards[i].gameObject, i < count);
            ArrangeCards();
            if (visible) return;
            visible = true; busy = true;
            WorkUi.Show(panelGroup, true);
            if (panelGroup != null) panelGroup.alpha = 0;
            onOpen.Invoke();
            tween?.Kill();
            tween = Fade(1, openSeconds).OnComplete(() => { tween = null; busy = false; });
        }
        private void Select(string id)
        {
            if (!visible || busy || string.IsNullOrEmpty(id)) return;
            busy = true;
            onSelect.Invoke(id);
            tween?.Kill();
            tween = Fade(0, selectSeconds).OnComplete(() => { tween = null; Selected?.Invoke(id); });
        }
        public void CancelSelection()
        {
            tween?.Kill(); tween = null; busy = false;
            if (panelGroup != null) panelGroup.alpha = 1;
        }
        private void OnRectTransformDimensionsChange() => ArrangeCards();
        private void ArrangeCards()
        {
            if (cardsRoot == null || count <= 0 || cardsRoot.rect.width <= 0) return;
            var layout = cardsRoot.GetComponent<HorizontalLayoutGroup>();
            float gap = layout != null ? layout.spacing : 20;
            float width = Mathf.Max(1, Mathf.Min(cardSize.x, (cardsRoot.rect.width - gap * (count - 1)) / count));
            float height = Mathf.Min(cardSize.y, cardsRoot.rect.height);
            for (int i = 0; i < count; i++)
                if (cards[i] != null) ((RectTransform)cards[i].transform).sizeDelta = new Vector2(width, height);
        }
        private Tween Fade(float alpha, float seconds) => panelGroup != null
            ? DOTween.To(() => panelGroup.alpha, value => panelGroup.alpha = value, alpha, seconds).SetUpdate(true)
            : DOVirtual.DelayedCall(seconds, () => { });
        public void Hide()
        {
            tween?.Kill(); tween = null;
            visible = false; busy = false;
            WorkUi.Show(panelGroup, false);
        }
    }
}
