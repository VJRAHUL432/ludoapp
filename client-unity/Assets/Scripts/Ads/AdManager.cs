using System;
using UnityEngine;
using Ludo.Core;

namespace Ludo.Ads
{
    /// <summary>
    /// Modular ad façade. AdMob SDK is intentionally NOT referenced here.
    /// To enable later:
    ///   1) Import Google Mobile Ads Unity plugin.
    ///   2) Define <c>ENABLE_ADMOB</c> in Player Settings → Scripting Define Symbols (Android).
    ///   3) Implement the <see cref="IAdProvider"/> wrapper around MobileAds and inject it.
    /// Until then, all calls become safe no-ops.
    /// </summary>
    public class AdManager
    {
        private readonly AppConfig _cfg;
        private IAdProvider _provider;
        private bool _gameplayLocked;

        public bool Enabled => _cfg.enableAdmob && _provider != null;

        public static AdManager Create(AppConfig cfg) => new AdManager(cfg);

        private AdManager(AppConfig cfg)
        {
            _cfg = cfg;
            // Provider wired by the platform-specific Bootstrapper component
            // when ENABLE_ADMOB is defined; null otherwise.
        }

        public void AttachProvider(IAdProvider provider)
        {
            _provider = provider;
            try { _provider?.Initialize(_cfg); }
            catch (Exception e) { Debug.LogError($"[Ads] provider init failed: {e}"); }
        }

        /// <summary>Lock during a player turn / reconnect; ads will be suppressed.</summary>
        public void SetGameplayLocked(bool locked) => _gameplayLocked = locked;

        public void ShowBanner()
        {
            if (!Enabled || _gameplayLocked) return;
            try { _provider.ShowBanner(); }
            catch (Exception e) { Debug.LogError($"[Ads] banner: {e}"); }
        }

        public void HideBanner()
        {
            if (!Enabled) return;
            try { _provider.HideBanner(); } catch { }
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            if (!Enabled || _gameplayLocked) { onClosed?.Invoke(); return; }
            try { _provider.ShowInterstitial(onClosed); }
            catch (Exception e) { Debug.LogError($"[Ads] interstitial: {e}"); onClosed?.Invoke(); }
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            if (!Enabled || _gameplayLocked) { onResult?.Invoke(false); return; }
            try { _provider.ShowRewarded(onResult); }
            catch (Exception e) { Debug.LogError($"[Ads] rewarded: {e}"); onResult?.Invoke(false); }
        }
    }

    public interface IAdProvider
    {
        void Initialize(AppConfig cfg);
        void ShowBanner();
        void HideBanner();
        void ShowInterstitial(Action onClosed);
        void ShowRewarded(Action<bool> onResult);
    }
}
