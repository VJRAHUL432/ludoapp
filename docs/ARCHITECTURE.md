# Architecture

This document explains how the pieces fit together. For setup instructions see the root [README](../README.md).

## High-level flow

```
+------------------+       HTTPS REST       +------------------+
|  Unity (Android) | <--------------------> |  Express (REST)  |
|                  |     /api/auth/*        |                  |
|                  |     /api/user/me       +---------+--------+
|                  |     /api/match/...               |
|                  |                                  v
|                  |       Socket.IO          +---------------+
|                  | <----------------------> |  Game Server  |
|                  |   roll / move / resume   +-------+-------+
+--------+---------+   pong / state / event           |
         |                                            |
         |                                  +---------v---------+
         |                                  |  Redis (live state |
         |                                  |  + matchmaking q)  |
         |                                  +---------+---------+
         |                                            |
         |                                  +---------v---------+
         +----------------------------------|  PostgreSQL        |
                                             |  (history)        |
                                             +-------------------+
```

## Server authority

The client only ever sends *intents*:

- `game:roll` — "I'd like to roll".
- `game:move` — "I'd like to move token N".
- `game:resume` — "Replay state for matchId".

The server is the only thing that:

1. Generates dice values (`crypto.randomInt`).
2. Mutates `match:<id>` in Redis under a per-match lock.
3. Broadcasts deltas (`DICE_ROLLED`, `TOKEN_MOVED`, `TOKEN_KILLED`, ...).
4. Persists outcomes to Postgres at end-of-match.

This means a tampered APK cannot fake a six, fake a kill, or fake a win.

## Idempotency

Every roll/move carries an `actionId` UUID. The engine maintains a recent
`appliedActions` ring buffer per match and ignores duplicates, so a packet
replayed during reconnect cannot create duplicate turns or moves.

## Reconnect

The client keeps `matchId` in PlayerPrefs. On socket connect after a drop,
it emits `game:resume` and the server replays the latest snapshot. The
per-player `connected` flag in the state lets the UI render "reconnecting…"
indicators for AFK seats.

## Turn timeouts

Each match has a single Node-side `setTimeout` (see `socket/turnTimer.js`).
On expiry we call `engine.applyTimeout`, which:

1. Auto-rolls if the seat hadn't yet.
2. Picks a heuristic token if one can move.
3. Otherwise passes the turn.

This guarantees the match always advances even if a client disappears.

## Crash safety

- Backend: `uncaughtException` and `unhandledRejection` handlers log but
  never exit; only `SIGINT`/`SIGTERM` perform a graceful shutdown.
- Client: `GlobalExceptionHandler` hooks `Application.logMessageReceivedThreaded`
  and `AppDomain.CurrentDomain.UnhandledException`. Forward to Crashlytics
  in `Report()` once the SDK is added.
- Every callback wrapped in try/catch (`EventBus`, `MatchController`,
  `SocketClient`).

## AdMob (modular)

`Ludo.Ads.AdManager` is a façade. It delegates to an `IAdProvider` only
when `ENABLE_ADMOB` is defined and a provider is attached. Until then, all
public methods are safe no-ops — ads cannot crash gameplay because the SDK
isn't even compiled in.
