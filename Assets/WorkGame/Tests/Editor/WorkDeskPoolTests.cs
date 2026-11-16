using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace WorkGame.Tests
{
    public sealed class WorkDeskPoolTests
    {
        private GameObject root;
        private RectTransform area;
        private WorkDeskView desk;
        private WorkPaperItemView first;
        private WorkPaperItemView second;
        private WorkSystem system;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DeskTest", typeof(RectTransform));
            desk = root.AddComponent<WorkDeskView>();
            area = (RectTransform)new GameObject("Area", typeof(RectTransform)).transform;
            area.SetParent(root.transform);
            area.sizeDelta = new Vector2(1200, 800);
            first = MakePaper("First");
            second = MakePaper("Second");
            SetField(desk, "looseRoot", area);
            SetField(desk, "paperViews", new List<WorkPaperItemView> { first, second });
            var papers = new List<WorkPaper>();
            for (int i = 1; i <= 4; i++)
                papers.Add(new WorkPaper(i, 1, "01", "Test", "제품", "점수", "", PickRule.Highest, PaperDisplay.Number,
                    new[] { new WorkChoice("A", 1), new WorkChoice("B", 2), new WorkChoice("C", 3) }));
            system = new WorkSystem(new WorkSettings(), papers);
            system.StartRun();
            desk.SetWorking(true);
        }

        [TearDown]
        public void TearDown()
        {
            system.Dispose();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CompletedSlotReusesSameObjectWithoutMovingOtherPapers()
        {
            desk.ShowPapers(system.MonthPapers, 1);
            Assert.That(desk.MaxVisible, Is.EqualTo(2));
            Assert.That(first.PaperId, Is.EqualTo(1));
            Vector2 secondPosition = second.Rect.anchoredPosition;
            SubmitFirst();
            desk.ShowPapers(system.MonthPapers, 1);
            Assert.That(first.PaperId, Is.EqualTo(3));
            Assert.That(second.PaperId, Is.EqualTo(2));
            Assert.That(second.Rect.anchoredPosition, Is.EqualTo(secondPosition));
            Assert.That(area.childCount, Is.EqualTo(2));
        }

        [Test]
        public void WaitingForAnimationDelaysReuseUntilFinishRemove()
        {
            SetField(first, "waitForRemoveAnimation", true);
            desk.ShowPapers(system.MonthPapers, 1);
            SubmitFirst();
            desk.ShowPapers(system.MonthPapers, 1);
            Assert.That(first.IsLeaving, Is.True);
            Assert.That(first.PaperId, Is.EqualTo(1));
            first.FinishRemove();
            desk.ShowPapers(system.MonthPapers, 1);
            Assert.That(first.PaperId, Is.EqualTo(3));
            Assert.That(first.IsLeaving, Is.False);
        }

        [Test]
        public void ClearingReturnsViewsWithoutDestroyingThem()
        {
            desk.ShowPapers(system.MonthPapers, 1);
            desk.ClearPapers();
            Assert.That(first != null && second != null, Is.True);
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.IsBound, Is.False);
            desk.ShowPapers(system.MonthPapers, 1);
            Assert.That(first.PaperId, Is.EqualTo(1));
            Assert.That(area.childCount, Is.EqualTo(2));
        }

        [Test]
        public void InsufficientAreaDoesNotForceOverlappingViews()
        {
            area.sizeDelta = new Vector2(90, 90);
            desk.ShowPapers(system.MonthPapers, 1);
            Assert.That(desk.MaxVisible, Is.Zero);
            Assert.That(first.IsBound, Is.False);
            Assert.That(system.PendingCount, Is.EqualTo(4));
        }

        private WorkPaperItemView MakePaper(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(area);
            ((RectTransform)go.transform).sizeDelta = new Vector2(120, 160);
            return go.AddComponent<WorkPaperItemView>();
        }

        private void SubmitFirst()
        {
            system.SelectPaper(1);
            system.StartHold(1, 2);
            system.AddTime(0.5);
        }

        private static void SetField(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
