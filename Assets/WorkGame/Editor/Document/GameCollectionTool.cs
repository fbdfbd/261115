using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class GameCollectionTool : EditorWindow
    {
        private const string ScenePath = "Assets/WorkGame/Scenes/01_Title.unity";
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private CareConfigSO config;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2 pageSize = new Vector2(1760, 980);
        [SerializeField] private float gap = 12;
        [SerializeField] private float imageHeight = 420;
        [SerializeField] private int fontSize = 26;
        [SerializeField] private Color pageColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] private Color cardColor = new Color(0.89f, 0.91f, 0.87f);
        private static readonly Color Ink = new Color(0.17f, 0.21f, 0.19f);
        private string message;
        [MenuItem("Work Game/UI/Game Collection Tool")]
        public static void Open() => GetWindow<GameCollectionTool>("달이의 성장 기록");
        private void OnEnable()
        {
            if (pageSize == new Vector2(1300, 800)) pageSize = new Vector2(1760, 980);
            if (imageHeight == 280) imageHeight = 420;
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (config == null) config = AssetDatabase.LoadAssetAtPath<CareConfigSO>("Assets/WorkGame/Data/Care/CareConfig.asset");
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkGame/Prefab/GameCollection.prefab");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("5×5 목록 + 조건을 공개하는 상세 페이지", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            config = (CareConfigSO)EditorGUILayout.ObjectField("육성 데이터", config, typeof(CareConfigSO), false);
            pageSize = EditorGUILayout.Vector2Field("창 크기", pageSize);
            gap = EditorGUILayout.Slider("카드 간격", gap, 8, 24);
            imageHeight = EditorGUILayout.Slider("상세 이미지 높이", imageHeight, 280, 520);
            fontSize = EditorGUILayout.IntSlider("본문 크기", fontSize, 24, 30);
            pageColor = EditorGUILayout.ColorField("창 배경", pageColor);
            cardColor = EditorGUILayout.ColorField("카드 배경", cardColor);
            EditorGUILayout.HelpBox("16:9 이미지 갤러리입니다. 이름은 별도 배경 없이 이미지 위에 겹칩니다. 5×5 앵커와 비율 조절로 창 크기에 따라 맞춰집니다. 상세 본문만 스크롤합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || config == null || pageSize.x < 1000 || pageSize.y < 700))
                if (GUILayout.Button("모음집 프리팹 생성", GUILayout.Height(34))) Save();
            prefab = (GameObject)EditorGUILayout.ObjectField("배치할 프리팹", prefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || prefab == null))
                if (GUILayout.Button("01_Title에 배치 및 모음집 연결", GUILayout.Height(34))) Place();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("모음집 저장", "GameCollection", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("CollectionBuild", typeof(RectTransform)); parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(Build(parent.transform).gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; message = "생성 완료. 배치 버튼으로 타이틀에 연결하세요.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("GameCollection", parent); Area(root, 0, 0, 1, 1);
            var group = root.gameObject.AddComponent<CanvasGroup>(); var view = root.gameObject.AddComponent<GameCollectionView>();
            var controller = root.gameObject.AddComponent<GameCollectionRoot>();
            var background = Rect("BackgroundClose", root); Area(background, 0, 0, 1, 1);
            var backgroundImage = Image(background, new Color(0, 0, 0, 0.72f), true);
            var backgroundButton = background.gameObject.AddComponent<Button>(); backgroundButton.targetGraphic = backgroundImage;
            backgroundButton.transition = Selectable.Transition.None;
            var page = Rect("Page", root); page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = pageSize; Image(page, pageColor, true);
            var title = Text("Title", page, "달이의 성장 기록", 42); Area(title.rectTransform, 0.2f, 0.87f, 0.8f, 0.97f);
            var count = Text("Count", page, "발견 0 / 25", 26); Area(count.rectTransform, 0.03f, 0.89f, 0.18f, 0.96f);
            var back = Button("Back", page, "목록으로"); Area((RectTransform)back.transform, 0.03f, 0.89f, 0.18f, 0.96f);
            var close = Button("Close", page, "닫기"); Area((RectTransform)close.transform, 0.86f, 0.89f, 0.97f, 0.96f);
            var line = Rect("HeaderDivider", page); Area(line, 0.04f, 0.85f, 0.96f, 0.85f); line.sizeDelta = new Vector2(0, 2); Image(line, cardColor);
            var list = Rect("ListPage", page); Area(list, 0.04f, 0.06f, 0.96f, 0.84f);
            var grid = Rect("Cards", list); Area(grid, 0, 0, 1, 1); FitPicture(grid);
            var cards = new List<GameCollectionItemView>();
            for (int i = 0; i < 25; i++)
            {
                int x = i % 5, y = i / 5;
                var card = Card(grid, i);
                var rect = (RectTransform)card.transform; Area(rect, x / 5f, 1 - (y + 1) / 5f, (x + 1) / 5f, 1 - y / 5f);
                rect.offsetMin = Vector2.one * (gap / 2); rect.offsetMax = -rect.offsetMin; cards.Add(card);
            }
            var detail = Rect("DetailPage", page); Area(detail, 0.04f, 0.06f, 0.96f, 0.84f);
            Image(detail, Color.clear, true); detail.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", detail); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 16;
            layout.childAlignment = TextAnchor.UpperCenter; layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var imageRow = Rect("ImageRow", content); imageRow.sizeDelta = new Vector2(0, imageHeight);
            var imageFrame = Rect("ImageFrame", imageRow); Area(imageFrame, 0, 0, 1, 1); FitPicture(imageFrame); Image(imageFrame, cardColor);
            var picture = Picture(imageFrame);
            var dim = Rect("LockedDim", imageFrame); Area(dim, 0, 0, 1, 1); Image(dim, new Color(0, 0, 0, 0.85f));
            var description = TextBlock("Description", content, "엔딩 설명", fontSize);
            var heading = Text("ConditionsTitle", content, "달성 조건", 32); heading.rectTransform.sizeDelta = new Vector2(0, 44);
            var main = TextBlock("MainConditions", content, "지식 A 이상", fontSize);
            var detailsTitle = Text("DetailsTitle", content, "세부 능력치", 28); detailsTitle.rectTransform.sizeDelta = new Vector2(0, 40);
            var detailRow = Rect("DetailConditions", content); detailRow.sizeDelta = new Vector2(0, 166);
            var columns = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                columns[i] = Text(((CareStat)i).ToString(), detailRow, CareStatNames.GetName((CareStat)i), fontSize);
                Area(columns[i].rectTransform, i / 4f, 0, (i + 1) / 4f, 1);
                columns[i].rectTransform.offsetMin = new Vector2(12, 0); columns[i].rectTransform.offsetMax = new Vector2(-12, 0);
                columns[i].alignment = TextAlignmentOptions.TopLeft; columns[i].overflowMode = TextOverflowModes.Overflow;
            }
            var scroll = detail.gameObject.AddComponent<ScrollRect>(); scroll.viewport = detail; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            var fields = new SerializedObject(view);
            Set(fields, "panelGroup", group); Set(fields, "closeButton", close); Set(fields, "backgroundButton", backgroundButton);
            Set(fields, "backButton", back); Set(fields, "titleText", title); Set(fields, "countText", count);
            Set(fields, "listPage", list.gameObject); Set(fields, "detailPage", detail.gameObject);
            Set(fields, "detailImage", picture); Set(fields, "detailDim", dim.gameObject); Set(fields, "descriptionText", description);
            Set(fields, "mainConditionsText", main); Set(fields, "detailConditionsTitle", detailsTitle); Set(fields, "detailConditionsRow", detailRow);
            Set(fields, "detailScroll", scroll); Set(fields, "detailContent", content);
            var array = fields.FindProperty("items"); array.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            array = fields.FindProperty("detailConditions"); array.arraySize = columns.Length;
            for (int i = 0; i < columns.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = columns[i];
            fields.ApplyModifiedPropertiesWithoutUndo();
            fields = new SerializedObject(controller); Set(fields, "config", config); Set(fields, "view", view); fields.ApplyModifiedPropertiesWithoutUndo();
            detail.gameObject.SetActive(false); back.gameObject.SetActive(false); return root;
        }
        private GameCollectionItemView Card(Transform parent, int index)
        {
            var root = Rect("Ending" + (index + 1).ToString("00"), parent); var image = Image(root, Color.clear, true);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var frame = Rect("ImageFrame", root); Area(frame, 0, 0, 1, 1); FitPicture(frame); Image(frame, cardColor);
            var picture = Picture(frame);
            var dim = Rect("LockedDim", frame); Area(dim, 0, 0, 1, 1); Image(dim, new Color(0, 0, 0, 0.85f));
            var name = Text("Name", frame, "???", 26); Area(name.rectTransform, 0.04f, 0.02f, 0.96f, 0.32f); name.color = Color.white;
            var item = root.gameObject.AddComponent<GameCollectionItemView>(); var fields = new SerializedObject(item);
            Set(fields, "button", button); Set(fields, "picture", picture); Set(fields, "lockedDim", dim.gameObject); Set(fields, "nameText", name);
            fields.ApplyModifiedPropertiesWithoutUndo(); return item;
        }
        private Image Picture(Transform parent)
        {
            var rect = Rect("Picture", parent); Area(rect, 0, 0, 1, 1); var image = Image(rect, Color.white);
            image.type = UnityEngine.UI.Image.Type.Simple; image.preserveAspect = true; image.enabled = false; return image;
        }
        private static void FitPicture(RectTransform rect)
        {
            var fit = rect.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
        }
        private TMP_Text TextBlock(string name, Transform parent, string value, int size)
        {
            var text = Text(name, parent, value, size); text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            var fit = text.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; return text;
        }
        private void Place()
        {
            try
            {
                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.isLoaded || SceneManager.sceneCount != 1)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
                var title = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameTitleRoot>(true)).FirstOrDefault();
                if (title == null) { message = "먼저 Game Title Tool로 타이틀을 배치하세요."; return; }
                var collection = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameCollectionRoot>(true)).FirstOrDefault();
                if (collection == null)
                {
                    var parent = title.GetComponentInParent<Canvas>().transform;
                    collection = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).GetComponent<GameCollectionRoot>();
                }
                var fields = new SerializedObject(title); Set(fields, "collectionRoot", collection); fields.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = collection.gameObject; message = "배치·모음집 버튼 연결 완료. 같은 패널을 중복 생성하지 않습니다.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
        }
        private Button Button(string name, Transform parent, string label)
        {
            var rect = Rect(name, parent); var image = Image(rect, cardColor, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var text = Text("Label", rect, label, 24); Area(text.rectTransform, 0, 0, 1, 1); return button;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = value;
            text.color = Ink; text.raycastTarget = false; text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text;
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
