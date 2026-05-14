using System;
using System.Collections;
using UnityEngine;

namespace Ludo.Ads
{
    /// <summary>
    /// Hidden DontDestroyOnLoad helper that lets non-MonoBehaviour code (like
    /// AdMobProvider) schedule delayed work via Unity coroutines.
    /// </summary>
    internal class CoroutineHost : MonoBehaviour
    {
        private static CoroutineHost _instance;

        private static CoroutineHost I
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Ludo.CoroutineHost");
                    DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _instance = go.AddComponent<CoroutineHost>();
                }
                return _instance;
            }
        }

        public static void Run(Action action, float delaySeconds)
        {
            I.StartCoroutine(I.RunDelayed(action, delaySeconds));
        }

        private IEnumerator RunDelayed(Action action, float seconds)
        {
            if (seconds > 0) yield return new WaitForSeconds(seconds);
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogError($"[CoroutineHost] {e}"); }
        }
    }
}
