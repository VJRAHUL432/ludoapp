'use strict';

const { OAuth2Client } = require('google-auth-library');
const config = require('../config/env');

let client = null;
function getClient() {
  if (!client) client = new OAuth2Client(config.google.clientId);
  return client;
}

/**
 * Verifies a Google ID token issued for our Android client.
 * Returns { sub, name, picture, email } or throws.
 */
async function verifyIdToken(idToken) {
  if (!config.google.clientId) {
    const err = new Error('Google sign-in not configured');
    err.status = 503;
    throw err;
  }
  const ticket = await getClient().verifyIdToken({
    idToken,
    audience: config.google.clientId,
  });
  const payload = ticket.getPayload();
  if (!payload || !payload.sub) {
    const err = new Error('Invalid Google token');
    err.status = 401;
    throw err;
  }
  return {
    sub: payload.sub,
    name: payload.name || 'Player',
    picture: payload.picture || null,
    email: payload.email || null,
  };
}

module.exports = { verifyIdToken };
