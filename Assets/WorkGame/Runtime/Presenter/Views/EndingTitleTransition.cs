using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorkGame
{
    // A temporary transition instance owns the cover across scene loading, then removes itself.
    public sealed class EndingTitleTransition : MonoBehaviour
    {
        public static void Play(DiagonalTransitionView prefab, Action covered)
        {
            var transition = Instantiate(prefab);
            DontDestroyOnLoad(transition.gameObject);
            var owner = transition.gameObject.AddComponent<EndingTitleTransition>();
            owner.StartCoroutine(owner.Run(transition, covered));
        }
        private IEnumerator Run(DiagonalTransitionView transition, Action covered)
        {
            int scene = SceneManager.GetActiveScene().handle;
            transition.Cover();
            while (transition.IsPlaying) yield return null;
            covered?.Invoke();
            float elapsed = 0;
            while (SceneManager.GetActiveScene().handle == scene && elapsed < 2)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            transition.Reveal();
            while (transition.IsPlaying) yield return null;
            Destroy(gameObject);
        }
    }
}
