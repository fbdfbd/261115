using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareEndingChoiceTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 cardSize = new Vector2(280, 380);
        [SerializeField] private int titleSize = 32;
        [SerializeField] private int descriptionSize = 24;
        [SerializeField] private float spacing = 20;
        [SerializeField] private Color backgroundColor = new Color(0.07f, 0.09f, 0.08f, 0.98f);
        [SerializeField] private Color cardColor = new Color(0.97f, 0.96f, 0.93f);
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;

        [MenuItem("Work Game/UI/Care Ending Choice Tool")]
        public static void Open() => GetWindow<CareEndingChoiceTool>("엔딩 선택");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("풀스크린 · 중앙 정렬 · 선택 카드 5개", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            cardSize = EditorGUILayout.Vector2Field("카드 크기", cardSize);
            titleSize = EditorGUILayout.IntSlider("엔딩 이름 크기", titleSize, 24, 42);
            descriptionSize = EditorGUILayout.IntSlider("설명 크기", descriptionSize, 20, 30);
            spacing = EditorGUILayout.Slider("카드 간격", spacing, 8, 48);
            backgroundColor = EditorGUILayout.ColorField("전체 배경", backgroundColor);
            cardColor = EditorGUILayout.ColorField("카드 배경", cardColor);
            EditorGUILayout.HelpBox("이미지 칸 없이 이름과 설명만 생성합니다. 후보 수에 따라 활성 카드만 중앙 정렬합니다. 안 본 엔딩은 ???, 본 엔딩은 이름을 표시합니다. 카드에는 Layout Element를 넣지 않습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || cardSize.x < 160 || cardSize.y < 240))
                if (GUILayout.Button("엔딩 선택 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("엔딩 선택 저장", "CareEndingChoice", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("EndingChoiceBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. 씬 View를 WorkRoot의 Care Ending Choice View에 연결하세요.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("CareEndingChoice", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<CareEndingChoiceView>();
            Image(root, backgroundColor, true);
            var title = Text("Title", root, "달이의 다음 이야기", 48);
            Area(title.rectTransform, 0.05f, 0.85f, 0.95f, 0.96f); title.color = cardColor;
            var subtitle = Text("Subtitle", root, "달이가 걸어갈 길을 골라 주세요", 28);
            Area(subtitle.rectTransform, 0.05f, 0.78f, 0.95f, 0.84f); subtitle.color = cardColor;
            var cardsRoot = Rect("Cards", root); Area(cardsRoot, 0.05f, 0.18f, 0.95f, 0.82f);
            var layout = cardsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            var cards = new List<CareEndingCardView>();
            for (int i = 0; i < 5; i++) cards.Add(BuildCard(cardsRoot, i));
            var hint = Text("Hint", root, "하나의 미래를 선택하세요", 24);
            Area(hint.rectTransform, 0.05f, 0.08f, 0.95f, 0.15f); hint.color = cardColor;
            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "cardsRoot", cardsRoot);
            fields.FindProperty("cardSize").vector2Value = cardSize;
            var list = fields.FindProperty("cards"); list.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }
        private CareEndingCardView BuildCard(Transform parent, int index)
        {
            var root = Rect("Card_" + (index + 1), parent); root.sizeDelta = cardSize;
            var background = Image(root, cardColor, true);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            var name = Text("Name", root, "???", titleSize);
            Area(name.rectTransform, 0.08f, 0.69f, 0.92f, 0.94f); FitText(name, 24, titleSize);
            var divider = Rect("Divider", root); Area(divider, 0.08f, 0.64f, 0.92f, 0.64f);
            divider.sizeDelta = new Vector2(0, 2); Image(divider, new Color(0.74f, 0.79f, 0.73f));
            var description = Text("Description", root, "달이가 선택할 수 있는 미래의 설명입니다.", descriptionSize);
            Area(description.rectTransform, 0.09f, 0.1f, 0.91f, 0.58f); FitText(description, 18, descriptionSize);
            var view = root.gameObject.AddComponent<CareEndingCardView>();
            var fields = new SerializedObject(view);
            Set(fields, "button", button); Set(fields, "nameText", name); Set(fields, "descriptionText", description);
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
        private static void FitText(TMP_Text text, int min, int max)
        {
            text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
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
