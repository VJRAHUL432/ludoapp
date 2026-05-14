'use strict';

const { verify } = require('../auth/jwt');

/**
 * Express middleware: requires `Authorization: Bearer <jwt>` header.
 * Sets req.user = { id, kind }.
 */
function authRequired(req, res, next) {
  const header = req.headers.authorization || '';
  const [scheme, token] = header.split(' ');
  if (scheme !== 'Bearer' || !token) {
    return res.status(401).json({ error: 'unauthorized' });
  }
  try {
    const payload = verify(token);
    req.user = { id: Number(payload.sub), kind: payload.kind };
    return next();
  } catch (e) {
    return res.status(401).json({ error: 'invalid_token' });
  }
}

module.exports = authRequired;
