using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareEndingTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(1300, 800);
        [SerializeField, Range(0.3f, 0.6f)] private float imageRatio = 0.44f;
        [SerializeField] private int fontSize = 28;
        [SerializeField] private float characterSeconds = 0.035f;
        [SerializeField] private Color paperColor = new Color(0.98f, 0.97f, 0.94f);
        [SerializeField] private Color frameColor = new Color(0.85f, 0.88f, 0.84f);
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;

        [MenuItem("Work Game/UI/Care Ending Tool")]
        public static void Open() => GetWindow<CareEndingTool>("달이 엔딩 편지");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("좌측 이미지 · 우측 편지", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            pageSize = EditorGUILayout.Vector2Field("페이지 크기", pageSize);
            imageRatio = EditorGUILayout.Slider("좌측 이미지 비율", imageRatio, 0.3f, 0.6f);
            fontSize = EditorGUILayout.IntSlider("편지 글자 크기", fontSize, 24, 36);
            characterSeconds = EditorGUILayout.Slider("한 글자 표시 간격", characterSeconds, 0.01f, 0.1f);
            paperColor = EditorGUILayout.ColorField("편지 배경", paperColor);
            frameColor = EditorGUILayout.ColorField("이미지 틀", frameColor);
            EditorGUILayout.HelpBox("한 글자씩 표시하고 줄 끝에서 멈춥니다. 출력 중 클릭하면 현재 줄을 완성하고, 다시 클릭하면 다음 줄을 시작합니다. 페이지 전체 출력 후 클릭하면 다음 페이지로 넘어갑니다. 마지막 편지까지 읽으면 타이틀 버튼을 표시합니다. 엔딩별 이미지·편지는 CareEndingSO.Pages에서 설정합니다. LayoutGroup·LayoutElement 없이 앵커로 배치합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || pageSize.x < 800 || pageSize.y < 500))
                if (GUILayout.Button("엔딩 패널 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("엔딩 패널 저장", "CareEnding", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("EndingBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave;
            parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. 씬의 CareEndingView를 WorkRoot에 연결하면 마지막 버튼의 타이틀 이동도 자동 연결됩니다.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally
            {
                foreach (var mask in parent.GetComponentsInChildren<RectMask2D>(true)) mask.enabled = false;
                DestroyImmediate(parent);
            }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("CareEnding", parent);
            Area(root, 0, 0, 1, 1);
            Image(root, new Color(0.08f, 0.11f, 0.09f, 0.95f), true);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var click = root.gameObject.AddComponent<CareDialogueClickArea>();
            var view = root.gameObject.AddComponent<CareEndingView>();
            var page = Rect("Page", root);
            page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = pageSize;
            Image(page, paperColor);

            var title = Text("Title", page, "달이가 보낸 편지", fontSize + 10);
            Area(title.rectTransform, 0.05f, 0.88f, 0.85f, 0.97f);
            var pageNumber = Text("PageNumber", page, "1 / 2", fontSize - 4);
            Area(pageNumber.rectTransform, 0.85f, 0.88f, 0.95f, 0.97f);
            pageNumber.alignment = TextAlignmentOptions.MidlineRight;
            var divider = Rect("HeaderDivider", page);
            Area(divider, 0.05f, 0.875f, 0.95f, 0.875f);
            divider.sizeDelta = new Vector2(0, 2);
            Image(divider, frameColor);

            var body = Rect("Body", page);
            Area(body, 0.05f, 0.15f, 0.95f, 0.85f);
            var frame = Rect("PictureFrame", body);
            Area(frame, 0, 0, imageRatio - 0.02f, 1);
            Image(frame, frameColor);
            var pictureRect = Rect("Picture", frame);
            Area(pictureRect, 0, 0, 1, 1, 10);
            var pictureGroup = pictureRect.gameObject.AddComponent<CanvasGroup>();
            var picture = Image(pictureRect, Color.white);
            picture.preserveAspect = true; picture.enabled = false;

            var letter = Rect("Letter", body);
            Area(letter, imageRatio + 0.02f, 0, 1, 1);
            Image(letter, new Color(1, 1, 1, 0.5f));
            var scroll = letter.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24;
            var viewport = Rect("Viewport", letter);
            Area(viewport, 0, 0, 1, 1, 24);
            Image(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Text("LetterText", viewport, "나르미에게.\n\n달이의 편지가 한 글자씩 나타납니다.", fontSize);
            Area(text.rectTransform, 0, 1, 1, 1);
            text.rectTransform.pivot = new Vector2(0.5f, 1);
            text.rectTransform.sizeDelta = new Vector2(0, 600);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 12;
            text.overflowMode = TextOverflowModes.Overflow;
            scroll.viewport = viewport; scroll.content = text.rectTransform;

            var hint = Text("ContinueHint", page, "화면을 눌러 계속", fontSize - 6);
            Area(hint.rectTransform, 0.05f, 0.035f, 0.7f, 0.12f);
            var titleButtonRect = Rect("TitleButton", page);
            Area(titleButtonRect, 0.72f, 0.035f, 0.95f, 0.12f);
            var buttonImage = Image(titleButtonRect, frameColor, true);
            var titleButton = titleButtonRect.gameObject.AddComponent<Button>();
            titleButton.targetGraphic = buttonImage;
            var label = Text("Text", titleButtonRect, "타이틀로", fontSize);
            Area(label.rectTransform, 0, 0, 1, 1, 6);
            label.alignment = TextAlignmentOptions.Center;
            titleButtonRect.gameObject.SetActive(false);

            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "clickArea", click);
            Set(fields, "picture", picture); Set(fields, "pictureGroup", pictureGroup);
            Set(fields, "titleText", title); Set(fields, "letterText", text); Set(fields, "pageText", pageNumber);
            Set(fields, "letterScroll", scroll); Set(fields, "hintText", hint); Set(fields, "titleButton", titleButton);
            fields.FindProperty("characterSeconds").floatValue = characterSeconds;
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Ink;
            text.richText = false; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
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
        private static void Area(RectTransform rect, float x0, float y0, float x1, float y1, float padding = 0)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding;
        }
        private static void Set(SerializedObject fields, string name, UnityEngine.Object value) => fields.FindProperty(name).objectReferenceValue = value;
    }
}
