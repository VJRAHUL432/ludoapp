package com.ludo.app.ui

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ludo.app.game.Board
import com.ludo.app.game.LudoEngine
import com.ludo.app.game.TokenStateKind
import kotlinx.coroutines.delay

private val SeatColors = listOf(
    Color(0xFFEB3338),  // Red
    Color(0xFF33B84C),  // Green
    Color(0xFFF5C729),  // Yellow
    Color(0xFF2E72EA),  // Blue
)

@Composable
fun GameScreen(
    mode: GameMode,
    onMatchEnded: (winnerSeat: Int, players: List<LudoEngine.PlayerInfo>) -> Unit,
    onLeaveToMenu: () -> Unit,
) {
    val players = remember(mode) { playersFor(mode) }
    val engine = remember(mode) { LudoEngine(players) }
    var refresh by remember { mutableIntStateOf(0) }
    var diceVisual by remember { mutableStateOf<Int?>(null) }
    var busy by remember { mutableStateOf(false) }

    fun bump() { refresh++ }

    // Bot driver — runs whenever it's a bot's turn.
    LaunchedEffect(refresh) {
        if (engine.status != "active") return@LaunchedEffect
        val current = engine.seatToPlayer[engine.currentSeat] ?: return@LaunchedEffect
        if (!current.isBot) return@LaunchedEffect
        if (busy) return@LaunchedEffect
        busy = true
        delay(600)
        // Spin the dice visually
        repeat(6) {
            diceVisual = (1..6).random()
            delay(50)
        }
        engine.roll()
        diceVisual = engine.dice
        bump()
        if (engine.status == "finished") { busy = false; onMatchEnded(engine.winnerSeat ?: 0, players); return@LaunchedEffect }
        if (engine.mustMove && engine.dice != null) {
            delay(550)
            val pick = engine.pickBotToken(engine.currentSeat, engine.dice!!)
            if (pick >= 0) { engine.move(pick); bump() }
        }
        diceVisual = engine.dice
        busy = false
        if (engine.status == "finished") onMatchEnded(engine.winnerSeat ?: 0, players)
    }

    Column(
        modifier = Modifier.fillMaxSize().background(Color(0xFF101218)),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        // Top status bar
        Row(
            modifier = Modifier.fillMaxWidth().height(80.dp).background(Color(0x40000000)),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween,
        ) {
            Button(
                onClick = onLeaveToMenu,
                modifier = Modifier.padding(start = 12.dp).height(48.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF2A2E3A)),
            ) { Text("Menu", fontSize = 14.sp) }

            val current = engine.seatToPlayer[engine.currentSeat]
            val statusText = when {
                engine.status == "finished" -> "Winner: ${seatName(engine.winnerSeat ?: 0)}"
                engine.dice == null -> "${current?.name ?: "?"} — Roll!"
                engine.mustMove -> "${current?.name ?: "?"} — Tap a token"
                else -> "${current?.name ?: "?"}"
            }
            Text(
                text = statusText,
                color = Color.White,
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(end = 12.dp),
            )
        }

        Spacer(Modifier.height(8.dp))

        // The board (square, fills width)
        Box(
            modifier = Modifier.fillMaxWidth().aspectRatio(1f).padding(8.dp),
        ) {
            BoardView(
                engine = engine,
                onTokenTap = { seat, idx ->
                    if (busy || engine.status != "active") return@BoardView
                    val current = engine.seatToPlayer[engine.currentSeat]
                    if (current == null || current.isBot) return@BoardView
                    if (engine.currentSeat != seat) return@BoardView
                    if (engine.dice == null || !engine.mustMove) return@BoardView
                    if (!engine.canMove(seat, idx, engine.dice!!)) return@BoardView
                    engine.move(idx)
                    diceVisual = engine.dice
                    bump()
                    if (engine.status == "finished") onMatchEnded(engine.winnerSeat ?: 0, players)
                },
            )
        }

        Spacer(Modifier.weight(1f))

        // Bottom HUD: dice + roll
        Row(
            modifier = Modifier.fillMaxWidth().height(140.dp).background(Color(0x60000000)),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(24.dp, Alignment.CenterHorizontally),
        ) {
            DiceView(value = diceVisual ?: engine.dice)
            val current = engine.seatToPlayer[engine.currentSeat]
            val canRoll = engine.status == "active" &&
                engine.dice == null &&
                current != null && !current.isBot && !busy
            Button(
                onClick = {
                    if (!canRoll) return@Button
                    busy = true
                    engine.roll()
                    diceVisual = engine.dice
                    bump()
                    busy = false
                    if (engine.status == "finished") onMatchEnded(engine.winnerSeat ?: 0, players)
                },
                enabled = canRoll,
                modifier = Modifier.size(width = 160.dp, height = 80.dp),
                shape = RoundedCornerShape(16.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFF2E72EA),
                    disabledContainerColor = Color(0xFF2E72EA).copy(alpha = 0.3f),
                ),
            ) {
                Text("ROLL", fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
            }
        }
    }
}

