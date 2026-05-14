'use strict';

const logger = require('../config/logger');
const { ClientEvents, ServerEvents } = require('./protocol');
const engine = require('../game/engine');
const store = require('../game/store');
const matchmaking = require('../matchmaking/service');
const matchesDb = require('../db/matches');
const usersDb = require('../db/users');
const config = require('../config/env');
const { scheduleTurnTimer, clearTurnTimer } = require('./turnTimer');

const room = (matchId) => `match:${matchId}`;

// --------- helpers shared by handlers ---------

function emitError(socket, code) {
  socket.emit(ServerEvents.GAME_ERROR, { code });
}

function sendState(io, state) {
  io.to(room(state.matchId)).emit(ServerEvents.GAME_STATE, engine.publicView(state));
}

function broadcastEvents(io, matchId, events) {
  if (!events?.length) return;
  for (const e of events) {
    io.to(room(matchId)).emit(ServerEvents.GAME_EVENT, e);
  }
}

async function persistFinishedMatch(state) {
  try {
    const winnerSeat = state.winnerSeat;
    const winner = state.players.find((p) => p.seat === winnerSeat);
    await usersDb.applyMatchResult({
      winnerId: winner?.userId || null,
      players: state.players.map((p) => ({
        userId: p.userId,
        isWinner: p.seat === winnerSeat,
      })),
    });
  } catch (err) {
    logger.error({ err, matchId: state.matchId }, 'failed to apply match result');
  }
}

function setupTurnTimerFor(io, matchId) {
  scheduleTurnTimer(matchId, async () => {
    try {
      await store.withMatchLock(matchId, async () => {
        const state = await store.loadMatch(matchId);
        if (!state || state.status !== 'active') return;
        const seat = state.turn.seat;
        const result = engine.applyTimeout(state, { seat });
        if (result.ok) {
          await store.saveMatch(state);
          broadcastEvents(io, matchId, result.events);
          if (state.status === 'finished') {
            await persistFinishedMatch(state);
            clearTurnTimer(matchId);
          } else {
            setupTurnTimerFor(io, matchId);
          }
        }
      });
    } catch (err) {
      logger.error({ err, matchId }, 'turn timer fn failed');
    }
  });
}

// --------- registered on each connected socket ---------

