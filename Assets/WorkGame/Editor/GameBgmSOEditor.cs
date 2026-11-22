using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkGame.Editor
{
    // Derive the authoring list from the actual Care data. Opening the Inspector never edits the SO.
    public static class GameBgmEventCatalog
    {
        public sealed class Entry
        {
            public string DialogueId;
            public int Month;
            public string Preview;
            public readonly List<string> Contexts = new List<string>();
        }

        public static Entry[] Build(CareDialogueFile data)
        {
            var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var dialogues = (data.Dialogues ?? Array.Empty<CareDialogueData>()).Where(x => x != null && !string.IsNullOrEmpty(x.Id))
                .GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
            void Add(string id, int month, string context)
            {
                if (string.IsNullOrEmpty(id) || !dialogues.TryGetValue(id, out var dialogue)) return;
                if (!result.TryGetValue(id, out var entry))
                {
                    var line = dialogue.Lines?.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(x.Text));
                    entry = new Entry { DialogueId = id, Month = month,
                        Preview = line == null ? "" : (string.IsNullOrEmpty(line.Speaker) ? "" : line.Speaker + ": ") + line.Text.Replace("\n", " ") };
                    result.Add(id, entry);
                }
                else if (entry.Month != month) entry.Month = 0;
                string label = month + "월 · " + context;
                if (!entry.Contexts.Contains(label)) entry.Contexts.Add(label);
            }
            if (data.Leave != null)
                for (int month = 1; month <= 12; month++)
                {
                    Add(data.Leave.GetDialogue(CareLeaveKind.Early, month), month, "Care 진입 · 조기 퇴근");
                    Add(data.Leave.GetDialogue(CareLeaveKind.SlightlyEarly, month), month, "Care 진입 · 조금 일찍 퇴근");
                    Add(data.Leave.GetDialogue(CareLeaveKind.OnTime, month), month, "Care 진입 · 정시 퇴근");
                    Add(data.Leave.GetDialogue(CareLeaveKind.Overtime, month), month, "Care 진입 · 야근");
                }
            foreach (var talk in data.MonthTalks ?? Array.Empty<CareMonthTalk>())
                if (talk != null) Add(talk.Dialogue, talk.Month, "달이와 대화");
            foreach (var promise in data.Promises ?? Array.Empty<CarePromiseData>())
            {
                if (promise == null) continue;
                Add(promise.OfferDialogue, promise.OfferMonth, "성장 결과 후 · 약속 제안 [" + promise.Id + "]");
                Add(promise.SuccessDialogue, promise.TargetMonth, "Care 진입 · 약속 성공 [" + promise.Id + "]");
                Add(promise.EarlySuccessDialogue, promise.TargetMonth, "Care 진입 · 약속 성공 / 일찍 귀가 [" + promise.Id + "]");
                Add(promise.FailureDialogue, promise.TargetMonth, "Care 진입 · 약속 실패 [" + promise.Id + "]");
            }
            foreach (var story in data.Stories ?? Array.Empty<CareStoryData>())
            {
                if (story == null) continue;
                Add(story.Dialogue, story.Month, "성장 결과 후 · 스토리 [" + story.Id + "]");
                Add(story.PromiseSuccessDialogue, story.Month, "성장 결과 후 · 스토리 / 약속 성공 버전 [" + story.Id + "]");
            }
            return result.Values.OrderBy(x => x.Month).ThenBy(x => x.Contexts[0], StringComparer.Ordinal).ToArray();
        }
    }

    [CustomEditor(typeof(GameBgmSO))]
    public sealed class GameBgmSOEditor : UnityEditor.Editor
    {
        private readonly Dictionary<int, bool> expanded = new Dictionary<int, bool>();
        private GameBgmEventCatalog.Entry[] catalog = Array.Empty<GameBgmEventCatalog.Entry>();
        private string sourceInfo, error, search = "";
        private bool showStored;
        private static GUIStyle eventHeader;
        private static readonly string[] Modes = { "기본 BGM 유지", "지정 곡 재생", "무음" };

        private void OnEnable() => LoadCatalog();

        private void LoadCatalog()
        {
            var entries = new List<GameBgmEventCatalog.Entry>();
            var sources = new HashSet<string>();
            var errors = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:CareConfigSO"))
            {
                var config = AssetDatabase.LoadAssetAtPath<CareConfigSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (config?.Dialogues == null) continue;
                string path = AssetDatabase.GetAssetPath(config.Dialogues);
                if (!sources.Add(path)) continue;
                try
                {
                    var data = JsonUtility.FromJson<CareDialogueFile>(config.Dialogues.text);
                    if (data != null) entries.AddRange(GameBgmEventCatalog.Build(data));
                }
                catch (Exception exception) { errors.Add(path + ": " + exception.Message); }
            }
            catalog = entries.GroupBy(x => x.DialogueId, StringComparer.Ordinal).Select(group =>
            {
                var first = group.First();
                foreach (var other in group.Skip(1))
                {
                    if (first.Month != other.Month) first.Month = 0;
                    foreach (string context in other.Contexts)
                        if (!first.Contexts.Contains(context)) first.Contexts.Add(context);
                }
                return first;
            }).OrderBy(x => x.Month).ThenBy(x => x.Contexts[0], StringComparer.Ordinal).ToArray();
            sourceInfo = string.Join("\n", sources.OrderBy(x => x, StringComparer.Ordinal));
            error = string.Join("\n", errors);
        }

        public override void OnInspectorGUI()
        {
            if (eventHeader == null) eventHeader = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "Events");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("이벤트별 BGM", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("월을 펼쳐 발생 시점과 첫 대사를 확인한 뒤 곡을 지정하세요. 선택지 후속 대화까지 유지합니다.\n"
                + "미설정은 기본 BGM 유지. 전환은 완전 페이드아웃 → 페이드인입니다.", MessageType.Info);
            if (GUILayout.Button("이벤트 목록 새로고침")) LoadCatalog();
            if (!string.IsNullOrEmpty(sourceInfo)) EditorGUILayout.LabelField("대화 원본", sourceInfo, EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (catalog.Length == 0) EditorGUILayout.HelpBox("CareConfigSO의 Dialogues에서 이벤트를 찾지 못했습니다.", MessageType.Warning);
            search = EditorGUILayout.TextField("이벤트 검색", search);
            var events = serializedObject.FindProperty("Events");
            var visible = catalog.Where(entry => string.IsNullOrWhiteSpace(search)
                || (entry.DialogueId + " " + entry.Preview + " " + string.Join(" ", entry.Contexts))
                    .IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var month in visible.GroupBy(entry => entry.Month))
            {
                if (!expanded.TryGetValue(month.Key, out bool open)) open = month.Key == 1;
                open = EditorGUILayout.Foldout(open || !string.IsNullOrWhiteSpace(search),
                    (month.Key == 0 ? "여러 달에서 공통 사용" : "MONTH " + month.Key.ToString("00")) + " · " + month.Count() + "개 이벤트", true);
                expanded[month.Key] = open;
                if (open) foreach (var entry in month) DrawEvent(events, entry);
            }
            showStored = EditorGUILayout.Foldout(showStored, "고급: 저장된 설정 / 삭제된 이벤트 정리", true);
            if (showStored) EditorGUILayout.PropertyField(events, true);
            var duplicate = ((GameBgmSO)target).Events?.Where(x => x != null && !string.IsNullOrEmpty(x.DialogueId))
                .GroupBy(x => x.DialogueId).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null) EditorGUILayout.HelpBox("중복 이벤트 ID: " + duplicate.Key + ". 첫 설정만 적용됩니다. 고급 목록에서 중복을 정리하세요.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawEvent(SerializedProperty events, GameBgmEventCatalog.Entry entry)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(string.Join(" / ", entry.Contexts), eventHeader);
                if (!string.IsNullOrEmpty(entry.Preview)) EditorGUILayout.LabelField("첫 대사: " + entry.Preview, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(entry.DialogueId, EditorStyles.miniLabel);
                SerializedProperty setting = null;
                for (int i = 0; i < events.arraySize; i++)
                {
                    var item = events.GetArrayElementAtIndex(i);
                    if (item.FindPropertyRelative("DialogueId").stringValue == entry.DialogueId) { setting = item; break; }
                }
                int mode = setting?.FindPropertyRelative("Mode").enumValueIndex ?? 0;
                int selected = EditorGUILayout.Popup("이벤트 동안", mode, Modes);
                if (selected != mode)
                {
                    if (setting == null)
                    {
                        int index = events.arraySize++;
                        setting = events.GetArrayElementAtIndex(index);
                        setting.FindPropertyRelative("DialogueId").stringValue = entry.DialogueId;
                        var newTrack = setting.FindPropertyRelative("Track");
                        newTrack.FindPropertyRelative("Clip").objectReferenceValue = null;
                        newTrack.FindPropertyRelative("Volume").floatValue = 1f;
                    }
                    setting.FindPropertyRelative("Mode").enumValueIndex = selected;
                    mode = selected;
                }
                if (mode == (int)GameEventBgmMode.Play && setting != null)
                {
                    var track = setting.FindPropertyRelative("Track");
                    EditorGUILayout.PropertyField(track.FindPropertyRelative("Clip"), new GUIContent("BGM 곡"));
                    EditorGUILayout.PropertyField(track.FindPropertyRelative("Volume"), new GUIContent("곡 음량"));
                    if (track.FindPropertyRelative("Clip").objectReferenceValue == null)
                        EditorGUILayout.HelpBox("곡이 비어 있어 기본 BGM을 유지합니다.", MessageType.Info);
                }
            }
        }
    }
}
