# AdMob Setup — Step by Step

This document walks you through enabling Google AdMob in the Unity client. The wiring is already done in code — you just need to import the SDK and flip a switch.

> **TL;DR**: Import the Google Mobile Ads `.unitypackage` → add `ENABLE_ADMOB` to scripting defines → build. You'll see Google's **test ads** out of the box (banner on menu, interstitial between matches, "Watch Ad → 2x Coins" rewarded button on the result screen).

---

## What's already in the repo

Already done — you do not need to do these:

| Layer | File | Purpose |
|---|---|---|
| Façade | `Assets/Scripts/Ads/AdManager.cs` | Public API used by gameplay code (`ShowBanner`, `ShowInterstitial`, `ShowRewarded`) |
| Bootstrap | `Assets/Scripts/Ads/AdsBootstrap.cs` | Auto-creates the AdManager on app start; attaches the AdMob provider when `ENABLE_ADMOB` is defined |
| Provider | `Assets/Scripts/Ads/AdMobProvider.cs` | Concrete Google Mobile Ads implementation with retry / auto-reload / safe fallbacks |
| Coroutine helper | `Assets/Scripts/Ads/CoroutineHost.cs` | Lets the provider schedule reload backoff |
| Game integration | `Assets/Scripts/Local/GameRoot.cs` | Banner on menu, interstitial after match, rewarded "double coins" |
| Config | `Assets/Resources/AppConfig.json` | Ad unit IDs (defaults to Google's official test units) |
| Manifest | `Assets/Plugins/Android/AndroidManifest.xml` | Internet permission + AdMob App ID meta-data |

---

## Step 1 — Download the Google Mobile Ads Unity plugin

1. Go to the official releases page:
   https://github.com/googleads/googleads-mobile-unity/releases
2. Download the latest `GoogleMobileAds-vX.X.X.unitypackage` (top of the list).

## Step 2 — Import into Unity

1. Open the project in Unity.
2. **Assets → Import Package → Custom Package…**
3. Select the `.unitypackage` you just downloaded.
4. The Import dialog appears. Leave **everything ticked** and click **Import**.
5. Unity will compile. Wait. You may see prompts about "External Dependency Manager" — accept them.

## Step 3 — Resolve Android dependencies

The plugin uses Google's "External Dependency Manager" to pull native AAR files.

1. **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
2. Wait. Unity downloads `play-services-ads` AARs into `Assets/Plugins/Android/`. This takes 1–3 minutes the first time.

If you see warnings about Gradle or Jetifier, accept the defaults — the SDK ships ready-to-go.

## Step 4 — Turn on the ENABLE_ADMOB define

This is the switch that activates `AdMobProvider.cs` (it's `#if ENABLE_ADMOB` gated so removing the SDK never breaks the build).

1. **File → Build Settings → Player Settings…**
2. **Player → Other Settings → Scripting Define Symbols** (Android tab specifically).
3. Add `ENABLE_ADMOB` to the list (separate multiple defines with `;`).
4. Press **Apply**. Unity recompiles.

You should now see in the Console at app start:
```
[Ads] AdMob provider attached.
[AdMob] SDK initialised
```

## Step 5 — Build & run

Build the APK as normal (`File → Build Settings → Build`). Install on your phone.

You should see:
- A **banner ad** at the bottom of the main menu screen.
- An **interstitial ad** when you tap "Play Again" or "Main Menu" after a match.
- A **rewarded ad** when you tap "Watch Ad → 2x Coins" on the result screen — finishing the ad doubles your displayed coin reward.

These are Google's **test ads**, marked "Test Ad" on screen. They're free, do not earn money, and are 100% safe (won't get your AdMob account banned).

---

## Step 6 — Switch to your real ad units (before Play Store)

When you're ready to ship and earn real money:

### 6a. Create an AdMob account
1. https://admob.google.com → sign up (free).
2. **Apps → Add App → Android → "App not yet published"** (or link to your Play Store listing if it exists).
3. AdMob gives you an **App ID** like `ca-app-pub-1234567890123456~1234567890`.

### 6b. Create three ad units
In your AdMob app dashboard:
1. **Ad units → Add ad unit → Banner** → name it "Ludo Banner" → Save. Note the unit ID.
2. **Add ad unit → Interstitial** → "Ludo Interstitial" → Save. Note the unit ID.
3. **Add ad unit → Rewarded** → "Ludo Rewarded" → reward 100 coins → Save. Note the unit ID.

You'll have IDs like `ca-app-pub-1234567890123456/9876543210`.

### 6c. Plug them into the project

**1. `Assets/Resources/AppConfig.json`:**
```json
{
  "enableAdmob": true,
  "useTestAdUnits": false,
  "androidAdmobAppId": "ca-app-pub-1234567890123456~1234567890",
  "androidBannerAdUnitId": "ca-app-pub-1234567890123456/1111111111",
  "androidInterstitialAdUnitId": "ca-app-pub-1234567890123456/2222222222",
  "androidRewardedAdUnitId": "ca-app-pub-1234567890123456/3333333333"
}
```

**2. `Assets/Plugins/Android/AndroidManifest.xml` — replace the App ID:**
```xml
<meta-data
    android:name="com.google.android.gms.ads.APPLICATION_ID"
    android:value="ca-app-pub-1234567890123456~1234567890" />
```

> ⚠ **CRITICAL**: The `APPLICATION_ID` meta-data **must match** the one in your AdMob console. If it doesn't, the SDK crashes the app on launch with `MissingApplicationIdException`.

**3. Rebuild & upload to Play Store.**

---

## Behaviour & policy notes

- **Banner**: shown only on the menu screen. Hidden during gameplay so it never overlaps the board.
- **Interstitial**: shown only **between matches** (after Play Again / Main Menu) — never mid-game, never during a player's turn. This is enforced by `SetGameplayLocked(true)` while a match is active.
- **Rewarded**: opt-in only — user must tap "Watch Ad → 2x Coins". They can ignore it and just hit Play Again.
- **Failure handling**: every ad call has a try/catch + onClosed fallback, so a network error or unloaded ad will **never** block UI flow. The user always continues to the next screen.
- **Reconnect-aware** (online mode, when you wire it later): `AdManager.SetGameplayLocked(true)` is called during reconnect attempts so an interstitial never pops over a "Reconnecting…" dialog.

## Removing AdMob

If you ever want to disable ads completely without ripping the SDK out:

1. Remove `ENABLE_ADMOB` from Scripting Define Symbols, or
2. Set `enableAdmob: false` in `AppConfig.json`.

Either makes every ad call a safe no-op. The code paths remain — you just won't see ads. The APK will still build with the SDK present.

## Removing the SDK entirely

If you want to strip the SDK out (e.g. for an ad-free build):

1. Delete `Assets/GoogleMobileAds/` and any `Assets/Plugins/Android/play-services-ads*` files.
2. Remove `ENABLE_ADMOB` from Scripting Define Symbols.
3. Remove the `<meta-data ... APPLICATION_ID ...>` block from `AndroidManifest.xml`.

The app compiles and runs normally.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `MissingApplicationIdException` on app launch | The `APPLICATION_ID` in `AndroidManifest.xml` is missing or doesn't match AdMob console. Use the test ID `ca-app-pub-3940256099942544~3347511713` for development. |
| Banner doesn't appear | Confirm `enableAdmob: true` in `AppConfig.json`. Check `adb logcat -s Unity` for `[AdMob] banner` errors. |
| Interstitial doesn't show | Likely still loading. Check Console — first interstitial takes ~1–3s after app start. The button works regardless (it just continues without an ad if not loaded). |
| Rewarded ad gives the reward but coins don't update | Make sure you're testing in Play vs Bot mode and you won — coins only show on the result screen. |
| `error: package com.google.android.gms.ads does not exist` at build | You haven't run **Force Resolve** (Step 3). Run it. |
| Build fails: "Multiple dex files define..." | Old Play Services in `Assets/Plugins/Android/` colliding with new ones. Delete the old AAR files and Force Resolve again. |
| Want to test with **your** AdMob account but in test mode | In `AdMobProvider.cs.Initialize`, before `MobileAds.Initialize`, call `MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = new List<string>{ "YOUR_DEVICE_ID" } })`. Get your device ID from the first `[AdMob] interstitial show failed` log line in `adb logcat`. |

---

## Reference

- AdMob Unity quick start: https://developers.google.com/admob/unity/quick-start
- Test ad units (always safe): https://developers.google.com/admob/unity/test-ads
- Banner sizes: https://developers.google.com/admob/unity/banner
- Mediation (later): https://developers.google.com/admob/unity/mediation
