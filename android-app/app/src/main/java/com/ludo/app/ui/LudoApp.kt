package com.ludo.app.ui

import android.app.Activity
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.ludo.app.game.LudoEngine
import com.ludo.app.ads.AdManager

/** Top-level UI router: Menu → Game → Result, with replays. */
@Composable
fun LudoApp(activity: Activity) {
    var screen: Screen by remember { mutableStateOf<Screen>(Screen.Menu) }

    when (val s = screen) {
        is Screen.Menu -> MenuScreen(
            onPick = { mode ->
                AdManager.gameplayLocked = true
                screen = Screen.Game(mode)
            },
        )

        is Screen.Game -> GameScreen(
            mode = s.mode,
            onMatchEnded = { winnerSeat, players ->
                AdManager.gameplayLocked = false
                screen = Screen.Result(winnerSeat, players, s.mode)
            },
            onLeaveToMenu = {
                AdManager.gameplayLocked = false
                screen = Screen.Menu
            },
        )

        is Screen.Result -> ResultScreen(
            activity = activity,
            winnerSeat = s.winnerSeat,
            players = s.players,
            onPlayAgain = {
                AdManager.showInterstitial(activity) {
                    AdManager.gameplayLocked = true
                    screen = Screen.Game(s.mode)
                }
            },
            onMainMenu = {
                AdManager.showInterstitial(activity) {
                    screen = Screen.Menu
                }
            },
        )
    }
}

sealed interface Screen {
    data object Menu : Screen
    data class Game(val mode: GameMode) : Screen
    data class Result(
        val winnerSeat: Int,
        val players: List<LudoEngine.PlayerInfo>,
        val mode: GameMode,
    ) : Screen
}

enum class GameMode(val label: String) {
    VsBot("Play vs Bot"),
    Local2P("2 Players Local"),
    Local4P("4 Players Local"),
}

fun playersFor(mode: GameMode): List<LudoEngine.PlayerInfo> = when (mode) {
    GameMode.VsBot -> listOf(
        LudoEngine.PlayerInfo(0, "You", false),
        LudoEngine.PlayerInfo(1, "Bot G", true),
        LudoEngine.PlayerInfo(2, "Bot Y", true),
        LudoEngine.PlayerInfo(3, "Bot B", true),
    )
    GameMode.Local2P -> listOf(
        LudoEngine.PlayerInfo(0, "Red", false),
        LudoEngine.PlayerInfo(2, "Yellow", false),
    )
    GameMode.Local4P -> listOf(
        LudoEngine.PlayerInfo(0, "Red", false),
        LudoEngine.PlayerInfo(1, "Green", false),
        LudoEngine.PlayerInfo(2, "Yellow", false),
        LudoEngine.PlayerInfo(3, "Blue", false),
    )
}
