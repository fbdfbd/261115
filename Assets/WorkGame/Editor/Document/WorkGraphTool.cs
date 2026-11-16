using System;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class WorkGraphTool : EditorWindow
    {
        private enum GraphKind { VerticalBar, HorizontalBar, Dots, Grade, Number }
        private enum NumberKind { Cost, Stock, Energy, Steps, Defect, Opportunity, Conflict, Cooperation }

        [SerializeField] private GraphKind kind;
        [SerializeField] private NumberKind numberKind;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private int fontSize = 29;
        [SerializeField] private int circleSize = 36;
        [SerializeField] private int circleGap = 8;
        [SerializeField] private RectTransform target;
        [SerializeField] private GameObject prefab;
        private string message;
        private bool hasError;

        private static readonly Color Ink = new Color(0.15f, 0.17f, 0.19f);
        private static readonly Color Accent = new Color(0.27f, 0.38f, 0.46f);
        private static readonly Color Pale = new Color(0.86f, 0.88f, 0.88f);
        private static readonly string[] GradeNames = { "D", "C", "B", "A", "S" };

        [MenuItem("Work Game/UI/Graph Prefab Tool")]
        public static void Open() => GetWindow<WorkGraphTool>("Graph Prefab");

        private void OnEnable()
        {
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (Selection.activeTransform is RectTransform selected && selected.name == "GraphContent") target = selected;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("그래프 내부 프리팹", EditorStyles.boldLabel);
            kind = (GraphKind)EditorGUILayout.Popup("표현", (int)kind,
                new[] { "세로 막대", "가로 막대", "원형 별점", "등급", "숫자 카드" });
            if (kind == GraphKind.Number)
                numberKind = (NumberKind)EditorGUILayout.Popup("숫자 예시", (int)numberKind,
                    new[] { "도입 비용", "재고량", "에너지 소비량", "행정절차 수", "불량률", "참여 기회", "이해충돌 위험도", "협업 실적" });
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            fontSize = EditorGUILayout.IntSlider("글자 크기", fontSize, 18, 40);
            circleSize = EditorGUILayout.IntSlider("별점 원 크기", circleSize, 16, 64);
            circleGap = EditorGUILayout.IntSlider("별점 행 Spacing", circleGap, -8, 24);
            EditorGUILayout.HelpBox("문서의 GraphContent 안에 넣는 내부 프리팹입니다. 루트는 Stretch / 여백 0이며, 문서 배경과 선택 버튼은 포함하지 않습니다. 값은 편집용 예시입니다.", MessageType.Info);

            using (new EditorGUI.DisabledScope(font == null))
            {
                if (GUILayout.Button("선택한 종류 생성")) SaveOne();
                if (GUILayout.Button("기본 5종 한 번에 생성")) SaveAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("문서에 넣어보기", EditorStyles.boldLabel);
            target = (RectTransform)EditorGUILayout.ObjectField("GraphContent", target, typeof(RectTransform), true);
            if (GUILayout.Button("현재 선택한 오브젝트를 GraphContent로 사용"))
                target = Selection.activeTransform as RectTransform;
            prefab = (GameObject)EditorGUILayout.ObjectField("내부 프리팹", prefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(target == null || prefab == null))
                if (GUILayout.Button("GraphContent에 넣기")) TryAction(Insert);
            EditorGUILayout.HelpBox("한 번에 한 종류를 넣어 확인하세요. 크기는 GraphContent의 LayoutElement / Min Height와 문서 크기로 조절합니다. 배치 버튼은 그래프가 들어갈 최소 높이도 확보합니다.", MessageType.None);
            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, hasError ? MessageType.Error : MessageType.Info);
        }

        private void SaveOne()
        {
            string path = EditorUtility.SaveFilePanelInProject("내부 프리팹 저장", GetName(), "prefab",
                "저장할 위치를 선택하세요.", "Assets/WorkGame/Prefab");
            if (!string.IsNullOrEmpty(path)) TryAction(() => Save(path));
        }

        private void SaveAll()
        {
            string folder = EditorUtility.SaveFolderPanel("기본 그래프 5종 저장", Application.dataPath, "");
            if (string.IsNullOrEmpty(folder)) return;
            TryAction(() =>
            {
                string relative = FileUtil.GetProjectRelativePath(folder);
                if (relative != "Assets" && !relative.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new InvalidOperationException("프로젝트 Assets 안의 폴더를 선택하세요.");
                var savedKind = kind;
                try
                {
                    foreach (GraphKind item in Enum.GetValues(typeof(GraphKind)))
                    {
                        kind = item;
                        Save(AssetDatabase.GenerateUniqueAssetPath(relative + "/" + GetName() + ".prefab"));
                    }
                }
                finally { kind = savedKind; }
                message = "그래프 내부 프리팹 5종을 생성했습니다.";
            });
        }

        private string GetName() => kind == GraphKind.Number ? "GraphNumber_" + numberKind : "Graph" + kind;

        private void Save(string path)
        {
            var preview = Node("GraphPreview", null);
            ((RectTransform)preview.transform).sizeDelta = new Vector2(900, 360);
            try
            {
                var root = Build(preview.transform);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                message = "생성 완료: " + path;
            }
            finally { DestroyImmediate(preview); }
        }

        private GameObject Build(Transform parent)
        {
            var root = Node(GetName(), parent);
            Stretch((RectTransform)root.transform);
            Column(root, 14).padding = new RectOffset(12, 12, 12, 12);
            string metric = "AI 기술 활용도";
            string unit = "%";
            string targetName = "제품";
            int[] values = { 30, 60, 90 };
            if (kind == GraphKind.VerticalBar) { metric = "원자재 공급 부족 위험도"; targetName = "원자재"; }
            if (kind == GraphKind.Dots) { metric = "제품 안전성"; unit = ""; values = new[] { 2, 4, 5 }; }
            if (kind == GraphKind.Grade) { metric = "기술 혁신성"; unit = ""; values = new[] { 2, 4, 5 }; }
            if (kind == GraphKind.Number) SetNumberExample(out metric, out unit, out targetName, out values);
            Label("MetricTitle", root.transform, metric, fontSize);
            if (!string.IsNullOrEmpty(unit))
                Label("UnitLabel", root.transform, "단위: " + unit, fontSize - 7).alignment = TextAlignmentOptions.TopRight;

            var content = Node("Candidates", root.transform);
            Element(content).flexibleHeight = 1;
            bool rows = kind == GraphKind.HorizontalBar || kind == GraphKind.Dots;
            if (rows) Column(content, 18);
            else Row(content, 20, true);
            for (int i = 0; i < 3; i++)
            {
                string letter = ((char)('A' + i)).ToString();
                string name = letter + " " + targetName;
                var candidate = Node("Candidate" + letter, content.transform);
                if (rows) Row(candidate, 16);
                else
                {
                    Column(candidate, 10).childAlignment = TextAnchor.MiddleCenter;
                    EqualWidth(candidate);
                }
                if (kind == GraphKind.VerticalBar) VerticalBar(candidate.transform, name, values[i]);
                else if (kind == GraphKind.HorizontalBar) HorizontalBar(candidate.transform, name, values[i]);
                else if (kind == GraphKind.Dots) Dots(candidate.transform, name, values[i]);
                else Card(candidate, name, kind == GraphKind.Grade ? GradeNames[values[i] - 1] : values[i].ToString("N0", CultureInfo.InvariantCulture));
            }
            return root;
        }

        private void VerticalBar(Transform parent, string name, int value)
        {
            Label("ValueLabel", parent, value + "%", fontSize).alignment = TextAlignmentOptions.Center;
            var bar = MakeSlider(parent, value, true);
            Element(bar.gameObject).minHeight = 80;
            Element(bar.gameObject).flexibleHeight = 1;
            Label("CandidateName", parent, name, fontSize - 3).alignment = TextAlignmentOptions.Center;
        }

        private void HorizontalBar(Transform parent, string name, int value)
        {
            var nameLabel = Label("CandidateName", parent, name, fontSize - 3);
            Element(nameLabel.gameObject).preferredWidth = 110;
            var bar = MakeSlider(parent, value, false);
            Element(bar.gameObject).flexibleWidth = 1;
            Element(bar.gameObject).preferredHeight = fontSize;
            var valueLabel = Label("ValueLabel", parent, value + "%", fontSize);
            valueLabel.alignment = TextAlignmentOptions.TopRight;
            Element(valueLabel.gameObject).preferredWidth = 100;
        }

        private void Dots(Transform parent, string name, int value)
        {
            var row = parent.GetComponent<HorizontalLayoutGroup>();
            row.spacing = circleGap;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.childControlWidth = row.childControlHeight = false;
            var label = Label("CandidateName", parent, name, fontSize - 3);
            ((RectTransform)label.transform).sizeDelta = new Vector2(
                Mathf.Ceil(label.GetPreferredValues(name).x), Mathf.Ceil(label.preferredHeight));
            var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (circle == null) throw new InvalidOperationException("Unity 기본 원형 스프라이트를 찾지 못했습니다.");
            for (int i = 0; i < 5; i++)
            {
                var dot = ImageNode("Dot" + (i + 1), parent, i < value ? Accent : Pale);
                dot.sprite = circle;
                dot.preserveAspect = true;
                ((RectTransform)dot.transform).sizeDelta = new Vector2(circleSize, circleSize);
            }
        }

        private void Card(GameObject root, string name, string value)
        {
            root.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.93f);
            root.GetComponent<Image>().raycastTarget = false;
            root.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(12, 12, 16, 16);
            Label("CandidateName", root.transform, name, fontSize - 3).alignment = TextAlignmentOptions.Center;
            Label("ValueLabel", root.transform, value, Mathf.RoundToInt(fontSize * 1.8f)).alignment = TextAlignmentOptions.Center;
        }

        private static Slider MakeSlider(Transform parent, int value, bool vertical)
        {
            var root = Node("BarSlider", parent);
            var background = ImageNode("Background", root.transform, Pale);
            Stretch((RectTransform)background.transform);
            var area = Node("FillArea", root.transform);
            Stretch((RectTransform)area.transform);
            var fill = ImageNode("Fill", area.transform, Accent);
            Stretch((RectTransform)fill.transform);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.direction = vertical ? Slider.Direction.BottomToTop : Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.value = value;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        private void SetNumberExample(out string metric, out string unit, out string targetName, out int[] values)
        {
            var metrics = new[] { "AI 도입 비용", "원자재 현재 재고량", "에너지 소비량", "불필요한 행정절차 수", "제품 불량률", "공공조달 참여 기회", "이해충돌 위험도", "기관 간 협업 실적" };
            var units = new[] { "만원", "톤", "kWh", "단계", "%", "회", "%", "건" };
            var targets = new[] { "제품", "원자재", "제품", "행정절차", "제품", "기업", "평가위원 후보", "기관" };
            int[][] samples = { new[] { 300, 850, 1200 }, new[] { 40, 120, 240 }, new[] { 60, 150, 270 }, new[] { 4, 10, 18 },
                new[] { 2, 7, 13 }, new[] { 2, 6, 10 }, new[] { 5, 15, 25 }, new[] { 1, 5, 10 } };
            int index = (int)numberKind;
            metric = metrics[index];
            unit = units[index];
            targetName = targets[index];
            values = samples[index];
        }

        private void Insert()
        {
            if (EditorUtility.IsPersistent(target))
                throw new InvalidOperationException("문서 프리팹을 Prefab Mode로 열거나 씬의 GraphContent를 선택하세요.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target);
            Undo.RegisterCreatedObjectUndo(instance, "Insert Graph Prefab");
            var rect = instance.transform as RectTransform;
            if (rect == null) { Undo.DestroyObjectImmediate(instance); throw new InvalidOperationException("UI 프리팹을 선택하세요."); }
            Stretch(rect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            var size = target.GetComponent<LayoutElement>();
            if (size == null) size = Undo.AddComponent<LayoutElement>(target.gameObject);
            Undo.RecordObject(size, "Fit Graph Height");
            size.minHeight = Mathf.Max(size.minHeight, Mathf.Ceil(LayoutUtility.GetPreferredHeight(rect)));
            PrefabUtility.RecordPrefabInstancePropertyModifications(size);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            Selection.activeGameObject = instance;
            message = "배치 완료. GraphContent의 Min Height를 조절해 보세요.";
        }

        private TextMeshProUGUI Label(string name, Transform parent, string value, int size)
        {
            var label = Node(name, parent).AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = Mathf.Max(12, size);
            label.text = value;
            label.color = Ink;
            label.raycastTarget = false;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.TopLeft;
            return label;
        }

        private static GameObject Node(string name, Transform parent)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = LayerMask.NameToLayer("UI");
            node.transform.SetParent(parent, false);
            return node;
        }

        private static Image ImageNode(string name, Transform parent, Color color)
        {
            var image = Node(name, parent).AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static VerticalLayoutGroup Column(GameObject node, int spacing)
        {
            var group = node.AddComponent<VerticalLayoutGroup>();
            Setup(group, spacing, false);
            return group;
        }

        private static HorizontalLayoutGroup Row(GameObject node, int spacing, bool fillHeight = false)
        {
            var group = node.AddComponent<HorizontalLayoutGroup>();
            Setup(group, spacing, fillHeight);
            return group;
        }

        private static void Setup(HorizontalOrVerticalLayoutGroup group, int spacing, bool fillHeight)
        {
            group.spacing = spacing;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = group is VerticalLayoutGroup;
            group.childForceExpandHeight = fillHeight;
            group.childAlignment = TextAnchor.MiddleLeft;
        }

        private static LayoutElement Element(GameObject node)
        {
            var element = node.GetComponent<LayoutElement>();
            return element != null ? element : node.AddComponent<LayoutElement>();
        }

        private static void EqualWidth(GameObject node)
        {
            var element = Element(node);
            element.minWidth = element.preferredWidth = 0;
            element.flexibleWidth = 1;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void TryAction(Action action)
        {
            hasError = false;
            try { action(); }
            catch (Exception e) { hasError = true; message = e.Message; Debug.LogException(e); }
        }
    }
}
