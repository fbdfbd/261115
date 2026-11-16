using UnityEditor;

namespace WorkGame.Editor
{
    [CustomEditor(typeof(WorkDeskView))]
    public sealed class WorkDeskViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var view = (WorkDeskView)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("연결된 종이 수", view.PoolCount);
                EditorGUILayout.IntField("최대 표시 장수", view.MaxVisible);
            }
            if (EditorApplication.isPlaying && UnityEngine.GUILayout.Button("배치 다시 계산")) view.RebuildLayout();
        }
    }
}
