using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkMonthReportView : MonoBehaviour
    {
        [Header("표시 그룹")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform document;
        [SerializeField] private CanvasGroup pointsGroup;
        [SerializeField] private CanvasGroup salaryGroup;
        [SerializeField] private Button confirmButton;
        [Header("문서 텍스트")]
        [SerializeField] private TMP_Text monthText;
        [SerializeField] private TMP_Text paperText;
        [SerializeField] private TMP_Text mistakeText;
        [SerializeField] private TMP_Text extraLabel;
        [SerializeField] private TMP_Text extraText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text salaryText;
        [SerializeField] private GameObject bonusRow;
        [SerializeField] private TMP_Text bonusLabel;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private TMP_Text moneyTotalText;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text confirmText;
        [Header("등장 순서 — 비어 있으면 연결된 텍스트의 부모 행 사용")]
        [SerializeField] private List<CanvasGroup> pointRows = new List<CanvasGroup>();
        [SerializeField] private List<CanvasGroup> salaryRows = new List<CanvasGroup>();
        [Header("한 줄씩 등장")]
        [SerializeField, Range(0.1f, 1)] private float rowStartScale = 0.9f;
        [SerializeField, Min(0.01f)] private float rowSeconds = 0.22f;
        [SerializeField, Min(0)] private float rowGap = 0.1f;
        [SerializeField] private Ease rowEase = Ease.OutBack;
        [Header("연출 시간 — 실제 시간 기준")]
        [SerializeField, Min(0)] private float openSeconds = 0.25f;
        [SerializeField, Min(0)] private float pointsDelay = 0.15f;
        [SerializeField, Min(0)] private float pointsSeconds = 0.25f;
        [SerializeField, Min(0)] private float salaryDelay = 0.2f;
        [SerializeField, Min(0)] private float salarySeconds = 0.25f;
        [SerializeField, Min(0)] private float closeSeconds = 0.2f;
        [Tooltip("화면 위쪽에서 내려올 때 문서 아래에 확보할 여백입니다.")]
        [SerializeField, Min(0)] private float riseDistance = 24;
        [Header("착지 후 작은 출렁임")]
        [SerializeField, Min(0.01f)] private float settleSeconds = 0.24f;
        [SerializeField, Min(0)] private float settleDistance = 12;
        [Header("추가 연출 연결 — 각 단계 시작 시 호출")]
        [SerializeField] private UnityEvent onOpen = new UnityEvent();
        [SerializeField] private UnityEvent onPoints = new UnityEvent();
        [SerializeField] private UnityEvent onSalary = new UnityEvent();
        [SerializeField] private UnityEvent onReady = new UnityEvent();
        [SerializeField] private UnityEvent onClose = new UnityEvent();

        public event Action Confirmed;
        public event Action<WorkSfxCue> SoundRequested;
        public bool IsOpen { get; private set; }
        private Sequence sequence;
        private Vector2 home;
        private bool hasHome;
        private readonly List<ReportRow> pointAnimation = new List<ReportRow>();
        private readonly List<ReportRow> salaryAnimation = new List<ReportRow>();
        private bool rowsSaved;

        private sealed class ReportRow
        {
            public CanvasGroup Group;
            public Transform Transform;
            public Vector3 Scale;
            public float Alpha;
        }

        private void Awake() => Hide();
        private void OnEnable()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        }
        private void OnDisable()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            Hide();
        }

        public void Open(MonthResult result)
        {
            if (result == null || IsOpen) return;
            SaveHome();
            SaveRows();
            ShowNumbers(result);
            sequence?.Kill();
            PrepareRows(pointAnimation);
            PrepareRows(salaryAnimation);
            IsOpen = true;
            WorkUi.Show(panelGroup, true);
            SetAlpha(panelGroup, 0);
            SetAlpha(pointsGroup, 0);
            SetAlpha(salaryGroup, 0);
            if (confirmButton != null) confirmButton.interactable = false;
            if (document != null)
            {
                float distance = riseDistance;
                if (document.parent is RectTransform parent)
                    distance += Mathf.Max(0, parent.rect.yMax - document.localPosition.y
                        + document.rect.height * document.pivot.y * Mathf.Abs(document.localScale.y));
                document.anchoredPosition = home + Vector2.up * distance;
            }

            sequence = DOTween.Sequence().SetUpdate(true);
            sequence.AppendCallback(() => onOpen.Invoke());
            sequence.Append(Fade(panelGroup, 1, openSeconds));
            if (document != null)
                sequence.Join(DOTween.To(() => document.anchoredPosition,
                    value => document.anchoredPosition = value, home, openSeconds).SetEase(Ease.OutCubic));
            // 착지한 다음에만 작게 흔들고, 기존 행 등장 순서는 그대로 이어갑니다.
            if (document != null && settleDistance > 0)
                sequence.Append(document.DOPunchPosition(new Vector3(0, -settleDistance, 0),
                    settleSeconds, 2, 0.4f));
            sequence.AppendInterval(pointsDelay);
            sequence.AppendCallback(() => onPoints.Invoke());
            sequence.Append(Fade(pointsGroup, 1, pointsSeconds));
            AppendRows(pointAnimation);
            sequence.AppendInterval(salaryDelay);
            sequence.AppendCallback(() => onSalary.Invoke());
            sequence.Append(Fade(salaryGroup, 1, salarySeconds));
            AppendRows(salaryAnimation);
            sequence.AppendCallback(() =>
            {
                if (confirmButton != null) confirmButton.interactable = true;
                onReady.Invoke();
                RequestSound(WorkSfxCue.ReportComplete);
            });
        }

        public void Hide()
        {
            sequence?.Kill();
            sequence = null;
            IsOpen = false;
            WorkUi.Show(panelGroup, false);
            RestoreRows(pointAnimation);
            RestoreRows(salaryAnimation);
            if (hasHome && document != null) document.anchoredPosition = home;
        }

        private void Confirm()
        {
            if (!IsOpen || (confirmButton != null && !confirmButton.interactable)) return;
            if (confirmButton != null) confirmButton.interactable = false;
            sequence?.Kill();
            sequence = DOTween.Sequence().SetUpdate(true);
            sequence.AppendCallback(() => onClose.Invoke());
            sequence.Append(Fade(panelGroup, 0, closeSeconds));
            sequence.AppendCallback(() => { Hide(); Confirmed?.Invoke(); });
        }

        private void ShowNumbers(MonthResult result)
        {
            SetText(monthText, $"2050년 {result.Month}월 · 성과 정산서");
            SetText(paperText, $"+{result.SkillPointsEarned:N0}");
            SetText(mistakeText, $"-{result.SkillPointPenalty:N0}");
            SetText(extraLabel, $"추가 포인트 ({result.ExtraPointPercent:0.#}%)");
            SetText(extraText, $"+{result.EarlyLeaveBonus:N0}");
            SetText(totalText, $"{result.SkillPointTotal:N0}");
            SetText(salaryText, $"{result.Salary:N0}");
            if (bonusRow != null) bonusRow.SetActive(result.Review != null);
            if (result.Review != null)
            {
                SetText(bonusLabel, $"보너스 · {result.Review.Grade} ({result.Review.BonusPercent}%)");
                SetText(bonusText, $"+{result.Review.Bonus:N0}");
            }
            SetText(moneyTotalText, $"{result.MoneyTotal:N0}");
            SetText(cashText, $"{result.MoneyBalance:N0}");
            SetText(confirmText, result.Month == 12 ? "확인" : "다음");
        }

        private void SaveHome()
        {
            if (hasHome || document == null) return;
            home = document.anchoredPosition;
            hasHome = true;
        }

        private void SaveRows()
        {
            if (rowsSaved) return;
            SaveRows(pointRows, pointAnimation, paperText, mistakeText, extraText, totalText);
            SaveRows(salaryRows, salaryAnimation, salaryText, bonusText, moneyTotalText);
            SaveTextRow(cashText, salaryAnimation);
            rowsSaved = true;
        }

        private static void SaveRows(List<CanvasGroup> assigned, List<ReportRow> saved, params TMP_Text[] fallback)
        {
            if (assigned.Count > 0)
            {
                foreach (var group in assigned) SaveRow(group, saved);
                return;
            }
            foreach (var text in fallback)
                SaveTextRow(text, saved);
        }

        private static void SaveTextRow(TMP_Text text, List<ReportRow> saved)
        {
            if (text == null || text.transform.parent == null) return;
            var row = text.transform.parent.gameObject;
            var group = row.GetComponent<CanvasGroup>();
            if (group == null) group = row.AddComponent<CanvasGroup>();
            SaveRow(group, saved);
        }

        private static void SaveRow(CanvasGroup group, List<ReportRow> saved)
        {
            if (group == null || saved.Exists(row => row.Group == group)) return;
            saved.Add(new ReportRow
            {
                Group = group, Transform = group.transform,
                Scale = group.transform.localScale, Alpha = group.alpha
            });
        }

        private void PrepareRows(List<ReportRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.Group == null) continue;
                row.Group.alpha = 0;
                row.Transform.localScale = row.Scale * rowStartScale;
            }
        }

        private void AppendRows(List<ReportRow> rows)
        {
            float start = sequence.Duration();
            int index = 0;
            foreach (var row in rows)
            {
                // 보너스가 없는 달에는 해당 행과 대기 간격도 생략합니다.
                if (row.Group == null || !row.Group.gameObject.activeInHierarchy) continue;
                float at = start + index++ * rowGap;
                sequence.InsertCallback(at, () => RequestSound(WorkSfxCue.ReportRow));
                sequence.Insert(at, Fade(row.Group, row.Alpha, rowSeconds).SetEase(Ease.OutQuad));
                sequence.Insert(at, DOTween.To(() => row.Transform.localScale,
                    value => row.Transform.localScale = value, row.Scale, rowSeconds).SetEase(rowEase));
            }
        }

        private void RequestSound(WorkSfxCue cue)
        {
            if (this != null && IsOpen && isActiveAndEnabled) SoundRequested?.Invoke(cue);
        }

        private static void RestoreRows(List<ReportRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.Group == null) continue;
                row.Group.alpha = row.Alpha;
                row.Transform.localScale = row.Scale;
            }
        }

        private static Tween Fade(CanvasGroup group, float alpha, float seconds) =>
            group != null
                ? DOTween.To(() => group.alpha, value => group.alpha = value, alpha, seconds)
                : DOVirtual.DelayedCall(seconds, () => { });
        private static void SetAlpha(CanvasGroup group, float alpha) { if (group != null) group.alpha = alpha; }
        private static void SetText(TMP_Text text, string value) { if (text != null) text.text = value; }
    }
}
