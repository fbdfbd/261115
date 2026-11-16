using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Feedback Settings")]
    public sealed class WorkFeedbackSettingsSO : ScriptableObject
    {
        [Tooltip("이번 달 누적 정답 수. 각 단계가 시작되는 최소 횟수입니다.")]
        public int[] SuccessCounts = { 1, 2, 4, 6, 10 };
        [Tooltip("이번 달 누적 오답 수. 각 단계가 시작되는 최소 횟수입니다.")]
        public int[] FailureCounts = { 1, 3, 5 };
    }
}
