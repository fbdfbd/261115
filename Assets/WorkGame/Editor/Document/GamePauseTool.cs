using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class GamePauseTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(460, 760);
        [SerializeField] private Vector2 helpSize = new Vector2(660, 600);
        [SerializeField] private int fontSize = 30;
        [SerializeField] private float spacing = 18;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color buttonColor = new Color(0.89f, 0.91f, 0.87f);
        [SerializeField, TextArea(6, 16)] private string helpText = "업무\n서류를 열고 알맞은 대상을 길게 눌러 제출하세요.\n배정된 서류를 모두 처리하면 일찍 퇴근할 수 있어요.\n\n육성\n시간표에 수업 4개를 편성하고 다음 달로 진행하세요.\n가구와 강화로 달이의 성장을 도울 수 있어요.";
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;

        [MenuItem("Work Game/UI/Game Pause Tool")]
        public static void Open() => GetWindow<GamePauseTool>("공용 정지 메뉴");
        private void OnEnable()
        {
            if (pageSize.y < 730) pageSize.y = 760;
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("업무 · 육성 공용 정지 / 도움말", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            pageSize = EditorGUILayout.Vector2Field("정지창 크기", pageSize);
            helpSize = EditorGUILayout.Vector2Field("도움말 크기", helpSize);
            fontSize = EditorGUILayout.IntSlider("버튼 글자", fontSize, 26, 34);
            spacing = EditorGUILayout.Slider("버튼 간격", spacing, 10, 24);
            pageColor = EditorGUILayout.ColorField("창 배경", pageColor);
            buttonColor = EditorGUILayout.ColorField("버튼 배경", buttonColor);
            EditorGUILayout.LabelField("도움말 내용 — 생성 후 TMP에서도 수정 가능");
            helpText = EditorGUILayout.TextArea(helpText, GUILayout.MinHeight(130));
            EditorGUILayout.HelpBox("이어하기 / 저장하기 / 불러오기 / 설정 / 타이틀 순서입니다. 씬에서 Work Root, Options Root, Save Slots Root를 연결하세요.", MessageType.Info);
            float neededHeight = (320 + spacing * 5 + 12) / 0.58f;
            if (pageSize.y < neededHeight) EditorGUILayout.HelpBox("현재 간격에는 높이 " + Mathf.CeilToInt(neededHeight) + " 이상이 필요합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || pageSize.x < 360 || pageSize.y < neededHeight
                || helpSize.x < 500 || helpSize.y < 400))
                if (GUILayout.Button("공용 정지 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("정지 메뉴 저장", "GamePause", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("PauseBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "완료. 공용 UI Canvas에 배치하고 GamePauseRoot의 Work Root / Options Root를 연결하세요.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("GamePause", parent); Area(root, 0, 0, 1, 1);
            var view = root.gameObject.AddComponent<GamePauseView>();
            var controller = root.gameObject.AddComponent<GamePauseRoot>();
            var pause = Rect("PausePanel", root); Area(pause, 0, 0, 1, 1);
            var pauseGroup = pause.gameObject.AddComponent<CanvasGroup>();
            var background = Background(pause);
            var page = Page(pause, pageSize);
            var title = Text("Title", page, "잠시 쉬어가기", 40); Area(title.rectTransform, 0.05f, 0.8f, 0.95f, 0.95f);
            var body = Rect("Body", page); Area(body, 0.1f, 0.18f, 0.9f, 0.76f);
            var layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var resume = Button("Resume", body, "이어하기");
            var save = Button("Save", body, "저장하기");
            var load = Button("Load", body, "불러오기");
            var settings = Button("Settings", body, "설정");
            var gap = Rect("TitleGap", body); gap.sizeDelta = new Vector2(0, 12);
            var titleButton = Button("ReturnToTitle", body, "타이틀로 돌아가기");
            var error = Text("Error", page, "", 22); Area(error.rectTransform, 0.06f, 0.03f, 0.94f, 0.16f);
            error.color = new Color(0.7f, 0.2f, 0.2f);

            var help = Rect("HelpPanel", root); Area(help, 0, 0, 1, 1);
            var helpGroup = help.gameObject.AddComponent<CanvasGroup>();
            var helpBackground = Background(help);
            var helpPage = Page(help, helpSize);
            title = Text("Title", helpPage, "도움말", 40); Area(title.rectTransform, 0.2f, 0.86f, 0.8f, 0.97f);
            var helpClose = Button("Close", helpPage, "닫기"); Area((RectTransform)helpClose.transform, 0.8f, 0.9f, 0.97f, 0.97f);
            helpClose.GetComponentInChildren<TMP_Text>().fontSize = 24;
            HelpBody(helpPage);

            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", pauseGroup); Set(fields, "resumeButton", resume); Set(fields, "backgroundButton", background);
            Set(fields, "settingsButton", settings); Set(fields, "saveButton", save); Set(fields, "loadButton", load);
            Set(fields, "titleButton", titleButton); Set(fields, "errorText", error);
            Set(fields, "helpGroup", helpGroup); Set(fields, "helpCloseButton", helpClose); Set(fields, "helpBackgroundButton", helpBackground);
            fields.ApplyModifiedPropertiesWithoutUndo();
            fields = new SerializedObject(controller); Set(fields, "view", view); fields.ApplyModifiedPropertiesWithoutUndo();
            helpGroup.alpha = 0; helpGroup.interactable = helpGroup.blocksRaycasts = false;
            return root;
        }
        private void HelpBody(Transform parent)
        {
            var viewport = Rect("Scroll", parent); Area(viewport, 0.08f, 0.08f, 0.92f, 0.82f);
            Image(viewport, Color.clear, true); viewport.gameObject.AddComponent<RectMask2D>();
            var text = Text("Content", viewport, helpText, 28);
            text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.anchorMin = new Vector2(0, 1); text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 1); text.rectTransform.sizeDelta = Vector2.zero;
            var fit = text.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = text.rectTransform;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        }
        private RectTransform Page(Transform parent, Vector2 size)
        {
            var page = Rect("Page", parent); page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = size; Image(page, pageColor, true); return page;
        }
        private Button Background(Transform parent)
        {
            var rect = Rect("BackgroundClose", parent); Area(rect, 0, 0, 1, 1);
            var image = Image(rect, new Color(0, 0, 0, 0.72f), true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            return button;
        }
        private Button Button(string name, Transform parent, string label)
        {
            var rect = Rect(name, parent); rect.sizeDelta = new Vector2(0, 64);
            var image = Image(rect, buttonColor, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var text = Text("Label", rect, label, fontSize); Area(text.rectTransform, 0.04f, 0, 0.96f, 1); return button;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value;
            text.fontSize = size; text.color = Ink; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
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
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void Set(SerializedObject fields, string name, UnityEngine.Object value) => fields.FindProperty(name).objectReferenceValue = value;
    }
}
