using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareMonthReportTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(1300, 800);
        [SerializeField] private int fontSize = 28;
        [SerializeField] private float imageShare = 0.42f;
        [SerializeField] private float statsWidth = 370;
        [SerializeField] private Color paperColor = new Color(0.98f, 0.97f, 0.94f);
        [SerializeField] private Color cardColor = new Color(0.89f, 0.93f, 0.88f);
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;

        [MenuItem("Work Game/UI/Care Month Report Tool")]
        public static void Open() => GetWindow<CareMonthReportTool>("달이 육성 결과");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("4:3 이미지 → 한마디 → 스텟 증감", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            pageSize = EditorGUILayout.Vector2Field("페이지 크기", pageSize);
            fontSize = EditorGUILayout.IntSlider("기준 글자 크기", fontSize, 24, 34);
            imageShare = EditorGUILayout.Slider("이미지 영역 높이 비율", imageShare, 0.3f, 0.42f);
            statsWidth = EditorGUILayout.Slider("세로 스텟 영역 폭", statsWidth, 320, 600);
            paperColor = EditorGUILayout.ColorField("패널 배경", paperColor);
            cardColor = EditorGUILayout.ColorField("스텟 카드 배경", cardColor);
            EditorGUILayout.HelpBox("4:3 이미지와 한 글자씩 나타나는 한마디 아래에 HUD 형태 스텟을 세로 4줄로 배치합니다. 전체 화면 클릭으로 연출을 즉시 표시하고, 다 표시된 뒤 다시 클릭하면 다음 단계로 갑니다. 줄 간격은 Stats의 Vertical, 줄 내부는 Horizontal Layout Group에서 조절합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || pageSize.x < 900 || pageSize.y < 700))
                if (GUILayout.Button("육성 결과 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("육성 결과 저장", "CareMonthReport", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("CareReportBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. 씬 View를 WorkRoot의 Care Month Report View에 연결하세요.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("CareMonthReport", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Image(root, new Color(0, 0, 0, 0.8f), true);
            var clickArea = root.gameObject.AddComponent<CareDialogueClickArea>();
            var view = root.gameObject.AddComponent<CareMonthReportView>();
            var page = Rect("Page", root);
            page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = pageSize;
            Image(page, paperColor);
            var title = Text("Title", page, "1월 · 1주차", fontSize + 8);
            Area(title.rectTransform, 0.06f, 0.92f, 0.94f, 0.99f);
            var imageArea = Rect("ImageArea", page);
            Area(imageArea, 0.06f, 0.49f, 0.94f, Mathf.Min(0.91f, 0.49f + imageShare));
            var frame = Rect("PictureFrame", imageArea);
            var aspect = frame.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 4f / 3f;
            Image(frame, cardColor);
            var pictureGroup = frame.gameObject.AddComponent<CanvasGroup>();
            var pictureRect = Rect("Picture", frame); Area(pictureRect, 0, 0, 1, 1);
            var picture = Image(pictureRect, Color.white); picture.preserveAspect = true; picture.enabled = false;
            var lineArea = Rect("Feedback", page); Area(lineArea, 0.06f, 0.4f, 0.94f, 0.48f);
            var lineGroup = lineArea.gameObject.AddComponent<CanvasGroup>();
            var line = Text("Line", lineArea, "달이의 수업 이야기가 여기에 표시됩니다.", fontSize);
            Area(line.rectTransform, 0, 0, 1, 1);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;

            var statsRoot = Rect("Stats", page);
            Area(statsRoot, 0.5f, 0.06f, 0.5f, 0.38f);
            statsRoot.sizeDelta = new Vector2(statsWidth, -4);
            var layout = statsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            var stats = new List<CareMonthStatView>();
            for (int i = 0; i < 4; i++) stats.Add(BuildStat(statsRoot, (CareStat)i));
            var hint = Text("ContinueHint", page, "화면을 눌러 계속", fontSize - 8);
            Area(hint.rectTransform, 0.06f, 0.008f, 0.94f, 0.045f);

            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "pictureGroup", pictureGroup);
            Set(fields, "picture", picture); Set(fields, "lineGroup", lineGroup); Set(fields, "lineText", line);
            Set(fields, "titleText", title); Set(fields, "clickArea", clickArea); Set(fields, "hintText", hint);
            var list = fields.FindProperty("stats"); list.arraySize = stats.Count;
            for (int i = 0; i < stats.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = stats[i];
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }
        private CareMonthStatView BuildStat(Transform parent, CareStat stat)
        {
            var root = Rect(stat.ToString(), parent); Image(root, Color.clear);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6; layout.padding = new RectOffset(0, 0, 6, 6);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            var name = Text("Name", root, CareStatNames.GetName(stat), fontSize - 2);
            var grade = Text("Grade", root, "C", fontSize + 4);
            var bar = Rect("Slider", root);
            var size = bar.gameObject.AddComponent<LayoutElement>();
            size.minWidth = 64; size.preferredWidth = 0; size.flexibleWidth = 1;
            var back = Rect("Background", bar); Area(back, 0, 0, 1, 1);
            Image(back, new Color(0.84f, 0.87f, 0.83f));
            var fill = Rect("Fill", bar); Area(fill, 0, 0, 1, 1);
            Image(fill, new Color(0.39f, 0.62f, 0.45f));
            var slider = bar.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(0);
            slider.interactable = false; slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            var value = Text("Value", bar, "0", fontSize - 4);
            Area(value.rectTransform, 0, 0, 1, 1); value.richText = true;
            var view = root.gameObject.AddComponent<CareMonthStatView>();
            var fields = new SerializedObject(view);
            fields.FindProperty("stat").enumValueIndex = (int)stat;
            Set(fields, "group", group); Set(fields, "nameText", name); Set(fields, "slider", slider);
            Set(fields, "valueText", value); Set(fields, "gradeText", grade);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Ink;
            text.raycastTarget = false; text.richText = false; text.alignment = TextAlignmentOptions.Center;
            return text;
        }
        private static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = raycast; return image;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        private static void Area(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void Set(SerializedObject fields, string name, UnityEngine.Object value) => fields.FindProperty(name).objectReferenceValue = value;
    }
}
