package com.ludo.app.game

import kotlin.random.Random

/**
 * Pure Kotlin Ludo rules engine — no Android dependencies, fully testable.
 *
 * Implements:
 *  - Roll dice; six grants extra turn; triple-six forfeits the turn
 *  - Token can leave base only on a six
 *  - Move forward on 52-cell track, then into 5-cell home column, then to centre
 *  - Land on opponent's token (non-safe cell) → kill it (sent to base)
 *  - All four tokens finished → seat wins
 *  - Last seat standing → match ends
 *  - Auto-pass turn if rolled value can't move any token
 *  - Bot helper: pickBotToken returns the best move index given a dice value
 */
class LudoEngine(players: List<PlayerInfo>) {

    data class PlayerInfo(
        val seat: Int,
        val name: String,
        val isBot: Boolean,
    )

    enum class EventKind {
        DiceRolled, NoMove, TripleSix, TurnPassed,
        TokenOut, TokenMoved, TokenKilled, TokenFinished,
        ExtraTurn, SeatFinished, MatchFinished,
    }

    data class Event(
        val kind: EventKind,
        val seat: Int = -1,
        val token: Int = -1,
        val dice: Int = 0,
    )

    val tokens: Array<Array<TokenState>> =
        Array(Board.SEAT_COUNT) { Array(Board.TOKENS_PER_SEAT) { TokenState() } }

    val activePlayers: List<PlayerInfo>
    val seatToPlayer: Map<Int, PlayerInfo>

    var status: String = "active"
        private set
    var winnerSeat: Int? = null
        private set
    val finishedSeats = mutableListOf<Int>()

    var currentSeat: Int
        private set
    var dice: Int? = null
        private set
    var sixStreak: Int = 0
        private set
    var mustMove: Boolean = false
        private set

    private val rng = Random.Default

    init {
        require(players.size in 2..4) { "Players must be 2..4" }
        require(players.map { it.seat }.toSet().size == players.size) { "Duplicate seats" }
        activePlayers = players.sortedBy { it.seat }
        seatToPlayer = activePlayers.associateBy { it.seat }
        currentSeat = firstActiveSeat()
    }

    private fun firstActiveSeat(): Int {
        for (s in 0 until Board.SEAT_COUNT) if (isSeatActive(s)) return s
        return 0
    }

    fun isSeatActive(seat: Int): Boolean =
        seatToPlayer.containsKey(seat) && seat !in finishedSeats

    // ---------------- Public actions ----------------

    /** Roll the dice for the current seat. Returns the resulting events. */
    fun roll(): List<Event> {
        val out = mutableListOf<Event>()
        if (status != "active" || dice != null) return out

        val v = rng.nextInt(1, 7)
        dice = v
        out += Event(EventKind.DiceRolled, currentSeat, dice = v)

        if (v == 6) {
            sixStreak++
            if (sixStreak >= 3) {
                out += Event(EventKind.TripleSix, currentSeat)
                passTurn(out)
                return out
            }
        }

        if (!hasAnyMovable(currentSeat, v)) {
            out += Event(EventKind.NoMove, currentSeat, dice = v)
            passTurn(out)
        } else {
            mustMove = true
        }
        return out
    }

    /** Move the given token using the current dice value. */
    fun move(tokenIndex: Int): List<Event> {
        val out = mutableListOf<Event>()
        if (status != "active" || dice == null || !mustMove) return out
        val d = dice!!
        val seat = currentSeat
        if (!canMove(seat, tokenIndex, d)) return out

        val t = tokens[seat][tokenIndex]
        var killed = false
        var finished = false
        var landingTrack: Int? = null

        when (t.kind) {
            TokenStateKind.Home -> {
                t.kind = TokenStateKind.Track
                t.track = Board.START_CELL[seat]
                t.home = -1
                landingTrack = t.track
                out += Event(EventKind.TokenOut, seat, tokenIndex)
            }
            TokenStateKind.Track -> {
                val traveled = Board.distanceFromStart(seat, t.track)
                val homeEntryDist = Board.distanceFromStart(seat, Board.HOME_ENTRY[seat])
                val newDist = traveled + d
                if (newDist <= homeEntryDist) {
                    t.track = (t.track + d) % Board.TRACK_LENGTH
                    landingTrack = t.track
                } else {
                    val intoHome = newDist - homeEntryDist - 1
                    if (intoHome == Board.HOME_PATH_LENGTH) {
                        t.kind = TokenStateKind.Finished
                        t.track = -1; t.home = -1
                        finished = true
                    } else {
                        t.kind = TokenStateKind.HomePath
                        t.track = -1
                        t.home = intoHome
                    }
                }
            }
            TokenStateKind.HomePath -> {
                val newPos = t.home + d
                if (newPos == Board.HOME_PATH_LENGTH) {
                    t.kind = TokenStateKind.Finished
                    t.home = -1
                    finished = true
                } else {
                    t.home = newPos
                }
            }
            else -> { /* finished — should not happen, canMove guards */ }
        }

        // Kill detection: only on main track, only on non-safe cells
        if (landingTrack != null && !Board.isSafeCell(landingTrack)) {
            for (s in 0 until Board.SEAT_COUNT) {
                if (s == seat) continue
                for (j in 0 until Board.TOKENS_PER_SEAT) {
                    val ot = tokens[s][j]
                    if (ot.kind == TokenStateKind.Track && ot.track == landingTrack) {
                        ot.resetToHome()
                        killed = true
                        out += Event(EventKind.TokenKilled, s, j)
                    }
                }
            }
        }

        out += Event(EventKind.TokenMoved, seat, tokenIndex)
        if (finished) out += Event(EventKind.TokenFinished, seat, tokenIndex)

        if (allFinished(seat)) {
            finishedSeats.add(seat)
            out += Event(EventKind.SeatFinished, seat)
            if (winnerSeat == null) winnerSeat = seat
            val remaining = (0 until Board.SEAT_COUNT).count { isSeatActive(it) }
            if (remaining <= 1) {
                status = "finished"
                out += Event(EventKind.MatchFinished, winnerSeat ?: 0)
                return out
            }
        }

        val extra = d == 6 || killed || finished
        if (extra) {
            dice = null
            mustMove = false
            out += Event(EventKind.ExtraTurn, seat)
        } else {
            passTurn(out)
        }
        return out
    }

