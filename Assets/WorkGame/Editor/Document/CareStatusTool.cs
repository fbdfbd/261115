using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareStatusTool : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(1300, 800);
        [SerializeField] private Vector2 hudSize = new Vector2(520, 300);
        [SerializeField] private float hudRowSpacing = 6;
        [SerializeField] private Color hudFillColor = new Color(0.39f, 0.62f, 0.45f);
        [SerializeField] private Color hudTrackColor = new Color(0.84f, 0.87f, 0.83f);
        [SerializeField, Range(0.28f, 0.45f)] private float leftShare = 0.36f;
        [SerializeField, Range(0.28f, 0.45f)] private float furnitureShare = 0.36f;
        [SerializeField] private int fontSize = 26;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color cardColor = new Color(0.92f, 0.93f, 0.9f);
        private string message;
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.19f);
        private static readonly Color[] Colors =
        {
            new Color(0.35f, 0.57f, 0.76f), new Color(0.38f, 0.65f, 0.48f),
            new Color(0.76f, 0.47f, 0.55f), new Color(0.77f, 0.62f, 0.32f)
        };

        [MenuItem("Work Game/UI/Care Status Tool")]
        public static void Open() => GetWindow<CareStatusTool>("달이의 상태");
        private void OnEnable()
        {
            if (hudSize.y < 240) hudSize = new Vector2(520, 300);
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("달이 HUD / 상태창", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            fontSize = EditorGUILayout.IntSlider("본문 크기", fontSize, 18, 32);
            pageSize = EditorGUILayout.Vector2Field("상태창 크기", pageSize);
            leftShare = EditorGUILayout.Slider("좌측 폭 비율", leftShare, 0.28f, 0.45f);
            furnitureShare = EditorGUILayout.Slider("좌측 가구 영역 높이 비율", furnitureShare, 0.28f, 0.45f);
            pageColor = EditorGUILayout.ColorField("창 배경", pageColor);
            cardColor = EditorGUILayout.ColorField("카드 배경", cardColor);
            EditorGUILayout.HelpBox("1300×800은 스킬트리 기본 크기와 같습니다. 생성 후 Page 크기를 바꾸면 앵커로 내부가 따라갑니다. LayoutGroup은 사용하지 않습니다. 가구 4자리 중 마지막은 빈 예비 자리이며, 실제 분류는 책상·장비·의자 3종입니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || pageSize.x < 900 || pageSize.y < 600))
                if (GUILayout.Button("상태창 프리팹 생성", GUILayout.Height(32))) Save(false);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HUD — 이름 · 등급 · 슬라이더 안 수치", EditorStyles.boldLabel);
            hudSize = EditorGUILayout.Vector2Field("HUD 크기", hudSize);
            if (GUILayout.Button("HUD 권장 크기 적용")) hudSize = new Vector2(520, 300);
            hudRowSpacing = EditorGUILayout.Slider("이름·등급·막대 간격", hudRowSpacing, 0, 24);
            hudFillColor = EditorGUILayout.ColorField("HUD 막대 색", hudFillColor);
            hudTrackColor = EditorGUILayout.ColorField("HUD 막대 배경", hudTrackColor);
            EditorGUILayout.HelpBox("세로 4줄입니다. 각 줄의 Horizontal Layout Group → Spacing / Padding으로 간격을 조절합니다. 이름·등급은 글자 폭을 따르고 막대가 남은 폭을 채웁니다. HUD 루트에는 배경용 투명 Image가 있습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null
                || hudSize.x < 360 || hudSize.y < 240))
                if (GUILayout.Button("주요 스탯 HUD 프리팹 생성", GUILayout.Height(32))) Save(true);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void Save(bool hud)
        {
            string path = EditorUtility.SaveFilePanelInProject("달이 UI 저장", hud ? "CareHud" : "CareStatus", "prefab",
                "프리팹 저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("CareStatusBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave;
            parent.SetActive(false);
            try
            {
                var root = hud ? BuildHud(parent.transform) : BuildStatus(parent.transform);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹을 저장하지 못했습니다.");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. 씬 Canvas에 배치한 View를 WorkRoot에 연결하세요.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally { DestroyImmediate(parent); }
        }

        private RectTransform BuildStatus(Transform parent)
        {
            var root = Rect("CareStatus", parent);
            Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<CareStatusView>();
            var outside = MakeButton("Outside", root, "", new Color(0, 0, 0, 0.55f));
            Area((RectTransform)outside.transform, 0, 0, 1, 1);
            var page = Rect("Page", root);
            page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = pageSize;
            Background(page, pageColor, true);
            var header = Rect("Header", page);
            Area(header, 0, 1, 1, 1);
            header.offsetMin = new Vector2(24, -72); header.offsetMax = new Vector2(-24, -16);
            var title = Text("Title", header, "달이의 상태", fontSize + 10);
            Area(title.rectTransform, 0, 0, 0.82f, 1);
            var close = MakeButton("Close", header, "닫기", cardColor);
            Area((RectTransform)close.transform, 0.88f, 0, 1, 1);

            var body = Rect("Body", page);
            Area(body, 0, 0, 1, 1);
            body.offsetMin = new Vector2(24, 24); body.offsetMax = new Vector2(-24, -88);
            var left = Rect("Left", body);
            Area(left, 0, 0, leftShare, 1); left.offsetMax = new Vector2(-12, 0);
            var right = Rect("Details", body);
            Area(right, leftShare, 0, 1, 1); right.offsetMin = new Vector2(12, 0);
            var summary = Rect("MainStats", left);
            Area(summary, 0, furnitureShare, 1, 1); summary.offsetMin = new Vector2(0, 12);
            var mainRows = new List<CareStatRowView>();
            for (int i = 0; i < 4; i++)
            {
                var row = MainRow(summary, (CareStat)i, Colors[i]);
                Area((RectTransform)row.transform, 0, 1 - (i + 1) / 4f, 1, 1 - i / 4f, 5);
                mainRows.Add(row);
            }

            var details = new List<CareStatRowView>();
            for (int i = 0; i < 4; i++)
            {
                var section = Rect("Group_" + ((CareStat)i), right);
                Area(section, 0, 1 - (i + 1) / 4f, 1, 1 - i / 4f, 5);
                Background(section, cardColor);
                var inner = Rect("Content", section); Area(inner, 0, 0, 1, 1, 12);
                var name = Text("Title", inner, CareStatNames.GetName((CareStat)i), fontSize);
                Area(name.rectTransform, 0, 0.78f, 1, 1);
                var line = Rect("Divider", inner); Area(line, 0, 0.76f, 1, 0.76f);
                line.sizeDelta = new Vector2(0, 1); Background(line, Colors[i]);
                for (int j = 0; j < 3; j++)
                {
                    var row = DetailRow(inner, (CareStat)(4 + i * 3 + j), Colors[i]);
                    Area((RectTransform)row.transform, 0, (2 - j) * 0.25f, 1, (3 - j) * 0.25f);
                    details.Add(row);
                }
            }

            var furniture = Rect("Equipped", left); Area(furniture, 0, 0, 1, furnitureShare);
            var label = Text("Title", furniture, "장착 중인 가구", fontSize - 2);
            Area(label.rectTransform, 0, 0.83f, 0.65f, 1);
            var open = MakeButton("OpenFurniture", furniture, "가구함", cardColor);
            Area((RectTransform)open.transform, 0.68f, 0.84f, 1, 1);
            var slots = new List<CareEquippedItemView>();
            for (int i = 0; i < 4; i++)
            {
                var slot = EquippedSlot(furniture, i);
                int column = i % 2, row = i / 2;
                Area((RectTransform)slot.transform, column * 0.5f, (1 - row) * 0.4f,
                    (column + 1) * 0.5f, (2 - row) * 0.4f, 4);
                slots.Add(slot);
            }
            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "furnitureButton", open);
            SetList(fields, "closeButtons", new UnityEngine.Object[] { outside, close });
            SetList(fields, "mainStats", mainRows.ToArray()); SetList(fields, "detailStats", details.ToArray());
            SetList(fields, "equipped", slots.ToArray());
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private RectTransform BuildHud(Transform parent)
        {
            var root = Rect("CareHud", parent);
            root.sizeDelta = hudSize;
            Background(root, Color.clear);
            var view = root.gameObject.AddComponent<CareHudView>();
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 48, 4);
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            var statsRoot = Rect("Stats", root);
            var stack = statsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 8;
            stack.childControlWidth = true; stack.childControlHeight = true;
            stack.childForceExpandWidth = true; stack.childForceExpandHeight = true;
            var rows = new List<CareStatRowView>();
            for (int i = 0; i < 4; i++)
            {
                var row = HudRow(statsRoot, (CareStat)i);
                rows.Add(row);
            }
            var open = MakeButton("Details", root, "자세히", cardColor);
            open.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Area((RectTransform)open.transform, 0.68f, 0.84f, 1, 1, 4);
            var fields = new SerializedObject(view);
            Set(fields, "detailsButton", open); SetList(fields, "mainStats", rows.ToArray());
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private CareStatRowView HudRow(Transform parent, CareStat stat)
        {
            var root = Rect(stat.ToString(), parent);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = hudRowSpacing;
            layout.padding = new RectOffset(0, 0, 6, 6);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            var name = Text("Name", root, CareStatNames.GetName(stat), fontSize);
            name.alignment = TextAlignmentOptions.Center;
            var grade = Text("Grade", root, "C", fontSize + 6);
            grade.alignment = TextAlignmentOptions.Center;
            var bar = Rect("Slider", root);
            var barSize = bar.gameObject.AddComponent<LayoutElement>();
            barSize.minWidth = 64; barSize.preferredWidth = 0; barSize.flexibleWidth = 1;
            var back = Rect("Background", bar); Area(back, 0, 0, 1, 1);
            Background(back, hudTrackColor);
            var fill = Rect("Fill", bar); Area(fill, 0, 0, 1, 1);
            Background(fill, hudFillColor);
            var slider = bar.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
            slider.transition = Selectable.Transition.None; slider.interactable = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.SetValueWithoutNotify(0);
            var value = Text("Value", bar, "0", fontSize - 2);
            Area(value.rectTransform, 0, 0, 1, 1);
            value.alignment = TextAlignmentOptions.Center;
            return BindRow(root, stat, name, value, grade, slider);
        }

        private CareStatRowView MainRow(Transform parent, CareStat stat, Color color)
        {
            var root = Rect(stat.ToString(), parent);
            Background(root, cardColor);
            var accent = Rect("Accent", root); Area(accent, 0, 0, 0, 1); accent.sizeDelta = new Vector2(4, 0);
            Background(accent, color);
            var name = Text("Name", root, CareStatNames.GetName(stat), fontSize);
            Area(name.rectTransform, 0.08f, 0.52f, 0.72f, 1);
            var value = Text("Value", root, "0", fontSize + 4);
            Area(value.rectTransform, 0.08f, 0, 0.72f, 0.55f);
            var grade = Text("Grade", root, "C", fontSize + 9);
            Area(grade.rectTransform, 0.72f, 0, 0.97f, 1); grade.alignment = TextAlignmentOptions.Center;
            return BindRow(root, stat, name, value, grade, null);
        }

        private CareStatRowView DetailRow(Transform parent, CareStat stat, Color color)
        {
            var root = Rect(stat.ToString(), parent);
            var name = Text("Name", root, CareStatNames.GetName(stat), fontSize - 3);
            Area(name.rectTransform, 0, 0, 0.26f, 1);
            var bar = Rect("Slider", root); Area(bar, 0.28f, 0.28f, 0.8f, 0.72f);
            var slider = bar.gameObject.AddComponent<Slider>();
            var back = Rect("Background", bar); Area(back, 0, 0, 1, 1); Background(back, new Color(0.82f, 0.84f, 0.8f));
            var fill = Rect("Fill", bar); Area(fill, 0, 0, 1, 1); Background(fill, color);
            slider.fillRect = fill; slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 300; slider.wholeNumbers = true;
            slider.transition = Selectable.Transition.None; slider.interactable = false; slider.value = 0;
            var value = Text("Value", root, "0", fontSize - 2);
            Area(value.rectTransform, 0.82f, 0, 1, 1); value.alignment = TextAlignmentOptions.MidlineRight;
            return BindRow(root, stat, name, value, null, slider);
        }

        private CareStatRowView BindRow(RectTransform root, CareStat stat, TMP_Text name, TMP_Text value, TMP_Text grade, Slider slider)
        {
            var view = root.gameObject.AddComponent<CareStatRowView>();
            var fields = new SerializedObject(view);
            fields.FindProperty("stat").enumValueIndex = (int)stat;
            Set(fields, "nameText", name); Set(fields, "valueText", value); Set(fields, "gradeText", grade); Set(fields, "slider", slider);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private CareEquippedItemView EquippedSlot(Transform parent, int index)
        {
            var button = MakeButton("Slot_" + index, parent, "", cardColor);
            var root = (RectTransform)button.transform;
            var kind = Text("Kind", root, index == 0 ? "책상" : index == 1 ? "장비" : index == 2 ? "의자" : "—", fontSize - 7);
            Area(kind.rectTransform, 0.06f, 0.65f, 0.94f, 1);
            var iconRect = Rect("Icon", root); Area(iconRect, 0.06f, 0.12f, 0.3f, 0.62f);
            var icon = Background(iconRect, Color.white); icon.preserveAspect = true; icon.enabled = false;
            var name = Text("Name", root, "비어 있음", fontSize - 6);
            Area(name.rectTransform, 0.34f, 0.05f, 0.95f, 0.68f);
            var view = root.gameObject.AddComponent<CareEquippedItemView>();
            var fields = new SerializedObject(view);
            fields.FindProperty("kind").enumValueIndex = Math.Min(index, 2);
            fields.FindProperty("reserved").boolValue = index == 3;
            Set(fields, "button", button); Set(fields, "icon", icon); Set(fields, "nameText", name);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private Button MakeButton(string name, Transform parent, string label, Color color)
        {
            var root = Rect(name, parent);
            var image = Background(root, color, true);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text("Text", root, label, fontSize - 4);
                Area(text.rectTransform, 0, 0, 1, 1, 4); text.alignment = TextAlignmentOptions.Center;
            }
            return button;
        }

        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = Ink; text.text = value;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            return text;
        }
        private static Image Background(RectTransform root, Color color, bool raycast = false)
        {
            var image = root.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast;
            return image;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false); return root;
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
