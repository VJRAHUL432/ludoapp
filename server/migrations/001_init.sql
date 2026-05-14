-- Ludo App initial schema
-- Idempotent: safe to run multiple times

CREATE TABLE IF NOT EXISTS users (
    id           BIGSERIAL PRIMARY KEY,
    auth_type    VARCHAR(16)  NOT NULL CHECK (auth_type IN ('guest', 'google')),
    auth_subject VARCHAR(128) NOT NULL,           -- google sub OR guest device id
    name         VARCHAR(40)  NOT NULL,
    avatar       VARCHAR(64)  NOT NULL DEFAULT 'avatar_01',
    coins        INTEGER      NOT NULL DEFAULT 500,
    wins         INTEGER      NOT NULL DEFAULT 0,
    losses       INTEGER      NOT NULL DEFAULT 0,
    rank_points  INTEGER      NOT NULL DEFAULT 1000,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    UNIQUE (auth_type, auth_subject)
);

CREATE INDEX IF NOT EXISTS idx_users_rank ON users (rank_points DESC);

CREATE TABLE IF NOT EXISTS matches (
    id           BIGSERIAL PRIMARY KEY,
    room_code    VARCHAR(8),
    match_type   VARCHAR(16)  NOT NULL CHECK (match_type IN ('online','private','local','bot')),
    status       VARCHAR(16)  NOT NULL DEFAULT 'pending'
                 CHECK (status IN ('pending','active','finished','aborted')),
    winner_id    BIGINT REFERENCES users(id) ON DELETE SET NULL,
    player_count SMALLINT     NOT NULL CHECK (player_count BETWEEN 2 AND 4),
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    finished_at  TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_matches_status     ON matches (status);
CREATE INDEX IF NOT EXISTS idx_matches_room_code  ON matches (room_code);

CREATE TABLE IF NOT EXISTS match_players (
    id        BIGSERIAL PRIMARY KEY,
    match_id  BIGINT NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    user_id   BIGINT REFERENCES users(id) ON DELETE SET NULL,
    seat      SMALLINT NOT NULL CHECK (seat BETWEEN 0 AND 3), -- 0=red 1=green 2=yellow 3=blue
    score     INTEGER  NOT NULL DEFAULT 0,
    is_bot    BOOLEAN  NOT NULL DEFAULT FALSE,
    UNIQUE (match_id, seat)
);

CREATE INDEX IF NOT EXISTS idx_match_players_match ON match_players (match_id);
CREATE INDEX IF NOT EXISTS idx_match_players_user  ON match_players (user_id);
