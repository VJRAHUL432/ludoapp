package com.ludo.app

import android.app.Application
import android.util.Log
import com.google.android.gms.ads.MobileAds
import com.ludo.app.ads.AdManager

/**
 * Application-wide bootstrap. Runs once on cold start.
 *
 * Initialises Google Mobile Ads (AdMob) and creates the singleton AdManager.
 * Crash-safe: failures here are logged but do not block app launch.
 */
class LudoApp : Application() {
    override fun onCreate() {
        super.onCreate()
        try {
            MobileAds.initialize(this) { initStatus ->
                Log.i(TAG, "MobileAds initialised: ${initStatus.adapterStatusMap}")
                AdManager.preload(this@LudoApp)
            }
        } catch (t: Throwable) {
            Log.e(TAG, "MobileAds init failed", t)
        }
    }

    companion object {
        private const val TAG = "LudoApp"
    }
}
