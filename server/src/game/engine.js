'use strict';

/**
 * Server-authoritative Ludo engine.
 *
 * Pure functions that take the current `state` and an action, and return:
 *   { ok: true, events: [...] }   on success (mutates state in place)
 *   { ok: false, code }           on rejection (state untouched)
 *
 * `events` is a list of small deltas the socket layer broadcasts to clients.
 */

const {
  SEAT_COUNT, TOKENS_PER_SEAT, TRACK_LENGTH, HOME_PATH_LENGTH,
  START_CELL, HOME_ENTRY, TOKEN_STATE, isSafeCell, distanceFromStart,
} = require('./board');
const { rollDie } = require('./dice');
const config = require('../config/env');

const MAX_APPLIED = 64;

// ---------- helpers ----------

function rememberAction(state, actionId) {
  if (!actionId) return;
  state.appliedActions.push(actionId);
  if (state.appliedActions.length > MAX_APPLIED) {
    state.appliedActions.splice(0, state.appliedActions.length - MAX_APPLIED);
  }
}

function isDuplicate(state, actionId) {
  return !!actionId && state.appliedActions.includes(actionId);
}

function nextSeat(state, fromSeat) {
  const seats = [];
  for (let i = 1; i <= SEAT_COUNT; i++) {
    const s = (fromSeat + i) % SEAT_COUNT;
    if (state.players.find((p) => p.seat === s) && !state.finishedSeats.includes(s)) {
      seats.push(s);
    }
  }
  return seats[0];
}

function hasAnyMovableToken(state, seat, dice) {
  for (let i = 0; i < TOKENS_PER_SEAT; i++) {
    if (canMoveToken(state, seat, i, dice).ok) return true;
  }
  return false;
}

/**
 * Validates whether a given token can move with the given dice value.
 * Pure / read-only.
 */
function canMoveToken(state, seat, tokenIndex, dice) {
  const t = state.tokens[seat]?.[tokenIndex];
  if (!t) return { ok: false, code: 'invalid_token' };

  if (t.state === TOKEN_STATE.FINISHED) return { ok: false, code: 'token_finished' };

  if (t.state === TOKEN_STATE.HOME) {
    if (dice !== 6) return { ok: false, code: 'need_six' };
    return { ok: true };
  }

  if (t.state === TOKEN_STATE.TRACK) {
    const traveled = distanceFromStart(seat, t.track);
    const homeEntryDistance = distanceFromStart(seat, HOME_ENTRY[seat]);
    // distance after this dice
    const newDistance = traveled + dice;
    const maxDistance = homeEntryDistance + HOME_PATH_LENGTH; // home_path[4] is the finish
    if (newDistance > maxDistance) return { ok: false, code: 'overshoot' };
    return { ok: true };
  }

  if (t.state === TOKEN_STATE.HOME_PATH) {
    if (t.home + dice >= HOME_PATH_LENGTH + 1) {
      // home_path indices 0..4, then +1 == finish
      if (t.home + dice > HOME_PATH_LENGTH) return { ok: false, code: 'overshoot' };
    }
    return { ok: true };
  }

  return { ok: false, code: 'invalid_state' };
}

/**
 * Move the token (mutates state.tokens[seat][tokenIndex] and may kill enemies).
 * Returns { events, killed: boolean, finished: boolean, landingTrackIndex|null }.
 */
