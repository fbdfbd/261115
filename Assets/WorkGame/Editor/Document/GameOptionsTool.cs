using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class GameOptionsTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(620, 700);
        [SerializeField] private int fontSize = 28;
        [SerializeField] private float spacing = 14;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color accentColor = new Color(0.34f, 0.51f, 0.42f);
        [SerializeField, TextArea(6, 14)] private string credits = "만든 사람들\n\n기획 · 개발\n이름\n\n아트 · 사운드\n이름\n\n플레이해 주셔서 감사합니다.";
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;

        [MenuItem("Work Game/UI/Game Options Tool")]
        public static void Open() => GetWindow<GameOptionsTool>("옵션 패널");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("작은 중앙 옵션창 + 크레딧", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            pageSize = EditorGUILayout.Vector2Field("창 크기", pageSize);
            fontSize = EditorGUILayout.IntSlider("본문 크기", fontSize, 24, 32);
            spacing = EditorGUILayout.Slider("행 간격", spacing, 6, 22);
            pageColor = EditorGUILayout.ColorField("창 배경", pageColor);
            accentColor = EditorGUILayout.ColorField("강조 색", accentColor);
            EditorGUILayout.LabelField("크레딧 내용");
            credits = EditorGUILayout.TextArea(credits, GUILayout.MinHeight(120));
            EditorGUILayout.HelpBox("Page는 중앙 피벗입니다. Body의 Vertical Layout Group으로 행 간격을, 각 행의 Height로 높이를 조절합니다. 생성된 프리팹에는 View와 Root가 연결되어 있습니다. 씬에서 Work Root와 Open Buttons만 연결하세요.", MessageType.Info);
            float neededHeight = (432 + spacing * 7) / 0.76f;
            if (pageSize.y < neededHeight) EditorGUILayout.HelpBox("현재 행 간격에는 창 높이가 " + Mathf.CeilToInt(neededHeight) + " 이상 필요합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || pageSize.x < 460 || pageSize.y < neededHeight))
                if (GUILayout.Button("옵션 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("옵션 저장", "GameOptions", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("OptionsBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "완료. Canvas 최상단에 배치하고 GameOptionsRoot의 Work Root / Open Buttons를 연결하세요.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("GameOptions", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<GameOptionsView>();
            var controller = root.gameObject.AddComponent<GameOptionsRoot>();
            var backdrop = Backdrop(root);
            var page = Page("Page", root, pageSize);
            Header(page, "옵션", out var close);
            var body = Rect("Body", page); Area(body, 0.08f, 0.08f, 0.92f, 0.84f);
            var layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

            var resolutionRow = Row("Resolution", body, 84);
            var resolutionLabel = Text("Label", resolutionRow, "해상도", fontSize);
            Area(resolutionLabel.rectTransform, 0, 0.55f, 1, 1);
            var dropdown = Dropdown(resolutionRow); Area((RectTransform)dropdown.transform, 0, 0, 1, 0.5f);
            var windowed = WindowedToggle(body);
            var music = VolumeRow("Music", body, "배경음", out var musicValue);
            var sound = VolumeRow("Sound", body, "효과음", out var soundValue);
            var divider = Row("Divider", body, 2); Image(divider, new Color(0.76f, 0.79f, 0.75f));
            var title = Button("ReturnToTitle", body, "타이틀로 돌아가기", accentColor);
            ((RectTransform)title.transform).sizeDelta = new Vector2(0, 54);
            title.GetComponentInChildren<TMP_Text>().color = Color.white;
            var creditButton = Button("Credits", body, "크레딧", new Color(0.89f, 0.91f, 0.87f));
            ((RectTransform)creditButton.transform).sizeDelta = new Vector2(0, 48);
            var error = Text("Error", body, "", 22); error.rectTransform.sizeDelta = new Vector2(0, 32);
            error.color = new Color(0.7f, 0.2f, 0.2f);

            var creditsRoot = Rect("CreditsPanel", root); Area(creditsRoot, 0, 0, 1, 1);
            var creditsGroup = creditsRoot.gameObject.AddComponent<CanvasGroup>();
            var creditsBackdrop = Backdrop(creditsRoot);
            var creditsPage = Page("Page", creditsRoot, pageSize);
            Header(creditsPage, "크레딧", out var creditsClose);
            CreditsBody(creditsPage);

            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "closeButton", close); Set(fields, "backgroundButton", backdrop);
            Set(fields, "resolutionDropdown", dropdown); Set(fields, "windowedToggle", windowed);
            Set(fields, "musicSlider", music); Set(fields, "musicValue", musicValue);
            Set(fields, "soundSlider", sound); Set(fields, "soundValue", soundValue);
            Set(fields, "titleButton", title); Set(fields, "creditsButton", creditButton); Set(fields, "creditsGroup", creditsGroup);
            Set(fields, "creditsCloseButton", creditsClose); Set(fields, "creditsBackgroundButton", creditsBackdrop);
            Set(fields, "errorText", error); fields.ApplyModifiedPropertiesWithoutUndo();
            fields = new SerializedObject(controller); Set(fields, "view", view); fields.ApplyModifiedPropertiesWithoutUndo();
            // 씬에서는 Awake가 숨깁니다. 프리팹 편집 중에는 옵션을 바로 볼 수 있습니다.
            creditsGroup.alpha = 0; creditsGroup.interactable = false; creditsGroup.blocksRaycasts = false;
            return root;
        }

        private RectTransform Page(string name, Transform parent, Vector2 size)
        {
            var page = Rect(name, parent); page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = size; Image(page, pageColor, true);
            return page;
        }
        private void Header(RectTransform page, string title, out Button close)
        {
            var text = Text("Title", page, title, 42); Area(text.rectTransform, 0.2f, 0.86f, 0.8f, 0.97f);
            close = Button("Close", page, "닫기", new Color(0.89f, 0.91f, 0.87f));
            Area((RectTransform)close.transform, 0.8f, 0.9f, 0.97f, 0.97f);
            close.GetComponentInChildren<TMP_Text>().fontSize = 24;
        }
        private Button Backdrop(Transform parent)
        {
            var rect = Rect("BackgroundClose", parent); Area(rect, 0, 0, 1, 1);
            var image = Image(rect, new Color(0, 0, 0, 0.72f), true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            return button;
        }
        private Toggle WindowedToggle(Transform parent)
        {
            var row = Row("Windowed", parent, 44);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var label = Text("Label", row, "창 모드", fontSize); label.rectTransform.sizeDelta = new Vector2(132, 44);
            var box = Rect("Toggle", row); box.sizeDelta = new Vector2(36, 36);
            var background = Image(box, new Color(0.79f, 0.83f, 0.78f), true);
            var mark = Rect("Checkmark", box); Area(mark, 0.2f, 0.2f, 0.8f, 0.8f);
            var toggle = box.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = background; toggle.graphic = Image(mark, accentColor);
            return toggle;
        }
        private Slider VolumeRow(string name, Transform parent, string label, out TMP_Text value)
        {
            var row = Row(name, parent, 84);
            var text = Text("Label", row, label, fontSize); Area(text.rectTransform, 0, 0.52f, 1, 1);
            var rect = Rect("Slider", row); Area(rect, 0, 0.04f, 0.84f, 0.45f);
            var track = Rect("Background", rect); Area(track, 0, 0.33f, 1, 0.67f); Image(track, new Color(0.79f, 0.83f, 0.78f));
            var fillArea = Rect("FillArea", rect); Area(fillArea, 0, 0.33f, 1, 0.67f);
            var fill = Rect("Fill", fillArea); Area(fill, 0, 0, 1, 1); Image(fill, accentColor);
            var handleArea = Rect("HandleArea", rect); Area(handleArea, 0, 0, 1, 1);
            handleArea.offsetMin = new Vector2(12, 0); handleArea.offsetMax = new Vector2(-12, 0);
            var handle = Rect("Handle", handleArea); handle.sizeDelta = new Vector2(24, 24);
            var handleImage = Image(handle, accentColor, true);
            var slider = rect.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.handleRect = handle;
            slider.targetGraphic = handleImage; slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            // 빈 트랙도 클릭할 수 있도록 투명 이미지를 둡니다.
            Image(rect, Color.clear, true);
            value = Text("Value", row, "100%", 24); Area(value.rectTransform, 0.85f, 0.04f, 1, 0.45f);
            return slider;
        }
        private TMP_Dropdown Dropdown(Transform parent)
        {
            var root = Rect("Dropdown", parent); var background = Image(root, Color.white, true);
            var label = Text("Label", root, "1920 x 1080", fontSize); Area(label.rectTransform, 0.08f, 0, 0.88f, 1);
            var arrow = Text("Arrow", root, "v", 24); Area(arrow.rectTransform, 0.88f, 0, 0.98f, 1);
            var template = Rect("Template", root);
            template.anchorMin = Vector2.zero; template.anchorMax = Vector2.right; template.pivot = new Vector2(0.5f, 1);
            template.sizeDelta = new Vector2(0, 260); template.anchoredPosition = new Vector2(0, -4); Image(template, Color.white, true);
            var viewport = Rect("Viewport", template); Area(viewport, 0, 0, 1, 1); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = new Vector2(0, 52);
            var item = Rect("Item", content); item.anchorMin = new Vector2(0, 0.5f); item.anchorMax = new Vector2(1, 0.5f);
            item.sizeDelta = new Vector2(0, 48);
            var itemBackground = Image(item, new Color(0.94f, 0.95f, 0.92f), true);
            var check = Rect("Selected", item); Area(check, 0.025f, 0.3f, 0.045f, 0.7f);
            var toggle = item.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = itemBackground; toggle.graphic = Image(check, accentColor);
            var itemText = Text("Label", item, "1920 x 1080", fontSize); Area(itemText.rectTransform, 0.08f, 0, 0.94f, 1);
            var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.scrollSensitivity = 35;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var dropdown = root.gameObject.AddComponent<TMP_Dropdown>(); dropdown.targetGraphic = background;
            dropdown.template = template; dropdown.captionText = label; dropdown.itemText = itemText;
            dropdown.options.Clear(); dropdown.options.Add(new TMP_Dropdown.OptionData("1920 x 1080")); dropdown.RefreshShownValue();
            template.gameObject.SetActive(false);
            return dropdown;
        }
        private void CreditsBody(Transform parent)
        {
            var viewport = Rect("Scroll", parent); Area(viewport, 0.08f, 0.08f, 0.92f, 0.84f);
            Image(viewport, Color.clear, true); viewport.gameObject.AddComponent<RectMask2D>();
            var text = Text("Content", viewport, credits, fontSize);
            text.rectTransform.anchorMin = new Vector2(0, 1); text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 1); text.rectTransform.sizeDelta = Vector2.zero;
            text.alignment = TextAlignmentOptions.Top; text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            var fit = text.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.content = text.rectTransform; scroll.viewport = viewport;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        }
        private Button Button(string name, Transform parent, string label, Color color)
        {
            var rect = Rect(name, parent); var image = Image(rect, color, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var text = Text("Label", rect, label, fontSize); Area(text.rectTransform, 0.04f, 0, 0.96f, 1);
            return button;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Ink;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        private static RectTransform Row(string name, Transform parent, float height)
        {
            var rect = Rect(name, parent); rect.sizeDelta = new Vector2(0, height); return rect;
        }
        private static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        private static void Area(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void Set(SerializedObject fields, string name, UnityEngine.Object value) => fields.FindProperty(name).objectReferenceValue = value;
    }
}
