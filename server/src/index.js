'use strict';

const http = require('http');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const compression = require('compression');

const config = require('./config/env');
const logger = require('./config/logger');
const { initDb, closeDb } = require('./db/pool');
const { initRedis, closeRedis } = require('./cache/redis');
const { errorHandler, notFoundHandler } = require('./middleware/errorHandler');
const { apiLimiter } = require('./middleware/rateLimit');
const apiRoutes = require('./routes');
const { attachSocketServer } = require('./socket');

async function bootstrap() {
  await initDb();
  await initRedis();

  const app = express();
  app.disable('x-powered-by');
  app.set('trust proxy', 1);

  app.use(helmet());
  app.use(cors({ origin: true, credentials: false }));
  app.use(compression());
  app.use(express.json({ limit: '64kb' }));
  app.use(apiLimiter);

  app.get('/health', (req, res) => res.json({ ok: true, time: Date.now() }));
  app.use('/api', apiRoutes);

  app.use(notFoundHandler);
  app.use(errorHandler);

  const server = http.createServer(app);
  attachSocketServer(server);

  server.listen(config.port, () => {
    logger.info({ port: config.port, env: config.env }, 'ludo-server up');
  });

  // Graceful shutdown
  const shutdown = async (signal) => {
    logger.warn({ signal }, 'shutting down');
    server.close(() => logger.info('http closed'));
    try {
      await closeDb();
      await closeRedis();
    } catch (err) {
      logger.error({ err }, 'error during shutdown');
    } finally {
      process.exit(0);
    }
  };
  process.on('SIGINT', () => shutdown('SIGINT'));
  process.on('SIGTERM', () => shutdown('SIGTERM'));

  // Crash safety: never let unhandled errors take the process down silently
  process.on('uncaughtException', (err) => {
    logger.fatal({ err }, 'uncaughtException');
  });
  process.on('unhandledRejection', (reason) => {
    logger.error({ reason }, 'unhandledRejection');
  });
}

bootstrap().catch((err) => {
  logger.fatal({ err }, 'failed to bootstrap');
  process.exit(1);
});
