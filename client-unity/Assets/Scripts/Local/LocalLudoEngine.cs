using System;
using System.Collections.Generic;

namespace Ludo.Local
{
    /// <summary>
    /// Pure-C# Ludo rules engine for offline play.
    /// Mirrors the server-side engine in server/src/game/engine.js exactly,
    /// so behaviour stays consistent if you later switch to online play.
    /// </summary>
    public class LocalLudoEngine
    {
        public const int SeatCount = 4;
        public const int TokensPerSeat = 4;

        public enum TokenStateKind { Home = 0, Track = 1, HomePath = 2, Finished = 3 }

        public class TokenState
        {
            public TokenStateKind kind = TokenStateKind.Home;
            public int track = -1;
            public int home  = -1;
            public void ResetToHome() { kind = TokenStateKind.Home; track = -1; home = -1; }
        }

        public class PlayerInfo
        {
            public int seat;
            public string name;
            public bool isBot;
            public bool active = true;     // false once seat finishes
        }

        public class TurnInfo
        {
            public int seat;
            public int? dice;
            public int sixStreak;
            public bool mustMove;
        }

        public enum EventKind
        {
            DiceRolled, NoMove, TripleSix, TurnPassed,
            TokenOut, TokenMoved, TokenKilled, TokenFinished,
            ExtraTurn, SeatFinished, MatchFinished
        }

        public struct Event
        {
            public EventKind kind;
            public int seat;
            public int token;
            public int dice;
            public int trackIndex;
            public int homeIndex;
            public TokenStateKind state;
        }

        public TokenState[][] Tokens;
        public List<PlayerInfo> Players;
        public TurnInfo Turn;
        public string Status;       // "active" | "finished"
        public int? WinnerSeat;
        public List<int> FinishedSeats;

        private readonly System.Random _rng = new System.Random();

        public LocalLudoEngine(IList<PlayerInfo> players)
        {
            if (players == null || players.Count < 2 || players.Count > 4)
                throw new ArgumentException("players must be 2..4");

            Players = new List<PlayerInfo>(players);
            Tokens = new TokenState[SeatCount][];
            for (int s = 0; s < SeatCount; s++)
            {
                Tokens[s] = new TokenState[TokensPerSeat];
                for (int t = 0; t < TokensPerSeat; t++) Tokens[s][t] = new TokenState();
            }
            // Mark seats with no player as "inactive" so turn-rotation skips them.
            var seatsWithPlayer = new HashSet<int>();
            foreach (var p in Players) seatsWithPlayer.Add(p.seat);
            for (int s = 0; s < SeatCount; s++)
            {
                if (!seatsWithPlayer.Contains(s))
                {
                    Players.Add(new PlayerInfo { seat = s, name = null, isBot = false, active = false });
                }
            }

            FinishedSeats = new List<int>();
            Turn = new TurnInfo { seat = FirstActiveSeat(), dice = null, mustMove = false, sixStreak = 0 };
            Status = "active";
        }

        private int FirstActiveSeat()
        {
            for (int s = 0; s < SeatCount; s++)
                if (IsSeatActive(s)) return s;
            return 0;
        }

        public bool IsSeatActive(int seat)
        {
            var p = Players.Find(pp => pp.seat == seat);
            return p != null && p.active && !FinishedSeats.Contains(seat);
        }

        // -------- Public API --------

        /// <summary>Roll the dice for the current seat. Returns the events that resulted.</summary>
        public List<Event> Roll()
        {
            var events = new List<Event>();
            if (Status != "active" || Turn.dice != null) return events;

            int dice = _rng.Next(1, 7);
            Turn.dice = dice;
            events.Add(new Event { kind = EventKind.DiceRolled, seat = Turn.seat, dice = dice });

            if (dice == 6)
            {
                Turn.sixStreak++;
                if (Turn.sixStreak >= 3)
                {
                    events.Add(new Event { kind = EventKind.TripleSix, seat = Turn.seat });
                    PassTurn(events);
                    return events;
                }
            }

            if (!HasAnyMovable(Turn.seat, dice))
            {
                events.Add(new Event { kind = EventKind.NoMove, seat = Turn.seat, dice = dice });
                PassTurn(events);
            }
            else
            {
                Turn.mustMove = true;
            }
            return events;
        }

