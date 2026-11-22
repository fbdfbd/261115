using UnityEngine;

namespace WorkGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterIdleAnimator : MonoBehaviour
    {
        [Header("호흡 / Breathing")]
        [Range(0f, 1f), Tooltip("0 = 정지, 1 = 기본 호흡 크기. 더 큰 움직임은 Breath.anim에서 수정합니다.")]
        public float breathStrength = 1f;
        [Range(0f, 3f), Tooltip("기본 주기 3.6초. 0은 일시정지, 2는 두 배 빠르게.")]
        public float breathSpeed = 1f;

        [Header("꼬리 / Tail")]
        [Range(0f, 1f)] public float tailStrength = 1f;
        [Range(0f, 3f), Tooltip("기본 주기 4.8초.")]
        public float tailSpeed = 1f;

        [Header("왼쪽 귀 / Left Ear")]
        [Range(0f, 1f)] public float leftEarStrength = 1f;
        [Range(0f, 3f), Tooltip("기본 패턴 8초. 대기 간격과 꿈틀 속도를 함께 조절합니다.")]
        public float leftEarSpeed = 1f;

        [Header("오른쪽 귀 / Right Ear")]
        [Range(0f, 1f)] public float rightEarStrength = 1f;
        [Range(0f, 3f)] public float rightEarSpeed = 1f;

        private Animator animator;
        private static readonly int[] StrengthIds =
        {
            Animator.StringToHash("BreathStrength"), Animator.StringToHash("TailStrength"),
            Animator.StringToHash("LeftEarStrength"), Animator.StringToHash("RightEarStrength")
        };
        private static readonly int[] SpeedIds =
        {
            Animator.StringToHash("BreathSpeed"), Animator.StringToHash("TailSpeed"),
            Animator.StringToHash("LeftEarSpeed"), Animator.StringToHash("RightEarSpeed")
        };

        private void OnEnable()
        {
            animator = GetComponent<Animator>();
            ApplySettings();
        }

        private void Update() => ApplySettings();

        private void ApplySettings()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            Set(0, breathStrength, breathSpeed);
            Set(1, tailStrength, tailSpeed);
            Set(2, leftEarStrength, leftEarSpeed);
            Set(3, rightEarStrength, rightEarSpeed);
        }

        private void Set(int index, float strength, float speed)
        {
            animator.SetFloat(StrengthIds[index], Mathf.Clamp01(strength));
            animator.SetFloat(SpeedIds[index], Mathf.Clamp(speed, 0f, 3f));
        }
    }
}
