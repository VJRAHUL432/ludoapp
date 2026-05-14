'use strict';

/**
 * Ludo board topology (Ludo-King-style).
 *
 *   - 4 seats: 0=red, 1=green, 2=yellow, 3=blue.
 *   - Each seat has 4 tokens.
 *   - Main track is 52 cells, indexed 0..51, shared by all players.
 *   - Each seat enters the track at a specific cell (START_CELL).
 *   - Each seat exits the track to its home column at HOME_ENTRY cell.
 *   - Home column is 5 cells (0..4) + center finish.
 *
 * Token states:
 *   HOME       : in base, needs a 6 to come out
 *   TRACK      : moving on the 52-cell loop, field `track` = 0..51
 *   HOME_PATH  : in private home column, field `home` = 0..4
 *   FINISHED   : reached center
 */

const SEAT_COUNT = 4;
const TOKENS_PER_SEAT = 4;
const TRACK_LENGTH = 52;
const HOME_PATH_LENGTH = 5;

// Where each seat puts its first token when it leaves base
const START_CELL = [0, 13, 26, 39];

// Cell from which the seat exits the main track and steps onto its home column.
// (Token is on this cell, then next move enters home_path[0].)
const HOME_ENTRY = [51, 12, 25, 38];

// Safe cells (yellow stars + each seat's start). Tokens on these cannot be killed.
// Standard Ludo King safe set:
const SAFE_CELLS = new Set([0, 8, 13, 21, 26, 34, 39, 47]);

const TOKEN_STATE = Object.freeze({
  HOME: 'HOME',
  TRACK: 'TRACK',
  HOME_PATH: 'HOME_PATH',
  FINISHED: 'FINISHED',
});

function isSafeCell(trackIndex) {
  return SAFE_CELLS.has(trackIndex);
}

/**
 * Number of steps from the seat's start cell to the given track index,
 * advancing forward (clockwise). 0..51.
 */
function distanceFromStart(seat, trackIndex) {
  const start = START_CELL[seat];
  return (trackIndex - start + TRACK_LENGTH) % TRACK_LENGTH;
}

module.exports = {
  SEAT_COUNT,
  TOKENS_PER_SEAT,
  TRACK_LENGTH,
  HOME_PATH_LENGTH,
  START_CELL,
  HOME_ENTRY,
  SAFE_CELLS,
  TOKEN_STATE,
  isSafeCell,
  distanceFromStart,
};
