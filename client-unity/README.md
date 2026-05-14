# Ludo Unity Client (Android)

A complete offline-playable Ludo game. The entire UI (menu, board, tokens, dice, win screen) is built **procedurally in code**, so you do not need to import any art, audio, prefabs, or scenes — just open the project and Build.

## What's included (offline, fully playable)

- **Play vs Bot** — you (Red) vs three AI opponents
- **2 Players Local** — Red & Yellow on the same device
- **4 Players Local** — full hot-seat
- Real Ludo rules: dice, six → extra turn, triple-six penalty, safe cells, kills, home column, win detection
- Bot AI: prefers kill > finish > release > advance

> The online multiplayer architecture (Socket.IO client, server-authoritative engine, matchmaking, reconnect) is also in this repo under `Assets/Scripts/Core|Networking|Game|UI` and `server/`. It compiles but is **not wired into the Main scene**. Wire it later when you're ready to ship online play.

---

## Build the APK (5 minutes)

### 1. Install Unity
- Get **Unity Hub** → install **Unity 2022.3 LTS** (any 2022.3.x will work).
- During install, tick the **Android Build Support** module (and its sub-modules: OpenJDK, Android SDK & NDK Tools).

### 2. Open the project
- Unity Hub → **Open** → select this folder: `ludoapp/client-unity/`.
- First open will take a few minutes (Unity imports + compiles).
- If Unity asks about missing `.meta` files, click **Continue / OK** — it generates them automatically.

### 3. Switch platform to Android
- `File → Build Settings…`
- Select **Android** → click **Switch Platform** (one-time, ~1 min).

### 4. Confirm Player Settings (already pre-configured, but verify)
- `Player Settings → Player → Other Settings`:
  - Minimum API Level: **Android 8.0 'Oreo' (API 26)**
  - Scripting Backend: **IL2CPP**
  - Target Architectures: tick **ARM64** (untick ARMv7 for smaller APK)
- `Player Settings → Player → Resolution and Presentation`:
  - Default Orientation: **Portrait**

### 5. Build
- `File → Build Settings…` → make sure **`Assets/Scenes/Main`** is in *Scenes In Build* (it is, by default).
- Click **Build** → choose an output folder → wait. You get `Ludo.apk`.

### 6. Install on phone
- Enable **Developer Options → USB debugging** on your Android phone.
- Plug in via USB and run:
  ```
  adb install -r path/to/Ludo.apk
  ```
- Or copy the APK to the phone and tap to install (allow "install from unknown sources").

That's it — you have a working Ludo game on your phone.

---

## Project Layout

```
client-unity/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity              # Empty scene — game boots procedurally
│   ├── Resources/
│   │   └── AppConfig.json          # Backend URL (only used in online mode)
│   └── Scripts/
│       ├── Local/                  # ★ The actual playable offline game
│       │   ├── GameRoot.cs         #    auto-bootstraps the entire UI
│       │   ├── LocalLudoEngine.cs  #    pure-C# Ludo rules engine
│       │   └── BoardCells.cs       #    15x15 grid geometry
│       ├── Core/                   # Online infra (compile-only for now)
│       ├── Networking/             # Online infra (compile-only for now)
│       ├── Game/                   # Online state mirror (compile-only)
│       ├── AI/                     # Online bot helper
│       ├── UI/                     # Online screen controllers
│       ├── Ads/                    # AdMob façade (modular, off)
│       ├── Audio/                  # Audio manager
│       ├── Pooling/                # Object pool
│       ├── Error/                  # Global exception handler
│       └── Utils/                  # Retry, SafeAsync
├── Packages/manifest.json
└── ProjectSettings/
```

## How the procedural UI works

`GameRoot.cs` is decorated with `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`. Unity calls that static method automatically once the scene loads, and we use it to instantiate the GameRoot MonoBehaviour. From there it builds:

1. A `Canvas` with `CanvasScaler` (1080×1920 reference, scales for any screen).
2. An `EventSystem` for input.
3. The Menu screen with three buttons.
4. When you pick a mode → Game screen: 15×15 colored grid, four corner bases, four home columns, 16 tokens, dice, roll button, status banner.
5. When the match ends → Result screen with "Play Again" / "Main Menu".

Sprites are generated at runtime: a white square (`Texture2D.whiteTexture`) and a procedurally-drawn anti-aliased white circle. The `LegacyRuntime.ttf` built-in font is used for all text. Zero external assets.

## Adding polish later (optional)

- **Real art**: drop sprites into `Assets/Sprites/` and replace `_squareSprite` / `_circleSprite` references in `GameRoot.cs`.
- **Audio**: drop clips into `Resources/Audio/sfx_dice.ogg`, `sfx_move.ogg`, `sfx_kill.ogg`, `sfx_win.ogg`, `music_bgm.ogg` — `AudioManager` will pick them up.
- **Animations**: replace the snap-to-cell logic in `GameRoot.RefreshAll` with a coroutine that lerps the token along the path.
- **Online**: wire a Socket.IO library into `Networking/SocketClient.cs`, set `AppConfig.json → socketUrl`, and use the existing `MatchController` flow.

## Troubleshooting

| Problem | Fix |
|---|---|
| Unity won't open the project | Make sure you're using **2022.3.x LTS**. Older 2021 won't recognise some APIs. |
| "Module com.unity.modules.ui not found" | Open `Window → Package Manager`, search "UI Toolkit" / "UGUI", install. |
| Build fails: "Android SDK not found" | `Edit → Preferences → External Tools` → tick **Use embedded JDK / SDK / NDK** or point to your install. |
| APK installs but black screen | Check `adb logcat -s Unity` for errors. Most often a missing `Main.unity` reference in Build Settings → re-add it. |
| App runs but board looks tiny | Resolution scaler picks portrait by default. Phone sideways? Lock orientation in Player Settings → Resolution & Presentation → Default Orientation = Portrait. |
