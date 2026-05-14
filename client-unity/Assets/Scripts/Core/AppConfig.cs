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
        public string androidAdmobAppId = "";
        public string androidBannerAdUnitId = "";
        public string androidInterstitialAdUnitId = "";
        public string androidRewardedAdUnitId = "";

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
