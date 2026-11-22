using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareScheduleTool : EditorWindow
    {
        [SerializeField] private CareConfigSO config;
        [SerializeField] private GameObject lessonPrefab;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Vector2 pageSize = new Vector2(1300, 940);
        [SerializeField] private int columns = 8;
        [SerializeField] private int fontSize = 26;
        [SerializeField] private float infoWidth = 300;
        [SerializeField] private float planHeight = 128;
        [SerializeField] private float previewHeight = 152;
        [SerializeField] private float gap = 8;
        [SerializeField] private float nodeSize = 100;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color cardColor = new Color(0.91f, 0.93f, 0.89f);
        private string message;
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);

        [MenuItem("Work Game/UI/Care Schedule Tool")]
        public static void Open() => GetWindow<CareScheduleTool>("달이 시간표");
        private void OnEnable()
        {
            if (pageSize.x == 1300 && (pageSize.y == 800 || pageSize.y == 900)) pageSize.y = 940;
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (config == null) config = AssetDatabase.LoadAssetAtPath<CareConfigSO>("Assets/WorkGame/Data/Care/CareConfig.asset");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("1. 공통 수업 노드", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            lockSprite = (Sprite)EditorGUILayout.ObjectField("자물쇠 이미지 (선택)", lockSprite, typeof(Sprite), false);
            fontSize = EditorGUILayout.IntSlider("기준 글자 크기", fontSize, 22, 30);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null))
                if (GUILayout.Button("수업 노드 프리팹 생성")) Save(true);
            lessonPrefab = (GameObject)EditorGUILayout.ObjectField("사용할 노드 프리팹", lessonPrefab, typeof(GameObject), false);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. 시간표 페이지", EditorStyles.boldLabel);
            config = (CareConfigSO)EditorGUILayout.ObjectField("육성 Config", config, typeof(CareConfigSO), false);
            pageSize = EditorGUILayout.Vector2Field("페이지 크기", pageSize);
            columns = EditorGUILayout.IntSlider("수업 가로 개수", columns, 4, 12);
            nodeSize = EditorGUILayout.Slider("정사각형 노드 크기", nodeSize, 80, 140);
            infoWidth = EditorGUILayout.Slider("좌측 상세 폭", infoWidth, 240, 400);
            planHeight = EditorGUILayout.Slider("하단 편성 높이", planHeight, 112, 180);
            previewHeight = EditorGUILayout.Slider("성장·시너지 높이", previewHeight, 140, 220);
            gap = EditorGUILayout.Slider("간격", gap, 4, 16);
            pageColor = EditorGUILayout.ColorField("페이지 배경", pageColor);
            cardColor = EditorGUILayout.ColorField("카드 배경", cardColor);
            EditorGUILayout.HelpBox("카테고리마다 가로 2 × 세로 4 수업을 한 배경으로 묶습니다. 좌측은 스크롤 없이 정사각형 이미지 → 제목 → 설명·효과·가격 순서입니다. 수업 목록만 창이 작아지면 스크롤됩니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || config == null || lessonPrefab == null
                || pageSize.x < 1100 || pageSize.y < 760))
                if (GUILayout.Button("시간표 프리팹 생성", GUILayout.Height(32))) Save(false);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save(bool node)
        {
            string path = EditorUtility.SaveFilePanelInProject("시간표 UI 저장", node ? "CareLessonNode" : "CareSchedule", "prefab",
                "저장할 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("ScheduleBuild", typeof(RectTransform));
            parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                if (!node && lessonPrefab.GetComponent<CareLessonNodeView>() == null)
                    throw new InvalidOperationException("CareLessonNodeView가 있는 프리팹을 지정하세요.");
                if (!node && (config.Lessons == null || config.Lessons.Any(x => x == null) || config.Synergies == null))
                    throw new InvalidOperationException("Config의 수업·시너지 목록을 확인하세요.");
                var root = node ? BuildLesson(parent.transform) : BuildPage(parent.transform);
                foreach (var part in root.GetComponentsInChildren<Component>(true))
                    if (part != null && PrefabUtility.IsPartOfPrefabInstance(part)) PrefabUtility.RecordPrefabInstancePropertyModifications(part);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                if (node) lessonPrefab = prefab;
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = node ? "노드 생성 완료. 이 노드로 시간표를 생성하세요." : "생성 완료. 씬 View와 메인 진입 버튼을 WorkRoot에 연결하세요.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally
            {
                foreach (var mask in parent.GetComponentsInChildren<RectMask2D>(true)) mask.enabled = false;
                DestroyImmediate(parent);
            }
        }

        private RectTransform BuildLesson(Transform parent)
        {
            var rect = Rect("CareLessonNode", parent); rect.sizeDelta = Vector2.one * nodeSize;
            var image = Image(rect, Color.white, true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            var highlight = ConfigureHighlight(rect, true);
            var iconRect = Rect("Icon", rect); Area(iconRect, 0.18f, 0.2f, 0.82f, 0.84f);
            var icon = Image(iconRect, Color.white); icon.preserveAspect = true; icon.enabled = false;
            var name = Text("ShortName", rect, "수업 이름", fontSize - 8);
            Area(name.rectTransform, 0.04f, 0.01f, 0.96f, 0.21f); name.alignment = TextAlignmentOptions.Center;
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
            var locked = Rect("LockOverlay", rect); Area(locked, 0, 0, 1, 1);
            var lockGroup = locked.gameObject.AddComponent<CanvasGroup>();
            lockGroup.interactable = false; lockGroup.blocksRaycasts = false;
            var dim = Rect("Dim", locked); Area(dim, 0, 0, 1, 1);
            Image(dim, new Color(0, 0, 0, 0.65f));
            var lockIcon = Rect("LockImage", locked);
            lockIcon.anchorMin = lockIcon.anchorMax = new Vector2(0.5f, 0.5f);
            lockIcon.sizeDelta = new Vector2(28, 28);
            var lockImage = Image(lockIcon, new Color(0.95f, 0.94f, 0.88f));
            lockImage.sprite = lockSprite; lockImage.preserveAspect = true; lockImage.enabled = lockSprite != null;
            var placeholder = Rect("Placeholder", locked);
            placeholder.anchorMin = placeholder.anchorMax = new Vector2(0.5f, 0.5f);
            placeholder.sizeDelta = new Vector2(18, 13); placeholder.anchoredPosition = new Vector2(0, -3);
            Image(placeholder, lockImage.color);
            // 자물쇠 이미지를 넣기 전까지만 표시하는 임시 도형입니다.
            foreach (var part in new[] { new Vector4(0.18f, 1, 0.32f, 1.5f), new Vector4(0.68f, 1, 0.82f, 1.5f), new Vector4(0.18f, 1.4f, 0.82f, 1.58f) })
            {
                var shackle = Rect("Shackle", placeholder);
                Area(shackle, part.x, part.y, part.z, part.w);
                Image(shackle, lockImage.color);
            }
            placeholder.gameObject.SetActive(lockSprite == null);
            var view = rect.gameObject.AddComponent<CareLessonNodeView>();
            var fields = new SerializedObject(view);
            Set(fields,"button",button); Set(fields,"nameText",name);
            Set(fields,"lockGroup",lockGroup); Set(fields,"unlockTarget",rect);
            Set(fields,"lockImage",lockImage); Set(fields,"lockPlaceholder",placeholder.gameObject);
            Set(fields,"icon",icon); Set(fields,"background",image); Set(fields,"highlight",highlight);
            fields.ApplyModifiedPropertiesWithoutUndo();
            ConfigureHighlight(rect, true);
            return rect;
        }

        private RectTransform BuildPage(Transform parent)
        {
            var root = Rect("CareSchedule",parent); Area(root,0,0,1,1);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<CareScheduleView>();
            var outside = Button("Outside",root,""); Area((RectTransform)outside.transform,0,0,1,1);
            outside.GetComponent<Image>().color = new Color(0,0,0,0.55f);
            var page = Rect("Page",root); page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f,0.5f);
            page.sizeDelta = pageSize; Image(page,pageColor,true);
            var stack = Vertical(page,10); stack.padding = new RectOffset(16,16,16,16);
            var header = Rect("Header",page); Size(header,height:56); Horizontal(header,12);
            var title = Text("Title",header,"달이의 시간표",fontSize+10); Size(title.rectTransform,flexWidth:1);
            var money = Text("Money",header,"보유 현금  0",fontSize); Size(money.rectTransform,width:300); money.alignment = TextAlignmentOptions.MidlineRight;
            var close = Button("Close",header,"닫기"); Size((RectTransform)close.transform,width:96);

            var main = Rect("Main",page); Size(main,height:260,flexHeight:1); Horizontal(main,16);
            var info = Rect("SelectedLesson",main); Size(info,width:infoWidth); Image(info,cardColor);
            var infoLayout = Vertical(info,6); infoLayout.padding = new RectOffset(12,12,12,12);
            var imageArea = Rect("ImageArea",info); Size(imageArea,height:144);
            var imageFrame = Rect("ImageFrame",imageArea);
            var imageAspect = imageFrame.gameObject.AddComponent<AspectRatioFitter>();
            imageAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; imageAspect.aspectRatio = 1;
            Image(imageFrame,new Color(0.84f,0.87f,0.83f));
            var iconRect = Rect("Icon",imageFrame); Area(iconRect,0,0,1,1);
            var selectedIcon = Image(iconRect,Color.white); selectedIcon.preserveAspect = true; selectedIcon.enabled = false;
            var selectedTitle = Text("Title",info,"수업을 선택하세요",fontSize);
            Size(selectedTitle.rectTransform,height:64); FitText(selectedTitle,22,fontSize);
            var description = Text("Description",info,"수업을 누르면 첫 빈 주차에 편성됩니다.",fontSize-6);
            Size(description.rectTransform,height:0,flexHeight:1).minHeight = 0; FitText(description,18,fontSize-6);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;
            var selectedGains = Text("Gains",info,"",fontSize-4);
            Size(selectedGains.rectTransform,height:48); FitText(selectedGains,18,fontSize-4);
            var selectedCost = Text("Cost",info,"",fontSize-2);
            Size(selectedCost.rectTransform,height:28); FitText(selectedCost,20,fontSize-2);
            var free = Rect("FreeLessons",info); Size(free,height:40); var freeLayout = Horizontal(free,6); freeLayout.childForceExpandWidth = true;
            var home = Button("Home",free,"홈스쿨링"); var rest = Button("Rest",free,"휴식");

            var catalog = Rect("LessonRows",main); Size(catalog,flexWidth:1);
            var catalogScroll = catalog.gameObject.AddComponent<ScrollRect>();
            catalogScroll.horizontal = false; catalogScroll.vertical = true; catalogScroll.scrollSensitivity = 24;
            catalogScroll.movementType = ScrollRect.MovementType.Clamped;
            var catalogViewport = Rect("Viewport",catalog); Area(catalogViewport,0,0,1,1);
            Image(catalogViewport,Color.clear,true); catalogViewport.gameObject.AddComponent<RectMask2D>();
            var catalogContent = Rect("Content",catalogViewport); Area(catalogContent,0,1,1,1); catalogContent.pivot = new Vector2(0.5f,1);
            var grid = catalogContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(nodeSize*2+gap+16,nodeSize*4+gap*3+48); grid.spacing = Vector2.one * gap;
            grid.padding = new RectOffset(8,8,8,8); grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = Mathf.Max(1,columns/2);
            catalogContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            catalogScroll.viewport = catalogViewport; catalogScroll.content = catalogContent;
            var square = catalogViewport.gameObject.AddComponent<CareSquareGrid>();
            var squareFields = new SerializedObject(square);
            Set(squareFields,"grid",grid); squareFields.FindProperty("nodeSize").floatValue = nodeSize;
            squareFields.FindProperty("maxColumns").intValue = columns; squareFields.ApplyModifiedPropertiesWithoutUndo();
            var definitions = config.Lessons.OrderBy(x=>x.UnlockMonth).ToArray();
            var nodes = new List<CareLessonNodeView>();
            var groups = new List<GridLayoutGroup>();
            foreach (var category in definitions.GroupBy(GetCategory).OrderBy(group => group.Key))
            {
                var lessons = category.OrderBy(lesson => lesson.UnlockMonth).ThenBy(lesson => lesson.Id).ToArray();
                var box = Rect(category.Key.ToString(),catalogContent);
                Image(box,GetCategoryColor(category.Key));
                var label = Text("Header",box,CareStatNames.GetName(category.Key),fontSize-4);
                Area(label.rectTransform,0,1,1,1);
                label.rectTransform.offsetMin = new Vector2(8,-32); label.rectTransform.offsetMax = new Vector2(-8,-4);
                var pair = Rect("Nodes",box); Area(pair,0,0,1,1);
                pair.offsetMin = new Vector2(8,8); pair.offsetMax = new Vector2(-8,-40);
                var pairGrid = pair.gameObject.AddComponent<GridLayoutGroup>();
                pairGrid.cellSize = Vector2.one*nodeSize; pairGrid.spacing = Vector2.one*gap;
                pairGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; pairGrid.constraintCount = 2;
                groups.Add(pairGrid);
                foreach (var lesson in lessons)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(lessonPrefab,pair);
                    instance.name = lesson.Id; instance.transform.localScale = Vector3.one;
                    var node = instance.GetComponent<CareLessonNodeView>();
                    var data = new SerializedObject(node); Set(data,"lesson",lesson); data.ApplyModifiedPropertiesWithoutUndo();
                    node.Show(lesson.UnlockMonth==1,false,false,Color.white); nodes.Add(node);
                }
            }
            SetList(squareFields,"groups",groups.ToArray()); squareFields.ApplyModifiedPropertiesWithoutUndo();

            var plan = Rect("PlanStrip",page); Size(plan,height:planHeight); Horizontal(plan,12);
            var totals = Rect("Totals",plan); Size(totals,width:210); Vertical(totals,8);
            Text("Title",totals,"이번 달 편성",fontSize-2);
            var planText = Text("Count",totals,"편성  0 / 4",fontSize-2);
            var costText = Text("Cost",totals,"수업료  0",fontSize-2);
            var weeksRoot = Rect("Weeks",plan); Size(weeksRoot,flexWidth:1); var weeksLayout = Horizontal(weeksRoot,10); weeksLayout.childForceExpandWidth=true;
            var weeks = new List<CareScheduleWeekView>();
            for(int i=0;i<4;i++) weeks.Add(BuildWeek(weeksRoot,i));

            var preview = Rect("Preview",page); Size(preview,height:previewHeight); Vertical(preview,8);
            var statStrip = Rect("Stats",preview); Size(statStrip,height:72);
            var statLayout = Horizontal(statStrip,12); statLayout.childForceExpandWidth = true;
            var stats = new List<CareScheduleStatView>();
            for (int i = 0; i < 4; i++) stats.Add(BuildStat(statStrip,(CareStat)i));
            var scrollRoot = Rect("Synergies",preview); Size(scrollRoot,height:56,flexHeight:1);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=24;
            var viewport=Rect("Viewport",scrollRoot); Area(viewport,0,0,1,1); Image(viewport,Color.clear,true); viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Content",viewport); Area(content,0,1,1,1); content.pivot=new Vector2(0.5f,1);
            var contentLayout=Vertical(content,4); contentLayout.childControlHeight=false;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport; scroll.content=content;
            var none=Text("None",content,"시너지 없음",fontSize-6); none.rectTransform.sizeDelta=new Vector2(0,28);
            var synergyRows=new List<CareScheduleSynergyView>();
            for(int i=0;i<config.Synergies.Count;i++)
            {
                var button=Button("Synergy_"+i,content,""); ((RectTransform)button.transform).sizeDelta=new Vector2(0,30);
                button.GetComponent<Image>().color=new Color(0.9f,0.92f,0.88f,0.3f);
                var label=Text("Text",button.transform,"",fontSize-6); Area(label.rectTransform,0,0,1,1,2); label.richText=true;
                var row=button.gameObject.AddComponent<CareScheduleSynergyView>();
                var fields=new SerializedObject(row); Set(fields,"button",button); Set(fields,"text",label); fields.ApplyModifiedPropertiesWithoutUndo();
                synergyRows.Add(row); button.gameObject.SetActive(false);
            }
            var help=Text("Help",page,"수업을 누르면 첫 빈 주차에 편성됩니다. 아래 주차를 누르면 해제됩니다.",fontSize-6);
            Size(help.rectTransform,height:28); help.alignment=TextAlignmentOptions.Center;
            var fieldsView=new SerializedObject(view);
            Set(fieldsView,"panelGroup",group); SetList(fieldsView,"closeButtons",new UnityEngine.Object[]{outside,close});
            Set(fieldsView,"homeButton",home); Set(fieldsView,"restButton",rest);
            SetList(fieldsView,"lessons",nodes.ToArray()); SetList(fieldsView,"weeks",weeks.ToArray()); SetList(fieldsView,"synergies",synergyRows.ToArray());
            SetList(fieldsView,"stats",stats.ToArray());
            Set(fieldsView,"selectedIcon",selectedIcon); Set(fieldsView,"selectedTitle",selectedTitle); Set(fieldsView,"selectedDescription",description);
            Set(fieldsView,"selectedGains",selectedGains); Set(fieldsView,"selectedCost",selectedCost);
            Set(fieldsView,"moneyText",money); Set(fieldsView,"planText",planText); Set(fieldsView,"costText",costText); Set(fieldsView,"noSynergy",none.gameObject);
            Set(fieldsView,"helpText",help);
            fieldsView.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static CareStat GetCategory(CareLessonSO lesson) => lesson.Gains
            .GroupBy(gain => CareStats.GetMain(gain.Stat)).OrderByDescending(group => group.Sum(gain => gain.Amount))
            .Select(group => group.Key).DefaultIfEmpty(CareStat.Knowledge).First();

        private static void FitText(TMP_Text text,int min,int max)
        {
            text.enableAutoSizing=true; text.fontSizeMin=min; text.fontSizeMax=Mathf.Max(min,max);
            text.textWrappingMode=TextWrappingModes.Normal; text.overflowMode=TextOverflowModes.Ellipsis;
        }

        private static Color GetCategoryColor(CareStat stat)
        {
            if (stat == CareStat.Knowledge) return new Color(0.89f,0.93f,0.98f);
            if (stat == CareStat.Fitness) return new Color(0.88f,0.94f,0.89f);
            if (stat == CareStat.Emotion) return new Color(0.98f,0.91f,0.93f);
            return new Color(0.98f,0.95f,0.86f);
        }

        private CareScheduleStatView BuildStat(Transform parent, CareStat stat)
        {
            var root = Rect(stat.ToString(),parent);
            var bar = Rect("Bar",root); Area(bar,0.1f,0.06f,1,0.94f);
            Image(bar,new Color(0.84f,0.87f,0.83f));
            var projected = BuildStatSlider("Preview",bar,new Color(0.63f,0.81f,0.66f));
            var current = BuildStatSlider("Current",bar,new Color(0.39f,0.62f,0.45f));
            var name = Text("Name",bar,CareStatNames.GetName(stat),fontSize);
            Area(name.rectTransform,0,0.44f,1,1); name.alignment = TextAlignmentOptions.Center;
            var value = Text("Value",bar,"0",fontSize-6);
            Area(value.rectTransform,0,0,1,0.48f); value.alignment = TextAlignmentOptions.Center;
            // 왼쪽 테두리 위에 등급 중심을 맞춥니다. 마지막 자식으로 두어 막대 앞에 표시합니다.
            var grade = Text("Grade",bar,"C",fontSize+8);
            Area(grade.rectTransform,0,0,0,1);
            grade.rectTransform.offsetMin = new Vector2(-24,0);
            grade.rectTransform.offsetMax = new Vector2(24,0);
            grade.alignment = TextAlignmentOptions.Center;
            var view = root.gameObject.AddComponent<CareScheduleStatView>();
            var data = new SerializedObject(view);
            data.FindProperty("stat").enumValueIndex = (int)stat;
            Set(data,"currentSlider",current); Set(data,"previewSlider",projected);
            Set(data,"nameText",name); Set(data,"valueText",value); Set(data,"gradeText",grade);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
        private Slider BuildStatSlider(string name, Transform parent, Color color)
        {
            var root = Rect(name,parent); Area(root,0,0,1,1);
            var fill = Rect("Fill",root); Area(fill,0,0,1,1);
            var image = Image(fill,color);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.targetGraphic = image;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(0);
            slider.interactable = false; slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }
        private CareScheduleWeekView BuildWeek(Transform parent,int index)
        {
            var button=Button("Week_"+(index+1),parent,""); var root=(RectTransform)button.transform; var highlight=ConfigureHighlight(root, false);
            var title=Text("WeekLabel",root,(index+1)+"주차",fontSize-8); Area(title.rectTransform,0.05f,0.78f,0.6f,1);
            var iconRect=Rect("Icon",root); Area(iconRect,0.36f,0.38f,0.64f,0.78f);
            var icon=Image(iconRect,Color.white); icon.preserveAspect=true; icon.enabled=false;
            var name=Text("Lesson",root,"",fontSize-2); Area(name.rectTransform,0.04f,0.15f,0.96f,0.44f); name.alignment=TextAlignmentOptions.Center;
            var cost=Text("Cost",root,"",fontSize-6); Area(cost.rectTransform,0.04f,0,0.96f,0.2f); cost.alignment=TextAlignmentOptions.Center;
            var tags=Text("SynergyMarks",root,"",fontSize-8); Area(tags.rectTransform,0.58f,0.76f,0.82f,1); tags.alignment=TextAlignmentOptions.MidlineRight; tags.richText=true;
            var view=root.gameObject.AddComponent<CareScheduleWeekView>(); var data=new SerializedObject(view);
            data.FindProperty("week").intValue=index; Set(data,"button",button); Set(data,"nameText",name);
            Set(data,"costText",cost); Set(data,"synergyText",tags); Set(data,"icon",icon); Set(data,"highlight",highlight); data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
        public static CareScheduleHighlight ConfigureHighlight(RectTransform rect, bool lessonBadge)
        {
            var outline = rect.GetComponent<Outline>();
            if (outline == null) outline = rect.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3, -3);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            var hint = rect.GetComponent<CareScheduleHighlight>();
            if (hint == null) hint = rect.gameObject.AddComponent<CareScheduleHighlight>();
            var frameRect = rect.Find("SynergyFrame") as RectTransform;
            if (frameRect != null) DestroyImmediate(frameRect.gameObject);
            var data = new SerializedObject(hint);
            Set(data, "outline", outline);
            if (lessonBadge)
            {
                var badgeRect = rect.Find("SynergyStar") as RectTransform;
                if (badgeRect == null)
                {
                    badgeRect = new GameObject("SynergyStar", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
                    badgeRect.SetParent(rect, false);
                }
                badgeRect.anchorMin = badgeRect.anchorMax = Vector2.one;
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.sizeDelta = new Vector2(24, 24); badgeRect.anchoredPosition = new Vector2(-15, -15);
                badgeRect.SetAsLastSibling();
                var star = badgeRect.GetComponent<UnityEngine.UI.Image>();
                star.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/WorkGame/Art/UI/Icon/Star.png");
                star.preserveAspect = true; star.raycastTarget = false;
                badgeRect.gameObject.SetActive(false);
                Set(data, "starBadge", star);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return hint;
        }
        private static LayoutElement Size(RectTransform rect,float width=-1,float height=-1,float flexWidth=0,float flexHeight=0)
        {
            var size=rect.gameObject.AddComponent<LayoutElement>(); size.preferredWidth=width; size.preferredHeight=height;
            size.flexibleWidth=flexWidth; size.flexibleHeight=flexHeight; return size;
        }
        private static HorizontalLayoutGroup Horizontal(RectTransform rect,float spacing)
        {
            var layout=rect.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing=spacing;
            layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandWidth=false; layout.childForceExpandHeight=true; return layout;
        }
        private static VerticalLayoutGroup Vertical(RectTransform rect,float spacing)
        {
            var layout=rect.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=spacing;
            layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandWidth=true; layout.childForceExpandHeight=false; return layout;
        }
        private Button Button(string name,Transform parent,string label)
        {
            var rect=Rect(name,parent); var image=Image(rect,cardColor,true); var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image;
            if(!string.IsNullOrEmpty(label)) { var text=Text("Text",rect,label,fontSize-6); Area(text.rectTransform,0,0,1,1,4); text.alignment=TextAlignmentOptions.Center; }
            return button;
        }
        private TMP_Text Text(string name,Transform parent,string value,int size)
        {
            var rect=Rect(name,parent); var text=rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSize=size;
            text.text=value; text.color=Ink; text.raycastTarget=false; text.richText=false; text.alignment=TextAlignmentOptions.MidlineLeft; return text;
        }
        private static Image Image(RectTransform rect,Color color,bool raycast=false)
        {
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=raycast; return image;
        }
        private static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false); return rect;
        }
        private static void Area(RectTransform rect,float x0,float y0,float x1,float y1,float padding=0)
        {
            rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1); rect.offsetMin=Vector2.one*padding; rect.offsetMax=Vector2.one*-padding;
        }
        private static void Set(SerializedObject data,string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
        private static void SetList(SerializedObject data,string name,UnityEngine.Object[] values)
        {
            var field=data.FindProperty(name); field.arraySize=values.Length;
            for(int i=0;i<values.Length;i++) field.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        }
    }
}
