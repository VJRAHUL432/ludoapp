'use strict';

require('dotenv').config();

const required = (key, fallback) => {
  const v = process.env[key] ?? fallback;
  if (v === undefined || v === null || v === '') {
    throw new Error(`Missing required env var: ${key}`);
  }
  return v;
};

const num = (key, fallback) => {
  const v = process.env[key];
  if (v === undefined || v === '') return fallback;
  const n = Number(v);
  if (Number.isNaN(n)) throw new Error(`Env ${key} must be a number`);
  return n;
};

const config = {
  env: process.env.NODE_ENV || 'development',
  port: num('PORT', 3000),

  jwt: {
    secret: required('JWT_SECRET', 'dev-only-change-me'),
    expiresIn: process.env.JWT_EXPIRES_IN || '7d',
  },

  pg: {
    host: process.env.PGHOST || 'localhost',
    port: num('PGPORT', 5432),
    user: process.env.PGUSER || 'ludo',
    password: process.env.PGPASSWORD || 'ludo',
    database: process.env.PGDATABASE || 'ludo',
    max: num('PG_POOL_MAX', 20),
    idleTimeoutMillis: 30000,
  },

  redis: {
    url: process.env.REDIS_URL || 'redis://127.0.0.1:6379',
  },

  google: {
    clientId: process.env.GOOGLE_CLIENT_ID || '',
  },

  game: {
    turnTimeoutMs: num('TURN_TIMEOUT_MS', 20000),
    heartbeatIntervalMs: num('HEARTBEAT_INTERVAL_MS', 5000),
    maxMissedHeartbeats: num('MAX_MISSED_HEARTBEATS', 3),
    reconnectGraceMs: num('RECONNECT_GRACE_MS', 30000),
  },
};

module.exports = config;