@Composable
private fun DiceView(value: Int?) {
    Box(
        modifier = Modifier
            .size(80.dp)
            .background(Color.White, RoundedCornerShape(12.dp)),
        contentAlignment = Alignment.Center,
    ) {
        Text(
            text = value?.toString() ?: "-",
            color = Color(0xFF101218),
            fontSize = 48.sp,
            fontWeight = FontWeight.ExtraBold,
        )
    }
}

@Composable
private fun BoardView(
    engine: LudoEngine,
    onTokenTap: (seat: Int, tokenIndex: Int) -> Unit,
) {
    val density = LocalDensity.current
    var sizePx by remember { mutableStateOf(Size.Zero) }
    val cellPx by remember(sizePx) { mutableStateOf(if (sizePx.width > 0f) sizePx.width / Board.GRID_SIZE else 0f) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFF1EFE6))
            .pointerInput(engine, cellPx) {
                detectTapGestures { tap ->
                    if (cellPx <= 0f) return@detectTapGestures
                    // Find which token (if any) was tapped
                    for (s in 0 until Board.SEAT_COUNT) {
                        for (i in 0 until Board.TOKENS_PER_SEAT) {
                            val (cx, cy) = tokenCell(engine, s, i) ?: continue
                            val px = cx * cellPx
                            val py = cy * cellPx
                            val r = cellPx * 0.40f
                            val dx = tap.x - px; val dy = tap.y - py
                            if (dx * dx + dy * dy <= r * r) {
                                onTokenTap(s, i)
                                return@detectTapGestures
                            }
                        }
                    }
                }
            },
    ) {
        Canvas(Modifier.fillMaxSize()) {
            sizePx = size
            val c = size.width / Board.GRID_SIZE

            // 1. Base off-white background grid + outlines
            for (gx in 0 until Board.GRID_SIZE) {
                for (gy in 0 until Board.GRID_SIZE) {
                    val cellColor = cellBackgroundColor(gx, gy)
                    drawRect(
                        color = cellColor,
                        topLeft = Offset(gx * c, gy * c),
                        size = Size(c, c),
                    )
                    drawRect(
                        color = Color(0x14000000),
                        topLeft = Offset(gx * c, gy * c),
                        size = Size(c, c),
                        style = Stroke(width = 1f),
                    )
                }
            }

            // 2. Highlight start cells with seat tint
            for (s in 0 until Board.SEAT_COUNT) {
                val (sx, sy) = Board.TRACK[Board.START_CELL[s]]
                drawRect(
                    color = SeatColors[s].copy(alpha = 0.35f),
                    topLeft = Offset(sx * c, sy * c),
                    size = Size(c, c),
                )
            }
            // Safe stars (subtle grey fill)
            for (idx in Board.SAFE_CELLS) {
                if (idx in Board.START_CELL.toList()) continue
                val (sx, sy) = Board.TRACK[idx]
                drawRect(
                    color = Color(0x22000000),
                    topLeft = Offset(sx * c, sy * c),
                    size = Size(c, c),
                )
            }

            // 3. Centre finish triangles
            drawRect(Color(0xFFE0E0E0), Offset(6 * c, 6 * c), Size(3 * c, 3 * c))
            drawRect(SeatColors[0], Offset(6 * c, 7 * c), Size(c, c))
            drawRect(SeatColors[1], Offset(7 * c, 6 * c), Size(c, c))
            drawRect(SeatColors[2], Offset(8 * c, 7 * c), Size(c, c))
            drawRect(SeatColors[3], Offset(7 * c, 8 * c), Size(c, c))
            drawRect(Color(0xFFCFCFCF), Offset(7 * c, 7 * c), Size(c, c))

            // 4. Tokens
            for (s in 0 until Board.SEAT_COUNT) {
                for (i in 0 until Board.TOKENS_PER_SEAT) {
                    val (cx, cy) = tokenCell(engine, s, i) ?: continue
                    val cxPx = cx * c
                    val cyPx = cy * c
                    val r = c * 0.40f

                    val canMove = engine.status == "active" &&
                        engine.currentSeat == s &&
                        engine.dice != null &&
                        engine.mustMove &&
                        engine.canMove(s, i, engine.dice!!) &&
                        engine.seatToPlayer[s]?.isBot == false

                    val tk = engine.tokens[s][i]
                    val faded = tk.kind == TokenStateKind.Finished
                    val color = if (faded) SeatColors[s].copy(alpha = 0.5f) else SeatColors[s]

                    // outer
                    drawCircle(color, radius = r, center = Offset(cxPx, cyPx))
                    // inner highlight
                    drawCircle(Color.White.copy(alpha = 0.7f), radius = r * 0.55f, center = Offset(cxPx, cyPx))
                    if (canMove) {
                        drawCircle(Color.White, radius = r * 1.05f, center = Offset(cxPx, cyPx), style = Stroke(width = 4f))
                    }
                }
            }
        }
    }
}

