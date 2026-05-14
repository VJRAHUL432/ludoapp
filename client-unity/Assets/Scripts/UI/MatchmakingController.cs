using UnityEngine;
using UnityEngine.UI;
using Ludo.Core;
using Ludo.Networking;
using Ludo.Audio;

namespace Ludo.UI
{
    /// <summary>
    /// Online matchmaking screen. Asks the socket layer to enqueue,
    /// listens for MM_MATCH_FOUND, then transitions to Game.
    /// </summary>
    public class MatchmakingController : MonoBehaviour
    {
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button cancelButton;

        private SocketClient _socket;

        private void Start()
        {
            _socket = ServiceLocator.Get<SocketClient>();
            _socket.OnEvent += HandleEvent;

            SetStatus("Searching for opponents...");
            _socket.Emit(ClientEvents.MmOnlineJoin, new { playerCount = 4 });

            if (cancelButton) cancelButton.onClick.AddListener(OnCancel);
        }

        private void OnDestroy()
        {
            if (_socket != null) _socket.OnEvent -= HandleEvent;
        }

        private void HandleEvent(string ev, string json)
        {
            if (ev == ServerEvents.MmQueued) SetStatus("Queued — waiting for players...");
            else if (ev == ServerEvents.MmMatchFound)
            {
                if (ServiceLocator.TryGet<AudioManager>(out var a)) a.PlaySfx(AudioManager.ClipClick);
                ScreenManager.Load(ScreenManager.Game);
            }
        }

        private void OnCancel()
        {
            _socket.Emit(ClientEvents.MmOnlineLeave, new { playerCount = 4 });
            ScreenManager.Load(ScreenManager.Home);
        }

        private void SetStatus(string s) { if (statusLabel) statusLabel.text = s; }
    }
}
