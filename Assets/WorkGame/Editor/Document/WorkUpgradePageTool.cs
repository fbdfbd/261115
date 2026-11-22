using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class WorkUpgradePageTool : EditorWindow
    {
        [SerializeField] private WorkUpgradeTreeSO tree;
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 pageSize = new Vector2(1300, 800);
        [SerializeField] private Vector2 nodeSize = new Vector2(152, 152);
        [SerializeField] private float topShare = 0.75f;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private float boxGap = 16;
        [SerializeField] private float detailWidth = 280;
        [SerializeField] private float strokeWidth = 3;
        [SerializeField] private int layoutVersion;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color boxColor = new Color(0.91f, 0.92f, 0.89f);
        [SerializeField] private Color lineColor = new Color(0.48f, 0.55f, 0.52f);
        private string message;
        private bool error;
        private Vector2 scroll;
        private const float Margin = 16;
        private const float Header = 56;
        private static readonly Color Ink = new Color(0.15f, 0.19f, 0.18f);

        private sealed class Family
        {
            public List<WorkUpgradeSO> Nodes;
            public readonly Dictionary<WorkUpgradeSO, float> Columns = new Dictionary<WorkUpgradeSO, float>();
            public readonly Dictionary<WorkUpgradeSO, int> Depths = new Dictionary<WorkUpgradeSO, int>();
            public int Leaves;
            public int Rows;
            public string Title;
        }

        [MenuItem("Work Game/UI/Upgrade Page Tool")]
        public static void Open() => GetWindow<WorkUpgradePageTool>("Upgrade Page");

        private void OnEnable()
        {
            if (layoutVersion < 2)
            {
                nodeSize = new Vector2(152, 152); fontSize = 24; topShare = 0.75f; boxGap = 16;
                layoutVersion = 2;
            }
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (tree == null) tree = AssetDatabase.LoadAssetAtPath<WorkUpgradeTreeSO>("Assets/WorkGame/Data/Upgrades/WorkUpgradeTree.asset");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("1. 공통 노드 프리팹", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            nodeSize = EditorGUILayout.Vector2Field("노드 크기", nodeSize);
            fontSize = EditorGUILayout.IntSlider("기준 글자 크기", fontSize, 16, 28);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null))
                if (GUILayout.Button("노드 프리팹 생성")) Run(SaveNode);
            nodePrefab = (GameObject)EditorGUILayout.ObjectField("사용할 노드 프리팹", nodePrefab, typeof(GameObject), false);
            EditorGUILayout.HelpBox("노드를 누르면 왼쪽에 상세가 표시되고, 0.5초 꾹 누르면 구매합니다. 드래그하면 구매를 취소하고 가로 스크롤합니다. 이전 버전 프리팹은 노드부터 다시 생성하세요.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. 트리 페이지", EditorStyles.boldLabel);
            tree = (WorkUpgradeTreeSO)EditorGUILayout.ObjectField("강화 트리 SO", tree, typeof(WorkUpgradeTreeSO), false);
            if (tree == null && GUILayout.Button("샘플 강화 SO 생성 메뉴 실행"))
                Run(() => {
                    EditorApplication.ExecuteMenuItem("Work Game/Upgrades/Create Sample Tree");
                    tree = AssetDatabase.LoadAssetAtPath<WorkUpgradeTreeSO>("Assets/WorkGame/Data/Upgrades/WorkUpgradeTree.asset");
                });
            pageSize = EditorGUILayout.Vector2Field("페이지 크기", pageSize);
            detailWidth = EditorGUILayout.Slider("왼쪽 상세 폭", detailWidth, 240, 400);
            topShare = EditorGUILayout.Slider("상단 트리 비율", topShare, 0.65f, 0.88f);
            boxGap = EditorGUILayout.Slider("박스 간격", boxGap, 0, 24);
            pageColor = EditorGUILayout.ColorField("페이지 색상", pageColor);
            boxColor = EditorGUILayout.ColorField("트리 박스 색상", boxColor);
            lineColor = EditorGUILayout.ColorField("연결선 색상", lineColor);
            strokeWidth = EditorGUILayout.Slider("공통 선 두께", strokeWidth, 1, 5);
            EditorGUILayout.HelpBox("왼쪽: 선택한 강화의 아이콘·제목·설명·클릭으로 전환하는 ON/OFF. 오른쪽 위: 트리 가로 스크롤. 오른쪽 아래: 단일 기능 가로 스크롤. LayoutGroup 없이 앵커로 배치합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || tree == null || nodePrefab == null || font == null))
                if (GUILayout.Button("노드 프리팹으로 페이지 생성", GUILayout.Height(32))) Run(SavePage);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if (GUILayout.Button("선택한 페이지·노드에 기본 꾸미기 적용")) Run(StyleSelected);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if (GUILayout.Button("선택한 페이지 UI·연결선 보정")) Run(RepairSelectedLinks);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, error ? MessageType.Error : MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void Run(Action action)
        {
            try { error = false; action(); }
            catch (Exception e) { error = true; message = e.Message; Debug.LogError(e.Message); }
        }

        private void RepairSelectedLinks()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) throw new InvalidOperationException("씬 또는 Project에서 강화 페이지를 선택하세요.");
            if (EditorUtility.IsPersistent(selected))
            {
                string path = AssetDatabase.GetAssetPath(selected);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { RepairLinks(root, false); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrepareUiCleanup(root); PrefabUtility.UnloadPrefabContents(root); }
            }
            else RepairLinks(selected, true);
            message = "연결선 및 스크롤 마스크 등록 갱신 완료. 씬에서 작업했다면 씬을 저장하세요.";
        }

        private static void RepairLinks(GameObject root, bool undo)
        {
            var masks = root.GetComponentsInChildren<RectMask2D>(true).Where(x => x.enabled).ToArray();
            foreach (var mask in masks) mask.enabled = false;
            try
            {
                foreach (var graphic in root.GetComponentsInChildren<WorkUpgradeLinksGraphic>(true))
                {
                    if (graphic.GetComponent<CanvasRenderer>() == null)
                    {
                        if (undo) Undo.AddComponent<CanvasRenderer>(graphic.gameObject);
                        else graphic.gameObject.AddComponent<CanvasRenderer>();
                    }
                    graphic.SetAllDirty();
                }
            }
            finally { foreach (var mask in masks) if (mask != null) mask.enabled = true; }
        }

        private RectTransform MakeBuildRoot()
        {
            var root = Make("__UpgradePrefabBuild");
            root.gameObject.SetActive(false);
            root.gameObject.hideFlags = HideFlags.HideAndDontSave;
            return root;
        }

        private static void PrepareUiCleanup(GameObject root)
        {
            if (root == null) return;
            // Unregister masks while every child RectTransform is still alive.
            foreach (var mask in root.GetComponentsInChildren<RectMask2D>(true)) mask.enabled = false;
            root.SetActive(false);
        }

        private static void DestroyBuild(RectTransform root)
        {
            if (root == null) return;
            PrepareUiCleanup(root.gameObject);
            DestroyImmediate(root.gameObject);
        }

        private void SaveNode()
        {
            int minHeight = Mathf.Max(144, Mathf.CeilToInt((fontSize - 2) * 2.5f + 96));
            if (nodeSize.x < 120 || nodeSize.y < minHeight)
                throw new InvalidOperationException($"현재 폰트의 노드 크기는 최소 120 × {minHeight}로 잡아주세요.");
            string path = EditorUtility.SaveFilePanelInProject("강화 노드 저장", "WorkUpgradeNode", "prefab", "노드를 저장하세요.", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var buildRoot = MakeBuildRoot();
            try
            {
                var root = BuildNode(buildRoot);
                nodePrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (nodePrefab == null) throw new InvalidOperationException("노드 저장에 실패했습니다.");
                Selection.activeObject = nodePrefab;
                message = "노드 프리팹 생성 완료. 꾸민 뒤 같은 프리팹으로 페이지를 생성하세요.";
            }
            finally { DestroyBuild(buildRoot); }
        }

        private GameObject BuildNode(Transform parent)
        {
            var root = Make("WorkUpgradeNode", parent);
            root.sizeDelta = nodeSize;
            var background = Image(root, Color.white, true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.88f, 0.96f, 0.91f);
            colors.disabledColor = new Color(0.72f, 0.75f, 0.72f);
            button.colors = colors;

            var fillRect = Make("HoldFill", root);
            Stretch(fillRect, Vector2.zero, Vector2.zero);
            var fill = Image(fillRect, new Color(0.25f, 0.65f, 0.4f, 0.25f), false);
            fill.sprite = FillSprite();
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            fill.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
            fill.fillAmount = 0;
            var selected = Make("Selected", root);
            Stretch(selected, Vector2.zero, Vector2.zero);
            Image(selected, new Color(0.25f, 0.65f, 0.4f, 0.15f), false);
            selected.gameObject.SetActive(false);

            var icon = Make("Icon", root);
            Place(icon, new Vector2(0.5f, 1), new Vector2(48, 48), new Vector2(0, -26));
            var iconImage = Image(icon, Color.white, false);
            iconImage.preserveAspect = true;
            iconImage.enabled = false;
            var name = Text("Name", root, "강화 이름", fontSize - 2, TextAlignmentOptions.Center);
            Stretch((RectTransform)name.transform, new Vector2(6, 42), new Vector2(-6, -54));
            var cost = Text("Cost", root, "300", fontSize - 2, TextAlignmentOptions.Center);
            Band((RectTransform)cost.transform, false, 22, 20, 4, 4);
            var onOff = Text("OnOff", root, "OFF", 18, TextAlignmentOptions.Center);
            Band((RectTransform)onOff.transform, false, 18, 1, 4, 4);
            onOff.gameObject.SetActive(false);
            var locked = Text("Locked", root, "잠김", 14, TextAlignmentOptions.Center);
            Place((RectTransform)locked.transform, Vector2.one, new Vector2(36, 20), new Vector2(-20, -12));
            locked.color = new Color(0.5f, 0.3f, 0.24f);
            var bought = Text("Bought", root, "완료", 14, TextAlignmentOptions.Center);
            Place((RectTransform)bought.transform, Vector2.one, new Vector2(36, 20), new Vector2(-20, -12));
            bought.color = new Color(0.15f, 0.5f, 0.28f);
            locked.gameObject.SetActive(false);
            bought.gameObject.SetActive(false);

            var hold = root.gameObject.AddComponent<WorkHoldButton>();
            var holdData = new SerializedObject(hold);
            Ref(holdData, "button", button); Ref(holdData, "fill", fill);
            holdData.ApplyModifiedPropertiesWithoutUndo();
            var view = root.gameObject.AddComponent<WorkUpgradeNodeView>();
            var data = new SerializedObject(view);
            Ref(data, "buyButton", button); Ref(data, "nameText", name); Ref(data, "costText", cost);
            Ref(data, "icon", iconImage); Ref(data, "lockedMark", locked.gameObject); Ref(data, "boughtMark", bought.gameObject);
            Ref(data, "holdButton", hold); Ref(data, "selectedMark", selected.gameObject); Ref(data, "onOffText", onOff);
            Ref(data, "background", background);
            data.ApplyModifiedPropertiesWithoutUndo();
            ApplyVisualStyle(root.gameObject, strokeWidth);
            return root.gameObject;
        }

        private void SavePage()
        {
            tree.Check();
            if (nodePrefab.GetComponent<WorkUpgradeNodeView>() == null || nodePrefab.GetComponent<RectTransform>() == null
                || PrefabUtility.GetPrefabAssetType(nodePrefab) == PrefabAssetType.NotAPrefab)
                throw new InvalidOperationException("루트에 WorkUpgradeNodeView가 있는 프리팹 에셋을 지정하세요.");
            var nodeData = new SerializedObject(nodePrefab.GetComponent<WorkUpgradeNodeView>());
            if (nodePrefab.GetComponent<WorkHoldButton>() == null || nodeData.FindProperty("onOffText").objectReferenceValue == null)
                throw new InvalidOperationException("새 버전 노드 프리팹을 먼저 생성하세요. 꾹 누르기와 OnOff Text 필드가 필요합니다.");
            var families = GetFamilies(out var singles);
            CheckSize(families, singles.Count);
            string path = EditorUtility.SaveFilePanelInProject("강화 페이지 저장", "WorkUpgradePage", "prefab", "페이지를 저장하세요.", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var buildRoot = MakeBuildRoot();
            try
            {
                var root = BuildPage(families, singles, buildRoot);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("페이지 저장에 실패했습니다.");
                Selection.activeObject = prefab;
                message = $"페이지 생성 완료: {tree.Nodes.Count}개 노드 / {families.Count}개 트리 박스 / 독립 노드 {singles.Count}개. WorkRoot의 Upgrade View에 씬 인스턴스를 연결하세요.";
            }
            finally { DestroyBuild(buildRoot); }
        }

        private GameObject BuildPage(List<Family> families, List<WorkUpgradeSO> singles, Transform parent)
        {
            var root = Make("WorkUpgradePage", parent);
            try
            {
                root.sizeDelta = pageSize;
                Image(root, pageColor, true);
                var group = root.gameObject.AddComponent<CanvasGroup>();
                var view = root.gameObject.AddComponent<WorkUpgradeTreeView>();
                var title = Text("Title", root, "업무 역량 개발", fontSize + 6, TextAlignmentOptions.MidlineLeft);
                Band((RectTransform)title.transform, true, 38, 8, Margin, 600);
                var pointsLabel = Text("PointsLabel", root, "숙련 포인트", fontSize - 2, TextAlignmentOptions.MidlineRight);
                Place((RectTransform)pointsLabel.transform, Vector2.one, new Vector2(150, 36), new Vector2(-310, -28));
                var points = Text("Points", root, "0", fontSize + 4, TextAlignmentOptions.MidlineRight);
                Place((RectTransform)points.transform, Vector2.one, new Vector2(150, 36), new Vector2(-155, -28));
                var close = Button(root, "Close", "X");
                Place((RectTransform)close.transform, Vector2.one, new Vector2(64, 38), new Vector2(-48, -28));
                UnityEventTools.AddPersistentListener(close.onClick, view.Close);

                var body = Make("Body", root);
                Stretch(body, new Vector2(Margin, Margin), new Vector2(-Margin, -Header));
                var detail = Make("Detail", body);
                Area(detail, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(detailWidth, 0));
                Image(detail, boxColor, false);
                var detailIcon = Make("Icon", detail);
                Place(detailIcon, new Vector2(0.5f, 1), new Vector2(96, 96), new Vector2(0, -66));
                var icon = Image(detailIcon, Color.white, false);
                icon.preserveAspect = true;
                icon.enabled = false;
                var detailTitle = Text("Title", detail, "강화를 선택하세요", fontSize + 4, TextAlignmentOptions.Center);
                Band((RectTransform)detailTitle.transform, true, 84, 132, 16, 16);
                var description = Text("Description", detail, "노드를 누르면 상세 내용을 확인할 수 있습니다.", fontSize, TextAlignmentOptions.TopLeft);
                Stretch((RectTransform)description.transform, new Vector2(16, 228), new Vector2(-16, -232));
                var cost = Text("Cost", detail, "", fontSize - 2, TextAlignmentOptions.MidlineLeft);
                Band((RectTransform)cost.transform, false, 34, 178, 16, 16);
                var status = Text("Status", detail, "", fontSize - 4, TextAlignmentOptions.TopLeft);
                Band((RectTransform)status.transform, false, 44, 126, 16, 16);
                var hint = Text("Hint", detail, "스킬을 꾹 눌러 구매", fontSize - 4, TextAlignmentOptions.Center);
                Band((RectTransform)hint.transform, false, 32, 88, 8, 8);
                var featureSwitch = HoldSwitch(detail, out var switchText);
                Band((RectTransform)featureSwitch.transform, false, 60, 18, 16, 16);
                featureSwitch.gameObject.SetActive(false);

                var right = Make("UpgradeArea", body);
                Stretch(right, new Vector2(detailWidth + 16, 0), Vector2.zero);
                var top = Make("TreeScroll", right);
                Area(top, new Vector2(0, 1 - topShare), Vector2.one, new Vector2(0, 8), Vector2.zero);
                var scroll = top.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = true;
                scroll.vertical = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = true;
                scroll.scrollSensitivity = 40;
                var viewport = Make("Viewport", top);
                Stretch(viewport, Vector2.zero, Vector2.zero);
                Image(viewport, Color.clear, true);
                viewport.gameObject.AddComponent<RectMask2D>();
                var content = Make("Content", viewport);
                float visibleWidth = pageSize.x - Margin * 2 - detailWidth - 16;
                float minWidth = families.Sum(MinWidth) + Mathf.Max(0, families.Count - 1) * boxGap;
                float contentWidth = Mathf.Max(visibleWidth, minWidth);
                content.anchorMin = Vector2.zero;
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 0.5f);
                content.sizeDelta = new Vector2(contentWidth, 0);
                content.anchoredPosition = Vector2.zero;
                scroll.viewport = viewport;
                scroll.content = content;

                var bottom = Make("Singles", right);
                Area(bottom, Vector2.zero, new Vector2(1, 1 - topShare), Vector2.zero, Vector2.zero);
                Image(bottom, boxColor, false);
                var singleTitle = Text("Title", bottom, "단일 기능", fontSize - 4, TextAlignmentOptions.MidlineLeft);
                Band((RectTransform)singleTitle.transform, true, 22, 0, 10, 10);
                var singleViewport = Make("Viewport", bottom);
                Stretch(singleViewport, new Vector2(6, 2), new Vector2(-6, -26));
                Image(singleViewport, Color.clear, true);
                singleViewport.gameObject.AddComponent<RectMask2D>();
                var singleArea = Make("Nodes", singleViewport);
                singleArea.anchorMin = Vector2.zero; singleArea.anchorMax = Vector2.one;
                singleArea.pivot = new Vector2(0, 0.5f);
                // Fill the live viewport, including scene-level page size overrides.
                // Only add width when the nodes actually need horizontal overflow.
                singleArea.sizeDelta = new Vector2(Mathf.Max(0, singles.Count * (nodeSize.x + 12) - (visibleWidth - 12)), 0);
                var singleScroll = singleViewport.gameObject.AddComponent<ScrollRect>();
                singleScroll.viewport = singleViewport; singleScroll.content = singleArea;
                singleScroll.horizontal = true; singleScroll.vertical = false;
                singleScroll.movementType = ScrollRect.MovementType.Clamped; singleScroll.scrollSensitivity = 40;
                var nodeViews = new List<WorkUpgradeNodeView>();
                for (int i = 0; i < singles.Count; i++)
                    nodeViews.Add(AddNode(singleArea, singles[i], new Vector2((i + 0.5f) / singles.Count, 0.5f)));

                float extra = families.Count == 0 ? 0 : (contentWidth - minWidth) / families.Count;
                float x = 0;
                foreach (var family in families)
                {
                    float boxWidth = MinWidth(family) + extra;
                    var box = Make(family.Nodes[0].Id + "_Tree", content);
                    Area(box, new Vector2(x / contentWidth, 0), new Vector2((x + boxWidth) / contentWidth, 1), Vector2.zero, Vector2.zero);
                    Image(box, boxColor, false);
                    var label = Text("Title", box, family.Title, fontSize, TextAlignmentOptions.Center);
                    Band((RectTransform)label.transform, true, 34, 0, 4, 4);
                    var area = Make("Nodes", box);
                    Stretch(area, new Vector2(6, 6), new Vector2(-6, -40));
                    var linksRect = Make("Links", area);
                    Stretch(linksRect, Vector2.zero, Vector2.zero);
                    linksRect.gameObject.AddComponent<CanvasRenderer>();
                    var links = linksRect.gameObject.AddComponent<WorkUpgradeLinksGraphic>();
                    links.color = lineColor;
                    links.raycastTarget = false;
                    var map = new Dictionary<WorkUpgradeSO, RectTransform>();
                    foreach (var node in family.Nodes)
                    {
                        var anchor = new Vector2((family.Columns[node] + 0.5f) / family.Leaves,
                            1 - (family.Depths[node] + 0.5f) / family.Rows);
                        var nodeView = AddNode(area, node, anchor);
                        nodeViews.Add(nodeView);
                        map[node] = (RectTransform)nodeView.transform;
                    }
                    foreach (var node in family.Nodes)
                        foreach (var required in node.Required)
                            links.Links.Add(new WorkUpgradeLinksGraphic.Link { From = map[required], To = map[node] });
                    links.SetVerticesDirty();
                    x += boxWidth + boxGap;
                }

                var data = new SerializedObject(view);
                Ref(data, "panelGroup", group); Ref(data, "pointsText", points);
                Ref(data, "selectionIcon", icon); Ref(data, "selectionTitle", detailTitle);
                Ref(data, "descriptionText", description); Ref(data, "selectionCost", cost); Ref(data, "selectionStatus", status);
                Ref(data, "featureSwitch", featureSwitch); Ref(data, "featureSwitchText", switchText);
                var list = data.FindProperty("nodes");
                list.arraySize = nodeViews.Count;
                for (int i = 0; i < nodeViews.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = nodeViews[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                ApplyVisualStyle(root.gameObject, strokeWidth);
                return root.gameObject;
            }
            catch { PrepareUiCleanup(root.gameObject); DestroyImmediate(root.gameObject); throw; }
        }

        private WorkUpgradeNodeView AddNode(RectTransform parent, WorkUpgradeSO node, Vector2 anchor)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, parent);
            instance.name = node.Id;
            Place((RectTransform)instance.transform, anchor, nodeSize, Vector2.zero);
            var view = instance.GetComponent<WorkUpgradeNodeView>();
            var data = new SerializedObject(view);
            Ref(data, "node", node);
            data.ApplyModifiedPropertiesWithoutUndo();
            view.Show(node.Required.Count > 0 ? WorkUpgradeState.Locked : WorkUpgradeState.Available, false);
            // Preserve the nested prefab link and only record instance overrides.
            foreach (var component in instance.GetComponentsInChildren<Component>(true))
                if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            return view;
        }

        private void StyleSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null || (selected.GetComponent<WorkUpgradeTreeView>() == null
                && selected.GetComponent<WorkUpgradeNodeView>() == null))
                throw new InvalidOperationException("강화 페이지 또는 노드 루트를 선택하세요.");
            if (EditorUtility.IsPersistent(selected))
            {
                string path = AssetDatabase.GetAssetPath(selected);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { ApplyVisualStyle(root, strokeWidth); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrepareUiCleanup(root); PrefabUtility.UnloadPrefabContents(root); }
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(selected, "Style Upgrade UI");
                ApplyVisualStyle(selected, strokeWidth);
                foreach (var component in selected.GetComponentsInChildren<Component>(true))
                    if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            message = "기본 꾸미기 적용 완료. 배치·스킬 연결·구매 방식은 유지합니다.";
        }

        // Shared by generation and in-place styling, so regenerating keeps the same look.
        public static void ApplyVisualStyle(GameObject root, float strokeWidth = 3)
        {
            var ink = new Color(0.25f, 0.12f, 0.07f);
            var muted = new Color(0.47f, 0.34f, 0.26f);
            var card = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/UI/Button_Default_x2.png");
            var yellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/UI/Button_Yellow_x2.png");
            var ribbon = GetTitleRibbon();
            if (card == null || yellow == null || ribbon == null)
                throw new InvalidOperationException("기본 꾸미기에 필요한 UI 이미지를 찾을 수 없습니다.");
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.color = ink;
            foreach (var node in root.GetComponentsInChildren<WorkUpgradeNodeView>(true))
            {
                Skin(node.GetComponent<Image>(), card, Color.white, strokeWidth);
                var button = node.GetComponent<Button>();
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1, 0.96f, 0.86f);
                    colors.selectedColor = colors.highlightedColor;
                    colors.pressedColor = new Color(0.93f, 0.85f, 0.68f);
                    button.colors = colors;
                }
                var data = new SerializedObject(node);
                data.FindProperty("boughtBackgroundColor").colorValue = new Color(1, 0.91f, 0.69f);
                var definition = data.FindProperty("node").objectReferenceValue as WorkUpgradeSO;
                data.ApplyModifiedPropertiesWithoutUndo();
                var name = node.transform.Find("Name") as RectTransform;
                if (name != null)
                    Stretch(name, new Vector2(12, 48), new Vector2(-12, definition != null && definition.Icon != null ? -54 : -28));
                var cost = node.transform.Find("Cost")?.GetComponent<TMP_Text>();
                if (cost != null) { cost.fontSize = 18; cost.color = muted; }
                var onOff = node.transform.Find("OnOff")?.GetComponent<TMP_Text>();
                if (onOff != null) { onOff.fontSize = 15; onOff.color = muted; }
                var fill = node.transform.Find("HoldFill") as RectTransform;
                if (fill != null)
                {
                    Stretch(fill, new Vector2(9, 9), new Vector2(-9, -9));
                    fill.GetComponent<Image>().color = new Color(0.98f, 0.72f, 0.22f, 0.28f);
                }
                var selected = node.transform.Find("Selected") as RectTransform;
                if (selected != null)
                {
                    Place(selected, new Vector2(0, 1), new Vector2(20, 20), new Vector2(20, -20));
                    var image = selected.GetComponent<Image>();
                    Skin(image, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/UI/Icon/Star.png"), Color.white, 1, false);
                    image.fillCenter = true;
                    image.raycastTarget = false;
                }
                foreach (string mark in new[] { "Locked", "Bought" })
                {
                    var text = node.transform.Find(mark)?.GetComponent<TMP_Text>();
                    if (text == null) continue;
                    text.fontSize = 12;
                    text.color = mark == "Bought" ? new Color(0.27f, 0.45f, 0.24f) : muted;
                    Place(text.rectTransform, Vector2.one, new Vector2(40, 18), new Vector2(-28, -18));
                }
            }
            if (root.GetComponent<WorkUpgradeTreeView>() == null) return;
            Skin(root.GetComponent<Image>(), card, new Color(1, 0.98f, 0.93f), strokeWidth);
            var detail = root.transform.Find("Body/Detail");
            if (detail != null)
            {
                Skin(detail.GetComponent<Image>(), card, new Color(1, 0.97f, 0.88f), strokeWidth);
                var banner = detail.Find("DetailRibbon") as RectTransform;
                if (banner == null) banner = NewDecoration("DetailRibbon", detail);
                Band(banner, true, 42, 12, 18, 18);
                Skin(banner.GetComponent<Image>(), ribbon, new Color(1, 0.92f, 0.73f), strokeWidth);
                var label = banner.GetComponentInChildren<TMP_Text>();
                if (label == null)
                {
                    var rect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
                    rect.SetParent(banner, false); Stretch(rect, Vector2.zero, Vector2.zero);
                    label = rect.gameObject.AddComponent<TextMeshProUGUI>();
                }
                label.font = detail.Find("Title").GetComponent<TMP_Text>().font;
                label.text = "스킬 상세"; label.fontSize = 22; label.color = ink;
                label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                var icon = detail.Find("Icon") as RectTransform;
                if (icon != null) Place(icon, new Vector2(0.5f, 1), new Vector2(64, 64), new Vector2(0, -100));
            }
            var singles = root.transform.Find("Body/UpgradeArea/Singles");
            if (singles != null)
            {
                Skin(singles.GetComponent<Image>(), card, new Color(1, 0.97f, 0.88f), strokeWidth);
                var label = singles.Find("Title")?.GetComponent<TMP_Text>();
                if (label != null) { label.fontSize = 16; Band(label.rectTransform, true, 22, 4, 14, 14); }
            }
            var content = root.transform.Find("Body/UpgradeArea/TreeScroll/Viewport/Content");
            if (content != null)
                foreach (Transform family in content)
                {
                    Skin(family.GetComponent<Image>(), card, new Color(0.96f, 0.94f, 0.87f), strokeWidth);
                    var title = family.Find("Title")?.GetComponent<TMP_Text>();
                    if (title != null)
                    {
                        title.fontSize = 20;
                        Band(title.rectTransform, true, 34, 3, 12, 12);
                        var backing = family.Find("TitleRibbon") as RectTransform;
                        if (backing == null) backing = NewDecoration("TitleRibbon", family);
                        Band(backing, true, 34, 3, 8, 8);
                        backing.SetAsFirstSibling();
                        Skin(backing.GetComponent<Image>(), ribbon, new Color(1, 0.92f, 0.73f), strokeWidth);
                    }
                }
            foreach (var links in root.GetComponentsInChildren<WorkUpgradeLinksGraphic>(true))
            { links.color = new Color(0.50f, 0.32f, 0.19f); links.Thickness = strokeWidth; links.SetVerticesDirty(); }
            var close = root.transform.Find("Close");
            if (close != null)
            {
                Place((RectTransform)close, Vector2.one, new Vector2(44, 38), new Vector2(-42, -29));
                var image = close.GetComponent<Image>();
                Skin(image, card, new Color(0.70f, 0.25f, 0.063f), strokeWidth);
                var text = close.GetComponentInChildren<TMP_Text>(true);
                if (text != null) { text.text = "X"; text.color = Color.white; text.fontSize = 24; }
            }
            var feature = detail != null ? detail.Find("FeatureSwitch") : null;
            if (feature != null)
            {
                Skin(feature.GetComponent<Image>(), yellow, Color.white, strokeWidth);
                var fill = feature.Find("HoldFill") as RectTransform;
                if (fill != null)
                {
                    Stretch(fill, new Vector2(8, 8), new Vector2(-8, -8));
                    var image = fill.GetComponent<Image>();
                    image.type = UnityEngine.UI.Image.Type.Filled;
                    image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                    image.fillAmount = 0;
                }
            }
            var points = root.transform.Find("Points") as RectTransform;
            var pointsLabel = root.transform.Find("PointsLabel") as RectTransform;
            if (points != null) Place(points, Vector2.one, new Vector2(118, 36), new Vector2(-132, -28));
            if (pointsLabel != null) Place(pointsLabel, Vector2.one, new Vector2(136, 36), new Vector2(-268, -28));
            var star = root.transform.Find("PointsStar") as RectTransform;
            if (star == null) star = NewDecoration("PointsStar", root.transform);
            Place(star, Vector2.one, new Vector2(28, 28), new Vector2(-350, -28));
            Skin(star.GetComponent<Image>(), AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/UI/Icon/Star.png"), Color.white, 1, false);
            var heading = root.transform.Find("Title")?.GetComponent<TMP_Text>();
            if (heading != null) { heading.fontSize = 28; Band(heading.rectTransform, true, 38, 8, 28, 420); }
        }

        private static Sprite GetTitleRibbon()
        {
            // A separate Sprite shares the existing texture; other screens keep their import settings.
            const string path = "Assets/WorkGame/Art/UI/UpgradeTitleRibbon.asset";
            var ribbon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (ribbon != null) return ribbon;
            var source = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/UI/Panel_Whip.png");
            if (source == null) return null;
            ribbon = Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(44, 32, 44, 32));
            ribbon.name = "UpgradeTitleRibbon";
            AssetDatabase.CreateAsset(ribbon, path);
            return ribbon;
        }

        private static RectTransform NewDecoration(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        private static void Skin(Image image, Sprite sprite, Color color, float strokeWidth, bool sliced = true)
        {
            if (image == null) return;
            image.sprite = sprite; image.color = color;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            // Source outlines: x2 buttons = 10 px, ribbon = 7 px. Convert to Canvas units.
            float sourceStroke = sprite != null && sprite.name == "UpgradeTitleRibbon" ? 7 : 10;
            float referencePpu = image.canvas != null ? image.canvas.referencePixelsPerUnit : 100;
            image.pixelsPerUnitMultiplier = sliced && sprite != null
                ? sourceStroke / Mathf.Max(1, strokeWidth) * referencePpu / sprite.pixelsPerUnit : 1;
            image.preserveAspect = !sliced;
        }

        private List<Family> GetFamilies(out List<WorkUpgradeSO> singles)
        {
            singles = new List<WorkUpgradeSO>();
            var result = new List<Family>();
            var remaining = new HashSet<WorkUpgradeSO>(tree.Nodes);
            foreach (var first in tree.Nodes)
            {
                if (!remaining.Remove(first)) continue;
                var connected = new HashSet<WorkUpgradeSO> { first };
                var queue = new Queue<WorkUpgradeSO>();
                queue.Enqueue(first);
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    foreach (var other in node.Required.Concat(tree.Nodes.Where(x => x.Required.Contains(node))))
                        if (remaining.Remove(other)) { connected.Add(other); queue.Enqueue(other); }
                }
                if (connected.Count == 1) { singles.Add(first); continue; }
                var family = new Family { Nodes = tree.Nodes.Where(connected.Contains).ToList() };
                var roots = family.Nodes.Where(x => x.Required.Count == 0).ToList();
                family.Title = Regex.Replace(roots[0].Name, @"\s+[IVX]+$", "");
                int cursor = 0;
                foreach (var root in roots) PlaceBranch(root, family, ref cursor);
                family.Leaves = cursor;
                foreach (var node in family.Nodes) Depth(node, family);
                family.Rows = family.Depths.Values.Max() + 1;
                result.Add(family);
            }
            return result;
        }

        private static float PlaceBranch(WorkUpgradeSO node, Family family, ref int cursor)
        {
            var children = family.Nodes.Where(x => x.Required.Count > 0 && x.Required[0] == node).ToList();
            float column;
            if (children.Count == 0) column = cursor++;
            else
            {
                var columns = new List<float>();
                foreach (var child in children) columns.Add(PlaceBranch(child, family, ref cursor));
                column = (columns.First() + columns.Last()) * 0.5f;
            }
            family.Columns[node] = column;
            return column;
        }

        private static int Depth(WorkUpgradeSO node, Family family)
        {
            if (family.Depths.TryGetValue(node, out int depth)) return depth;
            depth = node.Required.Count == 0 ? 0 : node.Required.Max(x => Depth(x, family)) + 1;
            family.Depths[node] = depth;
            return depth;
        }

        private float MinWidth(Family family) => family.Leaves * nodeSize.x + Mathf.Max(0, family.Leaves - 1) * 8 + 12;

        private void CheckSize(List<Family> families, int singles)
        {
            if (nodeSize.x < 120 || nodeSize.y < 144 || pageSize.x <= 0 || pageSize.y <= 0)
                throw new InvalidOperationException("페이지 크기와 노드 크기를 확인하세요. 노드는 최소 120 × 144입니다.");
            float rightWidth = nodeSize.x + 24;
            float minWidth = Mathf.Max(960, detailWidth + 16 + rightWidth + Margin * 2);
            float topHeight = families.Count == 0 ? 0 : families.Max(x => x.Rows * nodeSize.y + (x.Rows - 1) * 12 + 46);
            float bodyHeight = Mathf.Max((topHeight + 8) / topShare, (nodeSize.y + 28) / (1 - topShare));
            float minHeight = bodyHeight + Header + Margin;
            if (pageSize.x < minWidth || pageSize.y < minHeight)
                throw new InvalidOperationException($"현재 구성은 최소 {Mathf.CeilToInt(minWidth)} × {Mathf.CeilToInt(minHeight)}가 필요합니다. 페이지 크기·노드 크기·상단 비율을 조절하세요. 상단 가로 폭은 스크롤로 확장됩니다.");
        }

        private RectTransform Make(string name, Transform parent = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null) rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private Image Image(RectTransform rect, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private TMP_Text Text(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            var label = Make(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = Mathf.Max(12, size);
            label.enableAutoSizing = false;
            label.enableWordWrapping = true;
            label.color = Ink;
            label.text = text;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private Button Button(Transform parent, string name, string label)
        {
            var rect = Make(name, parent);
            var image = Image(rect, Color.white, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Text", rect, label, fontSize - 2, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform, new Vector2(4, 2), new Vector2(-4, -2));
            return button;
        }

        private WorkHoldButton HoldSwitch(Transform parent, out TMP_Text label)
        {
            var button = Button(parent, "FeatureSwitch", "ON/OFF");
            label = button.GetComponentInChildren<TMP_Text>(true);
            var fillRect = Make("HoldFill", button.transform);
            Stretch(fillRect, Vector2.zero, Vector2.zero);
            fillRect.SetAsFirstSibling();
            var fill = Image(fillRect, new Color(0.25f, 0.65f, 0.4f, 0.3f), false);
            fill.sprite = FillSprite();
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0;
            var hold = button.gameObject.AddComponent<WorkHoldButton>();
            var data = new SerializedObject(hold);
            Ref(data, "button", button); Ref(data, "fill", fill);
            data.ApplyModifiedPropertiesWithoutUndo();
            return hold;
        }

        private static Sprite FillSprite() => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/white.png")
            ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Area(RectTransform rect, Vector2 min, Vector2 max, Vector2 bottom, Vector2 top)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = bottom; rect.offsetMax = top;
        }

        private static void Stretch(RectTransform rect, Vector2 bottom, Vector2 top) => Area(rect, Vector2.zero, Vector2.one, bottom, top);

        private static void Band(RectTransform rect, bool top, float height, float inset, float left, float right)
        {
            rect.anchorMin = new Vector2(0, top ? 1 : 0);
            rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(0.5f, top ? 1 : 0);
            rect.offsetMin = new Vector2(left, top ? -inset - height : inset);
            rect.offsetMax = new Vector2(-right, top ? -inset : inset + height);
        }

        private static void Ref(SerializedObject data, string field, UnityEngine.Object value) => data.FindProperty(field).objectReferenceValue = value;
    }
}
