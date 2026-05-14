# Ludo Unity Client (Android)

This folder contains the Unity C# script architecture. To turn it into a working Unity project:

1. Open Unity Hub → New Project → 2D (URP) or 3D (URP) → Unity 2022.3 LTS or newer.
2. Set project location to this folder (or copy `Assets/Scripts` into a fresh project).
3. Switch platform to **Android** and configure:
   - Min API: 26 (Android 8.0 Oreo)
   - Scripting Backend: **IL2CPP**, target ARM64 (and ARMv7 if needed)
   - Managed Stripping Level: **Medium**
   - Texture compression: **ASTC**
   - .NET API Compatibility: **.NET Standard 2.1**
4. Install required packages via Package Manager:
   - **Newtonsoft Json** (`com.unity.nuget.newtonsoft-json`)
   - Optional later: Google Mobile Ads SDK, Google Play Games plugin
5. Import the `socket.io-client-csharp` library (NuGet for Unity, or as a DLL).
6. Drop `AppConfig.json` into `Assets/Resources/` with your backend URL.
7. Build APK / AAB.

## Script Map

| Folder            | Purpose                                                                |
| ----------------- | ---------------------------------------------------------------------- |
| `Core/`           | Bootstrap, GameManager, ServiceLocator, EventBus, AppConfig            |
| `Networking/`     | SocketClient, Protocol, Heartbeat, ReconnectController, ApiClient      |
| `Game/`           | Authoritative state mirror, board math, token rendering, dice         |
| `AI/`             | Local Ludo bot (Easy / Medium / Hard)                                  |
| `UI/`             | Screen controllers + screen manager                                    |
| `Ads/`            | AdManager façade (modular; AdMob lib gated by `ENABLE_ADMOB`)          |
| `Audio/`          | AudioManager (compressed, mute toggle, low GC)                         |
| `Pooling/`        | Generic object pool                                                    |
| `Error/`          | GlobalExceptionHandler + crash reporter hook                           |
| `Utils/`          | SafeAsync, RetryPolicy, Throttle, JsonUtil                             |
