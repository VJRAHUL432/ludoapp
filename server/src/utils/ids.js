'use strict';

const crypto = require('crypto');
const { v4: uuid } = require('uuid');

// avoid ambiguous chars (0/O, 1/I)
const ROOM_ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';

function makeRoomCode(len = 5) {
  const buf = crypto.randomBytes(len);
  let out = '';
  for (let i = 0; i < len; i++) {
    out += ROOM_ALPHABET[buf[i] % ROOM_ALPHABET.length];
  }
  return out;
}

function makeMatchId() {
  return `m_${uuid()}`;
}

function makeActionId() {
  return uuid();
}

module.exports = { makeRoomCode, makeMatchId, makeActionId };
