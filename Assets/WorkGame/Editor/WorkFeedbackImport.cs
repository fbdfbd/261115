using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    public static class WorkFeedbackImport
    {
        [MenuItem("Work Game/Import Feedback CSV")]
        public static void Import()
        {
            try
            {
                string text = File.ReadAllText("Assets/WorkGame/Samples/Feedback.csv");
                var lines = WorkCsv.Read(text, "Id", "Result", "Tier", "Text").Select(row => {
                    if (!Enum.TryParse(row["Result"], out FeedbackResult result)
                        || !int.TryParse(row["Tier"], out int tier))
                        throw new FormatException("Invalid feedback result or tier: " + row["Id"]);
                    return new WorkFeedbackLine { Id = row["Id"], Result = result, Tier = tier, Text = row["Text"] };
                }).ToList();
                // Validate every pool before editing assets.
                _ = new WorkFeedback(lines, new[] { 1, 2, 4, 6, 10 }, new[] { 1, 3, 5 }, 1);
                Directory.CreateDirectory(WorkDataMaker.Folder);
                AssetDatabase.Refresh();
                var data = WorkDataMaker.LoadOrCreate<WorkFeedbackSO>(WorkDataMaker.Folder + "/WorkFeedback.asset");
                var settings = WorkDataMaker.LoadOrCreate<WorkFeedbackSettingsSO>(WorkDataMaker.Folder + "/WorkFeedbackSettings.asset");
                Undo.RecordObject(data, "Import Feedback CSV");
                data.Lines = lines;
                EditorUtility.SetDirty(data);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Selection.activeObject = data;
                Debug.Log("판정 대사 가져오기 완료. 기존 단계 설정은 유지했습니다.", data);
            }
            catch (Exception e) { Debug.LogError("판정 대사 가져오기 실패: " + e.Message); }
        }

        [MenuItem("Work Game/Import Feedback CSV", true)]
        private static bool CanImport() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
