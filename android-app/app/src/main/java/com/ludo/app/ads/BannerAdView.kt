package com.ludo.app.ads

import android.view.ViewGroup
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import com.google.android.gms.ads.AdRequest
import com.google.android.gms.ads.AdSize
import com.google.android.gms.ads.AdView
import com.ludo.app.BuildConfig

/**
 * Compose wrapper around AdMob's classic AdView (banner).
 * Cleans up on disposal so we don't leak.
 */
@Composable
fun BannerAdView(modifier: Modifier = Modifier) {
    val viewHolder = remember { mutableStateOf<AdView?>(null) }
    DisposableEffect(Unit) {
        onDispose { viewHolder.value?.destroy() }
    }
    AndroidView(
        modifier = modifier.fillMaxWidth().height(60.dp),
        factory = { ctx ->
            AdView(ctx).also { v ->
                v.adUnitId = BuildConfig.AD_BANNER_UNIT
                v.setAdSize(AdSize.BANNER)
                v.layoutParams = ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                )
                v.loadAd(AdRequest.Builder().build())
                viewHolder.value = v
            }
        },
    )
}
