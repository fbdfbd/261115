using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public class WorkHudView : MonoBehaviour
    {
        [Header("기능 표시")]
        [SerializeField] private bool showMonth = true;
        [SerializeField] private bool showCategories = true;
        [SerializeField] private bool showProgressText = true;
        [SerializeField] private bool showProgressSlider = true;
        [SerializeField] private bool showSkillPoints = true;
        [SerializeField] private bool showTime = true;
        [SerializeField] private bool showTimeFill = true;
        [Header("월 / 카테고리")]
        [SerializeField] private TMP_Text monthText;
        [SerializeField] private List<WorkCategoryView> categorySlots = new List<WorkCategoryView>();
        [Header("처리 진행도")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;
        [Header("숙련 포인트 / 남은 시간")]
        [SerializeField] private TMP_Text skillPointsText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image timeFill;
        [Header("숙련 포인트 카운트업")]
        [SerializeField, Min(0.01f)] private float pointCountSeconds = 0.35f;
        [SerializeField, Min(0)] private float pointWaveHeight = 4;
        [Header("숙련 포인트 패널 광택 / 별 회전")]
        [SerializeField] private RectTransform pointPanel;
        [Tooltip("패널의 직접 자식. Anchor와 Pivot은 중앙, Layout Group에서는 Ignore Layout 설정.")]
        [SerializeField] private RectTransform pointShine;
        [SerializeField] private RectTransform pointStar;
        [SerializeField, Min(0)] private float pointStarAngle = 12f;
        [Header("숙련 포인트 판정 색상 — 끝나면 원래 TMP 색상으로 복귀")]
        [SerializeField] private Color pointSuccessColor = new Color(0.2f, 0.8f, 0.35f);
        [SerializeField] private Color pointFailureColor = new Color(1f, 0.25f, 0.25f);
        private int categoryCount;
        private bool categoryLayoutDirty = true;
        private readonly HashSet<RectTransform> categoryParents = new HashSet<RectTransform>();
        private int lastDone = -1;
        private int lastTotal = -1;
        private int lastSkillPoints = -1;
        private int lastSeconds = -1;
        private int shownPoints;
        private Tween pointTween;
        private Color pointNormalColor;
        private Color pointStartColor;
        private bool pointColorSaved;
        private TMP_MeshInfo[] pointMesh;
        private bool pointMeshReady;
        private Vector2 pointShineTravel;
        private Quaternion pointStarRotation;
        private bool pointTransformsActive;
        private bool pointBoosted;

        private void OnEnable()
        {
            categoryLayoutDirty = true;
            lastSkillPoints = lastSeconds = -1;
            SavePointColor();
            pointMeshReady = false;
            if (pointShine != null) pointShine.gameObject.SetActive(false);
            if (skillPointsText != null)
            {
                skillPointsText.OnPreRenderText += CachePointMesh;
                skillPointsText.havePropertiesChanged = true;
                skillPointsText.SetVerticesDirty();
            }
            if (timeFill != null)
            {
                timeFill.type = Image.Type.Filled;
                timeFill.fillMethod = Image.FillMethod.Radial360;
            }
        }

        private void OnDisable()
        {
            pointTween?.Kill();
            pointTween = null;
            if (skillPointsText != null) skillPointsText.OnPreRenderText -= CachePointMesh;
            ResetPointEffect();
        }

        private void LateUpdate()
        {
            if (!categoryLayoutDirty) return;
            categoryLayoutDirty = false;
            categoryParents.Clear();
            // Fit each label/card before the parent reads its width to arrange the row.
            foreach (var slot in categorySlots)
            {
                if (slot == null) continue;
                if (slot.gameObject.activeInHierarchy) slot.RefreshLayout();
                if (slot.transform.parent is RectTransform parent) categoryParents.Add(parent);
            }
            foreach (var parent in categoryParents) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        public void ShowMonth(int month, IEnumerable<WorkCategorySO> monthCategories)
        {
            if (monthText != null) monthText.text = month + "월";
            WorkUi.Show(monthText != null ? monthText.gameObject : null, showMonth);
            categoryCount = 0;
            foreach (var category in monthCategories)
            {
                while (categoryCount < categorySlots.Count && categorySlots[categoryCount] == null) categoryCount++;
                if (categoryCount >= categorySlots.Count)
                {
                    Debug.LogWarning("HUD 카테고리 표시 칸이 부족합니다. Category Slots에 배치한 객체를 더 연결하세요.", this);
                    break;
                }
                categorySlots[categoryCount].Show(category);
                categoryCount++;
            }
            ShowCategorySlots();
            categoryLayoutDirty = true;
        }

        public void ShowProgress(int done, int total)
        {
            WorkUi.Show(monthText != null ? monthText.gameObject : null, showMonth);
            ShowCategorySlots();
            WorkUi.Show(progressText != null ? progressText.gameObject : null, showProgressText);
            WorkUi.Show(progressSlider != null ? progressSlider.gameObject : null, showProgressSlider);
            if (lastDone == done && lastTotal == total) return;
            lastDone = done;
            lastTotal = total;
            if (progressText != null) progressText.text = done + "/" + total;
            SetProgress(total == 0 ? 0 : (float)done / total);
        }

        public void ShowSkillPoints(int points)
        {
            WorkUi.Show(skillPointsText != null ? skillPointsText.gameObject : null, showSkillPoints);
            if (lastSkillPoints == points) return;
            ChangePointText(points, lastSkillPoints >= 0);
        }

        public void ShowSkillPointResult(int points, bool isRight, bool isPointBoosted = false)
        {
            WorkUi.Show(skillPointsText != null ? skillPointsText.gameObject : null, showSkillPoints);
            ChangePointText(points, true, isRight ? pointSuccessColor : pointFailureColor, isRight,
                isRight && isPointBoosted);
        }

        private void ChangePointText(int points, bool animate, Color? flashColor = null, bool playShine = false,
            bool boosted = false)
        {
            SavePointColor();
            lastSkillPoints = points;
            pointTween?.Kill();
            pointTween = null;
            ResetPointEffect();
            pointBoosted = boosted;
            if (!animate || !showSkillPoints || !isActiveAndEnabled || skillPointsText == null) SetPointText(points);
            else
            {
                if (playShine && pointPanel != null && pointShine != null)
                {
                    // Clear even a rotated strip fully past the panel's edges.
                    pointShineTravel = new Vector2((pointPanel.rect.width + pointShine.rect.size.magnitude) * 0.5f,
                        pointPanel.rect.height * 0.5f);
                    pointShine.anchoredPosition = -pointShineTravel;
                    pointShine.gameObject.SetActive(true);
                }
                if (pointStar != null) pointStarRotation = pointStar.localRotation;
                pointTransformsActive = true;
                pointStartColor = flashColor ?? pointNormalColor;
                float duration = Mathf.Max(0.01f, pointCountSeconds) * (pointBoosted ? 1.6f : 1f);
                pointTween = DOTween.To(() => shownPoints, SetPointText, points, duration)
                    .SetEase(Ease.OutCubic).SetUpdate(true)
                    .OnUpdate(UpdatePointEffect)
                    .OnComplete(() => { pointTween = null; ResetPointEffect(); });
            }
        }

        private void SetPointText(int points)
        {
            shownPoints = points;
            if (skillPointsText == null) return;
            string next = points.ToString();
            if (skillPointsText.text == next) return;
            pointMeshReady = false;
            skillPointsText.text = next;
        }

        private void CachePointMesh(TMP_TextInfo text)
        {
            // TMP calls this after generating fresh geometry, before uploading it.
            // Its copy method reuses the buffers unless their capacity changes.
            pointMesh = text.CopyMeshInfoVertexData();
            pointMeshReady = true;
            if (pointTween != null) ApplyPointEffect(text, pointTween.ElapsedPercentage());
        }

        private void UpdatePointEffect()
        {
            if (pointTween == null) return;
            float progress = pointTween.ElapsedPercentage();
            float fade = pointBoosted ? 1f - progress : (1f - progress) * (1f - progress);
            float shineProgress = pointBoosted ? Mathf.Repeat(progress * 2f, 1f) : progress;
            if (pointPanel != null && pointShine != null && pointShine.gameObject.activeSelf)
                pointShine.anchoredPosition = Vector2.LerpUnclamped(-pointShineTravel, pointShineTravel, shineProgress);
            if (pointStar != null)
                pointStar.localRotation = pointStarRotation
                    * Quaternion.Euler(0, 0, Mathf.Sin(progress * Mathf.PI * (pointBoosted ? 10f : 4f))
                        * pointStarAngle * (pointBoosted ? 1.75f : 1f) * fade);
            if (skillPointsText == null || !skillPointsText.isActiveAndEnabled
                || !pointMeshReady || skillPointsText.havePropertiesChanged) return;
            ApplyPointEffect(skillPointsText.textInfo, progress);
            skillPointsText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private void ApplyPointEffect(TMP_TextInfo text, float progress)
        {
            float height = Mathf.Sin(progress * Mathf.PI) * pointWaveHeight * (pointBoosted ? 1.5f : 1f);
            for (int i = 0; i < text.characterCount; i++)
            {
                var letter = text.characterInfo[i];
                if (!letter.isVisible) continue;
                float offset = Mathf.Sin(i * 1.4f - progress * Mathf.PI * (pointBoosted ? 4f : 2f)) * height;
                int material = letter.materialReferenceIndex;
                var mesh = text.meshInfo[material];
                for (int corner = 0; corner < 4; corner++)
                {
                    int vertex = letter.vertexIndex + corner;
                    mesh.vertices[vertex] = pointMesh[material].vertices[vertex] + new Vector3(0, offset, 0);
                    Color original = pointMesh[material].colors32[vertex];
                    Color color = Color.Lerp(pointStartColor, original, progress);
                    color.a = original.a;
                    mesh.colors32[vertex] = color;
                }
            }
        }

        private void SavePointColor()
        {
            if (pointColorSaved || skillPointsText == null) return;
            pointNormalColor = skillPointsText.color;
            pointColorSaved = true;
        }

        private void ResetPointEffect()
        {
            if (pointShine != null) pointShine.gameObject.SetActive(false);
            if (pointTransformsActive)
            {
                if (pointStar != null) pointStar.localRotation = pointStarRotation;
                pointTransformsActive = false;
            }
            if (skillPointsText == null || !pointMeshReady || skillPointsText.havePropertiesChanged) return;
            var text = skillPointsText.textInfo;
            for (int i = 0; i < text.materialCount; i++)
            {
                System.Array.Copy(pointMesh[i].vertices, text.meshInfo[i].vertices, text.meshInfo[i].vertices.Length);
                System.Array.Copy(pointMesh[i].colors32, text.meshInfo[i].colors32, text.meshInfo[i].colors32.Length);
            }
            skillPointsText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        public void ShowTime(double remainingSeconds, double totalSeconds)
        {
            WorkUi.Show(timeText != null ? timeText.gameObject : null, showTime);
            WorkUi.Show(timeFill != null ? timeFill.gameObject : null, showTimeFill);
            int seconds = (int)System.Math.Ceiling(System.Math.Max(0, remainingSeconds));
            if (lastSeconds != seconds)
            {
                lastSeconds = seconds;
                if (timeText != null) timeText.text = seconds / 60 + ":" + (seconds % 60).ToString("00");
            }
            if (timeFill != null)
                timeFill.fillAmount = totalSeconds > 0 ? Mathf.Clamp01((float)(remainingSeconds / totalSeconds)) : 0;
        }

        // Replace/override this method when adding progress animation.
        public virtual void SetProgress(float progress)
        {
            if (progressSlider == null) return;
            progressSlider.minValue = 0;
            progressSlider.maxValue = 1;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(Mathf.Clamp01(progress));
        }

        private void ShowCategorySlots()
        {
            for (int i = 0; i < categorySlots.Count; i++)
            {
                var slot = categorySlots[i];
                if (slot == null) continue;
                bool visible = showCategories && i < categoryCount;
                if (slot.gameObject.activeSelf == visible) continue;
                WorkUi.Show(slot.gameObject, visible);
                categoryLayoutDirty = true;
            }
        }
    }
}
