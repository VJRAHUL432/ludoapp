package com.ludo.app.ui

import android.app.Activity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ludo.app.ads.AdManager
import com.ludo.app.game.LudoEngine

private val SeatColors = listOf(
    Color(0xFFEB3338), Color(0xFF33B84C), Color(0xFFF5C729), Color(0xFF2E72EA),
)

@Composable
fun ResultScreen(
    activity: Activity,
    winnerSeat: Int,
    players: List<LudoEngine.PlayerInfo>,
    onPlayAgain: () -> Unit,
    onMainMenu: () -> Unit,
) {
    val winner = players.firstOrNull { it.seat == winnerSeat }
    val baseCoins = if (winnerSeat == 0) 100 else 25
    var coins by remember { mutableIntStateOf(baseCoins) }
    var rewardClaimed by remember { mutableStateOf(false) }

    Column(
        modifier = Modifier.fillMaxSize().background(Color(0xCC101218)).padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text("GAME OVER", color = Color.White, fontSize = 48.sp, fontWeight = FontWeight.ExtraBold)
        Spacer(Modifier.height(24.dp))
        Text(
            "${winner?.name ?: "?"} (${seatName(winnerSeat)}) wins!",
            color = SeatColors[winnerSeat],
            fontSize = 28.sp,
            fontWeight = FontWeight.Bold,
        )
        Spacer(Modifier.height(16.dp))
        Text("+$coins coins", color = Color(0xFFF5C729), fontSize = 22.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(48.dp))

        Button(
            onClick = {
                if (rewardClaimed) return@Button
                AdManager.showRewarded(activity) { earned ->
                    if (earned) {
                        rewardClaimed = true
                        coins *= 2
                    } else if (!rewardClaimed) {
                        // No ad available — give a smaller bonus so the button still feels useful
                        rewardClaimed = true
                        coins += baseCoins
                    }
                }
            },
            enabled = !rewardClaimed,
            modifier = Modifier.fillMaxWidth().height(64.dp),
            shape = RoundedCornerShape(16.dp),
            colors = ButtonDefaults.buttonColors(
                containerColor = Color(0xFFF59E0B),
                disabledContainerColor = Color(0xFFF59E0B).copy(alpha = 0.3f),
            ),
        ) {
            Text(if (rewardClaimed) "Reward claimed" else "Watch Ad → 2x Coins", fontSize = 18.sp, fontWeight = FontWeight.Bold)
        }
        Spacer(Modifier.height(16.dp))
        Button(
            onClick = onPlayAgain,
            modifier = Modifier.fillMaxWidth().height(64.dp),
            shape = RoundedCornerShape(16.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF33B84C)),
        ) { Text("Play Again", fontSize = 20.sp, fontWeight = FontWeight.Bold) }
        Spacer(Modifier.height(12.dp))
        Button(
            onClick = onMainMenu,
            modifier = Modifier.fillMaxWidth().height(64.dp),
            shape = RoundedCornerShape(16.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF2A2E3A)),
        ) { Text("Main Menu", fontSize = 20.sp) }
    }
}

private fun seatName(seat: Int): String = when (seat) {
    0 -> "Red"; 1 -> "Green"; 2 -> "Yellow"; 3 -> "Blue"; else -> "?"
}
