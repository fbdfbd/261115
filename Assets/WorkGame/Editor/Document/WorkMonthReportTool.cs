using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class WorkMonthReportTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 documentSize = new Vector2(820, 900);
        [SerializeField] private int fontSize = 26;
        private string message;
        private static readonly Color Ink = new Color(0.16f, 0.18f, 0.21f);

        [MenuItem("Work Game/UI/Month Report Prefab Tool")]
        public static void Open() => GetWindow<WorkMonthReportTool>("이달의 성과");

        private void OnEnable()
        {
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("이달의 성과 · 정산서 생성", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            documentSize = EditorGUILayout.Vector2Field("문서 크기", documentSize);
            fontSize = EditorGUILayout.IntSlider("본문 글자 크기", fontSize, 18, 34);
            EditorGUILayout.HelpBox("배경 + 문서 + 확인 버튼을 만들고 View 필드를 연결합니다. 씬에는 직접 배치하세요. 세로 레이아웃은 분기 보너스 행이 없는 달의 빈 공간을 정리합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(font == null || documentSize.x < 600 || documentSize.y < 860))
                if (GUILayout.Button("프리팹 만들기", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("정산서 프리팹 저장", "WorkMonthReport", "prefab",
                "정산서 프리팹 저장 위치", "Assets/WorkGame");
            if (string.IsNullOrEmpty(path)) return;
            var buildRoot = new GameObject("ReportBuild", typeof(RectTransform));
            buildRoot.hideFlags = HideFlags.HideAndDontSave;
            buildRoot.SetActive(false);
            try
            {
                var root = Build(buildRoot.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장 실패");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. Canvas 아래 배치 후 WorkFlowRoot의 Month Report에 연결하세요.";
            }
            finally { DestroyImmediate(buildRoot); }
        }

        private RectTransform Build(Transform parent)
        {
            var root = Rect("WorkMonthReport", parent);
            Stretch(root, 0);
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0, 0, 0, 0.5f);
            var panelGroup = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<WorkMonthReportView>();
            var paper = Rect("Document", root);
            paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(0.5f, 0.5f);
            paper.sizeDelta = documentSize;
            paper.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.97f, 0.93f);
            var content = Rect("Content", paper);
            Stretch(content, 36);
            Stack(content, 10);

            Label("Title", content, "이달의 성과", fontSize + 14, 58);
            var month = Label("Month", content, "2050년 1월 · 성과 정산서", fontSize - 4, 32);
            Line(content);
            var points = Rect("Points", content);
            Stack(points, 8);
            var pointsGroup = points.gameObject.AddComponent<CanvasGroup>();
            Label("Section", points, "숙련 포인트", fontSize, 34);
            var paperValue = Row(points, "Paper", "서류 처리", "+800", out _);
            var mistake = Row(points, "Mistake", "실수", "-0", out _);
            mistake.color = new Color(0.72f, 0.23f, 0.22f);
            var extra = Row(points, "Extra", "추가 포인트 (0%)", "+0", out var extraLabel);
            var total = Row(points, "Total", "합계", "800", out _);
            total.fontStyle = FontStyles.Bold;
            Line(content);
            var salary = Rect("Salary", content);
            Stack(salary, 8);
            var salaryGroup = salary.gameObject.AddComponent<CanvasGroup>();
            Label("Section", salary, "급여", fontSize, 34);
            var salaryValue = Row(salary, "FixedSalary", "고정 월급", "1,000", out _);
            var bonus = Row(salary, "QuarterBonus", "분기 보너스 · S등급", "+3,000", out var bonusLabel);
            var moneyTotal = Row(salary, "MoneyTotal", "합계", "4,000", out _);
            var cash = Row(salary, "Cash", "보유 현금", "4,000", out _);
            var spacer = Rect("Space", content).gameObject.AddComponent<LayoutElement>();
            spacer.flexibleHeight = 1;
            var buttonRect = Rect("Confirm", content);
            buttonRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.3f, 0.35f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var confirm = Label("Text", buttonRect, "다음", fontSize, 0);
            Stretch(confirm.rectTransform, 8);
            confirm.alignment = TextAlignmentOptions.Center;
            confirm.color = Color.white;

            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", panelGroup);
            Set(fields, "document", paper);
            Set(fields, "pointsGroup", pointsGroup);
            Set(fields, "salaryGroup", salaryGroup);
            Set(fields, "confirmButton", button);
            Set(fields, "monthText", month);
            Set(fields, "paperText", paperValue);
            Set(fields, "mistakeText", mistake);
            Set(fields, "extraLabel", extraLabel);
            Set(fields, "extraText", extra);
            Set(fields, "totalText", total);
            Set(fields, "salaryText", salaryValue);
            Set(fields, "bonusRow", bonus.transform.parent.gameObject);
            Set(fields, "bonusLabel", bonusLabel);
            Set(fields, "bonusText", bonus);
            Set(fields, "moneyTotalText", moneyTotal);
            Set(fields, "cashText", cash);
            Set(fields, "confirmText", confirm);
            SetRows(fields, "pointRows", paperValue, mistake, extra, total);
            SetRows(fields, "salaryRows", salaryValue, bonus, moneyTotal, cash);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private TMP_Text Row(Transform parent, string name, string label, string value, out TMP_Text labelText)
        {
            var row = Rect(name, parent);
            row.gameObject.AddComponent<CanvasGroup>();
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            labelText = Label("Label", row, label, fontSize, 0);
            Stretch(labelText.rectTransform, 0);
            labelText.rectTransform.anchorMax = new Vector2(0.7f, 1);
            var text = Label("Value", row, value, fontSize, 0);
            Stretch(text.rectTransform, 0);
            text.rectTransform.anchorMin = new Vector2(0.7f, 0);
            text.alignment = TextAlignmentOptions.MidlineRight;
            return text;
        }

        private TMP_Text Label(string name, Transform parent, string value, int size, float height)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = Ink;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            if (height > 0) rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static void Line(Transform parent)
        {
            var rect = Rect("Divider", parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.65f, 0.65f, 0.62f);
            image.raycastTarget = false;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin;
            rect.offsetMax = Vector2.one * -margin;
        }

        private static void Stack(RectTransform rect, float spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void Set(SerializedObject target, string name, UnityEngine.Object value) =>
            target.FindProperty(name).objectReferenceValue = value;

        private static void SetRows(SerializedObject target, string name, params TMP_Text[] texts)
        {
            var rows = target.FindProperty(name);
            rows.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
                rows.GetArrayElementAtIndex(i).objectReferenceValue = texts[i].transform.parent.GetComponent<CanvasGroup>();
        }
    }
}
