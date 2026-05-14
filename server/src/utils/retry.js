'use strict';

/**
 * Retry an async function with exponential backoff + jitter.
 */
async function retry(fn, { attempts = 3, baseMs = 100, maxMs = 2000 } = {}) {
  let lastErr;
  for (let i = 0; i < attempts; i++) {
    try {
      return await fn();
    } catch (err) {
      lastErr = err;
      if (i === attempts - 1) break;
      const wait = Math.min(maxMs, baseMs * 2 ** i) * (0.5 + Math.random());
      await new Promise((r) => setTimeout(r, wait));
    }
  }
  throw lastErr;
}

module.exports = { retry };
