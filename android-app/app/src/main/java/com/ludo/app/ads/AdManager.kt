package com.ludo.app.ads

import android.app.Activity
import android.content.Context
import android.util.Log
import com.google.android.gms.ads.AdError
import com.google.android.gms.ads.AdRequest
import com.google.android.gms.ads.FullScreenContentCallback
import com.google.android.gms.ads.LoadAdError
import com.google.android.gms.ads.OnUserEarnedRewardListener
import com.google.android.gms.ads.interstitial.InterstitialAd
import com.google.android.gms.ads.interstitial.InterstitialAdLoadCallback
import com.google.android.gms.ads.rewarded.RewardedAd
import com.google.android.gms.ads.rewarded.RewardedAdLoadCallback
import com.ludo.app.BuildConfig

/**
 * Singleton AdMob façade.
 *
 *  - Loads interstitial + rewarded ads in the background.
 *  - Auto-reloads after each show.
 *  - SAFE: every public method tolerates "ad not yet loaded" — your gameplay
 *    flow is never blocked.
 *  - When [gameplayLocked] is true (during an active match), interstitials
 *    are suppressed.
 */
object AdManager {
    private const val TAG = "AdManager"

    var gameplayLocked: Boolean = false
        @JvmStatic set

    private var interstitial: InterstitialAd? = null
    private var rewarded: RewardedAd? = null
    private var loadingInterstitial = false
    private var loadingRewarded = false

    fun preload(ctx: Context) {
        loadInterstitial(ctx)
        loadRewarded(ctx)
    }

    // ------------------------------ Interstitial ------------------------------

    private fun loadInterstitial(ctx: Context) {
        if (loadingInterstitial || interstitial != null) return
        loadingInterstitial = true
        InterstitialAd.load(
            ctx,
            BuildConfig.AD_INTERSTITIAL_UNIT,
            AdRequest.Builder().build(),
            object : InterstitialAdLoadCallback() {
                override fun onAdFailedToLoad(error: LoadAdError) {
                    loadingInterstitial = false
                    Log.w(TAG, "interstitial load failed: ${error.message}")
                }
                override fun onAdLoaded(ad: InterstitialAd) {
                    loadingInterstitial = false
                    interstitial = ad
                }
            },
        )
    }

    fun showInterstitial(activity: Activity, onClosed: () -> Unit) {
        if (gameplayLocked) { onClosed(); return }
        val ad = interstitial
        if (ad == null) {
            onClosed()
            loadInterstitial(activity)
            return
        }
        ad.fullScreenContentCallback = object : FullScreenContentCallback() {
            override fun onAdDismissedFullScreenContent() {
                interstitial = null
                loadInterstitial(activity)
                runCatching { onClosed() }
            }
            override fun onAdFailedToShowFullScreenContent(p0: AdError) {
                interstitial = null
                loadInterstitial(activity)
                runCatching { onClosed() }
            }
        }
        try { ad.show(activity) } catch (t: Throwable) {
            Log.e(TAG, "interstitial.show", t); onClosed()
        }
    }

    // ------------------------------ Rewarded ------------------------------

    private fun loadRewarded(ctx: Context) {
        if (loadingRewarded || rewarded != null) return
        loadingRewarded = true
        RewardedAd.load(
            ctx,
            BuildConfig.AD_REWARDED_UNIT,
            AdRequest.Builder().build(),
            object : RewardedAdLoadCallback() {
                override fun onAdFailedToLoad(error: LoadAdError) {
                    loadingRewarded = false
                    Log.w(TAG, "rewarded load failed: ${error.message}")
                }
                override fun onAdLoaded(ad: RewardedAd) {
                    loadingRewarded = false
                    rewarded = ad
                }
            },
        )
    }

    fun showRewarded(activity: Activity, onResult: (earned: Boolean) -> Unit) {
        val ad = rewarded
        if (ad == null) {
            onResult(false)
            loadRewarded(activity)
            return
        }
        var earned = false
        ad.fullScreenContentCallback = object : FullScreenContentCallback() {
            override fun onAdDismissedFullScreenContent() {
                rewarded = null
                loadRewarded(activity)
                runCatching { onResult(earned) }
            }
            override fun onAdFailedToShowFullScreenContent(p0: AdError) {
                rewarded = null
                loadRewarded(activity)
                runCatching { onResult(false) }
            }
        }
        try {
            ad.show(activity, OnUserEarnedRewardListener { earned = true })
        } catch (t: Throwable) {
            Log.e(TAG, "rewarded.show", t); onResult(false)
        }
    }
}
