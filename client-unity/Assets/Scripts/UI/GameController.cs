using UnityEngine;
using UnityEngine.UI;
using Ludo.Core;
using Ludo.Game;
using Ludo.Audio;
using Ludo.Networking;

namespace Ludo.UI
{
    /// <summary>
    /// Wires the Game scene to MatchController + GameState.
    /// Renders nothing fancy here — visual prefabs (board, tokens, dice anim)
    /// listen to GameState.OnEngineEvent / OnStateChanged.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private Button rollButton;
        [SerializeField] private Button[] tokenButtons;        // size 4 — pick which of the local seat's tokens to move

        private MatchController _match;
        private GameState _state;
        private SessionStore _session;
        private AudioManager _audio;

        private void Start()
        {
            _state   = ServiceLocator.Get<GameState>();
            _session = ServiceLocator.Get<SessionStore>();
            _audio   = ServiceLocator.Get<AudioManager>();
            _match   = new MatchController();

            // Resume in case of a previous session.
            _match.RequestResume();

            if (rollButton) rollButton.onClick.AddListener(OnRoll);
            for (int i = 0; i < (tokenButtons?.Length ?? 0); i++)
            {
                int idx = i;
                if (tokenButtons[i]) tokenButtons[i].onClick.AddListener(() => OnMove(idx));
            }

            _state.OnStateChanged += Refresh;
            _state.OnEngineEvent += OnEngineEvent;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_state != null)
            {
                _state.OnStateChanged -= Refresh;
                _state.OnEngineEvent  -= OnEngineEvent;
            }
            _match?.Dispose();
        }

        private void OnRoll()
        {
            _audio?.PlaySfx(AudioManager.ClipDice);
            _match.RequestRoll();
        }

        private void OnMove(int tokenIndex)
        {
            _audio?.PlaySfx(AudioManager.ClipMove);
            _match.RequestMove(tokenIndex);
        }

        private void OnEngineEvent(string type, string json)
        {
            switch (type)
            {
                case EngineEventTypes.TokenKilled:  _audio?.PlaySfx(AudioManager.ClipKill); break;
                case EngineEventTypes.MatchFinished: _audio?.PlaySfx(AudioManager.ClipWin); break;
            }
        }

        private void Refresh()
        {
            // Local seat = the player whose userId matches session.
            int? localSeat = null;
            foreach (var p in _state.Players)
                if (p.userId == _session.UserId) { localSeat = p.seat; break; }
            bool myTurn = localSeat.HasValue && _state.Turn != null && _state.Turn.seat == localSeat.Value;

            if (rollButton) rollButton.interactable = myTurn && (_state.Turn?.dice == null);
            if (tokenButtons != null && localSeat.HasValue)
            {
                for (int i = 0; i < tokenButtons.Length; i++)
                {
                    if (!tokenButtons[i]) continue;
                    tokenButtons[i].interactable = myTurn && (_state.Turn?.mustMove ?? false);
                }
            }
        }
    }
}
