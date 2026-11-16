using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    public sealed class WorkDeskView : MonoBehaviour
    {
        [Header("배치 모드 — 둘 다 켜면 정리 모드 우선")]
        [SerializeField] private bool looseMode = true;
        [SerializeField] private bool sortedMode;
        [Header("컨테이너 / 씬에 배치한 종이 목록")]
        [SerializeField] private RectTransform looseRoot;
        [SerializeField] private RectTransform sortedRoot;
        [SerializeField] private List<WorkPaperItemView> paperViews = new List<WorkPaperItemView>();
        [Tooltip("한 번에 보여줄 수. 남는 객체는 재사용 대기하고, 처리한 자리에 다음 업무를 넣습니다.")]
        [SerializeField, Min(1)] private int maxPapers = 8;
        [Header("노이즈를 주는 일반 모드 배치")]
        [SerializeField] private Vector2 spreadPadding = new Vector2(24, 24);
        [SerializeField, Min(0)] private float paperGap = 12;
        [SerializeField, Min(0)] private float jitter = 8;
        [Tooltip("허용할 겹침 정도. 후보 위치 중 서로 덜 가리는 곳을 우선 선택합니다.")]
        [SerializeField, Range(0, 0.8f)] private float overlap = 0.2f;
        [SerializeField, Range(0, 45)] private float maxAngle = 8;
        [SerializeField] private int placementSeed = 1;
        private readonly List<WorkPaperItemView> pool = new List<WorkPaperItemView>();
        private readonly HashSet<int> boundIds = new HashSet<int>();
        private IReadOnlyList<WorkPaper> currentPapers = Array.Empty<WorkPaper>();
        private WorkDeskLayout layout;
        private bool poolReady;
        private bool layoutDirty = true;
        private bool refill;
        private bool syncing;
        private bool working;
        private int month;
        private RectTransform lastRoot;
        private Vector2 lastSize;

        public int PoolCount => pool.Count;
        public int MaxVisible { get; private set; }
        public event Action<int> PaperClicked;
        private RectTransform ActiveRoot => sortedMode && sortedRoot != null ? sortedRoot
            : looseMode ? looseRoot : null;

        public void ShowPapers(IReadOnlyList<WorkPaper> papers, int currentMonth = 0)
        {
            currentPapers = papers;
            if (month != currentMonth) { month = currentMonth; layoutDirty = true; }
            PreparePool();
            if (MaxVisible != Math.Min(pool.Count, Math.Min(maxPapers, papers.Count))) layoutDirty = true;
            if (NeedsLayout()) RebuildLayout();
            SyncPapers();
        }

        public void SetWorking(bool value)
        {
            working = value;
            UpdateVisibility();
        }

        public void SetModes(bool loose, bool sorted)
        {
            looseMode = loose;
            sortedMode = sorted;
            layoutDirty = true;
            UpdateVisibility();
        }

        public void ClearPapers()
        {
            PreparePool();
            currentPapers = Array.Empty<WorkPaper>();
            foreach (var view in pool) view.ResetPaper();
            refill = false;
        }

        private void LateUpdate()
        {
            if (!working) return;
            PreparePool();
            if (NeedsLayout()) { RebuildLayout(); refill = true; }
            if (refill) SyncPapers();
        }

        private bool NeedsLayout() => layoutDirty || ActiveRoot != lastRoot
            || (ActiveRoot != null && ActiveRoot.rect.size != lastSize);

        private void PreparePool()
        {
            if (poolReady) return;
            foreach (var old in pool)
            {
                if (old == null) continue;
                old.Returned -= RequestRefill;
                old.ResetPaper();
            }
            pool.Clear();
            var seen = new HashSet<WorkPaperItemView>();
            foreach (var view in paperViews)
            {
                if (view == null || !seen.Add(view)) continue;
                view.ResetPaper();
                view.Returned += RequestRefill;
                pool.Add(view);
            }
            poolReady = true;
            layoutDirty = true;
        }

        public void RebuildLayout()
        {
            PreparePool();
            lastRoot = ActiveRoot;
            lastSize = lastRoot != null ? lastRoot.rect.size : Vector2.zero;
            layoutDirty = false;
            MaxVisible = 0;
            layout = null;
            if (lastRoot == null) return;

            float width = 0, height = 0, hoverAngle = 0;
            foreach (var view in pool)
            {
                width = Mathf.Max(width, view.Rect.rect.width);
                height = Mathf.Max(height, view.Rect.rect.height);
                hoverAngle = Mathf.Max(hoverAngle, view.HoverAngleSpace);
            }
            int count = Math.Min(pool.Count, Math.Min(maxPapers, currentPapers.Count));
            if (lastRoot == sortedRoot) MaxVisible = count;
            else
            {
                layout = WorkDeskLayout.Create(lastSize.x - spreadPadding.x * 2, lastSize.y - spreadPadding.y * 2,
                    width, height, count, unchecked(placementSeed + month * 397), paperGap, jitter, maxAngle, hoverAngle, overlap);
                MaxVisible = layout.Spots.Count;
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var view = pool[i];
                if (i >= MaxVisible) { view.ResetPaper(); continue; }
                var rect = view.Rect;
                rect.SetParent(lastRoot, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                var spot = layout != null ? layout.Spots[i] : new PaperSpot(0, 0, 0);
                view.SetPose(new Vector2(spot.X, spot.Y), spot.Angle);
            }
            UpdateVisibility();
            refill = true;
        }

        private void SyncPapers()
        {
            if (syncing) { refill = true; return; }
            syncing = true;
            try
            {
                for (int i = 0; i < MaxVisible; i++)
                {
                    var view = pool[i];
                    if (view.IsBound && view.Paper.State != PaperState.Pending) view.PlayComplete();
                }
                boundIds.Clear();
                for (int i = 0; i < MaxVisible; i++) if (pool[i].IsBound) boundIds.Add(pool[i].PaperId);
                int next = 0;
                for (int i = 0; i < MaxVisible; i++)
                {
                    var view = pool[i];
                    if (view.IsBound) continue;
                    while (next < currentPapers.Count
                        && (currentPapers[next].State != PaperState.Pending || boundIds.Contains(currentPapers[next].Id))) next++;
                    if (next == currentPapers.Count) break;
                    var paper = currentPapers[next++];
                    boundIds.Add(paper.Id);
                    view.Bind(paper, ClickPaper);
                    view.PlayShow();
                }
                refill = false;
            }
            finally { syncing = false; }
        }

        private void UpdateVisibility()
        {
            WorkUi.Show(looseRoot != null ? looseRoot.gameObject : null, working && ActiveRoot == looseRoot);
            WorkUi.Show(sortedRoot != null ? sortedRoot.gameObject : null, working && ActiveRoot == sortedRoot);
        }

        private void RequestRefill() => refill = true;
        private void ClickPaper(int paperId) => PaperClicked?.Invoke(paperId);
        private void OnValidate() { poolReady = false; layoutDirty = true; refill = true; }
        private void OnDestroy()
        {
            foreach (var view in pool) if (view != null) view.Returned -= RequestRefill;
        }
    }
}
