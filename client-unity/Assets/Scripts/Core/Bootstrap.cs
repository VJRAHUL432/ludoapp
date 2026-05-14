using UnityEngine;
using Ludo.Audio;
using Ludo.Ads;
using Ludo.Error;
using Ludo.Networking;

namespace Ludo.Core
{
    /// <summary>
    /// First MonoBehaviour to run. Lives on a DontDestroyOnLoad GameObject
    /// in the Splash scene. Initialises all global managers in deterministic order.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        private static bool _initialised;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // Crash safety must be installed before anything else can throw.
            GlobalExceptionHandler.Install();
        }

        private void Awake()
        {
            if (_initialised) { Destroy(gameObject); return; }
            _initialised = true;
            DontDestroyOnLoad(gameObject);

            // Performance defaults — safe across devices.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount  = 0;
            Screen.sleepTimeout         = SleepTimeout.NeverSleep;

            var cfg = AppConfig.Get();

            // Order matters.
            var audio   = AudioManager.Create();
            var ads     = AdManager.Create(cfg);
            var api     = new ApiClient(cfg);
            var socket  = new SocketClient(cfg);
            var session = new SessionStore();
            var game    = new Game.GameState();

            ServiceLocator.Register(cfg);
            ServiceLocator.Register(audio);
            ServiceLocator.Register(ads);
            ServiceLocator.Register(api);
            ServiceLocator.Register(socket);
            ServiceLocator.Register(session);
            ServiceLocator.Register(game);

            Debug.Log("[Bootstrap] Initialised");
        }

        private void OnApplicationPause(bool paused)
        {
            // Pause sockets/ads when backgrounded. Saves battery + avoids
            // misleading reconnect storms.
            if (ServiceLocator.TryGet<SocketClient>(out var sock))
                sock.OnAppPause(paused);
        }
    }
}
