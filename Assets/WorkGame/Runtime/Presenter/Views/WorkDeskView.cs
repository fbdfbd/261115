using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkGame
{
    public sealed class WorkDeskView : MonoBehaviour
    {
        [Header("배치 모드 — 둘 다 켜면 정리 모드 우선")]
        [SerializeField] private bool looseMode = true;
        [SerializeField] private bool sortedMode;
        [Tooltip("플레이 중 정렬 해금·ON/OFF 상태를 무시하고 정렬 배치를 확인합니다.")]
        [SerializeField] private bool forceSorted;
        [Header("컨테이너 / 씬에 배치한 종이 목록")]
        [SerializeField] private RectTransform looseRoot;
        [SerializeField] private RectTransform sortedRoot;
        [Tooltip("선택 사항. Canvas 아래 별도 빈 RectTransform. LayoutGroup은 넣지 않습니다.")]
        [SerializeField] private RectTransform dragRoot;
        [Header("정렬 배치 — Sorted Root에는 LayoutGroup을 넣지 않습니다.")]
        [SerializeField, Min(1)] private int sortedColumns = 5;
        [SerializeField] private Vector2 sortedPadding = new Vector2(24, 24);
        [Tooltip("가로 / 세로 겹침 비율. 0이면 안 겹치고, 0.5면 종이 절반이 겹칩니다.")]
        [SerializeField] private Vector2 sortedOverlap = new Vector2(0.55f, 0.4f);
        [Tooltip("겹침을 적용한 뒤 더할 간격입니다. 영역이 작으면 자동으로 간격을 줄입니다.")]
        [SerializeField] private Vector2 sortedSpacing = new Vector2(12, 12);
        [SerializeField] private List<WorkPaperItemView> paperViews = new List<WorkPaperItemView>();
        [Tooltip("동시에 표시할 최대 서류 수. 연결된 종이가 부족하면 첫 번째 종이를 복제해 재사용합니다.")]
        [SerializeField, Min(1)] private int maxPapers = 90;
        [Header("노이즈를 주는 일반 모드 배치")]
        [SerializeField] private Vector2 spreadPadding = new Vector2(24, 24);
        [SerializeField, Min(0)] private float paperGap = 12;
        [SerializeField, Min(0)] private float jitter = 8;
        [Tooltip("허용할 겹침 정도. 후보 위치 중 서로 덜 가리는 곳을 우선 선택합니다.")]
        [SerializeField, Range(0, 0.8f)] private float overlap = 0.2f;
        [SerializeField, Range(0, 45)] private float maxAngle = 8;
        [SerializeField] private int placementSeed = 1;
        private readonly List<WorkPaperItemView> pool = new List<WorkPaperItemView>();
        private readonly List<WorkPaperItemView> generatedViews = new List<WorkPaperItemView>();
        private readonly HashSet<int> boundIds = new HashSet<int>();
        private IReadOnlyList<WorkPaper> currentPapers = Array.Empty<WorkPaper>();
        private WorkDeskLayout layout;
        private bool poolReady;
        private bool layoutDirty = true;
        private bool refill;
        private bool syncing;
        private bool working;
        private bool faxDragEnabled;
        private bool sortedReady;
        private int month;
        private RectTransform lastRoot;
        private Vector2 lastSize;

        public int PoolCount => pool.Count;
        public int MaxVisible { get; private set; }
        public event Action<int> PaperClicked;
        public WorkPaperItemView FindPaper(int paperId)
        {
            foreach (var view in pool)
                if (view != null && view.PaperId == paperId && !view.IsLeaving) return view;
            return null;
        }
        private bool IsSorted => forceSorted || sortedMode;
        private RectTransform ActiveRoot => IsSorted && sortedRoot != null ? sortedRoot
            : looseMode ? looseRoot : null;

        public void ShowPapers(IReadOnlyList<WorkPaper> papers, int currentMonth = 0)
        {
            currentPapers = IsSorted ? papers.OrderBy(x => x.CategoryId).ThenBy(x => x.Id).ToArray() : papers;
            if (month != currentMonth) { month = currentMonth; layoutDirty = true; sortedReady = false; }
            PreparePool();
            if (MaxVisible != Math.Min(pool.Count, Math.Min(maxPapers, papers.Count))) layoutDirty = true;
            if (NeedsLayout()) RebuildLayout();
            SyncPapers();
        }

        public void SetWorking(bool value)
        {
            if (working && !value)
                foreach (var paper in pool) paper.CancelDrag();
            working = value;
            UpdateVisibility();
        }

        public void SetModes(bool loose, bool sorted)
        {
            looseMode = loose;
            sortedMode = sorted;
            sortedReady = false;
            layoutDirty = true;
            UpdateVisibility();
        }

        public void SetSorted(bool enabled)
        {
            if (sortedMode == enabled) return;
            SetModes(true, enabled);
        }

        public void SetFaxDragEnabled(bool enabled)
        {
            if (faxDragEnabled == enabled) return;
            faxDragEnabled = enabled;
            foreach (var paper in pool) paper.SetDragEnabled(enabled);
        }

        public void ClearPapers()
        {
            PreparePool();
            currentPapers = Array.Empty<WorkPaper>();
            sortedReady = false;
            foreach (var view in pool)
                if (view != null) view.ResetPaper();
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
            if (poolReady) { GrowPool(); return; }
            foreach (var old in pool)
            {
                if (old == null) continue;
                old.Returned -= RequestRefill;
                old.DragStarted -= OnDragStarted;
                old.DragEnded -= OnDragEnded;
                old.ResetPaper();
            }
            pool.Clear();
            var seen = new HashSet<WorkPaperItemView>();
            foreach (var view in paperViews.Concat(generatedViews))
            {
                if (view == null || !seen.Add(view)) continue;
                RegisterView(view);
            }
            poolReady = true;
            layoutDirty = true;
            GrowPool();
        }

        private void RegisterView(WorkPaperItemView view)
        {
            view.ResetPaper();
            view.SetDragRoot(dragRoot);
            view.SetDragEnabled(faxDragEnabled);
            view.Returned += RequestRefill;
            view.SetHoverAllowed(true);
            view.DragStarted += OnDragStarted;
            view.DragEnded += OnDragEnded;
            pool.Add(view);
        }

        private void GrowPool()
        {
            int required = Math.Min(maxPapers, currentPapers.Count);
            if (pool.Count == 0 || pool.Count >= required) return;
            var template = pool[0];
            while (pool.Count < required)
            {
                var view = Instantiate(template, template.transform.parent, false);
                view.name = template.name + " (Pooled " + pool.Count + ")";
                generatedViews.Add(view);
                RegisterView(view);
            }
            layoutDirty = true;
        }

        public void RebuildLayout()
        {
            PreparePool();
            lastRoot = ActiveRoot;
            lastSize = lastRoot != null ? lastRoot.rect.size : Vector2.zero;
            layoutDirty = false;
            sortedReady = false;
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
            Vector2 sortedStep = GetSortedStep(width, height, count);
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
                view.CancelDrag();
                if (i >= MaxVisible) { view.ResetPaper(); continue; }
                var rect = view.Rect;
                rect.SetParent(lastRoot, false);
                bool sorted = lastRoot == sortedRoot;
                rect.anchorMin = rect.anchorMax = rect.pivot = sorted ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                var spot = layout != null ? layout.Spots[i] : new PaperSpot(0, 0, 0);
                Vector2 position = sorted ? GetSortedPosition(i, sortedStep) : new Vector2(spot.X, spot.Y);
                view.SetPose(position, spot.Angle);
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
                // 팩스 접수는 즉시 회수합니다. 다른 종이를 끄는 중이면 나머지 갱신은 놓은 뒤 처리합니다.
                foreach (var view in pool)
                    if (view.IsBound && view.Paper.IsInFax) view.ResetPaper();
                foreach (var view in pool)
                {
                    if (!view.IsDragging) continue;
                    refill = false; // DragEnded에서 한 번만 다시 요청합니다.
                    return;
                }
                for (int i = 0; i < MaxVisible; i++)
                {
                    var view = pool[i];
                    if (view.IsBound && view.Paper.State != PaperState.Pending) view.PlayComplete();
                }
                boundIds.Clear();
                for (int i = 0; i < MaxVisible; i++) if (pool[i].IsBound) boundIds.Add(pool[i].PaperId);
                int next = 0;
                bool addedPapers = false;
                for (int i = 0; i < MaxVisible; i++)
                {
                    var view = pool[i];
                    if (view.IsBound) continue;
                    while (next < currentPapers.Count
                        && (currentPapers[next].State != PaperState.Pending || currentPapers[next].IsInFax || boundIds.Contains(currentPapers[next].Id))) next++;
                    if (next == currentPapers.Count) break;
                    var paper = currentPapers[next++];
                    boundIds.Add(paper.Id);
                    view.Bind(paper, ClickPaper);
                    view.PlayShow();
                    addedPapers = true;
                }
                if (IsSorted && ActiveRoot == sortedRoot && (!sortedReady || addedPapers))
                {
                    ArrangeSortedPapers();
                    sortedReady = true;
                }
                foreach (var view in pool) view.ShowCategory(IsSorted && ActiveRoot == sortedRoot);
                refill = false;
            }
            finally { syncing = false; }
        }

        private void ArrangeSortedPapers()
        {
            // 새 서류를 보충한 경우에도 카테고리 순서로 배치합니다.
            var visible = pool.Take(MaxVisible).Where(x => x.IsBound && !x.IsLeaving)
                .OrderBy(x => x.Paper.CategoryId).ThenBy(x => x.PaperId).ToArray();
            float width = visible.Length == 0 ? 0 : visible.Max(x => x.Rect.rect.width);
            float height = visible.Length == 0 ? 0 : visible.Max(x => x.Rect.rect.height);
            Vector2 step = GetSortedStep(width, height, visible.Length);
            for (int i = 0; i < visible.Length; i++)
            {
                visible[i].SetPose(GetSortedPosition(i, step), 0);
                // 오른쪽 종이, 다음 줄의 종이가 위에 그려져 노출된 부분을 클릭할 수 있습니다.
                visible[i].Rect.SetAsLastSibling();
            }
        }

        private Vector2 GetSortedPosition(int index, Vector2 step) => new Vector2(
            sortedPadding.x + index % Math.Max(1, sortedColumns) * step.x,
            -sortedPadding.y - index / Math.Max(1, sortedColumns) * step.y);

        private Vector2 GetSortedStep(float width, float height, int count)
        {
            if (sortedRoot == null || count == 0) return Vector2.zero;
            // 남은 서류 수와 관계없이 설정한 열 수의 간격을 유지합니다.
            int columns = Math.Max(1, sortedColumns);
            int rows = (count + Math.Max(1, sortedColumns) - 1) / Math.Max(1, sortedColumns);
            float stepX = width * (1 - Mathf.Clamp(sortedOverlap.x, 0, 0.95f)) + sortedSpacing.x;
            float stepY = height * (1 - Mathf.Clamp(sortedOverlap.y, 0, 0.95f)) + sortedSpacing.y;
            return new Vector2(
                FitStep(stepX, sortedRoot.rect.width - sortedPadding.x * 2 - width, columns),
                FitStep(stepY, sortedRoot.rect.height - sortedPadding.y * 2 - height, rows));
        }

        private static float FitStep(float step, float room, int count) => count <= 1 ? 0
            : Mathf.Max(1, Mathf.Min(step, room / (count - 1)));

        private void UpdateVisibility()
        {
            WorkUi.Show(looseRoot != null ? looseRoot.gameObject : null, working && ActiveRoot == looseRoot);
            WorkUi.Show(sortedRoot != null ? sortedRoot.gameObject : null, working && ActiveRoot == sortedRoot);
        }

        private void RequestRefill() => refill = true;
        private void OnDragStarted()
        {
            foreach (var view in pool) view.SetHoverAllowed(false);
        }

        private void OnDragEnded()
        {
            bool canHover = !pool.Any(view => view.IsDragging);
            foreach (var view in pool) view.SetHoverAllowed(canHover);
            RequestRefill();
        }
        private void ClickPaper(int paperId) => PaperClicked?.Invoke(paperId);
        private void OnValidate() { poolReady = false; layoutDirty = true; refill = true; sortedReady = false; }
        private void OnDestroy()
        {
            foreach (var view in pool)
            {
                if (view == null) continue;
                view.Returned -= RequestRefill;
                view.DragStarted -= OnDragStarted;
                view.DragEnded -= OnDragEnded;
            }
            pool.Clear();
            boundIds.Clear();
            working = false;
        }
    }
}
