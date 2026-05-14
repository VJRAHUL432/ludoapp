using UnityEngine;

namespace Ludo.Core
{
    /// <summary>
    /// Persists JWT, deviceId and last matchId in PlayerPrefs.
    /// Exposed via ServiceLocator. Never store secrets beyond JWT here.
    /// </summary>
    public class SessionStore
    {
        private const string KeyToken    = "ludo.token";
        private const string KeyDevice   = "ludo.deviceId";
        private const string KeyMatch    = "ludo.lastMatchId";
        private const string KeyUserId   = "ludo.userId";
        private const string KeyMute     = "ludo.mute";

        public string Token
        {
            get => PlayerPrefs.GetString(KeyToken, "");
            set { PlayerPrefs.SetString(KeyToken, value ?? ""); PlayerPrefs.Save(); }
        }

        public string DeviceId
        {
            get
            {
                var existing = PlayerPrefs.GetString(KeyDevice, "");
                if (!string.IsNullOrEmpty(existing)) return existing;
                var v = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(v) || v == SystemInfo.unsupportedIdentifier)
                    v = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(KeyDevice, v);
                PlayerPrefs.Save();
                return v;
            }
        }

        public string LastMatchId
        {
            get => PlayerPrefs.GetString(KeyMatch, "");
            set { PlayerPrefs.SetString(KeyMatch, value ?? ""); PlayerPrefs.Save(); }
        }

        public long UserId
        {
            get => long.TryParse(PlayerPrefs.GetString(KeyUserId, "0"), out var v) ? v : 0;
            set { PlayerPrefs.SetString(KeyUserId, value.ToString()); PlayerPrefs.Save(); }
        }

        public bool Muted
        {
            get => PlayerPrefs.GetInt(KeyMute, 0) == 1;
            set { PlayerPrefs.SetInt(KeyMute, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        public void Clear()
        {
            PlayerPrefs.DeleteKey(KeyToken);
            PlayerPrefs.DeleteKey(KeyMatch);
            PlayerPrefs.DeleteKey(KeyUserId);
            PlayerPrefs.Save();
        }
    }
}
