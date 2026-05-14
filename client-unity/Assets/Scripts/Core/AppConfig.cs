using System;
using UnityEngine;

namespace Ludo.Core
{
    /// <summary>
    /// Loaded once at startup from Resources/AppConfig.json.
    /// Treat as immutable; do NOT mutate at runtime.
    /// </summary>
    [Serializable]
    public class AppConfig
    {
        public string apiBaseUrl = "http://localhost:3000";
        public string socketUrl = "http://localhost:3000";

        public int heartbeatIntervalMs = 5000;
        public int maxMissedHeartbeats = 3;

        public int reconnectInitialDelayMs = 500;
        public int reconnectMaxDelayMs = 8000;

        public int actionTimeoutMs = 8000;

        public bool enableAdmob = false;
        // When true, ignore the unit IDs above and use Google's official test
        // ad units. Always set to false for Play Store builds.
        public bool useTestAdUnits = true;
        public string androidAdmobAppId = "";
        public string androidBannerAdUnitId = "";
        public string androidInterstitialAdUnitId = "";
        public string androidRewardedAdUnitId = "";

        // Google's official test ad units. Showing these is allowed and won't
        // get your AdMob account banned (using your real units in test mode is
        // what gets you banned). See:
        // https://developers.google.com/admob/unity/test-ads
        public const string TestAppId          = "ca-app-pub-3940256099942544~3347511713";
        public const string TestBannerId       = "ca-app-pub-3940256099942544/6300978111";
        public const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewardedId     = "ca-app-pub-3940256099942544/5224354917";

        public string EffectiveBannerId       => useTestAdUnits ? TestBannerId       : androidBannerAdUnitId;
        public string EffectiveInterstitialId => useTestAdUnits ? TestInterstitialId : androidInterstitialAdUnitId;
        public string EffectiveRewardedId     => useTestAdUnits ? TestRewardedId     : androidRewardedAdUnitId;
        public string EffectiveAppId          => useTestAdUnits ? TestAppId          : androidAdmobAppId;

        private static AppConfig _instance;

        public static AppConfig Get()
        {
            if (_instance != null) return _instance;
            var ta = Resources.Load<TextAsset>("AppConfig");
            if (ta == null)
            {
                Debug.LogWarning("[AppConfig] AppConfig.json missing in Resources/. Using defaults.");
                _instance = new AppConfig();
            }
            else
            {
                try
                {
                    _instance = JsonUtility.FromJson<AppConfig>(ta.text) ?? new AppConfig();
                }
                catch (Exception e)
                {
                    Debug.LogError("[AppConfig] Failed to parse AppConfig.json: " + e.Message);
                    _instance = new AppConfig();
                }
            }
            return _instance;
        }
    }
}
