using UnityEngine;
using UnityEngine.UI;
using Ludo.Core;
using Ludo.Networking;

namespace Ludo.UI
{
    /// <summary>
    /// Login screen: guest (default) or Google (later).
    /// Google Sign-In requires a native plugin (Google Play Games / GoogleSignIn-Unity);
    /// integration is intentionally stubbed to keep this scaffold dependency-free.
    /// </summary>
    public class LoginController : MonoBehaviour
    {
        [SerializeField] private Button guestButton;
        [SerializeField] private Button googleButton;

        private void Start()
        {
            if (guestButton)  guestButton.onClick.AddListener(OnGuest);
            if (googleButton) googleButton.onClick.AddListener(OnGoogle);
        }

        private async void OnGuest()
        {
            var api = ServiceLocator.Get<ApiClient>();
            var session = ServiceLocator.Get<SessionStore>();
            try
            {
                var resp = await api.PostJsonAsync<AuthResponse>("/api/auth/guest", new GuestReq
                { deviceId = session.DeviceId });
                if (resp != null && !string.IsNullOrEmpty(resp.token))
                {
                    session.Token = resp.token;
                    if (resp.user != null) session.UserId = resp.user.id;
                    ScreenManager.Load(ScreenManager.Home);
                }
            }
            catch (System.Exception e) { Debug.LogError("[Login] guest failed: " + e); }
        }

        private void OnGoogle()
        {
            // Wire Google Sign-In plugin → exchange ID token at /api/auth/google.
            Debug.Log("[Login] Google Sign-In not wired yet (plugin pending).");
        }

        [System.Serializable] private class GuestReq { public string deviceId; }
        [System.Serializable] private class AuthResponse { public string token; public UserDto user; }
        [System.Serializable] private class UserDto { public long id; public string name; }
    }
}
