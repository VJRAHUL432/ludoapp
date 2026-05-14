'use strict';

const { verify } = require('../auth/jwt');
const users = require('../db/users');

/**
 * Socket.IO middleware: authenticates using `auth.token` JWT.
 * Sets socket.data.user = { id, name, avatar }.
 */
async function socketAuth(socket, next) {
  try {
    const token = socket.handshake?.auth?.token;
    if (!token) return next(new Error('unauthorized'));
    const payload = verify(token);
    const user = await users.findById(Number(payload.sub));
    if (!user) return next(new Error('user_not_found'));
    socket.data.user = {
      id: user.id,
      name: user.name,
      avatar: user.avatar,
    };
    return next();
  } catch (err) {
    return next(new Error('unauthorized'));
  }
}

module.exports = { socketAuth };
