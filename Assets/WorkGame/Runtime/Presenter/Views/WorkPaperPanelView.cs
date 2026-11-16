using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public sealed class WorkPaperPanelView : MonoBehaviour
    {
        [Header("공통 문서 정보")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private TMP_Text monthText;
        [SerializeField] private TMP_Text ruleText;
        [Header("내부 표현 / 선택 버튼 — A, B, C 순서")]
        [SerializeField] private WorkGraphView graphView;
        [SerializeField] private List<WorkChoiceView> choices = new List<WorkChoiceView>();
        [Header("터치 순간 덜컥")]
        [Tooltip("비워두면 이 문서 전체를 움직입니다.")]
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField, Range(0, 10)] private float bumpPixels = 3f;
        [SerializeField, Range(0.05f, 0.2f)] private float bumpSeconds = 0.1f;
        private bool layoutDirty;
        private Tween bumpTween;
        private Vector3 restPosition;

        public event Action<int> HoldStarted;
        public event Action HoldStopped;

        private void OnEnable()
        {
            if (shakeTarget == null) shakeTarget = (RectTransform)transform;
            restPosition = shakeTarget.localPosition;
            foreach (var choice in choices)
            {
                if (choice == null) continue;
                choice.HoldStarted += StartHold;
                choice.HoldStopped += StopHold;
            }
        }

        private void OnDisable()
        {
            ClearHold();
            foreach (var choice in choices)
            {
                if (choice == null) continue;
                choice.HoldStarted -= StartHold;
                choice.HoldStopped -= StopHold;
            }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ClearHold(); }
        private void OnApplicationPause(bool paused) { if (paused) ClearHold(); }

        public void Show(WorkPaper paper)
        {
            if (titleText != null) titleText.text = paper.CategoryName;
            if (numberText != null) numberText.text = "문서번호  " + WorkPaperText.GetNumber(paper);
            if (monthText != null) monthText.text = "시행  " + paper.Month + "달";
            if (ruleText != null) ruleText.gameObject.SetActive(false);
            if (graphView != null) graphView.Show(paper);
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice == null) continue;
                bool used = i < paper.Choices.Count;
                WorkUi.Show(choice.gameObject, used);
                if (used) choice.Show(i, paper.Choices[i].Name);
            }
            if (choices.Count < paper.Choices.Count)
                Debug.LogError("서류 선택지보다 연결한 선택 버튼이 적습니다.", this);
            ShowHold(-1, 0, true);
            layoutDirty = true;
        }

        public void ShowHold(int index, float progress, bool allowInput)
        {
            if (!allowInput) ResetBump();
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i] == null) continue;
                choices[i].SetInput(allowInput);
                choices[i].SetProgress(i == index ? progress : 0);
            }
        }

        public void ClearHold()
        {
            foreach (var choice in choices) if (choice != null) choice.CancelHold();
            ResetBump();
        }

        private void LateUpdate()
        {
            if (!layoutDirty) return;
            layoutDirty = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        private void StartHold(int index)
        {
            PlayBump();
            HoldStarted?.Invoke(index);
        }

        private void StopHold()
        {
            HoldStopped?.Invoke();
        }

        private void PlayBump()
        {
            if (!isActiveAndEnabled || bumpPixels <= 0) return;
            // Restart from the same position so rapid taps never add drift.
            ResetBump();
            bumpTween = shakeTarget.DOPunchPosition(new Vector3(0, -bumpPixels, 0),
                Mathf.Max(0.01f, bumpSeconds), 4, 0.2f).SetUpdate(true);
        }

        private void ResetBump()
        {
            bumpTween?.Kill();
            bumpTween = null;
            if (shakeTarget != null) shakeTarget.localPosition = restPosition;
        }
    }
}
