using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class GameTitleTool : EditorWindow
    {
        private const string ScenePath = "Assets/WorkGame/Scenes/01_Title.unity";
        private const string PrefabPath = "Assets/WorkGame/Prefab/GameTitle.prefab";
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 menuSize = new Vector2(440, 560);
        [SerializeField] private float leftMargin = 96;
        [SerializeField] private float spacing = 16;
        [SerializeField] private int fontSize = 34;
        [SerializeField] private Color buttonColor = new Color(0.89f, 0.91f, 0.87f);
        [SerializeField] private GameObject titlePrefab;
        [SerializeField] private GameObject optionsPrefab;
        [SerializeField] private GameObject slotsPrefab;
        private string message;

        [MenuItem("Work Game/UI/Game Title Tool")]
        public static void Open() => GetWindow<GameTitleTool>("타이틀 메뉴");
        private void OnEnable()
        {
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/RIDIBatang SDF.asset");
            if (titlePrefab == null) titlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (optionsPrefab == null) optionsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkGame/Prefab/GameOptions.prefab");
            if (slotsPrefab == null) slotsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkGame/Prefab/GameSaveSlots.prefab");
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("화면 왼쪽 · 새로하기 / 이어하기 / 모음집 / 설정 / 종료", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("폰트", font, typeof(TMP_FontAsset), false);
            menuSize = EditorGUILayout.Vector2Field("메뉴 크기", menuSize);
            leftMargin = EditorGUILayout.FloatField("왼쪽 여백", leftMargin);
            spacing = EditorGUILayout.Slider("버튼 간격", spacing, 8, 24);
            fontSize = EditorGUILayout.IntSlider("글자 크기", fontSize, 28, 40);
            buttonColor = EditorGUILayout.ColorField("버튼 배경", buttonColor);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || font == null || menuSize.x < 300 || menuSize.y < 408 + spacing * 4))
                if (GUILayout.Button("타이틀 프리팹 생성", GUILayout.Height(32))) SavePrefab();
            EditorGUILayout.Space();
            titlePrefab = (GameObject)EditorGUILayout.ObjectField("타이틀 프리팹", titlePrefab, typeof(GameObject), false);
            optionsPrefab = (GameObject)EditorGUILayout.ObjectField("옵션 프리팹", optionsPrefab, typeof(GameObject), false);
            slotsPrefab = (GameObject)EditorGUILayout.ObjectField("슬롯 프리팹", slotsPrefab, typeof(GameObject), false);
            EditorGUILayout.HelpBox("01_Title에 전용 Canvas·EventSystem과 메뉴·옵션·슬롯 프리팹을 배치하고 연결합니다. 모음집은 Game Collection Tool에서 별도로 배치·연결합니다. 이미 타이틀 메뉴가 있으면 중복 생성하지 않습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || titlePrefab == null || optionsPrefab == null || slotsPrefab == null))
                if (GUILayout.Button("01_Title에 배치 및 연결", GUILayout.Height(34))) Place();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void SavePrefab()
        {
            string path = EditorUtility.SaveFilePanelInProject("타이틀 저장", "GameTitle", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var parent = new GameObject("TitleBuild", typeof(RectTransform)); parent.hideFlags = HideFlags.HideAndDontSave; parent.SetActive(false);
            try
            {
                titlePrefab = PrefabUtility.SaveAsPrefabAsset(Build(parent.transform).gameObject, path);
                if (titlePrefab == null) throw new InvalidOperationException("프리팹을 저장하지 못했습니다.");
                Selection.activeObject = titlePrefab; message = "프리팹 생성 완료. 아래 배치 버튼으로 씬에 연결하세요.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
            finally { DestroyImmediate(parent); }
        }
        private RectTransform Build(Transform parent)
        {
            var root = Rect("GameTitle", parent); Area(root, 0, 0, 1, 1);
            var view = root.gameObject.AddComponent<GameTitleView>(); var controller = root.gameObject.AddComponent<GameTitleRoot>();
            var menu = Rect("LeftMenu", root); menu.anchorMin = menu.anchorMax = new Vector2(0, 0.5f);
            menu.pivot = new Vector2(0, 0.5f); menu.anchoredPosition = new Vector2(leftMargin, 0); menu.sizeDelta = menuSize;
            var group = menu.gameObject.AddComponent<CanvasGroup>();
            var background = menu.gameObject.AddComponent<Image>(); background.color = Color.clear; background.raycastTarget = false;
            var layout = menu.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = spacing; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var buttons = new[] { Button("NewGame", menu, "새로하기"), Button("Continue", menu, "이어하기"),
                Button("Collection", menu, "모음집"), Button("Settings", menu, "설정"), Button("Quit", menu, "종료") };
            var error = Text("Error", root, "", 24); error.rectTransform.anchorMin = error.rectTransform.anchorMax = new Vector2(0, 0.5f);
            error.rectTransform.pivot = new Vector2(0, 1); error.rectTransform.anchoredPosition = new Vector2(leftMargin, -menuSize.y / 2 - 12);
            error.rectTransform.sizeDelta = new Vector2(menuSize.x, 64); error.color = new Color(0.7f, 0.2f, 0.2f);
            var fields = new SerializedObject(view); Set(fields, "menuGroup", group); Set(fields, "errorText", error);
            string[] names = { "newGameButton", "continueButton", "collectionButton", "settingsButton", "quitButton" };
            for (int i = 0; i < names.Length; i++) Set(fields, names[i], buttons[i]);
            fields.ApplyModifiedPropertiesWithoutUndo();
            fields = new SerializedObject(controller); Set(fields, "view", view); fields.ApplyModifiedPropertiesWithoutUndo();
            return root;
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
                var existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameTitleRoot>(true)).FirstOrDefault();
                if (existing != null) { Selection.activeGameObject = existing.gameObject; message = "이미 배치되어 있습니다. 기존 메뉴를 선택했습니다."; return; }
                var canvasObject = new GameObject("Canvas_Title", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.GetComponent<Canvas>().sortingOrder = 100;
                var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
                var title = (GameObject)PrefabUtility.InstantiatePrefab(titlePrefab, canvasObject.transform);
                var options = (GameObject)PrefabUtility.InstantiatePrefab(optionsPrefab, canvasObject.transform);
                var slots = (GameObject)PrefabUtility.InstantiatePrefab(slotsPrefab, canvasObject.transform);
                var fields = new SerializedObject(title.GetComponent<GameTitleRoot>());
                Set(fields, "optionsRoot", options.GetComponent<GameOptionsRoot>()); Set(fields, "saveSlotsRoot", slots.GetComponent<GameSaveSlotsRoot>());
                fields.ApplyModifiedPropertiesWithoutUndo();
                if (!scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<EventSystem>(true) != null)) CreateEventSystem(scene);
                EnsureBuildScene(ScenePath); EnsureBuildScene("Assets/WorkGame/Scenes/02_Game.unity");
                SceneManager.SetActiveScene(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = title; message = "01_Title 배치·연결·저장 완료. 새로하기와 이어하기를 테스트하세요.";
            }
            catch (Exception error) { message = error.Message; Debug.LogException(error); }
        }
        private static void CreateEventSystem(Scene scene)
        {
            var root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(root, scene);
            var module = root.GetComponent<InputSystemUIInputModule>();
            string path = AssetDatabase.GUIDToAssetPath("ca9f5fa95ffab41fb9a615ab714db018");
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (actions == null) { module.AssignDefaultActions(); return; }
            module.actionsAsset = actions;
            var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>().ToArray();
            InputActionReference Find(string actionName)
            {
                var action = actions.FindAction(actionName);
                return references.FirstOrDefault(item => item.action != null && item.action.id == action?.id);
            }
            module.point = Find("UI/Point"); module.move = Find("UI/Navigate"); module.submit = Find("UI/Submit");
            module.cancel = Find("UI/Cancel"); module.leftClick = Find("UI/Click"); module.scrollWheel = Find("UI/ScrollWheel");
            module.rightClick = Find("UI/RightClick"); module.middleClick = Find("UI/MiddleClick");
        }
        private static void EnsureBuildScene(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList(); var item = scenes.FirstOrDefault(scene => scene.path == path);
            if (item == null) scenes.Add(new EditorBuildSettingsScene(path, true)); else item.enabled = true;
            if (path == ScenePath)
            {
                var title = scenes.First(scene => scene.path == path);
                scenes.Remove(title); scenes.Insert(0, title);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        private Button Button(string name, Transform parent, string label)
        {
            var rect = Rect(name, parent); rect.sizeDelta = new Vector2(0, 72);
            var image = rect.gameObject.AddComponent<Image>(); image.color = buttonColor;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var text = Text("Label", rect, label, fontSize); Area(text.rectTransform, 0.04f, 0, 0.96f, 1); return button;
        }
        private TMP_Text Text(string name, Transform parent, string value, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = value;
            text.color = new Color(0.17f, 0.21f, 0.19f); text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text;
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
