package com.ludo.app.ui

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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ludo.app.ads.BannerAdView

@Composable
fun MenuScreen(onPick: (GameMode) -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF101218))
            .padding(horizontal = 32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text(
            text = "LUDO",
            color = Color.White,
            fontSize = 96.sp,
            fontWeight = FontWeight.ExtraBold,
        )
        Text(
            text = "Choose a mode",
            color = Color(0xFFB6B9C2),
            fontSize = 18.sp,
        )
        Spacer(Modifier.height(48.dp))

        ModeButton("Play vs Bot",     Color(0xFFEB3338), onClick = { onPick(GameMode.VsBot) })
        Spacer(Modifier.height(16.dp))
        ModeButton("2 Players Local", Color(0xFF33B84C), onClick = { onPick(GameMode.Local2P) })
        Spacer(Modifier.height(16.dp))
        ModeButton("4 Players Local", Color(0xFF2E72EA), onClick = { onPick(GameMode.Local4P) })

        Spacer(Modifier.weight(1f))
        BannerAdView()
    }
}

@Composable
private fun ModeButton(label: String, bg: Color, onClick: () -> Unit) {
    Button(
        onClick = onClick,
        modifier = Modifier.fillMaxWidth().height(72.dp),
        shape = RoundedCornerShape(16.dp),
        colors = ButtonDefaults.buttonColors(containerColor = bg, contentColor = Color.White),
    ) {
        Text(label, fontSize = 22.sp, fontWeight = FontWeight.Bold)
    }
}
