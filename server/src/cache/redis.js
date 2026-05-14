'use strict';

const Redis = require('ioredis');
const config = require('../config/env');
const logger = require('../config/logger');

let client = null;

async function initRedis() {
  if (client) return client;
  client = new Redis(config.redis.url, {
    maxRetriesPerRequest: 3,
    enableReadyCheck: true,
    lazyConnect: false,
  });
  client.on('error', (err) => logger.error({ err }, 'redis error'));
  client.on('connect', () => logger.info('redis connected'));
  // sanity ping
  await client.ping();
  return client;
}

function getRedis() {
  if (!client) throw new Error('Redis not initialised');
  return client;
}

async function closeRedis() {
  if (client) {
    await client.quit().catch(() => {});
    client = null;
  }
}

// ---------- typed helpers ----------

const TTL = {
  session: 60 * 60,        // 1h
  match: 60 * 60 * 4,      // 4h
  room: 60 * 60 * 2,       // 2h
};

async function setJson(key, value, ttlSec) {
  const s = JSON.stringify(value);
  if (ttlSec) await getRedis().set(key, s, 'EX', ttlSec);
  else await getRedis().set(key, s);
}

async function getJson(key) {
  const v = await getRedis().get(key);
  return v ? JSON.parse(v) : null;
}

async function del(...keys) {
  if (!keys.length) return 0;
  return getRedis().del(...keys);
}

module.exports = { initRedis, getRedis, closeRedis, setJson, getJson, del, TTL };
