using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ludo.UI
{
    /// <summary>
    /// Async scene loader with a guard against double-loads and scene-name typos.
    /// Build settings must include each scene listed below.
    /// </summary>
    public static class ScreenManager
    {
        public const string Splash      = "Splash";
        public const string Login       = "Login";
        public const string Home        = "Home";
        public const string Matchmaking = "Matchmaking";
        public const string Lobby       = "Lobby";
        public const string Game        = "Game";
        public const string Result      = "Result";

        private static bool _loading;

        public static IEnumerator LoadAsync(string sceneName)
        {
            if (_loading) yield break;
            _loading = true;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null) { _loading = false; yield break; }
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            _loading = false;
        }

        public static void Load(string sceneName)
        {
            // Lightweight non-coroutine variant for places without a MonoBehaviour host.
            // Uses SceneManager.LoadSceneAsync without yielding; safe to call from UI buttons.
            if (_loading) return;
            _loading = true;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null) { _loading = false; return; }
            op.completed += _ => { _loading = false; };
        }
    }
}
