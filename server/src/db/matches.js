'use strict';

const { query, withTransaction } = require('./pool');

async function createMatch({ matchType, roomCode = null, players }) {
  return withTransaction(async (client) => {
    const m = await client.query(
      `INSERT INTO matches (room_code, match_type, status, player_count)
       VALUES ($1, $2, 'active', $3)
       RETURNING id, room_code, match_type, status, player_count, created_at`,
      [roomCode, matchType, players.length],
    );
    const matchId = m.rows[0].id;
    for (const p of players) {
      await client.query(
        `INSERT INTO match_players (match_id, user_id, seat, is_bot)
         VALUES ($1, $2, $3, $4)`,
        [matchId, p.userId || null, p.seat, !!p.isBot],
      );
    }
    return m.rows[0];
  });
}

async function finishMatch({ matchId, winnerUserId }) {
  await query(
    `UPDATE matches
        SET status = 'finished',
            winner_id = $2,
            finished_at = now()
      WHERE id = $1`,
    [matchId, winnerUserId || null],
  );
}

async function abortMatch({ matchId }) {
  await query(
    `UPDATE matches SET status='aborted', finished_at=now() WHERE id=$1`,
    [matchId],
  );
}

async function leaderboardTop(limit = 100) {
  const { rows } = await query(
    `SELECT id, name, avatar, wins, losses, rank_points
       FROM users ORDER BY rank_points DESC LIMIT $1`,
    [limit],
  );
  return rows;
}

module.exports = { createMatch, finishMatch, abortMatch, leaderboardTop };
