'use strict';

const jwt = require('jsonwebtoken');
const config = require('../config/env');

function sign(payload) {
  return jwt.sign(payload, config.jwt.secret, {
    expiresIn: config.jwt.expiresIn,
    algorithm: 'HS256',
  });
}

function verify(token) {
  return jwt.verify(token, config.jwt.secret, { algorithms: ['HS256'] });
}

module.exports = { sign, verify };
