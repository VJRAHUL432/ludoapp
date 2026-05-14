'use strict';

const users = require('../db/users');
const { sign } = require('./jwt');
const google = require('./google');
const logger = require('../config/logger');

function pickGuestName() {
  const n = Math.floor(1000 + Math.random() * 9000);
  return `Guest${n}`;
}

async function loginGuest({ deviceId, name }) {
  if (!deviceId) {
    const err = new Error('deviceId is required');
    err.status = 400;
    throw err;
  }
  const user = await users.upsertGuest({
    deviceId,
    name: name || pickGuestName(),
  });
  const token = sign({ sub: String(user.id), kind: 'guest' });
  return { token, user };
}

async function loginGoogle({ idToken }) {
  const profile = await google.verifyIdToken(idToken);
  let user = await users.findByAuth('google', profile.sub);
  if (!user) {
    user = await users.createUser({
      authType: 'google',
      authSubject: profile.sub,
      name: profile.name,
    });
    logger.info({ userId: user.id }, 'new google user');
  }
  const token = sign({ sub: String(user.id), kind: 'google' });
  return { token, user };
}

module.exports = { loginGuest, loginGoogle };
