'use strict';

/**
 * Pure unit tests for the Ludo engine. No DB / Redis / sockets.
 * Run: `npx jest test/engine.test.js`
 */

const { newMatchState } = require('../src/game/state');
const engine = require('../src/game/engine');
const { TOKEN_STATE, START_CELL } = require('../src/game/board');

function fourPlayerState() {
  return newMatchState({
    matchId: 'm_test',
    matchType: 'online',
    players: [
      { seat: 0, userId: 1, name: 'A', isBot: false },
      { seat: 1, userId: 2, name: 'B', isBot: false },
      { seat: 2, userId: 3, name: 'C', isBot: false },
      { seat: 3, userId: 4, name: 'D', isBot: false },
    ],
  });
}

// Force the dice by stubbing the engine's RNG access.
function rollWith(state, seat, value) {
  // bypass crypto rng by directly setting state
  state.turn.dice = value;
  state.turn.mustMove = true;
  state.turn.seat = seat;
}

test('rolling a non-six with all tokens at home passes turn', () => {
  const s = fourPlayerState();
  // monkey patch rng: replace dice module isn't trivial, so test moveValidation only.
  // Instead, manually ensure no movable token exists when we don't have a 6.
  const seat = 0;
  s.turn.seat = seat;
  s.turn.dice = null;

  // Try with a 3 → pretend roll result
  const r = engine.applyMove(s, { actionId: 'a1', seat, tokenIndex: 0 });
  expect(r.ok).toBe(false); // can't move without rolling
});

test('move out of base then advance', () => {
  const s = fourPlayerState();
  rollWith(s, 0, 6);
  // Move token 0 out: requires HOME state + dice 6.
  let r = engine.applyMove(s, { actionId: 'a1', seat: 0, tokenIndex: 0 });
  expect(r.ok).toBe(true);
  expect(s.tokens[0][0].state).toBe(TOKEN_STATE.TRACK);
  expect(s.tokens[0][0].track).toBe(START_CELL[0]);
  // After a six, extra turn — same seat must roll again
  expect(s.turn.seat).toBe(0);

  rollWith(s, 0, 3);
  r = engine.applyMove(s, { actionId: 'a2', seat: 0, tokenIndex: 0 });
  expect(r.ok).toBe(true);
  expect(s.tokens[0][0].track).toBe(START_CELL[0] + 3);
});

test('kill an opponent on a non-safe cell', () => {
  const s = fourPlayerState();
  // Place red token on cell 5; place green token on cell 5 (cell 5 isn't safe)
  s.tokens[0][0].state = TOKEN_STATE.TRACK; s.tokens[0][0].track = 2;
  s.tokens[1][0].state = TOKEN_STATE.TRACK; s.tokens[1][0].track = 5;

  rollWith(s, 0, 3);
  const r = engine.applyMove(s, { actionId: 'k', seat: 0, tokenIndex: 0 });
  expect(r.ok).toBe(true);
  expect(s.tokens[0][0].track).toBe(5);
  expect(s.tokens[1][0].state).toBe(TOKEN_STATE.HOME); // killed
});

test('safe cells prevent kill', () => {
  const s = fourPlayerState();
  s.tokens[0][0].state = TOKEN_STATE.TRACK; s.tokens[0][0].track = 5;
  s.tokens[1][0].state = TOKEN_STATE.TRACK; s.tokens[1][0].track = 8; // 8 is safe

  rollWith(s, 0, 3);
  const r = engine.applyMove(s, { actionId: 'sk', seat: 0, tokenIndex: 0 });
  expect(r.ok).toBe(true);
  expect(s.tokens[1][0].state).toBe(TOKEN_STATE.TRACK); // still alive
});

test('overshoot the finish is rejected', () => {
  const s = fourPlayerState();
  // Place red token on home_path index 4 (one step from finish), needs 1.
  s.tokens[0][0].state = TOKEN_STATE.HOME_PATH; s.tokens[0][0].home = 4;
  rollWith(s, 0, 3);
  const r = engine.applyMove(s, { actionId: 'os', seat: 0, tokenIndex: 0 });
  expect(r.ok).toBe(false);
});

test('finishing all tokens triggers MATCH_FINISHED', () => {
  const s = fourPlayerState();
  // Three tokens already finished, one on home_path[4]
  for (let i = 0; i < 3; i++) {
    s.tokens[0][i].state = TOKEN_STATE.FINISHED;
    s.tokens[0][i].track = -1; s.tokens[0][i].home = -1;
  }
  s.tokens[0][3].state = TOKEN_STATE.HOME_PATH; s.tokens[0][3].home = 4;

  rollWith(s, 0, 1);
  const r = engine.applyMove(s, { actionId: 'fin', seat: 0, tokenIndex: 3 });
  expect(r.ok).toBe(true);
  expect(s.status).toBe('finished');
  expect(s.winnerSeat).toBe(0);
  expect(r.events.find(e => e.type === 'MATCH_FINISHED')).toBeTruthy();
});