private fun cellBackgroundColor(gx: Int, gy: Int): Color {
    // Corners (6x6 base pads)
    val red = gx in 0..5 && gy in 0..5
    val green = gx in 9..14 && gy in 0..5
    val yellow = gx in 9..14 && gy in 9..14
    val blue = gx in 0..5 && gy in 9..14

    val base = when {
        red -> SeatColors[0]
        green -> SeatColors[1]
        yellow -> SeatColors[2]
        blue -> SeatColors[3]
        else -> Color(0xFFFAF7EE)
    }
    // Inner 4x4 white pad inside each base
    if (red && gx in 1..4 && gy in 1..4) return Color.White
    if (green && gx in 10..13 && gy in 1..4) return Color.White
    if (yellow && gx in 10..13 && gy in 10..13) return Color.White
    if (blue && gx in 1..4 && gy in 10..13) return Color.White

    // Home columns
    val seat = when {
        gy == 7 && gx in 1..5 -> 0   // Red
        gx == 7 && gy in 1..5 -> 1   // Green
        gy == 7 && gx in 9..13 -> 2  // Yellow
        gx == 7 && gy in 9..13 -> 3  // Blue
        else -> -1
    }
    if (seat >= 0) return SeatColors[seat]

    return base
}

/** Returns the (col, row) centre of a token in float cell coordinates, or null if finished + already drawn. */
private fun tokenCell(engine: LudoEngine, seat: Int, tokenIndex: Int): Pair<Float, Float>? {
    val t = engine.tokens[seat][tokenIndex]
    return when (t.kind) {
        TokenStateKind.Home -> {
            val (px, py) = Board.BASE_SLOTS[seat][tokenIndex]
            Pair(px, py)
        }
        TokenStateKind.Track -> {
            val (cx, cy) = Board.TRACK[t.track.coerceIn(0, Board.TRACK_LENGTH - 1)]
            Pair(cx + 0.5f, cy + 0.5f)
        }
        TokenStateKind.HomePath -> {
            val (cx, cy) = Board.HOME_COLUMN[seat][t.home.coerceIn(0, Board.HOME_PATH_LENGTH - 1)]
            Pair(cx + 0.5f, cy + 0.5f)
        }
        TokenStateKind.Finished -> Pair(7.5f + (tokenIndex - 1.5f) * 0.25f, 7.5f)
    }
}

private fun seatName(seat: Int): String = when (seat) {
    0 -> "Red"; 1 -> "Green"; 2 -> "Yellow"; 3 -> "Blue"; else -> "?"
}
