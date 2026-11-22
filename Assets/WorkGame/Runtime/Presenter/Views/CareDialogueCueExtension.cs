using System.Collections;
using UnityEngine;

namespace WorkGame
{
    // Timeline / Animator などを追加するときに継承します。数値報酬や保存はここに置きません。
    public abstract class CareDialogueCueExtension : MonoBehaviour
    {
        public abstract bool CanPlay(string key);
        public abstract IEnumerator Play(CareDialogueCue cue);
        public abstract void ApplyFinal(CareDialogueCue cue);
        public virtual void SetPaused(bool paused) { }
        public virtual void Cancel() { }
    }
}
