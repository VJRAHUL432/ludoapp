'use strict';

const { Pool } = require('pg');
const config = require('../config/env');
const logger = require('../config/logger');

let pool = null;

async function initDb() {
  if (pool) return pool;
  pool = new Pool({
    host: config.pg.host,
    port: config.pg.port,
    user: config.pg.user,
    password: config.pg.password,
    database: config.pg.database,
    max: config.pg.max,
    idleTimeoutMillis: config.pg.idleTimeoutMillis,
  });
  pool.on('error', (err) => logger.error({ err }, 'pg pool error'));
  // sanity ping
  await pool.query('SELECT 1');
  logger.info('postgres connected');
  return pool;
}

function getPool() {
  if (!pool) throw new Error('Postgres pool not initialised');
  return pool;
}

async function query(text, params) {
  const start = Date.now();
  try {
    const res = await getPool().query(text, params);
    if (Date.now() - start > 250) {
      logger.warn({ ms: Date.now() - start, text }, 'slow query');
    }
    return res;
  } catch (err) {
    logger.error({ err, text }, 'query failed');
    throw err;
  }
}

async function withTransaction(fn) {
  const client = await getPool().connect();
  try {
    await client.query('BEGIN');
    const result = await fn(client);
    await client.query('COMMIT');
    return result;
  } catch (err) {
    await client.query('ROLLBACK').catch(() => {});
    throw err;
  } finally {
    client.release();
  }
}

async function closeDb() {
  if (pool) {
    await pool.end();
    pool = null;
  }
}

module.exports = { initDb, getPool, query, withTransaction, closeDb };
