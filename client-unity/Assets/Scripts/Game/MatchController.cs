using System;
using System.Threading.Tasks;
using UnityEngine;
using Ludo.Core;
using Ludo.Networking;
using Ludo.Utils;

namespace Ludo.Game
{
    /// <summary>
    /// Translates user intent → socket commands, and socket events → GameState.
    /// Lives for the duration of a single match.
    /// </summary>
    public class MatchController
    {
        private readonly SocketClient _socket;
        private readonly GameState _state;
        private readonly SessionStore _session;

        public MatchController()
        {
            _socket  = ServiceLocator.Get<SocketClient>();
            _state   = ServiceLocator.Get<GameState>();
            _session = ServiceLocator.Get<SessionStore>();

            _socket.OnEvent += HandleSocketEvent;
        }

        public void Dispose() { _socket.OnEvent -= HandleSocketEvent; }

        public void RequestRoll()
        {
            if (string.IsNullOrEmpty(_state.MatchId)) return;
            _socket.Emit(ClientEvents.GameRoll, new
            {
                actionId = Guid.NewGuid().ToString(),
                matchId = _state.MatchId
            });
        }

        public void RequestMove(int tokenIndex)
        {
            if (string.IsNullOrEmpty(_state.MatchId)) return;
            _socket.Emit(ClientEvents.GameMove, new
            {
                actionId = Guid.NewGuid().ToString(),
                matchId = _state.MatchId,
                tokenIndex
            });
        }

        public void RequestResume()
        {
            var mid = _session.LastMatchId;
            if (string.IsNullOrEmpty(mid)) return;
            _socket.Emit(ClientEvents.GameResume, new { matchId = mid });
        }

        // --- inbound ---
        private void HandleSocketEvent(string ev, string json)
        {
            try
            {
                switch (ev)
                {
                    case ServerEvents.GameState:
                    {
                        var snap = JsonUtility.FromJson<GameStateDTO>(json);
                        if (!string.IsNullOrEmpty(snap?.matchId))
                            _session.LastMatchId = snap.matchId;
                        _state.ApplyFullSnapshot(snap);
                        break;
                    }
                    case ServerEvents.GameEvent:
                    {
                        var evt = JsonUtility.FromJson<GameEventDTO>(json);
                        if (evt != null) _state.ApplyEngineEvent(evt.type, evt);
                        break;
                    }
                    case ServerEvents.GameError:
                    {
                        Debug.LogWarning($"[Match] server error: {json}");
                        EventBus.Publish(new ToastEvent { Message = "Server rejected action" });
                        break;
                    }
                    case ServerEvents.MmMatchFound:
                    case ServerEvents.MmPrivateCreated:
                    case ServerEvents.MmPrivateJoined:
                    {
                        var snap = JsonUtility.FromJson<MatchEnvelope>(json);
                        if (snap?.state != null)
                        {
                            _state.ApplyFullSnapshot(snap.state);
                            if (!string.IsNullOrEmpty(snap.matchId))
                                _session.LastMatchId = snap.matchId;
                        }
                        break;
                    }
                }
            }
            catch (Exception e) { Debug.LogError($"[Match] handle event failed: {e}"); }
        }

        [Serializable] private class MatchEnvelope
        {
            public string matchId;
            public string code;
            public GameStateDTO state;
        }
    }

    public struct ToastEvent { public string Message; }
}
