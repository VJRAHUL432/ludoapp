'use strict';

/**
 * Wire protocol constants. Single source of truth shared with the Unity client
 * (mirrored in client-unity/Assets/Scripts/Networking/Protocol.cs).
 */

const ClientEvents = Object.freeze({
  // matchmaking
  MM_PRIVATE_CREATE: 'mm:private:create',     // {playerCount}
  MM_PRIVATE_JOIN:   'mm:private:join',       // {code}
  MM_ONLINE_JOIN:    'mm:online:join',        // {playerCount}
  MM_ONLINE_LEAVE:   'mm:online:leave',       // {playerCount}
  MM_BOT_START:      'mm:bot:start',          // {difficulty}

  // game
  GAME_RESUME:       'game:resume',           // {matchId}
  GAME_ROLL:         'game:roll',             // {actionId, matchId}
  GAME_MOVE:         'game:move',             // {actionId, matchId, tokenIndex}
  GAME_LEAVE:        'game:leave',            // {matchId}

  // misc
  PING:              'ping',                  // {t}
});

const ServerEvents = Object.freeze({
  // matchmaking
  MM_PRIVATE_CREATED: 'mm:private:created',   // {matchId, code, state}
  MM_PRIVATE_JOINED:  'mm:private:joined',    // {matchId, state}
  MM_QUEUED:          'mm:queued',            // {playerCount, position}
  MM_MATCH_FOUND:     'mm:match:found',       // {matchId, state}

  // game state
  GAME_STATE:         'game:state',           // full snapshot
  GAME_EVENT:         'game:event',           // single delta event from engine
  GAME_ERROR:         'game:error',           // {code}

  // session / connection
  PONG:               'pong',                 // {t, server}
  RECONNECT_OK:       'reconnect:ok',
  KICK:               'kick',                 // {reason}
});

// Engine event types that may appear inside `game:event`/`game:state`:
//   DICE_ROLLED, NO_MOVE, TRIPLE_SIX, TURN_PASSED, TOKEN_OUT, TOKEN_MOVED,
//   TOKEN_KILLED, TOKEN_FINISHED, EXTRA_TURN, SEAT_FINISHED, MATCH_FINISHED,
//   TURN_TIMEOUT.

module.exports = { ClientEvents, ServerEvents };