function register(io, socket) {
  const user = socket.data.user;
  logger.info({ userId: user.id, sid: socket.id }, 'socket connected');

  // Persist session for reconnect lookups
  store.saveSession(user.id, { socketId: socket.id, lastSeen: Date.now() })
    .catch((err) => logger.error({ err }, 'session save failed'));

  // ---- ping/pong heartbeat ----
  socket.on(ClientEvents.PING, (msg = {}) => {
    socket.emit(ServerEvents.PONG, { t: msg.t, server: Date.now() });
  });

  // ---- matchmaking ----
  socket.on(ClientEvents.MM_PRIVATE_CREATE, async (msg = {}, ack) => {
    try {
      const playerCount = Number(msg.playerCount) || 4;
      const { matchId, code, state } = await matchmaking.createPrivateRoom({
        host: user, playerCount,
      });
      socket.join(room(matchId));
      socket.data.matchId = matchId;
      socket.emit(ServerEvents.MM_PRIVATE_CREATED, {
        matchId, code, state: engine.publicView(state),
      });
      if (typeof ack === 'function') ack({ ok: true, matchId, code });
    } catch (err) {
      logger.warn({ err: err.message }, 'private create failed');
      if (typeof ack === 'function') ack({ ok: false, code: err.message });
    }
  });

  socket.on(ClientEvents.MM_PRIVATE_JOIN, async (msg = {}, ack) => {
    try {
      const code = String(msg.code || '').toUpperCase();
      const matchId = await matchmaking.joinPrivateRoom({ code, user });
      const state = await store.loadMatch(matchId);
      if (!state) throw Object.assign(new Error('match_not_found'), { status: 404 });

      // add player into next free seat if not already in
      const already = state.players.some((p) => p.userId === user.id);
      if (!already) {
        if (state.players.length >= (state.expectedPlayers || 4)) {
          throw Object.assign(new Error('room_full'), { status: 409 });
        }
        const nextSeat = state.players.length;
        state.players.push({
          seat: nextSeat, userId: user.id, name: user.name, isBot: false,
          connected: true, missedTurns: 0,
        });
        // Start the match when room is full
        if (state.players.length === (state.expectedPlayers || state.players.length)) {
          state.status = 'active';
          state.turn.rollExpiresAt = Date.now() + config.game.turnTimeoutMs;
          setupTurnTimerFor(io, matchId);
        }
        await store.saveMatch(state);
      }
      socket.join(room(matchId));
      socket.data.matchId = matchId;
      socket.emit(ServerEvents.MM_PRIVATE_JOINED, {
        matchId, state: engine.publicView(state),
      });
      sendState(io, state);
      if (typeof ack === 'function') ack({ ok: true, matchId });
    } catch (err) {
      if (typeof ack === 'function') ack({ ok: false, code: err.message });
    }
  });

  socket.on(ClientEvents.MM_ONLINE_JOIN, async (msg = {}, ack) => {
    try {
      const playerCount = Number(msg.playerCount) || 4;
      const formed = await matchmaking.joinOnlineQueue({ user, playerCount });
      socket.emit(ServerEvents.MM_QUEUED, { playerCount });
      if (formed) {
        const { matchId, state } = formed;
        // Move all queued players into the match room. They're identified by user id;
        // we look them up via session store and emit found.
        for (const p of state.players) {
          const sess = await store.loadSession(p.userId);
          if (sess?.socketId) {
            const targetSocket = io.sockets.sockets.get(sess.socketId);
            if (targetSocket) {
              targetSocket.join(room(matchId));
              targetSocket.data.matchId = matchId;
              targetSocket.emit(ServerEvents.MM_MATCH_FOUND, {
                matchId, state: engine.publicView(state),
              });
            }
          }
        }
        setupTurnTimerFor(io, matchId);
      }
      if (typeof ack === 'function') ack({ ok: true });
    } catch (err) {
      if (typeof ack === 'function') ack({ ok: false, code: err.message });
    }
  });

  socket.on(ClientEvents.MM_BOT_START, async (msg = {}, ack) => {
    try {
      const { matchId, state } = await matchmaking.createBotMatch({
        user, difficulty: msg.difficulty || 'medium',
      });
      socket.join(room(matchId));
      socket.data.matchId = matchId;
      sendState(io, state);
      setupTurnTimerFor(io, matchId);
      if (typeof ack === 'function') ack({ ok: true, matchId });
    } catch (err) {
      if (typeof ack === 'function') ack({ ok: false, code: err.message });
    }
  });

  // ---- in-game ----

  socket.on(ClientEvents.GAME_RESUME, async (msg = {}, ack) => {
    try {
      const matchId = String(msg.matchId || '');
      const state = await store.loadMatch(matchId);
      if (!state) {
        if (typeof ack === 'function') ack({ ok: false, code: 'match_not_found' });
        return;
      }
      const player = state.players.find((p) => p.userId === user.id);
      if (!player) {
        if (typeof ack === 'function') ack({ ok: false, code: 'not_in_match' });
        return;
      }
      player.connected = true;
      socket.join(room(matchId));
      socket.data.matchId = matchId;
      await store.saveMatch(state);
      socket.emit(ServerEvents.RECONNECT_OK);
      socket.emit(ServerEvents.GAME_STATE, engine.publicView(state));
      if (typeof ack === 'function') ack({ ok: true });
    } catch (err) {
      if (typeof ack === 'function') ack({ ok: false, code: err.message });
    }
  });

  socket.on(ClientEvents.GAME_ROLL, async (msg = {}, ack) => {
    const matchId = String(msg.matchId || socket.data.matchId || '');
    if (!matchId) return emitError(socket, 'no_match');
    try {
      await store.withMatchLock(matchId, async () => {
        const state = await store.loadMatch(matchId);
        if (!state) return emitError(socket, 'match_not_found');
        const player = state.players.find((p) => p.userId === user.id);
        if (!player) return emitError(socket, 'not_in_match');
        const result = engine.applyRoll(state, {
          actionId: msg.actionId, seat: player.seat,
        });
        if (!result.ok) return emitError(socket, result.code);
        await store.saveMatch(state);
        broadcastEvents(io, matchId, result.events);
        if (state.status === 'finished') {
          await persistFinishedMatch(state);
          clearTurnTimer(matchId);
        } else {
          setupTurnTimerFor(io, matchId);
        }
        if (typeof ack === 'function') ack({ ok: true });
      });
    } catch (err) {
      logger.error({ err, matchId }, 'roll failed');
      emitError(socket, 'internal');
    }
  });

  socket.on(ClientEvents.GAME_MOVE, async (msg = {}, ack) => {
    const matchId = String(msg.matchId || socket.data.matchId || '');
    if (!matchId) return emitError(socket, 'no_match');
    try {
      await store.withMatchLock(matchId, async () => {
        const state = await store.loadMatch(matchId);
        if (!state) return emitError(socket, 'match_not_found');
        const player = state.players.find((p) => p.userId === user.id);
        if (!player) return emitError(socket, 'not_in_match');
        const tokenIndex = Number(msg.tokenIndex);
        if (!Number.isInteger(tokenIndex) || tokenIndex < 0 || tokenIndex > 3) {
          return emitError(socket, 'invalid_token_index');
        }
        const result = engine.applyMove(state, {
          actionId: msg.actionId, seat: player.seat, tokenIndex,
        });
        if (!result.ok) return emitError(socket, result.code);
        await store.saveMatch(state);
        broadcastEvents(io, matchId, result.events);
        if (state.status === 'finished') {
          await persistFinishedMatch(state);
          clearTurnTimer(matchId);
        } else {
          setupTurnTimerFor(io, matchId);
        }
        if (typeof ack === 'function') ack({ ok: true });
      });
    } catch (err) {
      logger.error({ err, matchId }, 'move failed');
      emitError(socket, 'internal');
    }
  });

  socket.on(ClientEvents.GAME_LEAVE, async (msg = {}) => {
    const matchId = String(msg.matchId || socket.data.matchId || '');
    if (!matchId) return;
    socket.leave(room(matchId));
    socket.data.matchId = null;
  });

  // ---- disconnect ----
  socket.on('disconnect', async (reason) => {
    logger.info({ userId: user.id, reason }, 'socket disconnected');
    try {
      const matchId = socket.data.matchId;
      if (matchId) {
        const state = await store.loadMatch(matchId);
        if (state) {
          const p = state.players.find((pp) => pp.userId === user.id);
          if (p) p.connected = false;
          await store.saveMatch(state);
        }
      }
    } catch (err) {
      logger.error({ err }, 'disconnect cleanup failed');
    }
  });
}

module.exports = { register };
