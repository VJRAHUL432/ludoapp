# Ludo — Native Android App

A complete, lightweight Ludo game built with **Kotlin + Jetpack Compose**.
Designed to open and build directly in **Android Studio** — no Unity, no
heavy installs. Perfect for a MacBook.

## What's inside

- **Kotlin + Jetpack Compose** UI (the modern Android way)
- Pure-Kotlin Ludo rules engine (`com.ludo.app.game.LudoEngine`)
- Procedural board rendered with `Canvas` (no images required)
- AdMob: banner on menu, interstitial between matches, rewarded "2x coins"
- Three modes: vs Bot (smart AI), 2-player local, 4-player local
- Crash-safe: every ad call has fallbacks, gameplay never blocks

## Project size

About **40 MB** before build (Gradle + Compose libs). Your MacBook will be fine.

## Build it (5 minutes)

1. Open Android Studio → **File → Open** → select the `android-app/` folder
2. Wait for "Gradle Sync" to finish (first time: 5–10 min, downloads dependencies)
3. Plug in your phone with USB debugging on
4. Press the green ▶ Run button — it builds, installs, and launches

That's it. You'll see test ads from Google's official test units, safe to use in development.

## Going live (replace test ads)

1. Get an AdMob account: <https://admob.google.com>
2. Create your app + 3 ad units (banner, interstitial, rewarded)
3. Edit `app/build.gradle.kts`:
   - Set `USE_TEST_ADS` = `false`
   - Replace the four `AD_*` strings with your real IDs
4. Edit `app/src/main/AndroidManifest.xml`:
   - Replace the `APPLICATION_ID` meta-data value with your real AdMob App ID
5. Rebuild → upload to Play Store

## Project layout

```
android-app/
├── app/
│   ├── build.gradle.kts                 # Module config (SDK versions, deps, AdMob IDs)
│   ├── proguard-rules.pro
│   └── src/main/
│       ├── AndroidManifest.xml          # Permissions, AdMob app ID, launcher activity
│       ├── res/                         # Icons, colors, strings, theme
│       └── java/com/ludo/app/
│           ├── LudoApp.kt               # Application class — boots AdMob
│           ├── MainActivity.kt          # Entry point, sets up Compose
│           ├── ads/                     # AdMob façade + banner Compose wrapper
│           ├── game/                    # Pure-Kotlin Ludo engine + board geometry
│           └── ui/                      # Compose screens: Menu, Game, Result
├── build.gradle.kts                     # Top-level
├── settings.gradle.kts                  # Module list
├── gradle.properties                    # JVM args, AndroidX, etc.
└── gradle/libs.versions.toml            # Single source of truth for dep versions
```
