'use strict';

const pino = require('pino');
const config = require('./env');

const logger = pino({
  level: process.env.LOG_LEVEL || (config.env === 'production' ? 'info' : 'debug'),
  base: { service: 'ludo-server' },
  redact: {
    paths: ['req.headers.authorization', '*.password', '*.token'],
    censor: '[REDACTED]',
  },
  transport:
    config.env === 'development'
      ? { target: 'pino-pretty', options: { colorize: true, singleLine: false } }
      : undefined,
});

module.exports = logger;
