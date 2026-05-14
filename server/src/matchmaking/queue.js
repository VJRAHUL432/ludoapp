'use strict';

/**
 * Online matchmaking via Redis lists.
 * One queue per requested player count (2/3/4).
 * `tryFormMatch` atomically pops up to N players when queue is full.
 */

const { getRedis } = require('../cache/redis');

const queueKey = (count) => `queue:online:${count}`;

async function enqueue(playerCount, payload) {
  const r = getRedis();
  await r.rpush(queueKey(playerCount), JSON.stringify(payload));
}

async function leaveQueue(playerCount, userId) {
  const r = getRedis();
  // Best-effort: rebuild the list without this user.
  const key = queueKey(playerCount);
  const all = await r.lrange(key, 0, -1);
  const tx = r.multi().del(key);
  for (const raw of all) {
    try {
      const v = JSON.parse(raw);
      if (v.userId !== userId) tx.rpush(key, raw);
    } catch (_) {/* ignore */}
  }
  await tx.exec();
}

async function tryFormMatch(playerCount) {
  const r = getRedis();
  const key = queueKey(playerCount);
  const len = await r.llen(key);
  if (len < playerCount) return null;
  const popped = [];
  for (let i = 0; i < playerCount; i++) {
    const raw = await r.lpop(key);
    if (!raw) {
      // partial pop: push back and abort
      for (const item of popped) await r.lpush(key, JSON.stringify(item));
      return null;
    }
    try { popped.push(JSON.parse(raw)); }
    catch (_) { /* drop malformed */ }
  }
  if (popped.length !== playerCount) {
    for (const item of popped) await r.rpush(key, JSON.stringify(item));
    return null;
  }
  return popped;
}

module.exports = { enqueue, leaveQueue, tryFormMatch };
