using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class WorkDocumentTool : EditorWindow
    {
        private enum BodyLayout { Stacked, SideBySide }

        [SerializeField] private BodyLayout bodyLayout;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private Vector2 size = new Vector2(1300, 800);
        [SerializeField] private float fontSize = 24;
        [SerializeField] private bool addToScene;
        private string message;
        private bool hasError;

        private static readonly Color Ink = new Color(0.15f, 0.17f, 0.19f);
        private static readonly Color Muted = new Color(0.40f, 0.43f, 0.46f);
        private static readonly Color Rule = new Color(0.70f, 0.71f, 0.70f);
        private static readonly Color ButtonFace = new Color(0.96f, 0.96f, 0.94f);

        [MenuItem("Work Game/UI/Document Prefab Tool")]
        public static void Open() => GetWindow<WorkDocumentTool>("Document Prefab");

        private void OnEnable()
        {
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (titleFont == null)
                titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Freesentation-7Bold SDF.asset");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("공문서 프리팹 생성", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("uGUI + TextMeshPro 문서 틀입니다. 문서 배경·제목·문서정보·빈 그래프 영역·선택 버튼 3개와 디바이더를 생성합니다.", MessageType.Info);
            int selected = EditorGUILayout.Popup("본문 배치", (int)bodyLayout,
                new[] { "중간 그래프 + 하단 버튼", "왼쪽 그래프 + 오른쪽 버튼" });
            bodyLayout = (BodyLayout)selected;
            size = EditorGUILayout.Vector2Field("기준 크기", size);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("본문 폰트", font, typeof(TMP_FontAsset), false);
            titleFont = (TMP_FontAsset)EditorGUILayout.ObjectField("제목 폰트", titleFont, typeof(TMP_FontAsset), false);
            fontSize = EditorGUILayout.IntSlider("기준 글자 크기", Mathf.RoundToInt(fontSize), 18, 36);
            addToScene = EditorGUILayout.Toggle("생성 후 씬에도 배치", addToScene);
            EditorGUILayout.HelpBox("루트 높이는 직접 조절할 수 있습니다. 글자는 줄바꿈으로 높이가 늘고, 남는 높이는 GraphContent가 사용합니다. 아주 큰 폰트나 긴 문구에는 문서 높이도 늘려 주세요. 버튼의 길게 누르기 동작은 나중에 연결합니다.", MessageType.None);

            using (new EditorGUI.DisabledScope(font == null || size.x <= 0 || size.y <= 0))
                if (GUILayout.Button("프리팹 만들기", GUILayout.Height(34))) SavePrefab();

            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, hasError ? MessageType.Error : MessageType.Info);
        }

        private void SavePrefab()
        {
            string path = EditorUtility.SaveFilePanelInProject("문서 프리팹 저장", "WorkDocument", "prefab",
                "프리팹 저장 위치를 선택하세요.", "Assets/WorkGame");
            if (string.IsNullOrEmpty(path)) return;

            GameObject root = null;
            try
            {
                hasError = false;
                root = BuildDocument();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("프리팹을 저장하지 못했습니다.");
                if (addToScene) AddPreview(prefab, size);
                else Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                message = "생성 완료: " + path;
            }
            catch (Exception e)
            {
                hasError = true;
                message = e.Message;
                Debug.LogException(e);
            }
            finally
            {
                if (root != null) DestroyImmediate(root);
            }
        }

        private GameObject BuildDocument()
        {
            var root = Node("WorkDocument", null);
            var background = root.AddComponent<Image>();
            background.color = new Color(0.99f, 0.98f, 0.96f);
            background.raycastTarget = false;
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = size;
            var layout = Column(root, 14);
            layout.padding = new RectOffset(40, 40, 32, 28);

            BuildHeader(root.transform);
            Divider("HeaderDivider", root.transform);
            BuildBody(root.transform);
            Divider("FooterDivider", root.transform);
            Text("Footer", root.transform, "※ 제출한 추천 결과는 근무평가에 반영됩니다.", 0.72f, Muted);
            return root;
        }

        private void BuildHeader(Transform parent)
        {
            var header = Node("Header", parent);
            Column(header, 8);
            var office = Node("OfficeRow", header.transform);
            Row(office, 20);
            var officeName = Text("OfficeName", office.transform, "조달청  |  공공조달 업무", 0.8f, Ink);
            officeName.font = titleFont != null ? titleFont : font;
            Width(officeName.gameObject, 1);
            var label = Text("DocumentType", office.transform, "업무 검토 · 내부용", 0.72f, Muted);
            label.alignment = TextAlignmentOptions.TopRight;
            Width(label.gameObject, 1);
            Divider("TopRule", header.transform, 2, Ink);

            var title = Text("DocumentTitle", header.transform, "공공조달 검토 요청서", 1.8f, Ink);
            title.font = titleFont != null ? titleFont : font;
            title.alignment = TextAlignmentOptions.Top;
            MetaRow("DocumentInfo", header.transform, "문서번호  조달검토-01-001", "시행  1달");
            Text("WorkRule", header.transform, "AI 기술 활용도가 가장 높은 제품을 추천하여 주시기 바랍니다.", 1, Ink);
        }

        private void MetaRow(string name, Transform parent, string left, string right)
        {
            var row = Node(name, parent);
            Row(row, 24);
            Width(Text("Left", row.transform, left, 0.75f, Muted).gameObject, 1);
            var rightText = Text("Right", row.transform, right, 0.75f, Muted);
            rightText.alignment = TextAlignmentOptions.TopRight;
            Width(rightText.gameObject, 1);
        }

        private void BuildBody(Transform parent)
        {
            bool stacked = bodyLayout == BodyLayout.Stacked;
            var body = Node("Body", parent);
            if (stacked) Column(body, 18);
            else Row(body, 28);
            Height(body, 0, 1);

            var graph = Node("GraphArea", body.transform);
            Column(graph, 10);
            Height(graph, 0, 1);
            if (!stacked) Width(graph, 2);
            Text("SectionTitle", graph.transform, "01  검토 자료", 0.85f, Ink);
            var slot = Node("GraphContent", graph.transform);
            Height(slot, 180, 1); // Empty: place the chosen graph view here later.

            var choices = Node("ChoicesArea", body.transform);
            Column(choices, 10);
            if (!stacked) Width(choices, 1);
            if (stacked)
            {
                var heading = Node("ChoiceHeading", choices.transform);
                Row(heading, 16);
                Width(Text("SectionTitle", heading.transform, "02  추천 대상", 0.85f, Ink).gameObject, 1);
                var hint = Text("HoldHint", heading.transform, "대상을 길게 눌러 제출하세요.", 0.75f, Muted);
                hint.alignment = TextAlignmentOptions.TopRight;
                Width(hint.gameObject, 2);
            }
            else Text("SectionTitle", choices.transform, "02  추천 대상", 0.85f, Ink);
            var buttons = Node("ChoiceButtons", choices.transform);
            if (stacked)
            {
                var row = Row(buttons, 14);
                row.childForceExpandHeight = true; // Match the tallest wrapped label.
            }
            else Column(buttons, 12);
            for (int i = 0; i < 3; i++) MakeChoice(buttons.transform, i);
            if (!stacked)
                Text("HoldHint", choices.transform, "대상을 길게 눌러 제출하세요.", 0.75f, Muted);
        }

        private void MakeChoice(Transform parent, int index)
        {
            string letter = ((char)('A' + index)).ToString();
            var root = Node("Choice" + letter, parent);
            var image = root.AddComponent<Image>();
            image.color = ButtonFace;
            var outline = root.AddComponent<Outline>();
            outline.effectColor = Rule;
            outline.effectDistance = new Vector2(1, -1);
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.91f, 0.94f, 0.96f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.89f);
            button.colors = colors;
            var layout = Column(root, 6);
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.childAlignment = TextAnchor.MiddleCenter;
            Height(root, 92);
            Width(root, 1);

            // The whole Choice is the button. Fill sits behind its text and does not consume layout space.
            var fill = Node("Fill", root.transform);
            fill.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch((RectTransform)fill.transform);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fillImage.color = new Color(0.78f, 0.85f, 0.90f);
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0;

            var name = Text("ChoiceName", root.transform, letter + " 제품", 1.25f, Ink);
            name.alignment = TextAlignmentOptions.Center;
        }

        private TextMeshProUGUI Text(string name, Transform parent, string value, float scale, Color color)
        {
            var text = Node(name, parent).AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = Mathf.RoundToInt(fontSize * scale) + 5;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            // TMP reports preferred height to the parent layout. Do not add fixed text heights.
            return text;
        }

        private static GameObject Node(string name, Transform parent)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = LayerMask.NameToLayer("UI");
            node.transform.SetParent(parent, false);
            return node;
        }

        private static VerticalLayoutGroup Column(GameObject node, float spacing)
        {
            var layout = node.AddComponent<VerticalLayoutGroup>();
            SetupLayout(layout, spacing);
            return layout;
        }

        private static HorizontalLayoutGroup Row(GameObject node, float spacing)
        {
            var layout = node.AddComponent<HorizontalLayoutGroup>();
            SetupLayout(layout, spacing);
            return layout;
        }

        private static void SetupLayout(HorizontalOrVerticalLayoutGroup layout, float spacing)
        {
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static LayoutElement Element(GameObject node)
        {
            var element = node.GetComponent<LayoutElement>();
            return element != null ? element : node.AddComponent<LayoutElement>();
        }

        private static void Height(GameObject node, float min, float flexible = 0)
        {
            var element = Element(node);
            // A layout group's own content must still determine its minimum height.
            if (min > 0) element.minHeight = min;
            element.flexibleHeight = flexible;
        }

        private static void Width(GameObject node, float flexible)
        {
            var element = Element(node);
            element.minWidth = 0;
            element.preferredWidth = 0;
            element.flexibleWidth = flexible;
        }

        private static void Divider(string name, Transform parent, float height = 1, Color? color = null)
        {
            var divider = Node(name, parent);
            var image = divider.AddComponent<Image>();
            image.color = color ?? Rule;
            image.raycastTarget = false;
            var element = Element(divider);
            element.minHeight = element.preferredHeight = height;
            element.flexibleHeight = 0;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AddPreview(GameObject prefab, Vector2 referenceSize)
        {
            var canvas = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                var root = new GameObject("DocumentPreviewCanvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(root, "Create Document Preview");
                canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = referenceSize;
                scaler.matchWidthOrHeight = 0.5f;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Place Work Document");
            Stretch((RectTransform)instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            Selection.activeGameObject = instance;
        }
    }
}
