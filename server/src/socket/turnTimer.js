'use strict';

/**
 * Per-match turn timer. Schedules a single-shot timeout that calls the
 * engine's applyTimeout when the current seat takes too long.
 *
 * Timers live only on the node that owns the match (single node deploy).
 * For multi-node, hoist this to a delayed Redis stream.
 */

const config = require('../config/env');

const timers = new Map(); // matchId -> NodeJS.Timeout

function clearTurnTimer(matchId) {
  const t = timers.get(matchId);
  if (t) {
    clearTimeout(t);
    timers.delete(matchId);
  }
}

function scheduleTurnTimer(matchId, fn) {
  clearTurnTimer(matchId);
  const t = setTimeout(() => {
    timers.delete(matchId);
    Promise.resolve(fn()).catch(() => {});
  }, config.game.turnTimeoutMs);
  // Don't keep the event loop alive just for this timer.
  if (typeof t.unref === 'function') t.unref();
  timers.set(matchId, t);
}

module.exports = { scheduleTurnTimer, clearTurnTimer };
