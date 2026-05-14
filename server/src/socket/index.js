'use strict';

const { Server } = require('socket.io');
const logger = require('../config/logger');
const config = require('../config/env');
const { socketAuth } = require('./auth');
const handlers = require('./handlers');

function attachSocketServer(httpServer) {
  const io = new Server(httpServer, {
    // Server-side ping (for transport-level keepalive). We also have an
    // app-level ping/pong for measuring latency from the client.
    pingInterval: config.game.heartbeatIntervalMs,
    pingTimeout:  config.game.heartbeatIntervalMs * config.game.maxMissedHeartbeats,
    cors: { origin: true, methods: ['GET', 'POST'] },
    // small payload limit to deter abuse
    maxHttpBufferSize: 32 * 1024,
  });

  io.use(socketAuth);
  io.on('connection', (socket) => handlers.register(io, socket));

  io.engine.on('connection_error', (err) => {
    logger.warn({ err: err.message, code: err.code }, 'socket connection error');
  });

  return io;
}

module.exports = { attachSocketServer };
