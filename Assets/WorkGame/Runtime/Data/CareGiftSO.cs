using System.Collections.Generic;
using UnityEngine;

namespace WorkGame
{
    [CreateAssetMenu(menuName = "Work Game/Care/Gift")]
    public sealed class CareGiftSO : ScriptableObject
    {
        public string Id;
        public string Name;
        [TextArea] public string Description;
        public Sprite Icon;
        public CareGiftKind Kind;
        [Min(0)] public int Cost;
        [Tooltip("구매 순간 한 번 적용합니다. 음수도 가능합니다.")]
        public int Friendship;
        [Tooltip("구매 후 활성화한 동안 적용합니다. 종류별 하나만 활성화합니다.")]
        public List<CareGiftBonus> Bonuses = new List<CareGiftBonus>();
        public CareGift CreateGift() => new CareGift(Id, Name, Kind, Cost, Friendship, Bonuses);
    }
}
