using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Tests
{
    public sealed class WorkDocumentStyleTests
    {
        private const string StylePath = "Assets/WorkGame/Data/WorkDocumentStyle.asset";

        [Test]
        public void ColorsAreDistinctAndStableAcrossOtherPapersWithoutChangingGameplayRandom()
        {
            var style = ScriptableObject.CreateInstance<WorkDocumentStyleSO>();
            var previousState = Random.state;
            try
            {
                Random.InitState(1234);
                float expected = Random.value;
                Random.InitState(1234);
                var paper = Paper(0, PaperDisplay.HorizontalBar);
                var colors = Enumerable.Range(0, 3).Select(i => style.GetBarColor(paper, i)).ToArray();
                for (int id = 1; id <= 100; id++)
                {
                    var other = Paper(id, PaperDisplay.VerticalBar);
                    Assert.That(Enumerable.Range(0, 3).Select(i => style.GetBarColor(other, i)).Distinct().Count(), Is.EqualTo(3));
                }
                Assert.That(Enumerable.Range(0, 3).Select(i => style.GetBarColor(paper, i)), Is.EqualTo(colors));
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally
            {
                Random.state = previousState;
                Object.DestroyImmediate(style);
            }
        }

        [TestCase("WorkDocument_Graph_H", PaperDisplay.HorizontalBar)]
        [TestCase("WorkDocument_Graph_V", PaperDisplay.VerticalBar)]
        public void AuthoredBarPrefabsUseSharedStyleAndKeepColorsOnRefresh(string name, PaperDisplay display)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkGame/Prefab/" + name + ".prefab");
            var root = Object.Instantiate(prefab.GetComponentInChildren<WorkBarGraphView>(true).gameObject);
            try
            {
                var view = root.GetComponent<WorkBarGraphView>();
                var data = new SerializedObject(view);
                Assert.That(data.FindProperty("documentStyle").objectReferenceValue, Is.EqualTo(AssetDatabase.LoadAssetAtPath<WorkDocumentStyleSO>(StylePath)));
                var paper = Paper(101, display);
                view.Show(paper);
                var items = data.FindProperty("items");
                var colors = new Color[3];
                for (int i = 0; i < 3; i++)
                {
                    var bar = (Slider)items.GetArrayElementAtIndex(i).FindPropertyRelative("Bar").objectReferenceValue;
                    colors[i] = bar.fillRect.GetComponent<Graphic>().color;
                }
                Assert.That(colors.Distinct().Count(), Is.EqualTo(3));
                view.Show(Paper(102, display));
                view.Show(paper, true);
                for (int i = 0; i < 3; i++)
                {
                    var bar = (Slider)items.GetArrayElementAtIndex(i).FindPropertyRelative("Bar").objectReferenceValue;
                    Assert.That(bar.fillRect.GetComponent<Graphic>().color, Is.EqualTo(colors[i]));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NumericUnitsAreMutedAndReusedGradeLabelsHaveNoUnitMarkup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkGame/Prefab/WorkDocument_Grade.prefab");
            var root = Object.Instantiate(prefab.GetComponentInChildren<WorkGradeGraphView>(true).gameObject);
            try
            {
                var view = root.GetComponent<WorkGradeGraphView>();
                var data = new SerializedObject(view);
                Assert.That(data.FindProperty("documentStyle").objectReferenceValue, Is.EqualTo(AssetDatabase.LoadAssetAtPath<WorkDocumentStyleSO>(StylePath)));
                var label = new SerializedObject(data.FindProperty("items").GetArrayElementAtIndex(0).FindPropertyRelative("ValueText").objectReferenceValue);
                view.Show(Paper(1, PaperDisplay.Number, "kWh"));
                label.Update();
                Assert.That(label.FindProperty("m_text").stringValue, Is.EqualTo("1<color=#8A9481FF>kWh</color>"));
                Assert.That(label.FindProperty("m_isRichText").boolValue, Is.True);
                view.Show(Paper(2, PaperDisplay.Grade));
                label.Update();
                Assert.That(label.FindProperty("m_text").stringValue, Is.EqualTo("D"));
                view.Show(Paper(3, PaperDisplay.Number));
                label.Update();
                Assert.That(label.FindProperty("m_text").stringValue, Is.EqualTo("1"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static WorkPaper Paper(int id, PaperDisplay display, string unit = "") =>
            new WorkPaper(id, 1, "test", "Test", "Target", "Metric", unit, PickRule.Highest, display,
                new[] { new WorkChoice("A", 1), new WorkChoice("B", 2), new WorkChoice("C", 3) });
    }
}
