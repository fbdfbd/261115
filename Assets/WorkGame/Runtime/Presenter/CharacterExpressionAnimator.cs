using UnityEngine;

namespace WorkGame
{
    public enum CharacterExpression { Normal = 0, Cry = 1, Angry = 2, Happy = 3 }

    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class CharacterExpressionAnimator : MonoBehaviour
    {
        [Header("표정 / Expression")]
        [Tooltip("Normal 기본 / Cry 울음 / Angry 화남 / Happy 웃음")]
        public CharacterExpression expression;
        [Range(0f, 1f), Tooltip("표정은 유지하면서 작은 동작의 크기를 조절합니다. 0 = 동작 없음.")]
        public float motionStrength = 1f;
        [Range(0f, 3f), Tooltip("표정 동작 속도. 0 = 일시정지, 1 = 기본 속도.")]
        public float motionSpeed = 1f;

        private Animator animator;
        private static readonly int ExpressionId = Animator.StringToHash("Expression");
        private static readonly int StrengthId = Animator.StringToHash("ExpressionStrength");
        private static readonly int SpeedId = Animator.StringToHash("ExpressionSpeed");

        private void OnEnable()
        {
            animator = GetComponent<Animator>();
            Apply();
        }

        private void Update() => Apply();

        public void SetExpression(CharacterExpression value)
        {
            expression = value;
            Apply();
        }

        public void SetExpression(int value) => SetExpression((CharacterExpression)Mathf.Clamp(value, 0, 3));
        public void Normal() => SetExpression(CharacterExpression.Normal);
        public void Cry() => SetExpression(CharacterExpression.Cry);
        public void Angry() => SetExpression(CharacterExpression.Angry);
        public void Happy() => SetExpression(CharacterExpression.Happy);

        private void Apply()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(ExpressionId, Mathf.Clamp((int)expression, 0, 3));
            animator.SetFloat(StrengthId, Mathf.Clamp01(motionStrength));
            animator.SetFloat(SpeedId, Mathf.Clamp(motionSpeed, 0f, 3f));
        }
    }
}
