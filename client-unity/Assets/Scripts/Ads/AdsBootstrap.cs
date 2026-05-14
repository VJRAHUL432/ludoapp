using UnityEngine;
using Ludo.Core;

namespace Ludo.Ads
{
    /// <summary>
    /// One-shot AdMob bootstrap. Auto-instantiates the AdManager into the
    /// ServiceLocator and (when ENABLE_ADMOB is defined) wires the AdMob
    /// provider so the app starts loading interstitials in the background.
    ///
    /// This is intentionally lightweight: removing the SDK from the project
    /// will not break anything because the provider is gated by ENABLE_ADMOB.
    /// </summary>
    public static class AdsBootstrap
    {
        private static bool _initialised;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (_initialised) return;
            _initialised = true;

            var cfg = AppConfig.Get();
            if (!cfg.enableAdmob)
            {
                Debug.Log("[Ads] enableAdmob=false — ads disabled.");
                return;
            }

            var mgr = AdManager.Create(cfg);

#if ENABLE_ADMOB
            var provider = new AdMobProvider();
            mgr.AttachProvider(provider);
            Debug.Log("[Ads] AdMob provider attached.");
#else
            Debug.Log("[Ads] ENABLE_ADMOB define is OFF — ads will be no-ops. " +
                      "Import Google Mobile Ads SDK and add ENABLE_ADMOB to enable.");
#endif

            // Register so other code (GameRoot) can resolve it.
            if (!ServiceLocator.TryGet<AdManager>(out _))
                ServiceLocator.Register(mgr);
        }
    }
}
