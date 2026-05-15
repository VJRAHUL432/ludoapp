package com.ludo.app.game

/**
 * Hardcoded 15x15 Ludo board geometry.
 *
 *  - 4 seats: 0=red (top-left), 1=green (top-right), 2=yellow (bottom-right), 3=blue (bottom-left)
 *  - 52-cell main track, indexed 0..51, shared by all players (clockwise from Red's start)
 *  - Each seat has a 5-cell home column leading to the centre finish
 *  - 8 safe cells (start cells + 4 stars) where tokens cannot be killed
 *
 * Coordinates are (col, row) on a 15x15 grid, origin TOP-LEFT.
 */
object Board {
    const val GRID_SIZE = 15
    const val TRACK_LENGTH = 52
    const val HOME_PATH_LENGTH = 5
    const val SEAT_COUNT = 4
    const val TOKENS_PER_SEAT = 4

    /** Each seat's start cell on the 52-cell loop. */
    val START_CELL = intArrayOf(0, 13, 26, 39)

    /** The last main-track cell before each seat's home column. */
    val HOME_ENTRY = intArrayOf(51, 12, 25, 38)

    /** Safe cells: each seat's start cell plus four "star" cells. */
    val SAFE_CELLS = setOf(0, 8, 13, 21, 26, 34, 39, 47)

    /** 52 main-track cells, clockwise from Red's start at (1,6). */
    val TRACK = arrayOf(
        Pair(1, 6), Pair(2, 6), Pair(3, 6), Pair(4, 6), Pair(5, 6),    // 0..4
        Pair(6, 5), Pair(6, 4), Pair(6, 3), Pair(6, 2), Pair(6, 1), Pair(6, 0), // 5..10
        Pair(7, 0), Pair(8, 0),                                         // 11..12
        Pair(8, 1), Pair(8, 2), Pair(8, 3), Pair(8, 4), Pair(8, 5),    // 13..17
        Pair(9, 6), Pair(10, 6), Pair(11, 6), Pair(12, 6), Pair(13, 6), Pair(14, 6), // 18..23
        Pair(14, 7), Pair(14, 8),                                       // 24..25
        Pair(13, 8), Pair(12, 8), Pair(11, 8), Pair(10, 8), Pair(9, 8),// 26..30
        Pair(8, 9), Pair(8, 10), Pair(8, 11), Pair(8, 12), Pair(8, 13), Pair(8, 14), // 31..36
        Pair(7, 14), Pair(6, 14),                                       // 37..38
        Pair(6, 13), Pair(6, 12), Pair(6, 11), Pair(6, 10), Pair(6, 9),// 39..43
        Pair(5, 8), Pair(4, 8), Pair(3, 8), Pair(2, 8), Pair(1, 8), Pair(0, 8), // 44..49
        Pair(0, 7), Pair(0, 6),                                         // 50..51
    )

    /** Home columns (5 cells each), index 0 = first cell after entering, index 4 = adjacent to centre. */
    val HOME_COLUMN = arrayOf(
        // Red — across row 7 from left
        arrayOf(Pair(1,7), Pair(2,7), Pair(3,7), Pair(4,7), Pair(5,7)),
        // Green — down column 7 from top
        arrayOf(Pair(7,1), Pair(7,2), Pair(7,3), Pair(7,4), Pair(7,5)),
        // Yellow — across row 7 from right
        arrayOf(Pair(13,7), Pair(12,7), Pair(11,7), Pair(10,7), Pair(9,7)),
        // Blue — up column 7 from bottom
        arrayOf(Pair(7,13), Pair(7,12), Pair(7,11), Pair(7,10), Pair(7,9)),
    )

    /** 4 base slots per seat (where tokens sit when in HOME). */
    val BASE_SLOTS = arrayOf(
        // Red — top-left corner
        arrayOf(Pair(1.5f, 1.5f), Pair(3.5f, 1.5f), Pair(1.5f, 3.5f), Pair(3.5f, 3.5f)),
        // Green — top-right corner
        arrayOf(Pair(10.5f, 1.5f), Pair(12.5f, 1.5f), Pair(10.5f, 3.5f), Pair(12.5f, 3.5f)),
        // Yellow — bottom-right corner
        arrayOf(Pair(10.5f, 10.5f), Pair(12.5f, 10.5f), Pair(10.5f, 12.5f), Pair(12.5f, 12.5f)),
        // Blue — bottom-left corner
        arrayOf(Pair(1.5f, 10.5f), Pair(3.5f, 10.5f), Pair(1.5f, 12.5f), Pair(3.5f, 12.5f)),
    )

    fun isSafeCell(trackIndex: Int): Boolean = trackIndex in SAFE_CELLS

    /** 0..51 — distance walked along the loop from a seat's start to a track index. */
    fun distanceFromStart(seat: Int, trackIndex: Int): Int =
        ((trackIndex - START_CELL[seat]) % TRACK_LENGTH + TRACK_LENGTH) % TRACK_LENGTH
}

enum class TokenStateKind { Home, Track, HomePath, Finished }

class TokenState(
    var kind: TokenStateKind = TokenStateKind.Home,
    var track: Int = -1,
    var home: Int = -1,
) {
    fun resetToHome() { kind = TokenStateKind.Home; track = -1; home = -1 }
}
