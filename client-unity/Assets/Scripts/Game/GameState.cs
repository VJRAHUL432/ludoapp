using System;
using System.Collections.Generic;
using UnityEngine;
using Ludo.Core;
using Ludo.Networking;

namespace Ludo.Game
{
    /// <summary>
    /// Authoritative state mirror — populated by GAME_STATE/GAME_EVENT from the server.
    /// The client NEVER mutates this in response to user input directly; it sends an
    /// intent (roll/move) and waits for the server to broadcast back deltas.
    /// </summary>
    public class GameState
    {
        public string MatchId;
        public string MatchType;
        public string Status;
        public int? WinnerSeat;
        public List<int> FinishedSeats = new List<int>();

        public List<Player> Players = new List<Player>();
        public Token[][] Tokens = new Token[4][];   // [seat][tokenIndex]

        public TurnInfo Turn = new TurnInfo();

        public event Action OnStateChanged;
        public event Action<string, string> OnEngineEvent; // (type, jsonPayload)

        public GameState()
        {
            for (int s = 0; s < 4; s++)
            {
                Tokens[s] = new Token[4];
                for (int t = 0; t < 4; t++) Tokens[s][t] = new Token();
            }
        }

        public void Reset()
        {
            MatchId = null; MatchType = null; Status = null; WinnerSeat = null;
            FinishedSeats.Clear();
            Players.Clear();
            for (int s = 0; s < 4; s++)
                for (int t = 0; t < 4; t++)
                    Tokens[s][t].Reset();
            Turn = new TurnInfo();
            OnStateChanged?.Invoke();
        }

        public void ApplyFullSnapshot(GameStateDTO dto)
        {
            if (dto == null) return;
            MatchId = dto.matchId; MatchType = dto.matchType; Status = dto.status;
            WinnerSeat = dto.winnerSeat;
            FinishedSeats = dto.finishedSeats != null ? new List<int>(dto.finishedSeats) : new List<int>();
            Players = dto.players != null ? new List<Player>(dto.players) : new List<Player>();
            if (dto.tokens != null)
            {
                for (int s = 0; s < 4; s++)
                {
                    if (dto.tokens.TryGetValue(s.ToString(), out var arr))
                    {
                        for (int t = 0; t < 4 && t < arr.Length; t++) Tokens[s][t] = arr[t];
                    }
                }
            }
            if (dto.turn != null) Turn = dto.turn;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Apply a single engine event delta to local state. Mirrors server engine.js.
        /// Defensive about missing fields — bad data must NEVER crash.
        /// </summary>
        public void ApplyEngineEvent(string type, GameEventDTO e)
        {
            try
            {
                switch (type)
                {
                    case EngineEventTypes.DiceRolled:
                        Turn.dice = e.dice;
                        Turn.seat = e.seat;
                        break;
                    case EngineEventTypes.TurnPassed:
                        Turn.dice = null;
                        Turn.seat = e.seat;
                        Turn.mustMove = false;
                        break;
                    case EngineEventTypes.ExtraTurn:
                        Turn.dice = null;
                        Turn.mustMove = false;
                        break;
                    case EngineEventTypes.TokenOut:
                    case EngineEventTypes.TokenMoved:
                        if (Valid(e.seat, e.token))
                        {
                            var tk = Tokens[e.seat.Value][e.token.Value];
                            tk.state = e.state ?? tk.state;
                            tk.track = e.track ?? tk.track;
                            tk.home  = e.home  ?? tk.home;
                        }
                        break;
                    case EngineEventTypes.TokenKilled:
                        if (Valid(e.seat, e.token))
                            Tokens[e.seat.Value][e.token.Value].Reset();
                        break;
                    case EngineEventTypes.TokenFinished:
                        if (Valid(e.seat, e.token))
                            Tokens[e.seat.Value][e.token.Value].state = "FINISHED";
                        break;
                    case EngineEventTypes.SeatFinished:
                        if (e.seat.HasValue && !FinishedSeats.Contains(e.seat.Value))
                            FinishedSeats.Add(e.seat.Value);
                        break;
                    case EngineEventTypes.MatchFinished:
                        Status = "finished";
                        WinnerSeat = e.winnerSeat;
                        break;
                }
            }
            catch (Exception ex) { Debug.LogError($"[GameState] applyEvent failed: {ex}"); }

            OnEngineEvent?.Invoke(type, JsonUtility.ToJson(e));
            OnStateChanged?.Invoke();
        }

        private static bool Valid(int? seat, int? token) =>
            seat.HasValue && token.HasValue && seat.Value >= 0 && seat.Value < 4
            && token.Value >= 0 && token.Value < 4;
    }

    [Serializable] public class Player
    {
        public int seat;
        public long? userId;
        public string name;
        public bool isBot;
        public bool connected;
        public int missedTurns;
    }

    [Serializable] public class Token
    {
        public string state = "HOME";
        public int track = -1;
        public int home  = -1;
        public void Reset() { state = "HOME"; track = -1; home = -1; }
    }

    [Serializable] public class TurnInfo
    {
        public int seat;
        public int? dice;
        public int sixStreak;
        public bool mustMove;
        public long? rolledAt;
        public long? rollExpiresAt;
        public long? moveExpiresAt;
    }

    /// <summary>Snapshot DTO emitted by server's engine.publicView().</summary>
    [Serializable] public class GameStateDTO
    {
        public string matchId;
        public string matchType;
        public string status;
        public List<Player> players;
        public Dictionary<string, Token[]> tokens;
        public TurnInfo turn;
        public int? winnerSeat;
        public List<int> finishedSeats;
    }

    /// <summary>Single engine event delta.</summary>
    [Serializable] public class GameEventDTO
    {
        public string type;
        public int? seat;
        public int? token;
        public int? dice;
        public string state;
        public int? track;
        public int? home;
        public int? winnerSeat;
        public int? cell;
    }
}
