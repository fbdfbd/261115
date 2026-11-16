using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    public static class WorkDataMaker
    {
        public const string Folder = "Assets/WorkGame/Data";
        public const string ConfigPath = Folder + "/WorkConfig.asset";

        // Safe to run again: existing settings, categories and plans are preserved.
        [MenuItem("Work Game/Create Sample Data")]
        public static void CreateSampleData()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var config = LoadOrCreate<WorkConfigSO>(ConfigPath);
            var plan = LoadOrCreate<WorkPlanSO>(Folder + "/WorkPlan.asset");
            if (config.Plan == null) config.Plan = plan;

            // One CSV owns the default metrics; existing assets keep their edited values.
            string csv = File.ReadAllText("Assets/WorkGame/Samples/Categories.csv");
            var rows = WorkCsvImport.ReadCategories(csv);
            foreach (var group in rows.GroupBy(x => x["Id"]))
            {
                string path = $"{Folder}/Category_{group.Key}.asset";
                if (AssetDatabase.LoadAssetAtPath<WorkCategorySO>(path) != null) continue;
                var category = LoadOrCreate<WorkCategorySO>(path);
                WorkCsvImport.FillCategory(category, group.ToList());
                EditorUtility.SetDirty(category);
            }
            // PoC balance only: start with two categories; unlock the others gradually.
            if (plan.Batches.Count == 0)
            {
                for (int month = 1; month <= 12; month++)
                {
                    int unlocked = Mathf.Min(15, 2 + (month - 1) * 2);
                    for (int i = 1; i <= unlocked; i++)
                        plan.Batches.Add(new WorkBatch { Month = month, Count = 2,
                            Category = AssetDatabase.LoadAssetAtPath<WorkCategorySO>($"{Folder}/Category_{i:00}.asset") });
                }
            }
            EditorUtility.SetDirty(plan);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log("Work Game sample data is ready. Open Work Game/Data Editor or PoC.");
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static WorkCategorySO[] FindCategories()
        {
            return AssetDatabase.FindAssets("t:WorkCategorySO", new[] { "Assets/WorkGame" })
                .Select(x => AssetDatabase.LoadAssetAtPath<WorkCategorySO>(AssetDatabase.GUIDToAssetPath(x)))
                .OrderBy(x => x.Id).ToArray();
        }
    }
}
