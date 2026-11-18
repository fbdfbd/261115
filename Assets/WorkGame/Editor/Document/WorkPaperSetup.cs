using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Editor
{
    public static class WorkPaperSetup
    {
        [MenuItem("Work Game/UI/Connect Selected Paper")]
        public static void ConnectSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) { Debug.LogWarning("문서 전체 루트를 선택하세요."); return; }
            try
            {
                if (EditorUtility.IsPersistent(selected))
                {
                    string path = AssetDatabase.GetAssetPath(selected);
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        Connect(root, false);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                else
                {
                    Undo.IncrementCurrentGroup();
                    int group = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Connect Paper Details");
                    Connect(selected, true);
                    Undo.CollapseUndoOperations(group);
                }
                Debug.Log("상세 서류 연결 완료: " + selected.name, selected);
            }
            catch (Exception e) { Debug.LogError(e.Message, selected); }
        }

        [MenuItem("Work Game/UI/Connect Selected Paper", true)]
        private static bool CanConnect() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void Connect(GameObject root, bool undo)
        {
            // Validate the authored hierarchy before adding or changing any components.
            var title = Need<TMP_Text>(root.transform, "Header/DocumentTitle");
            var number = Need<TMP_Text>(root.transform, "Header/DocumentInfo/Left");
            var month = Need<TMP_Text>(root.transform, "Header/DocumentInfo/Right");
            var rule = Need<TMP_Text>(root.transform, "Header/WorkRule");
            var content = NeedTransform(root.transform, "Body/GraphArea/GraphContent");
            var candidates = content.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Candidates");
            if (candidates == null) throw new InvalidOperationException("GraphContent 안의 Candidates를 찾지 못했습니다.");
            var graphRoot = candidates.parent;
            var metric = Need<TMP_Text>(graphRoot, "MetricTitle");
            var unit = graphRoot.Find("UnitLabel")?.GetComponent<TMP_Text>();
            var rows = new Transform[3];
            var choiceRoots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                string letter = ((char)('A' + i)).ToString();
                rows[i] = NeedTransform(candidates, "Candidate" + letter);
                Need<TMP_Text>(rows[i], "CandidateName");
                choiceRoots[i] = NeedTransform(root.transform, "Body/ChoicesArea/ChoiceButtons/Choice" + letter);
                Need<Button>(choiceRoots[i], "");
                Need<TMP_Text>(choiceRoots[i], "ChoiceName");
                Need<Image>(choiceRoots[i], "Fill");
            }
            bool bars = rows[0].Find("BarSlider") != null;
            bool dots = rows[0].Find("Dot1") != null;
            foreach (var row in rows)
            {
                if (bars) { Need<Slider>(row, "BarSlider"); Need<TMP_Text>(row, "ValueLabel"); }
                else if (dots) for (int i = 1; i <= 5; i++) Need<Image>(row, "Dot" + i);
                else Need<TMP_Text>(row, "ValueLabel");
            }

            WorkGraphView graph;
            if (bars) graph = GetOrAdd<WorkBarGraphView>(graphRoot.gameObject, undo);
            else if (dots) graph = GetOrAdd<WorkDotsGraphView>(graphRoot.gameObject, undo);
            else graph = GetOrAdd<WorkGradeGraphView>(graphRoot.gameObject, undo);
            var graphData = new SerializedObject(graph);
            graphData.FindProperty("metricTitle").objectReferenceValue = metric;
            graphData.FindProperty("unitText").objectReferenceValue = unit;
            var style = graphData.FindProperty("documentStyle");
            if (style != null && style.objectReferenceValue == null)
                style.objectReferenceValue = AssetDatabase.LoadAssetAtPath<WorkDocumentStyleSO>("Assets/WorkGame/Data/WorkDocumentStyle.asset");
            var items = graphData.FindProperty("items");
            items.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Root").objectReferenceValue = rows[i].gameObject;
                item.FindPropertyRelative("NameText").objectReferenceValue = Need<TMP_Text>(rows[i], "CandidateName");
                if (bars)
                {
                    item.FindPropertyRelative("Bar").objectReferenceValue = Need<Slider>(rows[i], "BarSlider");
                    item.FindPropertyRelative("ValueText").objectReferenceValue = Need<TMP_Text>(rows[i], "ValueLabel");
                }
                else if (dots)
                {
                    var images = item.FindPropertyRelative("Dots");
                    images.arraySize = 5;
                    for (int dot = 0; dot < 5; dot++)
                        images.GetArrayElementAtIndex(dot).objectReferenceValue = Need<Image>(rows[i], "Dot" + (dot + 1));
                }
                else item.FindPropertyRelative("ValueText").objectReferenceValue = Need<TMP_Text>(rows[i], "ValueLabel");
            }
            Apply(graphData, undo);

            var buttons = new WorkChoiceView[3];
            for (int i = 0; i < 3; i++)
            {
                buttons[i] = GetOrAdd<WorkChoiceView>(choiceRoots[i].gameObject, undo);
                var buttonData = new SerializedObject(buttons[i]);
                buttonData.FindProperty("button").objectReferenceValue = choiceRoots[i].GetComponent<Button>();
                buttonData.FindProperty("nameText").objectReferenceValue = Need<TMP_Text>(choiceRoots[i], "ChoiceName");
                buttonData.FindProperty("fill").objectReferenceValue = Need<Image>(choiceRoots[i], "Fill");
                Apply(buttonData, undo);
            }

            var panel = GetOrAdd<WorkPaperPanelView>(root, undo);
            GetOrAdd<WorkPaperClickBlocker>(root, undo);
            var panelData = new SerializedObject(panel);
            panelData.FindProperty("titleText").objectReferenceValue = title;
            panelData.FindProperty("numberText").objectReferenceValue = number;
            panelData.FindProperty("monthText").objectReferenceValue = month;
            panelData.FindProperty("ruleText").objectReferenceValue = rule;
            panelData.FindProperty("graphView").objectReferenceValue = graph;
            var choices = panelData.FindProperty("choices");
            choices.arraySize = 3;
            for (int i = 0; i < 3; i++) choices.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            Apply(panelData, undo);
        }

        private static T GetOrAdd<T>(GameObject target, bool undo) where T : Component
        {
            var value = target.GetComponent<T>();
            return value != null ? value : undo ? Undo.AddComponent<T>(target) : target.AddComponent<T>();
        }

        private static Transform NeedTransform(Transform root, string path)
        {
            var found = string.IsNullOrEmpty(path) ? root : root.Find(path);
            return found != null ? found : throw new InvalidOperationException("연결할 경로가 없습니다: " + path);
        }

        private static T Need<T>(Transform root, string path) where T : Component
        {
            var target = NeedTransform(root, path);
            var value = target.GetComponent<T>();
            return value != null ? value : throw new InvalidOperationException($"{target.name}에 {typeof(T).Name}이 없습니다.");
        }

        private static void Apply(SerializedObject data, bool undo)
        {
            if (undo) data.ApplyModifiedProperties();
            else data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data.targetObject);
            if (PrefabUtility.IsPartOfPrefabInstance(data.targetObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(data.targetObject);
        }
    }
}
