using System;
using System.Collections.Generic;
using Ludo.Game;

namespace Ludo.AI
{
    public enum BotDifficulty { Easy, Medium, Hard }

    /// <summary>
    /// Pure local Ludo bot. Used by the offline "Play vs Computer" mode.
    /// Online bot decisions are made on the server.
    ///
    /// Strategy (Hard):
    ///   1. Kill an opponent if possible.
    ///   2. Move a token that is about to finish.
    ///   3. Release a token from base on a 6 if a fresh start cell is empty.
    ///   4. Move the token closest to home.
    ///   5. Save a vulnerable token (one that could be killed next turn).
    /// </summary>
    public static class LudoBot
    {
        public static int PickToken(GameState state, int seat, int dice, BotDifficulty difficulty)
        {
            var legal = LegalTokens(state, seat, dice);
            if (legal.Count == 0) return -1;
            if (difficulty == BotDifficulty.Easy) return legal[Rand.Next(legal.Count)];

            // Score-based selection
            int best = legal[0];
            int bestScore = int.MinValue;
            foreach (var t in legal)
            {
                int score = ScoreMove(state, seat, t, dice, difficulty);
                if (score > bestScore) { bestScore = score; best = t; }
            }
            return best;
        }

        private static List<int> LegalTokens(GameState s, int seat, int dice)
        {
            var list = new List<int>(4);
            for (int i = 0; i < 4; i++)
            {
                if (CanMove(s, seat, i, dice)) list.Add(i);
            }
            return list;
        }

        private static bool CanMove(GameState s, int seat, int tokenIndex, int dice)
        {
            var t = s.Tokens[seat][tokenIndex];
            if (t.state == "FINISHED") return false;
            if (t.state == "HOME") return dice == 6;

            if (t.state == "TRACK")
            {
                int traveled = (t.track - BoardLayout.StartCell[seat] + 52) % 52;
                int homeEntry = (BoardLayout.HomeEntry[seat] - BoardLayout.StartCell[seat] + 52) % 52;
                int newDist = traveled + dice;
                int max = homeEntry + BoardLayout.HomePathLength;
                return newDist <= max;
            }
            if (t.state == "HOME_PATH")
                return t.home + dice <= BoardLayout.HomePathLength;
            return false;
        }

        private static int ScoreMove(GameState s, int seat, int tokenIndex, int dice, BotDifficulty diff)
        {
            var t = s.Tokens[seat][tokenIndex];
            int score = 0;

            // Predict landing cell on track
            int? landing = PredictLandingTrack(s, seat, tokenIndex, dice);

            // 1. Killing an opponent on a non-safe cell
            if (landing.HasValue && !BoardLayout.IsSafeCell(landing.Value))
            {
                if (HasOpponentAt(s, seat, landing.Value)) score += 100;
            }

            // 2. Finishing a token
            if (t.state == "HOME_PATH" && t.home + dice == BoardLayout.HomePathLength)
                score += 80;

            // 3. Releasing on a 6 from base
            if (t.state == "HOME" && dice == 6) score += 30;

            // 4. Closer to home gets a small bonus (Hard only)
            if (diff == BotDifficulty.Hard && t.state == "TRACK")
                score += (t.track - BoardLayout.StartCell[seat] + 52) % 52;

            // 5. Save vulnerable token: prefer landing on safe cell
            if (landing.HasValue && BoardLayout.IsSafeCell(landing.Value)) score += 10;

            // Tiny randomisation so the bot isn't perfectly deterministic.
            score += Rand.Next(0, 5);

            return score;
        }

        private static int? PredictLandingTrack(GameState s, int seat, int tokenIndex, int dice)
        {
            var t = s.Tokens[seat][tokenIndex];
            if (t.state == "HOME") return dice == 6 ? (int?)BoardLayout.StartCell[seat] : null;
            if (t.state == "TRACK")
            {
                int traveled = (t.track - BoardLayout.StartCell[seat] + 52) % 52;
                int homeEntry = (BoardLayout.HomeEntry[seat] - BoardLayout.StartCell[seat] + 52) % 52;
                if (traveled + dice <= homeEntry) return (t.track + dice) % 52;
                return null; // moves into home column
            }
            return null;
        }

        private static bool HasOpponentAt(GameState s, int seat, int trackIndex)
        {
            for (int o = 0; o < 4; o++)
            {
                if (o == seat) continue;
                for (int i = 0; i < 4; i++)
                {
                    var ot = s.Tokens[o][i];
                    if (ot.state == "TRACK" && ot.track == trackIndex) return true;
                }
            }
            return false;
        }

        private static class Rand
        {
            private static readonly Random _r = new Random();
            public static int Next(int max) => _r.Next(max);
            public static int Next(int min, int max) => _r.Next(min, max);
        }
    }
}
