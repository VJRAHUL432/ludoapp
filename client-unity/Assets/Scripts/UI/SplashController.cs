using System.Threading.Tasks;
using UnityEngine;
using Ludo.Core;
using Ludo.Networking;
using Ludo.Utils;

namespace Ludo.UI
{
    /// <summary>
    /// Splash controller: sets up session, attempts auto-login (guest if no token),
    /// then routes to Home (or Login).
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        [SerializeField] private float minDisplaySeconds = 1.0f;

        private async void Start()
        {
            var startedAt = Time.realtimeSinceStartup;
            var session = ServiceLocator.Get<SessionStore>();
            var api = ServiceLocator.Get<ApiClient>();

            // Auto guest-login if no token yet.
            if (string.IsNullOrEmpty(session.Token))
            {
                try
                {
                    var resp = await api.PostJsonAsync<AuthResponse>("/api/auth/guest", new GuestReq
                    {
                        deviceId = session.DeviceId
                    });
                    if (resp != null && !string.IsNullOrEmpty(resp.token))
                    {
                        session.Token = resp.token;
                        if (resp.user != null) session.UserId = resp.user.id;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("[Splash] guest login failed: " + e.Message); }
            }

            // Maintain min display so the splash isn't a flicker.
            var elapsed = Time.realtimeSinceStartup - startedAt;
            var remaining = Mathf.Max(0, minDisplaySeconds - elapsed);
            if (remaining > 0) await Task.Delay(Mathf.RoundToInt(remaining * 1000));

            if (string.IsNullOrEmpty(session.Token))
                ScreenManager.Load(ScreenManager.Login);
            else
                ScreenManager.Load(ScreenManager.Home);
        }

        [System.Serializable] private class GuestReq { public string deviceId; }
        [System.Serializable] private class AuthResponse { public string token; public UserDto user; }
        [System.Serializable] private class UserDto
        {
            public long id;
            public string name;
            public string avatar;
            public int coins;
            public int wins;
            public int losses;
            public int rank_points;
        }
    }
}
