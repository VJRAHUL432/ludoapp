'use strict';

/**
 * Matchmaking service: creates private rooms and runs the online queue.
 * Returned matches are saved to Redis (authoritative state) and Postgres
 * (historical record). Socket layer hands subsequent actions.
 */

const logger = require('../config/logger');
const { newMatchState } = require('../game/state');
const { saveMatch, saveRoom, loadRoom } = require('../game/store');
const { makeMatchId, makeRoomCode } = require('../utils/ids');
const queue = require('./queue');
const matchesDb = require('../db/matches');
const config = require('../config/env');

const MAX_ROOM_TRIES = 5;

// ---------- private rooms ----------

async function createPrivateRoom({ host, playerCount }) {
  if (![2, 3, 4].includes(playerCount)) {
    const err = new Error('player_count_invalid');
    err.status = 400; throw err;
  }
  let code;
  for (let i = 0; i < MAX_ROOM_TRIES; i++) {
    const c = makeRoomCode(5);
    if (!(await loadRoom(c))) { code = c; break; }
  }
  if (!code) throw new Error('could_not_allocate_room');

  const matchId = makeMatchId();
  const state = newMatchState({
    matchId,
    matchType: 'private',
    players: [{ seat: 0, userId: host.id, name: host.name, isBot: false }],
  });
  // expand seat info: the room can host up to playerCount players
  state.expectedPlayers = playerCount;
  state.roomCode = code;
  state.status = 'pending'; // not active until full

  await saveMatch(state);
  await saveRoom(code, matchId);
  logger.info({ matchId, code, host: host.id }, 'private room created');
  return { matchId, code, state };
}

async function joinPrivateRoom({ code, user }) {
  const room = await loadRoom(code);
  if (!room) {
    const err = new Error('room_not_found'); err.status = 404; throw err;
  }
  return room.matchId;
}

// ---------- online matchmaking ----------

async function joinOnlineQueue({ user, playerCount = 4 }) {
  await queue.enqueue(playerCount, {
    userId: user.id,
    name: user.name,
    avatar: user.avatar,
    enqueuedAt: Date.now(),
  });
  return tryStartOnlineMatch(playerCount);
}

async function tryStartOnlineMatch(playerCount) {
  const formed = await queue.tryFormMatch(playerCount);
  if (!formed) return null;
  const matchId = makeMatchId();
  const players = formed.map((p, idx) => ({
    seat: idx,
    userId: p.userId,
    name: p.name,
    isBot: false,
  }));
  const state = newMatchState({ matchId, matchType: 'online', players });
  state.turn.rollExpiresAt = Date.now() + config.game.turnTimeoutMs;
  await saveMatch(state);
  // Persist to Postgres for history (best effort)
  try {
    await matchesDb.createMatch({
      matchType: 'online',
      players: players.map((p) => ({ userId: p.userId, seat: p.seat, isBot: false })),
    });
  } catch (err) {
    logger.error({ err, matchId }, 'failed to persist match row');
  }
  logger.info({ matchId, players: players.length }, 'online match started');
  return { matchId, state };
}

// ---------- bot match (offline-style on server, used for "play vs computer" online if desired) ----------

async function createBotMatch({ user, difficulty = 'medium' }) {
  const matchId = makeMatchId();
  const players = [
    { seat: 0, userId: user.id, name: user.name, isBot: false },
    { seat: 1, userId: null, name: 'BotA', isBot: true },
    { seat: 2, userId: null, name: 'BotB', isBot: true },
    { seat: 3, userId: null, name: 'BotC', isBot: true },
  ];
  const state = newMatchState({ matchId, matchType: 'bot', players });
  state.botDifficulty = difficulty;
  state.turn.rollExpiresAt = Date.now() + config.game.turnTimeoutMs;
  await saveMatch(state);
  return { matchId, state };
}

module.exports = {
  createPrivateRoom,
  joinPrivateRoom,
  joinOnlineQueue,
  tryStartOnlineMatch,
  createBotMatch,
};
