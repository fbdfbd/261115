using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame
{
    public class WorkHudView : MonoBehaviour
    {
        public event System.Action<WorkSfxCue> SoundRequested;
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
        [Tooltip("Optional full-size flowing fill. Leave the Slider Fill Rect empty when using this.")]
        [SerializeField] private WorkProgressFlow progressFlow;
        [Header("숙련 포인트 / 남은 시간")]
        [SerializeField] private TMP_Text skillPointsText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image timeFill;
        [Header("강화 장식 — I, II, III 순서")]
        [Tooltip("구매한 단계까지 누적 활성화")]
        [SerializeField] private GameObject[] highlightObjects = new GameObject[3];
        [Tooltip("구매한 단계까지 누적 활성화")]
        [SerializeField] private GameObject[] instantObjects = new GameObject[3];
        [Tooltip("현재 단계 하나만 활성화. 미구매 시 모두 숨김")]
        [SerializeField] private GameObject[] pointFlatObjects = new GameObject[2];
        [Tooltip("현재 단계 하나만 활성화. 미구매 시 모두 숨김")]
        [SerializeField] private GameObject[] pointPercentObjects = new GameObject[2];
        [Tooltip("행운의 숙련, 행운 확률 I, II 순서. 현재 단계 하나만 활성화")]
        [SerializeField] private GameObject[] pointLuckObjects = new GameObject[3];
        [Header("빠른 손 — 줄어들지 않는 시계 테두리")]
        [SerializeField] private Image timerFrame;
        [Tooltip("빠른 손 I, II, III 순서. 기본 외형은 Image의 시작 Sprite 사용")]
        [SerializeField] private Sprite[] timerFrameSprites = new Sprite[3];
        [Header("숙련 포인트 카운트업")]
        [SerializeField, Min(0.01f)] private float pointCountSeconds = 0.35f;
        [SerializeField, Min(0)] private float pointWaveHeight = 4;
        [Header("숙련 포인트 패널 광택 / 별 회전")]
        [SerializeField] private RectTransform pointPanel;
        [Tooltip("패널의 직접 자식. Anchor와 Pivot은 중앙, Layout Group에서는 Ignore Layout 설정.")]
        [SerializeField] private RectTransform pointShine;
        [SerializeField, Min(0.01f)] private float pointShineSeconds = 0.42f;
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
        private Tween pointShineTween;
        private Color pointNormalColor;
        private Color pointStartColor;
        private bool pointColorSaved;
        private TMP_MeshInfo[] pointMesh;
        private bool pointMeshReady;
        private Vector2 pointShineTravel;
        private Quaternion pointStarRotation;
        private bool pointTransformsActive;
        private bool pointBoosted;
        private Sprite defaultTimerFrame;
        private bool timerFrameSaved;

        public void ShowUpgrades(int highlight, int instant, int pointFlat, int pointPercent, int pointLuck, int time)
        {
            ShowUpgradeObjects(highlightObjects, highlight, true);
            ShowUpgradeObjects(instantObjects, instant, true);
            ShowUpgradeObjects(pointFlatObjects, pointFlat, false);
            ShowUpgradeObjects(pointPercentObjects, pointPercent, false);
            ShowUpgradeObjects(pointLuckObjects, pointLuck, false);
            if (timerFrame == null) return;
            if (!timerFrameSaved)
            {
                defaultTimerFrame = timerFrame.sprite;
                timerFrameSaved = true;
            }
            var sprite = time > 0 && time <= timerFrameSprites.Length ? timerFrameSprites[time - 1] : null;
            timerFrame.sprite = sprite != null ? sprite : defaultTimerFrame;
        }

        private static void ShowUpgradeObjects(GameObject[] objects, int level, bool cumulative)
        {
            for (int i = 0; i < objects.Length; i++)
                WorkUi.Show(objects[i], cumulative ? i < level : i == level - 1);
        }

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
            StopPointShine();
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
            if (lastDone >= 0 && lastTotal == total && done > lastDone)
                SoundRequested?.Invoke(WorkSfxCue.Progress);
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

        public void ShowSkillPointResult(int points, bool isRight, bool isPointBoosted = false, float shineDelay = 0)
        {
            WorkUi.Show(skillPointsText != null ? skillPointsText.gameObject : null, showSkillPoints);
            ChangePointText(points, true, isRight ? pointSuccessColor : pointFailureColor, isRight,
                isRight && isPointBoosted, shineDelay);
        }

        private void ChangePointText(int points, bool animate, Color? flashColor = null, bool playShine = false,
            bool boosted = false, float shineDelay = 0)
        {
            SavePointColor();
            bool gainedPoints = animate && lastSkillPoints >= 0 && points > lastSkillPoints;
            lastSkillPoints = points;
            if (gainedPoints) SoundRequested?.Invoke(WorkSfxCue.SkillPointsGain);
            pointTween?.Kill();
            pointTween = null;
            StopPointShine();
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
                    // 숫자 카운트와 분리해 종이가 나간 뒤에도 광택이 잠깐 남습니다.
                    pointShineTween = DOTween.To(() => 0f, UpdatePointShine, 1f,
                            Mathf.Max(0.01f, pointShineSeconds) * (pointBoosted ? 1.6f : 1f))
                        .SetDelay(Mathf.Max(0, shineDelay)).SetEase(Ease.Linear).SetUpdate(true)
                        .OnStart(() => pointShine.gameObject.SetActive(true))
                        .OnComplete(() => { pointShineTween = null; pointShine.gameObject.SetActive(false); });
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

        private void UpdatePointShine(float progress)
        {
            float sweep = pointBoosted && progress < 1 ? Mathf.Repeat(progress * 2f, 1f) : progress;
            pointShine.anchoredPosition = Vector2.LerpUnclamped(-pointShineTravel, pointShineTravel, sweep);
        }

        private void StopPointShine()
        {
            pointShineTween?.Kill();
            pointShineTween = null;
            if (pointShine != null) pointShine.gameObject.SetActive(false);
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

        public virtual void SetProgress(float progress)
        {
            if (progressFlow != null) progressFlow.SetProgress(progress);
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
