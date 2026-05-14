'use strict';

/**
 * Match state store backed by Redis.
 * Reads/writes the full authoritative state JSON for a match,
 * plus indexes session -> matchId so we can rebuild on reconnect.
 */

const { setJson, getJson, getRedis, del, TTL } = require('../cache/redis');

const matchKey   = (matchId) => `match:${matchId}`;
const roomKey    = (code)    => `room:${code.toUpperCase()}`;
const sessionKey = (userId)  => `session:${userId}`;

async function saveMatch(state) {
  await setJson(matchKey(state.matchId), state, TTL.match);
}

async function loadMatch(matchId) {
  return getJson(matchKey(matchId));
}

async function deleteMatch(matchId) {
  await del(matchKey(matchId));
}

async function saveRoom(code, matchId) {
  await setJson(roomKey(code), { matchId }, TTL.room);
}

async function loadRoom(code) {
  return getJson(roomKey(code));
}

async function deleteRoom(code) {
  await del(roomKey(code));
}

async function saveSession(userId, payload) {
  await setJson(sessionKey(userId), payload, TTL.session);
}

async function loadSession(userId) {
  return getJson(sessionKey(userId));
}

async function clearSession(userId) {
  await del(sessionKey(userId));
}

/**
 * Per-match mutex to serialise applyRoll/applyMove. Prevents two clients
 * racing the same match. Uses Redis SET NX with short TTL.
 */
async function withMatchLock(matchId, fn, { timeoutMs = 1500 } = {}) {
  const r = getRedis();
  const key = `lock:${matchId}`;
  const token = `${process.pid}:${Date.now()}:${Math.random().toString(36).slice(2)}`;
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const ok = await r.set(key, token, 'PX', 2000, 'NX');
    if (ok === 'OK') {
      try {
        return await fn();
      } finally {
        // best-effort token-checked release
        const v = await r.get(key);
        if (v === token) await r.del(key);
      }
    }
    await new Promise((res) => setTimeout(res, 25));
  }
  const err = new Error('lock_timeout');
  err.status = 503;
  throw err;
}

module.exports = {
  saveMatch, loadMatch, deleteMatch,
  saveRoom, loadRoom, deleteRoom,
  saveSession, loadSession, clearSession,
  withMatchLock,
};
