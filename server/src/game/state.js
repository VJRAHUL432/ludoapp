'use strict';

const {
  SEAT_COUNT, TOKENS_PER_SEAT, TOKEN_STATE,
} = require('./board');

/**
 * Pure factory for a fresh authoritative match state.
 *
 * Shape:
 * {
 *   matchId, matchType, status, players: [{seat, userId, name, isBot, connected}],
 *   tokens: { [seat]: [{state, track, home}] x4 },
 *   turn: { seat, dice: null|number, sixStreak: 0|1|2, mustMove: boolean,
 *           rolledAt: number|null, rollExpiresAt: number|null,
 *           moveExpiresAt: number|null },
 *   appliedActions: [actionId,...]   // last N idempotency keys
 *   winnerSeat: null|0..3,
 *   finishedSeats: [],               // seats that finished, ranked
 *   rngSeed: hex string,             // for audit
 * }
 */
function freshTokens() {
  const tokens = {};
  for (let s = 0; s < SEAT_COUNT; s++) {
    tokens[s] = [];
    for (let i = 0; i < TOKENS_PER_SEAT; i++) {
      tokens[s].push({ state: TOKEN_STATE.HOME, track: -1, home: -1 });
    }
  }
  return tokens;
}

function newMatchState({ matchId, matchType, players }) {
  // players: [{ seat, userId, name, isBot }]
  return {
    matchId,
    matchType,
    status: 'active',
    createdAt: Date.now(),
    players: players.map((p) => ({
      seat: p.seat,
      userId: p.userId || null,
      name: p.name || `P${p.seat}`,
      isBot: !!p.isBot,
      connected: true,
      missedTurns: 0,
    })),
    tokens: freshTokens(),
    turn: {
      seat: 0,
      dice: null,
      sixStreak: 0,
      mustMove: false,
      rolledAt: null,
      rollExpiresAt: null,
      moveExpiresAt: null,
    },
    appliedActions: [],
    winnerSeat: null,
    finishedSeats: [],
  };
}

module.exports = { newMatchState };
