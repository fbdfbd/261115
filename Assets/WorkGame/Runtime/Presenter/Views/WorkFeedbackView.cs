using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    public sealed class WorkFeedbackView : MonoBehaviour
    {
        [Header("씬에 배치한 말풍선 3개 — 같은 부모 / 아래쪽 앵커·피벗")]
        [SerializeField] private List<WorkFeedbackBubbleView> bubbles = new List<WorkFeedbackBubbleView>();
        [Header("색상")]
        [SerializeField] private Color successColor = Color.white;
        [SerializeField] private Color failureColor = new Color(1, 0.38f, 0.38f);
        [Header("시간")]
        [SerializeField, Min(0)] private float staySeconds = 0.5f;
        [SerializeField, Min(0.01f)] private float riseSeconds = 0.7f;
        [SerializeField, Min(0.01f)] private float appearSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float moveSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float fastExitSeconds = 0.12f;
        [Header("배치 — 부모 패널의 아래쪽 기준")]
        [SerializeField, Min(0)] private float bottomSpace = 12;
        [SerializeField, Min(0)] private float gap = 12;
        [SerializeField, Min(0)] private float risePixels = 45;
        private readonly List<WorkFeedbackBubbleView> pool = new List<WorkFeedbackBubbleView>();
        private readonly List<WorkFeedbackBubbleView> active = new List<WorkFeedbackBubbleView>();
        private readonly Queue<WorkFeedbackMessage> waiting = new Queue<WorkFeedbackMessage>();

        private void OnEnable()
        {
            pool.Clear();
            foreach (var bubble in bubbles)
            {
                if (bubble == null || pool.Contains(bubble) || pool.Count == 3) continue;
                bubble.Prepare();
                bubble.ResetView();
                bubble.Finished += OnFinished;
                pool.Add(bubble);
            }
        }

        private void OnDisable()
        {
            Clear();
            foreach (var bubble in pool) if (bubble != null) bubble.Finished -= OnFinished;
            pool.Clear();
        }

        public void Show(WorkFeedbackMessage message)
        {
            if (!isActiveAndEnabled || message == null || pool.Count == 0) return;
            waiting.Enqueue(message);
            ShowWaiting();
        }

        private void ShowWaiting()
        {
            while (waiting.Count > 0 && active.Count < pool.Count)
            {
                var bubble = pool.Find(x => !x.IsShowing);
                if (bubble == null) break;
                var message = waiting.Dequeue();
                bubble.transform.SetAsLastSibling();
                bubble.Show(message.Text, message.Result == FeedbackResult.Success ? successColor : failureColor,
                    bottomSpace, staySeconds, riseSeconds, risePixels, appearSeconds);
                active.Add(bubble);
                MoveBubbles();
            }
            if (waiting.Count > 0 && active.Count > 0) active[0].FinishSoon(fastExitSeconds);
        }

        private void OnFinished(WorkFeedbackBubbleView bubble)
        {
            active.Remove(bubble);
            MoveBubbles();
            ShowWaiting();
        }

        private void MoveBubbles()
        {
            float y = bottomSpace;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].MoveTo(y, moveSeconds);
                y += active[i].Height + gap;
            }
        }

        public void Clear()
        {
            waiting.Clear();
            foreach (var bubble in pool) if (bubble != null) bubble.ResetView();
            active.Clear();
        }
    }
}
