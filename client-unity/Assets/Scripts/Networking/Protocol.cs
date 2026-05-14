namespace Ludo.Networking
{
    /// <summary>
    /// Wire protocol — must match server/src/socket/protocol.js.
    /// Keeping these as constants (not enums) avoids allocation when emitting.
    /// </summary>
    public static class ClientEvents
    {
        public const string MmPrivateCreate = "mm:private:create";
        public const string MmPrivateJoin   = "mm:private:join";
        public const string MmOnlineJoin    = "mm:online:join";
        public const string MmOnlineLeave   = "mm:online:leave";
        public const string MmBotStart      = "mm:bot:start";

        public const string GameResume      = "game:resume";
        public const string GameRoll        = "game:roll";
        public const string GameMove        = "game:move";
        public const string GameLeave       = "game:leave";

        public const string Ping            = "ping";
    }

    public static class ServerEvents
    {
        public const string MmPrivateCreated = "mm:private:created";
        public const string MmPrivateJoined  = "mm:private:joined";
        public const string MmQueued         = "mm:queued";
        public const string MmMatchFound     = "mm:match:found";

        public const string GameState        = "game:state";
        public const string GameEvent        = "game:event";
        public const string GameError        = "game:error";

        public const string Pong             = "pong";
        public const string ReconnectOk      = "reconnect:ok";
        public const string Kick             = "kick";
    }

    public static class EngineEventTypes
    {
        public const string DiceRolled    = "DICE_ROLLED";
        public const string NoMove        = "NO_MOVE";
        public const string TripleSix     = "TRIPLE_SIX";
        public const string TurnPassed    = "TURN_PASSED";
        public const string TokenOut      = "TOKEN_OUT";
        public const string TokenMoved    = "TOKEN_MOVED";
        public const string TokenKilled   = "TOKEN_KILLED";
        public const string TokenFinished = "TOKEN_FINISHED";
        public const string ExtraTurn     = "EXTRA_TURN";
        public const string SeatFinished  = "SEAT_FINISHED";
        public const string MatchFinished = "MATCH_FINISHED";
        public const string TurnTimeout   = "TURN_TIMEOUT";
    }
}
