using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Feedback Lines")]
    public sealed class WorkFeedbackSO : ScriptableObject
    {
        public List<WorkFeedbackLine> Lines = new List<WorkFeedbackLine>();
    }
}
