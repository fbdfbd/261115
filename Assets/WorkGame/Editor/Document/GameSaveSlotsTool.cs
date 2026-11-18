using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class GameSaveSlotsTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(1000, 800);
        [SerializeField] private float slotHeight = 96;
        [SerializeField] private float spacing = 12;
        [SerializeField] private int fontSize = 26;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color slotColor = new Color(0.89f, 0.91f, 0.87f);
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;

        [MenuItem("Work Game/UI/Game Save Slots Tool")]
        public static void Open() => GetWindow<GameSaveSlotsTool>("이어하기 슬롯");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("중앙 정렬 · 자동 1개 + 수동 4개", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            pageSize = EditorGUILayout.Vector2Field("창 크기", pageSize);
            slotHeight = EditorGUILayout.Slider("슬롯 높이", slotHeight, 88, 130);
            spacing = EditorGUILayout.Slider("슬롯 간격", spacing, 8, 24);
            fontSize = EditorGUILayout.IntSlider("능력치 글자", fontSize, 24, 30);
            pageColor = EditorGUILayout.ColorField("창 배경", pageColor);
            slotColor = EditorGUILayout.ColorField("슬롯 배경", slotColor);
            EditorGUILayout.HelpBox("저장/불러오기에 같은 패널을 사용합니다. 월과 대표 스탯 4개만 표시합니다. 저장은 수동 4칸, 불러오기는 저장된 5칸을 선택할 수 있습니다. View·Root·카드 5개는 자동 연결합니다.", MessageType.Info);
            float neededHeight = (slotHeight * 5 + spacing * 4) / 0.71f;
            if (pageSize.y < neededHeight) EditorGUILayout.HelpBox("현재 설정에는 창 높이가 " + Mathf.CeilToInt(neededHeight) + " 이상 필요합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || pageSize.x < 900 || pageSize.y < neededHeight))
                if (GUILayout.Button("이어하기 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("이어하기 저장", "GameSaveSlots", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("SaveSlotsBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "완료. Canvas에 배치하고 나중에 이어하기 버튼을 Root의 Open Buttons에 연결하세요. OpenLoad()로도 열 수 있습니다.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("GameSaveSlots", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<GameSaveSlotsView>();
            var controller = root.gameObject.AddComponent<GameSaveSlotsRoot>();
            var background = Rect("BackgroundClose", root); Area(background, 0, 0, 1, 1);
            var backgroundImage = Image(background, new Color(0, 0, 0, 0.72f), true);
            var backgroundButton = background.gameObject.AddComponent<Button>();
            backgroundButton.targetGraphic = backgroundImage; backgroundButton.transition = Selectable.Transition.None;
            var page = Rect("Page", root); page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = pageSize; Image(page, pageColor, true);
            var title = Text("Title", page, "이어하기", 44); Area(title.rectTransform, 0.2f, 0.86f, 0.8f, 0.97f);
            var close = Rect("Close", page); Area(close, 0.85f, 0.91f, 0.96f, 0.97f);
            var closeImage = Image(close, slotColor, true); var closeButton = close.gameObject.AddComponent<Button>(); closeButton.targetGraphic = closeImage;
            var closeLabel = Text("Label", close, "닫기", 24); Area(closeLabel.rectTransform, 0, 0, 1, 1);

            var body = Rect("Slots", page); Area(body, 0.06f, 0.12f, 0.94f, 0.83f);
            var layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var cards = new List<GameSaveSlotItemView>();
            for (int i = 0; i < 5; i++) cards.Add(Card(body, i));
            var error = Text("Error", page, "", 24); Area(error.rectTransform, 0.06f, 0.025f, 0.94f, 0.09f);
            error.color = new Color(0.7f, 0.2f, 0.2f);
            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "closeButton", closeButton);
            Set(fields, "backgroundButton", backgroundButton); Set(fields, "errorText", error); Set(fields, "titleText", title);
            var list = fields.FindProperty("slots"); list.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            fields.ApplyModifiedPropertiesWithoutUndo();
            fields = new SerializedObject(controller); Set(fields, "view", view); fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }
        private GameSaveSlotItemView Card(Transform parent, int index)
        {
            var root = Rect(index == 0 ? "AutoSave" : "Slot" + index, parent); root.sizeDelta = new Vector2(0, slotHeight);
            var image = Image(root, slotColor, true); var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Text("SlotName", root, index == 0 ? "자동 저장" : "슬롯 " + index, 28);
            Area(label.rectTransform, 0.02f, 0.1f, 0.18f, 0.9f);
            var month = Text("Month", root, "1월", 28); Area(month.rectTransform, 0.19f, 0.1f, 0.33f, 0.9f);
            var divider = Rect("Divider", root); Area(divider, 0.34f, 0.18f, 0.34f, 0.82f);
            divider.sizeDelta = new Vector2(1, 0); Image(divider, new Color(0.75f, 0.79f, 0.74f));
            var values = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                values[i] = Text(((CareStat)i).ToString(), root, CareStatNames.GetName((CareStat)i) + "\n0", fontSize);
                Area(values[i].rectTransform, 0.35f + i * 0.16f, 0.08f, 0.51f + i * 0.16f, 0.92f);
            }
            var view = root.gameObject.AddComponent<GameSaveSlotItemView>();
            var fields = new SerializedObject(view);
            Set(fields, "button", button); Set(fields, "slotText", label); Set(fields, "monthText", month);
            var list = fields.FindProperty("statTexts"); list.arraySize = 4;
            for (int i = 0; i < 4; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            fields.ApplyModifiedPropertiesWithoutUndo(); return view;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Ink;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
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
