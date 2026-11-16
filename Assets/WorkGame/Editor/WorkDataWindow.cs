using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    public sealed class WorkDataWindow : EditorWindow
    {
        [SerializeField] private WorkConfigSO config;
        private int tab;
        private Vector2 scroll;
        private WorkCsvImport import;
        private string message;
        private bool hasError;
        private int fromMonth = 1;
        private int toMonth = 12;
        private int batchCount = 5;
        private WorkCategorySO batchCategory;

        [MenuItem("Work Game/Data Editor")]
        public static void Open() => GetWindow<WorkDataWindow>("Work Data");

        private void OnEnable()
        {
            if (config == null) config = AssetDatabase.LoadAssetAtPath<WorkConfigSO>(WorkDataMaker.ConfigPath);
        }

        private void OnGUI()
        {
            config = (WorkConfigSO)EditorGUILayout.ObjectField("Config", config, typeof(WorkConfigSO), false);
            if (GUILayout.Button("Create Sample Data (keep existing values)"))
            {
                WorkDataMaker.CreateSampleData();
                config = AssetDatabase.LoadAssetAtPath<WorkConfigSO>(WorkDataMaker.ConfigPath);
            }
            if (config == null) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Check Data")) Run(() => {
                    var errors = config.GetErrors();
                    if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
                    message = "Data is valid.";
                });
                if (GUILayout.Button("Save Assets")) AssetDatabase.SaveAssets();
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, hasError ? MessageType.Error : MessageType.Info);
            tab = GUILayout.Toolbar(tab, new[] { "Settings", "Categories", "Month Plan", "CSV Import" });
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (tab == 0) DrawAsset(config);
            if (tab == 1) foreach (var category in WorkDataMaker.FindCategories())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"{category.Id} / {category.Title}", EditorStyles.boldLabel);
                    DrawAsset(category);
                }
            }
            if (tab == 2) DrawPlan();
            if (tab == 3) DrawImport();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawAsset(UnityEngine.Object asset)
        {
            var data = new SerializedObject(asset);
            data.Update();
            var property = data.GetIterator();
            if (property.NextVisible(true))
                while (property.NextVisible(false)) EditorGUILayout.PropertyField(property, true);
            data.ApplyModifiedProperties(); // Unity handles Undo and dirty state.
        }

        private void DrawPlan()
        {
            if (config.Plan == null) return;
            EditorGUILayout.LabelField("Bulk count (existing rows only)", EditorStyles.boldLabel);
            fromMonth = EditorGUILayout.IntSlider("From month", fromMonth, 1, 12);
            toMonth = EditorGUILayout.IntSlider("To month", toMonth, fromMonth, 12);
            batchCategory = (WorkCategorySO)EditorGUILayout.ObjectField("Category (empty = all)", batchCategory, typeof(WorkCategorySO), false);
            batchCount = EditorGUILayout.IntField("Count", batchCount);
            if (GUILayout.Button("Apply Count"))
            {
                Undo.RecordObject(config.Plan, "Change Work Counts");
                foreach (var batch in config.Plan.Batches.Where(x => x.Month >= fromMonth && x.Month <= toMonth && (batchCategory == null || x.Category == batchCategory)))
                    batch.Count = Math.Max(0, batchCount);
                EditorUtility.SetDirty(config.Plan);
            }
            DrawAsset(config.Plan);
        }

        private void DrawImport()
        {
            EditorGUILayout.HelpBox("CSV는 ID로 갱신합니다. 카테고리의 지표는 CSV 내용으로 교체하고, 월 계획은 해당 행만 갱신합니다. CSV에 없는 SO/월 배정은 유지합니다.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Read Categories CSV")) ReadCsv(true);
                if (GUILayout.Button("Read Months CSV")) ReadCsv(false);
            }
            if (import == null) return;
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(import.Preview);
            if (GUILayout.Button("Apply Preview")) Run(() => {
                import.Apply(config);
                import = null;
                message = "CSV applied. Existing assets kept their references.";
            });
        }

        private void ReadCsv(bool categories)
        {
            string path = EditorUtility.OpenFilePanel("Read Work CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(path)) return;
            import = null;
            Run(() => {
                var next = new WorkCsvImport(File.ReadAllText(path), categories);
                next.Check(config);
                import = next;
                message = "Preview ready. No assets changed yet.";
            });
        }

        private void Run(Action action)
        {
            hasError = false;
            try { action(); }
            catch (Exception e) { hasError = true; message = e.Message; }
        }
    }
}
