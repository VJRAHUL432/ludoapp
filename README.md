# Ludo App — Android Multiplayer Ludo Game

A modular, scalable, server-authoritative Ludo game for Android, inspired by Ludo King.

> **Status:**
> - ✅ **Offline gameplay (vs Bot, 2P local, 4P local) is complete and APK-buildable.** Open `client-unity/` in Unity 2022.3 LTS, switch to Android, click Build. See [`client-unity/README.md`](./client-unity/README.md).
> - ✅ Online multiplayer **backend** is complete: Node.js + Express + Socket.IO + Postgres + Redis, server-authoritative engine, matchmaking, private rooms, reconnect, JWT auth.
> - 🟡 Online multiplayer **client wiring** (Socket.IO library + transport adapter) is left as a follow-up — pick a C# Socket.IO library and implement `ISocketTransport` in `client-unity/Assets/Scripts/Networking/SocketClient.cs`.

---

## Tech Stack

| Layer        | Technology                                         |
| ------------ | -------------------------------------------------- |
| Game Engine  | Unity (C#) — Android only, min SDK 26 (Android 8+) |
| Backend      | Node.js + Express + Socket.IO                      |
| Database     | PostgreSQL                                         |
| Cache / RT   | Redis (rooms, sessions, matchmaking, reconnect)    |
| Auth         | JWT (guest + Google)                               |
| Ads          | Google AdMob (modular, off by default)             |

---

## Repository Layout

```
ludoapp/
├── server/                 # Node.js backend (REST + Socket.IO)
│   ├── src/
│   │   ├── config/         # env, logger, db, redis
│   │   ├── auth/           # JWT, guest, google
│   │   ├── db/             # postgres pool + queries
│   │   ├── cache/          # redis client
│   │   ├── game/           # SERVER-AUTHORITATIVE Ludo engine
│   │   ├── matchmaking/    # online queue + private rooms
│   │   ├── socket/         # Socket.IO handlers, heartbeat
│   │   ├── middleware/     # rate limit, validate, errors
│   │   ├── routes/         # REST endpoints
│   │   └── utils/          # ids, retry, security
│   ├── migrations/
│   ├── package.json
│   └── .env.example
│
├── client-unity/           # Unity Android project (C# scripts)
│   └── Assets/Scripts/
│       ├── Core/           # Bootstrap, GameManager, EventBus, ServiceLocator
│       ├── Networking/     # SocketClient, Reconnect, Heartbeat, Protocol
│       ├── Game/           # Ludo board, tokens, dice, turns
│       ├── AI/             # Bot (Easy/Medium/Hard)
│       ├── UI/             # Screens (Splash, Home, Lobby, Game, Result)
│       ├── Ads/            # AdManager (AdMob, modular)
│       ├── Audio/          # AudioManager (compressed, toggle)
│       ├── Pooling/        # ObjectPool
│       ├── Error/          # GlobalExceptionHandler, CrashReporter
│       └── Utils/          # SafeAsync, Coroutines, Throttle
│
├── docs/                   # Architecture & runbook docs
└── README.md
```

---

## Quick Start

### 1. Backend

```bash
cd server
cp .env.example .env       # fill in JWT_SECRET, DB creds, etc.
npm install
npm run migrate            # creates Postgres schema
npm run dev                # starts on :3000
```

### 2. Unity Client

1. Open `client-unity/` in Unity 2022.3 LTS or newer.
2. Switch platform to **Android** (`File → Build Settings → Android`).
3. In `PlayerSettings`:
   - Min API: **26 (Android 8.0)**
   - Target API: latest
   - Scripting Backend: **IL2CPP**, ARM64
   - Strip Engine Code, Managed Stripping: **Medium**
4. Set the backend URL in `Assets/Resources/AppConfig.json`.
5. Build APK / AAB.

---

## Game Modes

1. **Play vs Computer** — fully offline, AI bot (Easy / Medium / Hard).
2. **Local Multiplayer** — 2–4 players on the same device, offline.
3. **Private Room** — host generates a 5-char room code (e.g. `K8L72`), friends join.
4. **Online Multiplayer** — automatic matchmaking via Redis queue.

All online modes use **server-authoritative** logic — clients never compute the truth, only render it.

---

## Architecture Highlights

### Server Authority
- Dice rolls happen on the **server**.
- Token movement, kills, safe-zones, win detection are validated on the **server**.
- Clients send *intents* (`rollDice`, `moveToken`); server emits *state deltas*.

### Reconnect & Crash Safety
- Each match has a `matchId` stored in Redis with the full authoritative state.
- On disconnect, client retains `matchId` + `playerToken`; on reconnect server replays the latest state.
- Heartbeat (ping/pong) every 5s; missed 3 → mark AFK, auto-play timeout move.
- Idempotent action IDs prevent duplicate packets from causing duplicate turns.

### Android Optimizations
- IL2CPP + ARM64 only.
- Object pooling for tokens, dice particles, kill effects.
- Compressed textures (ASTC), audio (Vorbis low quality).
- No allocations in `Update()`; coroutines for transitions.
- Async scene loading with progress.
- Battery-friendly — pauses sockets in background.

### Crash Prevention
- `GlobalExceptionHandler` hooks `Application.logMessageReceived` + `AppDomain.UnhandledException`.
- All async code via `SafeAsync` wrapper with try/catch + timeout.
- Null-safety helpers; defensive guards before every state mutation.
- Backoff retry on socket/API.

### Ads (modular, off until enabled)
- `AdManager` is a single facade. AdMob SDK references are isolated and can be activated by setting `ENABLE_ADMOB` define.
- Never shown during a player's turn or during reconnect.

---

## Database Schema (Postgres)

See `server/migrations/001_init.sql`. Tables: `users`, `matches`, `match_players`.

## Cache Schema (Redis)

| Key                         | Purpose                       |
| --------------------------- | ----------------------------- |
| `session:<userId>`          | active socket id, expires 1h  |
| `match:<matchId>`           | full game state JSON          |
| `room:<roomCode>`           | private room → matchId        |
| `queue:online:<playerCount>`| matchmaking FIFO list         |

---

## Roadmap

- [x] Backend foundation: auth, db, redis, sockets, game engine
- [x] Unity client architecture (scripts)
- [ ] Unity scenes, art, sound assets (artist work)
- [ ] AdMob integration enabled
- [ ] Google Sign-In native plugin
- [ ] Play Store submission build
- [ ] Stress / chaos testing harness

---

## License

Proprietary — all rights reserved.
