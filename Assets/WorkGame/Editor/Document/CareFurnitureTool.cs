using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareFurnitureTool : EditorWindow
    {
        [SerializeField] private CareConfigSO config;
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(1300, 800);
        [SerializeField] private Vector2 nodeSize = new Vector2(190, 190);
        [SerializeField] private float equippedShare = 0.22f;
        [SerializeField] private float gap = 12;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color rowColor = new Color(0.9f, 0.92f, 0.88f);
        [SerializeField] private Color nodeColor = new Color(0.95f, 0.95f, 0.92f);
        private string message;
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);

        [MenuItem("Work Game/UI/Care Furniture Tool")]
        public static void Open() => GetWindow<CareFurnitureTool>("달이 가구함");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (config == null) config = AssetDatabase.LoadAssetAtPath<CareConfigSO>("Assets/WorkGame/Data/Care/CareConfig.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("1. 공통 가구 노드", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            fontSize = EditorGUILayout.IntSlider("본문 글자 크기", fontSize, 18, 30);
            nodeSize = EditorGUILayout.Vector2Field("노드 기준 크기", nodeSize);
            nodeColor = EditorGUILayout.ColorField("노드 배경", nodeColor);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || nodeSize.x < 120 || nodeSize.y < 140))
                if (GUILayout.Button("가구 노드 프리팹 생성")) Save(true);
            nodePrefab = (GameObject)EditorGUILayout.ObjectField("사용할 노드 프리팹", nodePrefab, typeof(GameObject), false);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. 가구함 페이지", EditorStyles.boldLabel);
            config = (CareConfigSO)EditorGUILayout.ObjectField("육성 Config", config, typeof(CareConfigSO), false);
            pageSize = EditorGUILayout.Vector2Field("페이지 크기", pageSize);
            equippedShare = EditorGUILayout.Slider("왼쪽 장착 칸 비율", equippedShare, 0.16f, 0.3f);
            gap = EditorGUILayout.Slider("행·노드 간격", gap, 0, 24);
            pageColor = EditorGUILayout.ColorField("페이지 배경", pageColor);
            rowColor = EditorGUILayout.ColorField("행 배경", rowColor);
            EditorGUILayout.HelpBox("기본 1300×800. 책상·장비·의자 3행이며 현재 Config의 물품을 배치합니다. LayoutGroup·LayoutElement 없이 앵커로 배치합니다. 생성 후 Page 크기와 노드 내부 앵커를 직접 수정할 수 있습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || config == null
                || nodePrefab == null || pageSize.x < 900 || pageSize.y < 600))
                if (GUILayout.Button("가구함 프리팹 생성", GUILayout.Height(32))) Save(false);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void Save(bool node)
        {
            string path = EditorUtility.SaveFilePanelInProject("가구 UI 저장", node ? "CareFurnitureNode" : "CareFurniture",
                "prefab", "저장할 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("FurnitureBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave;
            parent.SetActive(false);
            try
            {
                if (!node && nodePrefab.GetComponent<CareFurnitureNodeView>() == null)
                    throw new InvalidOperationException("CareFurnitureNodeView가 있는 노드 프리팹을 지정하세요.");
                if (!node && (config.Gifts == null || config.Gifts.Any(x => x == null)))
                    throw new InvalidOperationException("Config의 선물 목록에 비어 있는 항목이 있습니다.");
                var root = node ? BuildNode(parent.transform) : BuildPage(parent.transform);
                foreach (var part in root.GetComponentsInChildren<Component>(true))
                    if (part != null && PrefabUtility.IsPartOfPrefabInstance(part)) PrefabUtility.RecordPrefabInstancePropertyModifications(part);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                if (node) nodePrefab = prefab;
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = node ? "노드 생성 완료. 이 프리팹으로 페이지를 생성하세요." : "가구함 생성 완료. 씬 View와 진입 버튼을 WorkRoot에 연결하세요.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally { DestroyImmediate(parent); }
        }

        private RectTransform BuildNode(Transform parent)
        {
            var root = Rect("CareFurnitureNode", parent); root.sizeDelta = nodeSize;
            var background = Image(root, nodeColor, true);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.27f, 0.53f, 0.31f); outline.effectDistance = new Vector2(2, -2); outline.enabled = false;
            var fill = Rect("BuyFill", root); Area(fill, 0, 0, 1, 1);
            var fillImage = Image(fill, new Color(0.38f, 0.65f, 0.43f, 0.35f));
            fillImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/white.png");
            if (fillImage.sprite == null) fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
            fillImage.fillAmount = 0;
            var name = Text("Name", root, "가구 이름", fontSize - 2);
            Area(name.rectTransform, 0.05f, 0.7f, 0.95f, 0.98f); name.alignment = TextAlignmentOptions.Center;
            var iconRect = Rect("Icon", root); Area(iconRect, 0.1f, 0.23f, 0.9f, 0.68f);
            var icon = Image(iconRect, Color.white); icon.preserveAspect = true; icon.enabled = false;
            var price = Text("PriceOrState", root, "250", fontSize);
            Area(price.rectTransform, 0.05f, 0.02f, 0.95f, 0.22f); price.alignment = TextAlignmentOptions.Center;
            var hold = root.gameObject.AddComponent<WorkHoldButton>();
            var holdFields = new SerializedObject(hold);
            Set(holdFields, "button", button); Set(holdFields, "fill", fillImage);
            holdFields.ApplyModifiedPropertiesWithoutUndo();
            var view = root.gameObject.AddComponent<CareFurnitureNodeView>();
            var fields = new SerializedObject(view);
            Set(fields, "button", button); Set(fields, "holdButton", hold); Set(fields, "nameText", name);
            Set(fields, "priceText", price); Set(fields, "icon", icon); Set(fields, "background", background); Set(fields, "activeOutline", outline);
            fields.FindProperty("normalColor").colorValue = nodeColor;
            fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private RectTransform BuildPage(Transform parent)
        {
            var root = Rect("CareFurniture", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<CareFurnitureView>();
            var outside = Button("Outside", root, "", new Color(0, 0, 0, 0.55f));
            Area((RectTransform)outside.transform, 0, 0, 1, 1);
            var page = Rect("Page", root); page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = pageSize; Image(page, pageColor, true);
            var header = Rect("Header", page); Area(header, 0, 1, 1, 1);
            header.offsetMin = new Vector2(24, -72); header.offsetMax = new Vector2(-24, -16);
            var title = Text("Title", header, "달이의 가구함", fontSize + 10); Area(title.rectTransform, 0, 0, 0.45f, 1);
            var money = Text("Money", header, "보유 현금  0", fontSize);
            Area(money.rectTransform, 0.45f, 0, 0.84f, 1); money.alignment = TextAlignmentOptions.MidlineRight;
            var close = Button("Close", header, "닫기", rowColor); Area((RectTransform)close.transform, 0.88f, 0, 1, 1);
            var body = Rect("Rows", page); Area(body, 0, 0, 1, 1);
            body.offsetMin = new Vector2(24, 68); body.offsetMax = new Vector2(-24, -88);
            var nodes = new List<CareFurnitureNodeView>();
            var equipped = new List<CareEquippedItemView>();
            var kinds = (CareGiftKind[])Enum.GetValues(typeof(CareGiftKind));
            for (int i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                var row = Rect(kind.ToString(), body);
                Area(row, 0, 1 - (i + 1f) / kinds.Length, 1, 1 - i / (float)kinds.Length, gap / 2);
                Image(row, rowColor);
                var left = Rect("Equipped", row); Area(left, 0, 0, equippedShare, 1, 8);
                var label = Text("Kind", left, KindName(kind), fontSize);
                Area(label.rectTransform, 0, 0.82f, 1, 1); label.alignment = TextAlignmentOptions.Center;
                var slot = EquippedSlot(left, kind); Area((RectTransform)slot.transform, 0.06f, 0, 0.94f, 0.8f);
                equipped.Add(slot);
                var divider = Rect("Divider", row); Area(divider, equippedShare, 0.08f, equippedShare, 0.92f);
                divider.sizeDelta = new Vector2(1, 0); Image(divider, new Color(0.65f, 0.7f, 0.64f));
                var list = Rect("Items", row); Area(list, equippedShare, 0, 1, 1, 8);
                var gifts = config.Gifts.Where(x => x.Kind == kind).ToArray();
                for (int j = 0; j < gifts.Length; j++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, list);
                    instance.name = gifts[j].Id;
                    var rect = (RectTransform)instance.transform;
                    Area(rect, j / (float)gifts.Length, 0, (j + 1f) / gifts.Length, 1, gap / 2);
                    rect.localScale = Vector3.one;
                    var node = instance.GetComponent<CareFurnitureNodeView>();
                    var fields = new SerializedObject(node); Set(fields, "gift", gifts[j]); fields.ApplyModifiedPropertiesWithoutUndo();
                    node.Show(false, false, true);
                    nodes.Add(node);
                }
            }
            var hint = Text("Hint", page, "길게 눌러 구매 · 보유 가구는 눌러 장착·해제", fontSize - 2);
            Area(hint.rectTransform, 0, 0, 1, 0); hint.rectTransform.offsetMin = new Vector2(24, 16); hint.rectTransform.offsetMax = new Vector2(-24, 56);
            hint.alignment = TextAlignmentOptions.Center;
            var data = new SerializedObject(view);
            Set(data, "panelGroup", group); Set(data, "moneyText", money);
            SetList(data, "closeButtons", new UnityEngine.Object[] { outside, close });
            SetList(data, "nodes", nodes.ToArray()); SetList(data, "equipped", equipped.ToArray());
            data.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private CareEquippedItemView EquippedSlot(Transform parent, CareGiftKind kind)
        {
            var button = Button("Slot", parent, "", nodeColor); var rect = (RectTransform)button.transform;
            var iconRect = Rect("Icon", rect); Area(iconRect, 0.15f, 0.3f, 0.85f, 0.95f);
            var icon = Image(iconRect, Color.white); icon.preserveAspect = true; icon.enabled = false;
            var name = Text("ItemName", rect, "비어 있음", fontSize - 2);
            Area(name.rectTransform, 0.04f, 0, 0.96f, 0.28f); name.alignment = TextAlignmentOptions.Center;
            var view = rect.gameObject.AddComponent<CareEquippedItemView>();
            var fields = new SerializedObject(view); fields.FindProperty("kind").enumValueIndex = (int)kind;
            Set(fields, "button", button); Set(fields, "icon", icon); Set(fields, "nameText", name);
            fields.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private Button Button(string name, Transform parent, string label, Color color)
        {
            var rect = Rect(name, parent); var image = Image(rect, color, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text("Text", rect, label, fontSize - 2);
                Area(text.rectTransform, 0, 0, 1, 1, 4); text.alignment = TextAlignmentOptions.Center;
            }
            return button;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Ink;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false; return text;
        }
        private static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }
        private static string KindName(CareGiftKind kind) => kind == CareGiftKind.Desk ? "책상" : kind == CareGiftKind.Equipment ? "장비" : "의자";
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
