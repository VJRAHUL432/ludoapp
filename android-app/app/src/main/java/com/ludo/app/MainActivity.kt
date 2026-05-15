package com.ludo.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import com.ludo.app.ui.LudoApp

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            LudoTheme {
                Surface(
                    modifier = Modifier.fillMaxSize().background(Color(0xFF101218)),
                    color = Color(0xFF101218)
                ) {
                    LudoApp(activity = this)
                }
            }
        }
    }
}

@Composable
private fun LudoTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = darkColorScheme(
            primary = Color(0xFF2E72EA),
            secondary = Color(0xFFF5C729),
            background = Color(0xFF101218),
            surface = Color(0xFF1B1F2A),
            onBackground = Color.White,
            onSurface = Color.White,
        ),
        content = content,
    )
}