        /// <summary>Move a specific token using the current dice value.</summary>
        public List<Event> Move(int tokenIndex)
        {
            var events = new List<Event>();
            if (Status != "active" || Turn.dice == null || !Turn.mustMove) return events;
            if (!CanMove(Turn.seat, tokenIndex, Turn.dice.Value)) return events;

            int dice = Turn.dice.Value;
            int seat = Turn.seat;
            bool killed = false;
            bool finished = false;
            int? landingTrack = null;

            var t = Tokens[seat][tokenIndex];

            if (t.kind == TokenStateKind.Home)
            {
                t.kind = TokenStateKind.Track;
                t.track = BoardCells.StartCell[seat];
                t.home = -1;
                landingTrack = t.track;
                events.Add(new Event { kind = EventKind.TokenOut, seat = seat, token = tokenIndex, trackIndex = t.track });
            }
            else if (t.kind == TokenStateKind.Track)
            {
                int traveled = BoardCells.DistanceFromStart(seat, t.track);
                int homeEntryDist = BoardCells.DistanceFromStart(seat, BoardCells.HomeEntry[seat]);
                int newDist = traveled + dice;
                if (newDist <= homeEntryDist)
                {
                    t.track = (t.track + dice) % BoardCells.TrackLength;
                    landingTrack = t.track;
                }
                else
                {
                    int intoHome = newDist - homeEntryDist - 1;
                    if (intoHome == BoardCells.HomePathLength)
                    {
                        t.kind = TokenStateKind.Finished;
                        t.track = -1; t.home = -1;
                        finished = true;
                    }
                    else
                    {
                        t.kind = TokenStateKind.HomePath;
                        t.track = -1;
                        t.home = intoHome;
                    }
                }
            }
            else if (t.kind == TokenStateKind.HomePath)
            {
                int newPos = t.home + dice;
                if (newPos == BoardCells.HomePathLength)
                {
                    t.kind = TokenStateKind.Finished;
                    t.home = -1;
                    finished = true;
                }
                else
                {
                    t.home = newPos;
                }
            }

            // Kill detection
            if (landingTrack.HasValue && !BoardCells.IsSafeCell(landingTrack.Value))
            {
                for (int s = 0; s < SeatCount; s++)
                {
                    if (s == seat) continue;
                    for (int j = 0; j < TokensPerSeat; j++)
                    {
                        var ot = Tokens[s][j];
                        if (ot.kind == TokenStateKind.Track && ot.track == landingTrack.Value)
                        {
                            ot.ResetToHome();
                            killed = true;
                            events.Add(new Event { kind = EventKind.TokenKilled, seat = s, token = j });
                        }
                    }
                }
            }

            events.Add(new Event
            {
                kind = EventKind.TokenMoved,
                seat = seat,
                token = tokenIndex,
                state = t.kind,
                trackIndex = t.track,
                homeIndex = t.home
            });
            if (finished)
                events.Add(new Event { kind = EventKind.TokenFinished, seat = seat, token = tokenIndex });

            // Win detection
            if (AllFinished(seat))
            {
                FinishedSeats.Add(seat);
                events.Add(new Event { kind = EventKind.SeatFinished, seat = seat });
                if (WinnerSeat == null) WinnerSeat = seat;
                int activeRemaining = 0;
                for (int s = 0; s < SeatCount; s++)
                    if (IsSeatActive(s)) activeRemaining++;
                if (activeRemaining <= 1)
                {
                    Status = "finished";
                    events.Add(new Event { kind = EventKind.MatchFinished, seat = WinnerSeat ?? 0 });
                    return events;
                }
            }

            // Extra turn rule
            bool extra = dice == 6 || killed || finished;
            if (extra)
            {
                Turn.dice = null;
                Turn.mustMove = false;
                events.Add(new Event { kind = EventKind.ExtraTurn, seat = seat });
            }
            else
            {
                PassTurn(events);
            }
            return events;
        }

