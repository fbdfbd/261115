using System;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    public sealed class WorkPocWindow : EditorWindow
    {
        [SerializeField] private WorkConfigSO config;
        [SerializeField] private int seed = 1;
        private WorkSystem system;
        private Vector2 scroll;
        private double lastTime;
        private bool ticking;
        private string error;
        private Rect heldRect;

        [MenuItem("Work Game/PoC")]
        public static void Open() => GetWindow<WorkPocWindow>("Work PoC");

        private void OnEnable()
        {
            if (config == null) config = AssetDatabase.LoadAssetAtPath<WorkConfigSO>(WorkDataMaker.ConfigPath);
            lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            Stop();
        }

        private void OnLostFocus() => system?.StopHold();

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            double seconds = now - lastTime;
            lastTime = now;
            if (system == null || !ticking || EditorApplication.isPlayingOrWillChangePlaymode) return;
            system.AddTime(seconds);
            Repaint();
        }

        private void Stop()
        {
            ticking = false;
            system?.Dispose();
            system = null;
        }

        private void OnGUI()
        {
            config = (WorkConfigSO)EditorGUILayout.ObjectField("Config", config, typeof(WorkConfigSO), false);
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("Start / Restart"))
            {
                Stop();
                try
                {
                    if (config == null) throw new InvalidOperationException("Create sample data first.");
                    system = config.CreateSystem(seed);
                    system.StartRun();
                    ticking = true;
                    lastTime = EditorApplication.timeSinceStartup;
                    error = null;
                }
                catch (Exception e) { error = e.Message; }
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (system == null) return;

            if (Event.current.type == EventType.MouseDrag && !heldRect.Contains(Event.current.mousePosition))
                system.StopHold();

            var run = system.Run;
            EditorGUILayout.LabelField($"MONTH {run.Month}/12  |  {run.Phase}  |  {run.Seconds:F1}s");
            EditorGUILayout.LabelField($"근태 {run.Attendance}  |  누적 보너스 {run.Bonus}");
            EditorGUILayout.LabelField($"남은 서류 {system.PendingCount}개");
            bool wasTicking = ticking;
            ticking = EditorGUILayout.Toggle("Real time", ticking);
            if (wasTicking && !ticking) system.StopHold();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+10 seconds")) system.AddTime(10);
                if (GUILayout.Button("+120 seconds")) system.AddTime(120);
                using (new EditorGUI.DisabledScope(!system.CanLeave))
                    if (GUILayout.Button("퇴근")) system.LeaveWork();
                using (new EditorGUI.DisabledScope(run.Phase != WorkPhase.AfterWork))
                    if (GUILayout.Button("다음 달")) system.StartNextMonth();
            }
            if (run.LastResult != null)
            {
                var result = run.LastResult;
                EditorGUILayout.LabelField($"지난 퇴근: {result.Month}월 / 초과근무 {result.OvertimeSeconds:F1}초 / 미처리 {result.Pending}");
                if (result.Review != null)
                    EditorGUILayout.LabelField($"분기평가 {result.Review.Grade} / {result.Review.Right}/{result.Review.Total} / {result.Review.RightPercent:F1}% / 보너스 {result.Review.Bonus}");
            }
            EditorGUILayout.HelpBox("선택지를 꾹 눌러 제출. 손을 떼거나 창을 벗어나면 취소. 막대는 숫자와 비율로, 별점·등급은 해당 기호로 표시합니다.", MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            var paper = system.CurrentPaper;
            if (paper != null) DrawPaper(paper);
            else if (run.Phase == WorkPhase.Working)
                EditorGUILayout.HelpBox("모든 업무 완료! 지금 퇴근할 수 있습니다.", MessageType.Info);
            EditorGUILayout.EndScrollView();

            if (Event.current.rawType == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow)
                system.StopHold();
        }

        private void DrawPaper(WorkPaper paper)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"#{paper.Id} / {paper.Month}월 / {paper.CategoryShortName}");
                EditorGUILayout.LabelField(paper.CategoryName, EditorStyles.miniLabel);
                string rule = paper.Rule == PickRule.Highest ? "가장 높은 값" : "가장 낮은 값";
                EditorGUILayout.LabelField($"{paper.Metric}: {rule} 선택 ({paper.Display})");
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = 0; i < paper.Choices.Count; i++)
                    {
                        var choice = paper.Choices[i];
                        string label = $"{choice.Name}: {paper.GetChoiceText(i)}";
                        if (paper.Display == PaperDisplay.HorizontalBar || paper.Display == PaperDisplay.VerticalBar)
                            label += $" ({paper.GetBarFill(i):P0})";
                        var rect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.button, GUILayout.Height(30));
                        GUI.Box(rect, label, GUI.skin.button);
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                        {
                            system.StartHold(paper.Id, i);
                            heldRect = rect;
                            Event.current.Use();
                        }
                    }
                }
                if (system.HeldPaperId == paper.Id)
                    EditorGUI.ProgressBar(GUILayoutUtility.GetRect(10, 16), (float)system.HoldProgress, "처리 중");
            }
        }
    }
}
