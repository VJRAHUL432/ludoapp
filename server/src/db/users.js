'use strict';

const { query, withTransaction } = require('./pool');

async function findByAuth(authType, authSubject) {
  const { rows } = await query(
    `SELECT id, auth_type, name, avatar, coins, wins, losses, rank_points
       FROM users
      WHERE auth_type = $1 AND auth_subject = $2`,
    [authType, authSubject],
  );
  return rows[0] || null;
}

async function findById(id) {
  const { rows } = await query(
    `SELECT id, auth_type, name, avatar, coins, wins, losses, rank_points
       FROM users WHERE id = $1`,
    [id],
  );
  return rows[0] || null;
}

async function createUser({ authType, authSubject, name, avatar = 'avatar_01' }) {
  const { rows } = await query(
    `INSERT INTO users (auth_type, auth_subject, name, avatar)
     VALUES ($1, $2, $3, $4)
     RETURNING id, auth_type, name, avatar, coins, wins, losses, rank_points`,
    [authType, authSubject, name, avatar],
  );
  return rows[0];
}

async function upsertGuest({ deviceId, name }) {
  return withTransaction(async (client) => {
    const existing = await client.query(
      `SELECT id, auth_type, name, avatar, coins, wins, losses, rank_points
         FROM users WHERE auth_type='guest' AND auth_subject=$1`,
      [deviceId],
    );
    if (existing.rows[0]) return existing.rows[0];
    const { rows } = await client.query(
      `INSERT INTO users (auth_type, auth_subject, name)
       VALUES ('guest', $1, $2)
       RETURNING id, auth_type, name, avatar, coins, wins, losses, rank_points`,
      [deviceId, name],
    );
    return rows[0];
  });
}

async function applyMatchResult({ winnerId, players }) {
  // players: [{ userId, isWinner }]
  return withTransaction(async (client) => {
    for (const p of players) {
      if (!p.userId) continue;
      if (p.isWinner) {
        await client.query(
          `UPDATE users
              SET wins = wins + 1,
                  coins = coins + 100,
                  rank_points = rank_points + 25,
                  updated_at = now()
            WHERE id = $1`,
          [p.userId],
        );
      } else {
        await client.query(
          `UPDATE users
              SET losses = losses + 1,
                  rank_points = GREATEST(0, rank_points - 10),
                  updated_at = now()
            WHERE id = $1`,
          [p.userId],
        );
      }
    }
  });
}

module.exports = { findByAuth, findById, createUser, upsertGuest, applyMatchResult };