        public bool CanMove(int seat, int tokenIndex, int dice)
        {
            var t = Tokens[seat][tokenIndex];
            if (t.kind == TokenStateKind.Finished) return false;
            if (t.kind == TokenStateKind.Home) return dice == 6;
            if (t.kind == TokenStateKind.Track)
            {
                int traveled = BoardCells.DistanceFromStart(seat, t.track);
                int homeEntryDist = BoardCells.DistanceFromStart(seat, BoardCells.HomeEntry[seat]);
                int newDist = traveled + dice;
                int max = homeEntryDist + BoardCells.HomePathLength;
                return newDist <= max;
            }
            if (t.kind == TokenStateKind.HomePath)
                return t.home + dice <= BoardCells.HomePathLength;
            return false;
        }

        public bool HasAnyMovable(int seat, int dice)
        {
            for (int i = 0; i < TokensPerSeat; i++)
                if (CanMove(seat, i, dice)) return true;
            return false;
        }

        private bool AllFinished(int seat)
        {
            for (int i = 0; i < TokensPerSeat; i++)
                if (Tokens[seat][i].kind != TokenStateKind.Finished) return false;
            return true;
        }

        private void PassTurn(List<Event> events)
        {
            Turn.dice = null;
            Turn.mustMove = false;
            Turn.sixStreak = 0;
            int next = NextActiveSeat(Turn.seat);
            Turn.seat = next;
            events.Add(new Event { kind = EventKind.TurnPassed, seat = next });
        }

        private int NextActiveSeat(int from)
        {
            for (int i = 1; i <= SeatCount; i++)
            {
                int s = (from + i) % SeatCount;
                if (IsSeatActive(s)) return s;
            }
            return from;
        }

        // -------- Bot helper --------

        /// <summary>Simple bot: prefer kill > finish > release > advance furthest.</summary>
        public int PickBotToken(int seat, int dice)
        {
            int best = -1;
            int bestScore = int.MinValue;
            for (int i = 0; i < TokensPerSeat; i++)
            {
                if (!CanMove(seat, i, dice)) continue;
                int score = ScoreMove(seat, i, dice);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        private int ScoreMove(int seat, int tokenIndex, int dice)
        {
            var t = Tokens[seat][tokenIndex];
            int score = 0;

            int? landing = null;
            if (t.kind == TokenStateKind.Home && dice == 6)
                landing = BoardCells.StartCell[seat];
            else if (t.kind == TokenStateKind.Track)
            {
                int traveled = BoardCells.DistanceFromStart(seat, t.track);
                int homeEntryDist = BoardCells.DistanceFromStart(seat, BoardCells.HomeEntry[seat]);
                if (traveled + dice <= homeEntryDist)
                    landing = (t.track + dice) % BoardCells.TrackLength;
            }

            // Kill
            if (landing.HasValue && !BoardCells.IsSafeCell(landing.Value))
            {
                for (int s = 0; s < SeatCount; s++)
                {
                    if (s == seat) continue;
                    for (int j = 0; j < TokensPerSeat; j++)
                    {
                        if (Tokens[s][j].kind == TokenStateKind.Track && Tokens[s][j].track == landing.Value)
                            score += 200;
                    }
                }
            }

            // Finish a token
            if (t.kind == TokenStateKind.HomePath && t.home + dice == BoardCells.HomePathLength) score += 120;

            // Release from base on a 6
            if (t.kind == TokenStateKind.Home && dice == 6) score += 50;

            // Land on safe cell
            if (landing.HasValue && BoardCells.IsSafeCell(landing.Value)) score += 20;

            // Bias toward more progressed tokens
            if (t.kind == TokenStateKind.Track) score += BoardCells.DistanceFromStart(seat, t.track) / 4;
            if (t.kind == TokenStateKind.HomePath) score += 30 + t.home;

            return score;
        }
    }
}
