'use strict';

const crypto = require('crypto');

/**
 * Cryptographically-strong dice.
 * Server is the only authority — clients never roll.
 */
function rollDie() {
  // crypto.randomInt is uniform over [min, max).
  return crypto.randomInt(1, 7);
}

module.exports = { rollDie };
