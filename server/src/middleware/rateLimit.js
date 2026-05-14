'use strict';

const rateLimit = require('express-rate-limit');

const apiLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 120,                 // 120 req/min/ip
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'rate_limited' },
});

const authLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 20,                  // 20 auth attempts/min/ip
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'rate_limited' },
});

module.exports = { apiLimiter, authLimiter };