function moveToken(state, seat, tokenIndex, dice) {
  const t = state.tokens[seat][tokenIndex];
  const events = [];
  let killed = false;
  let finished = false;
  let landingTrackIndex = null;

  if (t.state === TOKEN_STATE.HOME) {
    // Comes out to start cell.
    t.state = TOKEN_STATE.TRACK;
    t.track = START_CELL[seat];
    t.home = -1;
    landingTrackIndex = t.track;
    events.push({ type: 'TOKEN_OUT', seat, token: tokenIndex, cell: t.track });
  } else if (t.state === TOKEN_STATE.TRACK) {
    const traveled = distanceFromStart(seat, t.track);
    const homeEntryDistance = distanceFromStart(seat, HOME_ENTRY[seat]);
    const newDistance = traveled + dice;
    if (newDistance <= homeEntryDistance) {
      t.track = (t.track + dice) % TRACK_LENGTH;
      landingTrackIndex = t.track;
    } else {
      // enter / advance into home path
      const intoHome = newDistance - homeEntryDistance - 1; // 0-based position in home column
      if (intoHome === HOME_PATH_LENGTH) {
        t.state = TOKEN_STATE.FINISHED;
        t.track = -1;
        t.home = -1;
        finished = true;
      } else {
        t.state = TOKEN_STATE.HOME_PATH;
        t.track = -1;
        t.home = intoHome;
      }
    }
  } else if (t.state === TOKEN_STATE.HOME_PATH) {
    const newPos = t.home + dice;
    if (newPos === HOME_PATH_LENGTH) {
      t.state = TOKEN_STATE.FINISHED;
      t.home = -1;
      finished = true;
    } else {
      t.home = newPos;
    }
  }

  // Kill detection on the main track only, on non-safe cells.
  if (landingTrackIndex !== null && !isSafeCell(landingTrackIndex)) {
    for (let s = 0; s < SEAT_COUNT; s++) {
      if (s === seat) continue;
      const opp = state.tokens[s];
      if (!opp) continue;
      for (let j = 0; j < opp.length; j++) {
        const ot = opp[j];
        if (ot.state === TOKEN_STATE.TRACK && ot.track === landingTrackIndex) {
          ot.state = TOKEN_STATE.HOME;
          ot.track = -1;
          ot.home = -1;
          killed = true;
          events.push({ type: 'TOKEN_KILLED', seat: s, token: j });
        }
      }
    }
  }

  events.push({
    type: 'TOKEN_MOVED',
    seat, token: tokenIndex,
    state: t.state,
    track: t.track,
    home: t.home,
  });
  if (finished) events.push({ type: 'TOKEN_FINISHED', seat, token: tokenIndex });

  return { events, killed, finished, landingTrackIndex };
}

function allTokensFinished(state, seat) {
  return state.tokens[seat].every((t) => t.state === TOKEN_STATE.FINISHED);
}

// ---------- public actions ----------

/**
 * Roll the dice for the current seat. Server picks the value.
 */
function applyRoll(state, { actionId, seat }) {
  if (state.status !== 'active') return { ok: false, code: 'not_active' };
  if (state.turn.seat !== seat)   return { ok: false, code: 'not_your_turn' };
  if (state.turn.dice !== null)   return { ok: false, code: 'already_rolled' };
  if (isDuplicate(state, actionId)) return { ok: true, events: [], duplicate: true };

  const dice = rollDie();
  state.turn.dice = dice;
  state.turn.rolledAt = Date.now();
  state.turn.moveExpiresAt = Date.now() + config.game.turnTimeoutMs;

  const events = [{ type: 'DICE_ROLLED', seat, dice }];

  // Triple six penalty: third six in a row -> turn forfeited.
  if (dice === 6) {
    state.turn.sixStreak += 1;
    if (state.turn.sixStreak >= 3) {
      events.push({ type: 'TRIPLE_SIX', seat });
      _passTurn(state, events);
      rememberAction(state, actionId);
      return { ok: true, events };
    }
  }

  // If no token can move with this dice -> pass turn.
  if (!hasAnyMovableToken(state, seat, dice)) {
    events.push({ type: 'NO_MOVE', seat, dice });
    _passTurn(state, events);
  } else {
    state.turn.mustMove = true;
  }

  rememberAction(state, actionId);
  return { ok: true, events };
}

/**
 * Move a specific token using the rolled dice.
 */
