// AdMobProvider — concrete implementation for Google Mobile Ads.
//
// IMPORTANT: This file is gated by the ENABLE_ADMOB scripting define.
// It only compiles after you:
//   1) Import the Google Mobile Ads Unity plugin (.unitypackage), and
//   2) Define ENABLE_ADMOB in Player Settings → Other Settings →
//      Scripting Define Symbols (Android tab).
//
// Without those steps the file is a no-op so Unity won't break.
//
// SDK download:
//   https://developers.google.com/admob/unity/quick-start
//
#if ENABLE_ADMOB

using System;
using UnityEngine;
using GoogleMobileAds.Api;
using Ludo.Core;

namespace Ludo.Ads
{
    /// <summary>
    /// Production-grade AdMob provider with:
    ///  - Lazy load on init + auto-reload after each show.
    ///  - Retry with exponential backoff on load failure.
    ///  - Safe fallbacks when the network is dead — never blocks the UI.
    /// </summary>
    public class AdMobProvider : IAdProvider
    {
        private AppConfig _cfg;
        private BannerView _banner;
        private InterstitialAd _interstitial;
        private RewardedAd _rewarded;

        private bool _bannerVisible;
        private int _interstitialBackoffMs = 1000;
        private int _rewardedBackoffMs     = 1000;
        private const int MaxBackoffMs     = 30000;

        public void Initialize(AppConfig cfg)
        {
            _cfg = cfg;
            try
            {
                MobileAds.Initialize(status =>
                {
                    Debug.Log("[AdMob] SDK initialised");
                    LoadInterstitial();
                    LoadRewarded();
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdMob] init failed: {e}");
            }
        }

        // ------------- Banner -------------

        public void ShowBanner()
        {
            try
            {
                if (_banner == null)
                {
                    _banner = new BannerView(_cfg.EffectiveBannerId, AdSize.Banner, AdPosition.Bottom);
                    var req = new AdRequest();
                    _banner.LoadAd(req);
                }
                _banner.Show();
                _bannerVisible = true;
            }
            catch (Exception e) { Debug.LogError($"[AdMob] banner show: {e}"); }
        }

        public void HideBanner()
        {
            try
            {
                if (_banner != null) _banner.Hide();
                _bannerVisible = false;
            }
            catch (Exception e) { Debug.LogError($"[AdMob] banner hide: {e}"); }
        }

        // ------------- Interstitial -------------

        private void LoadInterstitial()
        {
            try
            {
                var req = new AdRequest();
                InterstitialAd.Load(_cfg.EffectiveInterstitialId, req, (ad, err) =>
                {
                    if (err != null || ad == null)
                    {
                        Debug.LogWarning($"[AdMob] interstitial load failed: {err?.GetMessage()}");
                        ScheduleReloadInterstitial();
                        return;
                    }
                    _interstitial = ad;
                    _interstitialBackoffMs = 1000;
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        _interstitial = null;
                        LoadInterstitial();
                    };
                    ad.OnAdFullScreenContentFailed += (e) =>
                    {
                        Debug.LogWarning($"[AdMob] interstitial show failed: {e}");
                        _interstitial = null;
                        LoadInterstitial();
                    };
                });
            }
            catch (Exception e) { Debug.LogError($"[AdMob] interstitial: {e}"); }
        }

        private void ScheduleReloadInterstitial()
        {
            var delay = _interstitialBackoffMs;
            _interstitialBackoffMs = Math.Min(MaxBackoffMs, _interstitialBackoffMs * 2);
            CoroutineHost.Run(() => LoadInterstitial(), delay / 1000f);
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (_interstitial == null || !_interstitial.CanShowAd())
            {
                onClosed?.Invoke();           // never block flow if ad isn't ready
                LoadInterstitial();
                return;
            }
            try
            {
                _interstitial.OnAdFullScreenContentClosed += () => { try { onClosed?.Invoke(); } catch {} };
                _interstitial.Show();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdMob] interstitial show: {e}");
                onClosed?.Invoke();
            }
        }

        // ------------- Rewarded -------------

        private void LoadRewarded()
        {
            try
            {
                var req = new AdRequest();
                RewardedAd.Load(_cfg.EffectiveRewardedId, req, (ad, err) =>
                {
                    if (err != null || ad == null)
                    {
                        Debug.LogWarning($"[AdMob] rewarded load failed: {err?.GetMessage()}");
                        ScheduleReloadRewarded();
                        return;
                    }
                    _rewarded = ad;
                    _rewardedBackoffMs = 1000;
                    ad.OnAdFullScreenContentClosed += () => { _rewarded = null; LoadRewarded(); };
                    ad.OnAdFullScreenContentFailed += (e) => { _rewarded = null; LoadRewarded(); };
                });
            }
            catch (Exception e) { Debug.LogError($"[AdMob] rewarded: {e}"); }
        }

        private void ScheduleReloadRewarded()
        {
            var delay = _rewardedBackoffMs;
            _rewardedBackoffMs = Math.Min(MaxBackoffMs, _rewardedBackoffMs * 2);
            CoroutineHost.Run(() => LoadRewarded(), delay / 1000f);
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            if (_rewarded == null || !_rewarded.CanShowAd())
            {
                onResult?.Invoke(false);
                LoadRewarded();
                return;
            }
            try
            {
                bool earned = false;
                _rewarded.OnAdFullScreenContentClosed += () => { try { onResult?.Invoke(earned); } catch {} };
                _rewarded.Show(reward =>
                {
                    earned = true;
                    Debug.Log($"[AdMob] reward earned: {reward.Amount} {reward.Type}");
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdMob] rewarded show: {e}");
                onResult?.Invoke(false);
            }
        }
    }
}

#endif