    fun canMove(seat: Int, tokenIndex: Int, d: Int): Boolean {
        val t = tokens[seat][tokenIndex]
        return when (t.kind) {
            TokenStateKind.Finished -> false
            TokenStateKind.Home -> d == 6
            TokenStateKind.Track -> {
                val traveled = Board.distanceFromStart(seat, t.track)
                val homeEntryDist = Board.distanceFromStart(seat, Board.HOME_ENTRY[seat])
                traveled + d <= homeEntryDist + Board.HOME_PATH_LENGTH
            }
            TokenStateKind.HomePath -> t.home + d <= Board.HOME_PATH_LENGTH
        }
    }

    private fun hasAnyMovable(seat: Int, d: Int): Boolean =
        (0 until Board.TOKENS_PER_SEAT).any { canMove(seat, it, d) }

    private fun allFinished(seat: Int): Boolean =
        tokens[seat].all { it.kind == TokenStateKind.Finished }

    private fun passTurn(out: MutableList<Event>) {
        dice = null
        mustMove = false
        sixStreak = 0
        currentSeat = nextActiveSeat(currentSeat)
        out += Event(EventKind.TurnPassed, currentSeat)
    }

    private fun nextActiveSeat(from: Int): Int {
        for (i in 1..Board.SEAT_COUNT) {
            val s = (from + i) % Board.SEAT_COUNT
            if (isSeatActive(s)) return s
        }
        return from
    }

    // ---------------- Bot helper ----------------

    /** Heuristic move selector: kill > finish > release-from-base > advance. */
    fun pickBotToken(seat: Int, d: Int): Int {
        var bestIdx = -1
        var bestScore = Int.MIN_VALUE
        for (i in 0 until Board.TOKENS_PER_SEAT) {
            if (!canMove(seat, i, d)) continue
            val s = scoreMove(seat, i, d)
            if (s > bestScore) { bestScore = s; bestIdx = i }
        }
        return bestIdx
    }

    private fun scoreMove(seat: Int, tokenIndex: Int, d: Int): Int {
        val t = tokens[seat][tokenIndex]
        var score = 0
        var landing: Int? = null
        when (t.kind) {
            TokenStateKind.Home -> if (d == 6) landing = Board.START_CELL[seat]
            TokenStateKind.Track -> {
                val traveled = Board.distanceFromStart(seat, t.track)
                val homeEntryDist = Board.distanceFromStart(seat, Board.HOME_ENTRY[seat])
                if (traveled + d <= homeEntryDist) landing = (t.track + d) % Board.TRACK_LENGTH
            }
            else -> {}
        }
        if (landing != null && !Board.isSafeCell(landing)) {
            for (s in 0 until Board.SEAT_COUNT) {
                if (s == seat) continue
                for (j in 0 until Board.TOKENS_PER_SEAT) {
                    if (tokens[s][j].kind == TokenStateKind.Track && tokens[s][j].track == landing) {
                        score += 200
                    }
                }
            }
        }
        if (t.kind == TokenStateKind.HomePath && t.home + d == Board.HOME_PATH_LENGTH) score += 120
        if (t.kind == TokenStateKind.Home && d == 6) score += 50
        if (landing != null && Board.isSafeCell(landing)) score += 20
        if (t.kind == TokenStateKind.Track) score += Board.distanceFromStart(seat, t.track) / 4
        if (t.kind == TokenStateKind.HomePath) score += 30 + t.home
        return score
    }
}