function applyMove(state, { actionId, seat, tokenIndex }) {
  if (state.status !== 'active') return { ok: false, code: 'not_active' };
  if (state.turn.seat !== seat)   return { ok: false, code: 'not_your_turn' };
  if (state.turn.dice === null)   return { ok: false, code: 'roll_first' };
  if (!state.turn.mustMove)       return { ok: false, code: 'no_pending_move' };
  if (isDuplicate(state, actionId)) return { ok: true, events: [], duplicate: true };

  const dice = state.turn.dice;
  const check = canMoveToken(state, seat, tokenIndex, dice);
  if (!check.ok) return { ok: false, code: check.code };

  const { events, killed, finished } = moveToken(state, seat, tokenIndex, dice);

  // Win detection
  if (allTokensFinished(state, seat)) {
    state.finishedSeats.push(seat);
    events.push({ type: 'SEAT_FINISHED', seat });
    if (state.winnerSeat === null) state.winnerSeat = seat;
    // Game ends when only one player remains.
    const stillPlaying = state.players
      .map((p) => p.seat)
      .filter((s) => !state.finishedSeats.includes(s));
    if (stillPlaying.length <= 1) {
      state.status = 'finished';
      events.push({ type: 'MATCH_FINISHED', winnerSeat: state.winnerSeat, ranking: [...state.finishedSeats] });
      rememberAction(state, actionId);
      return { ok: true, events };
    }
  }

  // Extra turn rule: rolling a six OR killing OR finishing a token.
  const extraTurn = dice === 6 || killed || finished;
  if (extraTurn) {
    state.turn.dice = null;
    state.turn.mustMove = false;
    state.turn.moveExpiresAt = null;
    state.turn.rollExpiresAt = Date.now() + config.game.turnTimeoutMs;
    events.push({ type: 'EXTRA_TURN', seat });
  } else {
    _passTurn(state, events);
  }

  rememberAction(state, actionId);
  return { ok: true, events };
}

/**
 * Apply a turn timeout: current seat missed their turn.
 * If they hadn't rolled yet, roll for them; if they hadn't moved, auto-pick a token.
 */
function applyTimeout(state, { seat }) {
  if (state.status !== 'active') return { ok: false, code: 'not_active' };
  if (state.turn.seat !== seat)   return { ok: false, code: 'not_your_turn' };

  const events = [{ type: 'TURN_TIMEOUT', seat }];
  const player = state.players.find((p) => p.seat === seat);
  if (player) player.missedTurns = (player.missedTurns || 0) + 1;

  if (state.turn.dice === null) {
    // Auto roll then auto move (or pass)
    const r = applyRoll(state, { actionId: null, seat });
    if (r.ok) events.push(...r.events);
    if (state.turn.mustMove) {
      const idx = pickAutoTokenIndex(state, seat, state.turn.dice);
      if (idx >= 0) {
        const m = applyMove(state, { actionId: null, seat, tokenIndex: idx });
        if (m.ok) events.push(...m.events);
      } else {
        _passTurn(state, events);
      }
    }
  } else if (state.turn.mustMove) {
    const idx = pickAutoTokenIndex(state, seat, state.turn.dice);
    if (idx >= 0) {
      const m = applyMove(state, { actionId: null, seat, tokenIndex: idx });
      if (m.ok) events.push(...m.events);
    } else {
      _passTurn(state, events);
    }
  } else {
    _passTurn(state, events);
  }

  return { ok: true, events };
}

function pickAutoTokenIndex(state, seat, dice) {
  // Prefer: kill > finish a token > advance > release from base.
  let bestIdx = -1;
  let bestScore = -Infinity;
  for (let i = 0; i < TOKENS_PER_SEAT; i++) {
    const c = canMoveToken(state, seat, i, dice);
    if (!c.ok) continue;
    const t = state.tokens[seat][i];
    let score = 0;
    if (t.state === TOKEN_STATE.HOME) score += 10;
    if (t.state === TOKEN_STATE.TRACK) score += 5;
    if (t.state === TOKEN_STATE.HOME_PATH) score += 3;
    if (score > bestScore) { bestScore = score; bestIdx = i; }
  }
  return bestIdx;
}

function _passTurn(state, events) {
  state.turn.dice = null;
  state.turn.sixStreak = 0;
  state.turn.mustMove = false;
  state.turn.rolledAt = null;
  state.turn.moveExpiresAt = null;
  const next = nextSeat(state, state.turn.seat);
  state.turn.seat = next;
  state.turn.rollExpiresAt = Date.now() + config.game.turnTimeoutMs;
  events.push({ type: 'TURN_PASSED', seat: next });
}

// Public state snapshot for clients (omit secrets).
function publicView(state) {
  return {
    matchId: state.matchId,
    matchType: state.matchType,
    status: state.status,
    players: state.players,
    tokens: state.tokens,
    turn: state.turn,
    winnerSeat: state.winnerSeat,
    finishedSeats: state.finishedSeats,
  };
}

module.exports = {
  applyRoll,
  applyMove,
  applyTimeout,
  canMoveToken,
  pickAutoTokenIndex,
  publicView,
};
