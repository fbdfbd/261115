using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareDialogueTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 dialogueSize = new Vector2(1300, 260);
        [SerializeField] private Vector2 logSize = new Vector2(1100, 760);
        [SerializeField] private Vector2 choiceSize = new Vector2(760, 96);
        [SerializeField] private float bottomMargin = 32;
        [SerializeField] private float choiceGap = 12;
        [SerializeField] private int fontSize = 28;
        [SerializeField] private int choiceFontSize = 32;
        [SerializeField] private int speakerFontSize = 46;
        [SerializeField] private int layoutVersion;
        [SerializeField] private Color panelColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color buttonColor = new Color(0.87f, 0.91f, 0.86f);
        private string message;
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.18f);

        [MenuItem("Work Game/UI/Care Dialogue Tool")]
        public static void Open() => GetWindow<CareDialogueTool>("달이 대화창");
        private void OnEnable()
        {
            if (layoutVersion < 1)
            {
                choiceSize.y = Mathf.Max(96, choiceSize.y);
                choiceFontSize = 32; speakerFontSize = 46; layoutVersion = 1;
            }
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("대화창 + 내부 로그 패널", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            fontSize = EditorGUILayout.IntSlider("본문 크기", fontSize, 20, 36);
            choiceFontSize = EditorGUILayout.IntSlider("선택지 글자 크기", choiceFontSize, 20, 48);
            speakerFontSize = EditorGUILayout.IntSlider("화자 이름 크기", speakerFontSize, 24, 60);
            dialogueSize = EditorGUILayout.Vector2Field("하단 대화 패널 크기", dialogueSize);
            bottomMargin = EditorGUILayout.Slider("화면 아래 여백", bottomMargin, 0, 100);
            choiceSize = EditorGUILayout.Vector2Field("선택지 한 칸 크기", choiceSize);
            choiceGap = EditorGUILayout.Slider("선택지 간격", choiceGap, 0, 24);
            logSize = EditorGUILayout.Vector2Field("로그 패널 크기", logSize);
            panelColor = EditorGUILayout.ColorField("패널 배경", panelColor);
            buttonColor = EditorGUILayout.ColorField("버튼 배경", buttonColor);
            EditorGUILayout.HelpBox("전체 화면 클릭 영역, 하단 대화 패널·이름, 선택지 3개, 로그·스킵 버튼, 내부 세로 스크롤 로그를 생성합니다. LayoutGroup·LayoutElement 없이 앵커로 배치합니다. 정지·나가기 버튼은 씬에서 Ignored Click Areas에 연결하세요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null
                || dialogueSize.x < 700 || dialogueSize.y < 180 || logSize.x < 600 || logSize.y < 400
                || choiceSize.x < 400 || choiceSize.y < 48))
                if (GUILayout.Button("대화창 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("대화창 저장", "CareDialogue", "prefab",
                "대화창 프리팹 저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("DialogueBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                var root = Build(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. WorkRoot의 Care Dialogue View에 씬의 View를 연결하세요.";
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
            var root = Rect("CareDialogue", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<CareDialogueView>();
            var screen = Rect("ScreenClick", root); Area(screen, 0, 0, 1, 1);
            Image(screen, Color.clear, true);
            var click = screen.gameObject.AddComponent<CareDialogueClickArea>();
            var panel = Rect("DialoguePanel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0);
            panel.pivot = new Vector2(0.5f, 0); panel.sizeDelta = dialogueSize;
            panel.anchoredPosition = new Vector2(0, bottomMargin);
            Image(panel, panelColor);
            var namePlate = Rect("NamePlate", panel);
            namePlate.anchorMin = namePlate.anchorMax = new Vector2(0, 1); namePlate.pivot = Vector2.zero;
            namePlate.sizeDelta = new Vector2(260, 80); namePlate.anchoredPosition = new Vector2(24, 0);
            Image(namePlate, buttonColor);
            var speaker = Text("Speaker", namePlate, "달이", speakerFontSize);
            Area(speaker.rectTransform, 0, 0, 1, 1, 6); speaker.alignment = TextAlignmentOptions.Center;
            var body = Text("Body", panel, "나르미, 왔어? 오늘 이야기도 같이 들어 줘.", fontSize);
            Area(body.rectTransform, 0, 0, 1, 1);
            body.rectTransform.offsetMin = new Vector2(32, 56); body.rectTransform.offsetMax = new Vector2(-32, -28);
            body.alignment = TextAlignmentOptions.TopLeft; body.richText = false;
            var hint = Text("ContinueHint", panel, "화면을 눌러 계속", fontSize - 8);
            Area(hint.rectTransform, 0, 0, 1, 0);
            hint.rectTransform.offsetMin = new Vector2(24, 12); hint.rectTransform.offsetMax = new Vector2(-24, 44);
            hint.alignment = TextAlignmentOptions.MidlineRight;

            var toolbar = Rect("Toolbar", panel); Area(toolbar, 0.62f, 1, 1, 1);
            toolbar.offsetMin = new Vector2(0, 8); toolbar.offsetMax = new Vector2(-24, 56);
            var logButton = Button("Log", toolbar, "로그"); Area((RectTransform)logButton.transform, 0, 0, 0.47f, 1);
            var skip = Button("Skip", toolbar, "스킵"); Area((RectTransform)skip.transform, 0.53f, 0, 1, 1);
            skip.interactable = false;
            var choicesRoot = Rect("Choices", root);
            choicesRoot.anchorMin = choicesRoot.anchorMax = choicesRoot.pivot = new Vector2(0.5f, 0.5f);
            choicesRoot.sizeDelta = new Vector2(choiceSize.x, (choiceSize.y + choiceGap) * 3);
            choicesRoot.anchoredPosition = Vector2.zero;
            var choiceButtons = new List<Button>();
            var choiceTexts = new List<TMP_Text>();
            for (int i = 0; i < 3; i++)
            {
                var button = Button("Choice_" + (i + 1), choicesRoot, "선택지 " + (i + 1));
                var rect = (RectTransform)button.transform;
                Area(rect, 0, 1 - (i + 1f) / 3, 1, 1 - i / 3f);
                rect.offsetMin = new Vector2(0, choiceGap / 2); rect.offsetMax = new Vector2(0, -choiceGap / 2);
                var choiceText = button.GetComponentInChildren<TMP_Text>();
                choiceText.fontSize = choiceFontSize;
                choiceButtons.Add(button); choiceTexts.Add(choiceText);
            }

            var logRoot = Rect("LogPanel", root); Area(logRoot, 0, 0, 1, 1);
            var logGroup = logRoot.gameObject.AddComponent<CanvasGroup>();
            logGroup.alpha = 0; logGroup.interactable = false; logGroup.blocksRaycasts = false;
            var outside = Button("Outside", logRoot, "");
            Area((RectTransform)outside.transform, 0, 0, 1, 1);
            outside.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var logPage = Rect("Page", logRoot);
            logPage.anchorMin = logPage.anchorMax = logPage.pivot = new Vector2(0.5f, 0.5f); logPage.sizeDelta = logSize;
            Image(logPage, panelColor, true);
            var logTitle = Text("Title", logPage, "대화 기록", fontSize + 4);
            Area(logTitle.rectTransform, 0, 1, 0.7f, 1);
            logTitle.rectTransform.offsetMin = new Vector2(24, -68); logTitle.rectTransform.offsetMax = new Vector2(0, -16);
            var close = Button("Close", logPage, "닫기");
            Area((RectTransform)close.transform, 0.82f, 1, 1, 1);
            ((RectTransform)close.transform).offsetMin = new Vector2(0, -64);
            ((RectTransform)close.transform).offsetMax = new Vector2(-24, -16);
            var scrollRoot = Rect("Scroll", logPage); Area(scrollRoot, 0, 0, 1, 1);
            scrollRoot.offsetMin = new Vector2(24, 24); scrollRoot.offsetMax = new Vector2(-24, -84);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            var viewport = Rect("Viewport", scrollRoot); Area(viewport, 0, 0, 1, 1);
            Image(viewport, new Color(1, 1, 1, 0.03f), true); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport); Area(content, 0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = new Vector2(0, 500);
            var logText = Text("Text", content, "", fontSize - 4);
            Area(logText.rectTransform, 0, 0, 1, 1, 16); logText.alignment = TextAlignmentOptions.TopLeft; logText.richText = false;
            scroll.viewport = viewport; scroll.content = content;

            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "clickArea", click); Set(fields, "speakerText", speaker);
            Set(fields, "namePlate", namePlate.gameObject);
            Set(fields, "bodyText", body); Set(fields, "continueHint", hint.gameObject);
            Set(fields, "logButton", logButton); Set(fields, "skipButton", skip);
            SetList(fields, "choiceButtons", choiceButtons.ToArray()); SetList(fields, "choiceTexts", choiceTexts.ToArray());
            Set(fields, "choicesRoot", choicesRoot);
            Set(fields, "logGroup", logGroup); Set(fields, "logText", logText); Set(fields, "logContent", content);
            Set(fields, "logViewport", viewport); Set(fields, "logScroll", scroll);
            SetList(fields, "logCloseButtons", new UnityEngine.Object[] { outside, close });
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private Button Button(string name, Transform parent, string value)
        {
            var rect = Rect(name, parent); var image = Image(rect, buttonColor, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            if (!string.IsNullOrEmpty(value))
            {
                var text = Text("Text", rect, value, fontSize - 4);
                Area(text.rectTransform, 0, 0, 1, 1, 8); text.alignment = TextAlignmentOptions.Center;
            }
            return button;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Ink;
            text.raycastTarget = false; text.alignment = TextAlignmentOptions.MidlineLeft; return text;
        }
        private static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        private static void Area(RectTransform rect, float x0, float y0, float x1, float y1, float padding = 0)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding;
        }
        private static void Set(SerializedObject target, string name, UnityEngine.Object value) => target.FindProperty(name).objectReferenceValue = value;
        private static void SetList(SerializedObject target, string name, UnityEngine.Object[] values)
        {
            var field = target.FindProperty(name); field.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
