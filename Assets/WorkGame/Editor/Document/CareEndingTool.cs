using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public sealed class CareEndingTool : EditorWindow
    {
        [SerializeField] private bool showFinalCareerImage = true;
        [SerializeField] private float imageSeconds = 0.3f;
        [SerializeField] private float titleFadeSeconds = 0.6f;
        private string message;
        [MenuItem("Work Game/UI/Care Ending Tool")]
        public static void Open() => GetWindow<CareEndingTool>("달이 엔딩");
        private void OnGUI()
        {
            EditorGUILayout.LabelField("풀스크린 직업 이미지 · 공통 다이얼로그", EditorStyles.boldLabel);
            showFinalCareerImage = EditorGUILayout.Toggle("마지막 직업 이미지", showFinalCareerImage);
            imageSeconds = EditorGUILayout.Slider("이미지 페이드", imageSeconds, 0, 1);
            titleFadeSeconds = EditorGUILayout.Slider("타이틀 페이드", titleFadeSeconds, 0, 2);
            EditorGUILayout.HelpBox("WorkRoot의 기존 다이얼로그를 재사용합니다. 이 View의 Canvas 순서는 다이얼로그보다 낮게 설정하세요. 마지막 직업 이미지를 끄면 대화 종료 후 바로 페이드아웃합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if (GUILayout.Button("엔딩 프리팹 생성", GUILayout.Height(34))) Save();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        private void Save()
        {
            string path = EditorUtility.SaveFilePanelInProject("엔딩 저장", "CareEnding", "prefab", "저장 위치", "Assets/WorkGame/Prefab");
            if (string.IsNullOrEmpty(path)) return;
            var owner = new GameObject("EndingBuild", typeof(RectTransform));
            owner.hideFlags = HideFlags.HideAndDontSave; owner.SetActive(false);
            try
            {
                var root = Rect("CareEnding", owner.transform);
                var matte = root.gameObject.AddComponent<Image>(); matte.color = Color.black;
                var group = root.gameObject.AddComponent<CanvasGroup>();
                var click = root.gameObject.AddComponent<CareDialogueClickArea>();
                var view = root.gameObject.AddComponent<CareEndingView>();
                var picture = Rect("Picture", root);
                var pictureGroup = picture.gameObject.AddComponent<CanvasGroup>();
                var image = picture.gameObject.AddComponent<Image>(); image.enabled = false; image.raycastTarget = false;
                var fields = new SerializedObject(view);
                fields.FindProperty("panelGroup").objectReferenceValue = group;
                fields.FindProperty("clickArea").objectReferenceValue = click;
                fields.FindProperty("picture").objectReferenceValue = image;
                fields.FindProperty("pictureGroup").objectReferenceValue = pictureGroup;
                fields.FindProperty("showFinalCareerImage").boolValue = showFinalCareerImage;
                fields.FindProperty("imageSeconds").floatValue = imageSeconds;
                fields.FindProperty("titleFadeSeconds").floatValue = titleFadeSeconds;
                fields.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
                if (prefab == null) throw new InvalidOperationException("프리팹 저장에 실패했습니다.");
                Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
                message = "생성 완료. WorkRoot의 CareEndingView에 연결하세요.";
            }
            catch (Exception e) { message = e.Message; Debug.LogException(e); }
            finally { DestroyImmediate(owner); }
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
    }
}
