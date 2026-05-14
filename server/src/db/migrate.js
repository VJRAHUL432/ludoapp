'use strict';

/**
 * Simple SQL migration runner.
 * Reads /migrations/*.sql in alphabetical order and applies them.
 * Tracks applied files in `_migrations` table.
 */

const fs = require('fs');
const path = require('path');
const { initDb, getPool, closeDb } = require('./pool');
const logger = require('../config/logger');

async function ensureTable(client) {
  await client.query(`
    CREATE TABLE IF NOT EXISTS _migrations (
      filename   VARCHAR(255) PRIMARY KEY,
      applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
    )
  `);
}

async function run() {
  await initDb();
  const dir = path.join(__dirname, '..', '..', 'migrations');
  const files = fs.readdirSync(dir).filter((f) => f.endsWith('.sql')).sort();

  const pool = getPool();
  const client = await pool.connect();
  try {
    await ensureTable(client);
    const { rows } = await client.query('SELECT filename FROM _migrations');
    const applied = new Set(rows.map((r) => r.filename));

    for (const f of files) {
      if (applied.has(f)) {
        logger.info({ f }, 'skip (already applied)');
        continue;
      }
      const sql = fs.readFileSync(path.join(dir, f), 'utf8');
      logger.info({ f }, 'applying migration');
      await client.query('BEGIN');
      try {
        await client.query(sql);
        await client.query('INSERT INTO _migrations(filename) VALUES ($1)', [f]);
        await client.query('COMMIT');
      } catch (err) {
        await client.query('ROLLBACK');
        throw err;
      }
    }
    logger.info('migrations complete');
  } finally {
    client.release();
    await closeDb();
  }
}

run().catch((err) => {
  logger.fatal({ err }, 'migration failed');
  process.exit(1);
});
