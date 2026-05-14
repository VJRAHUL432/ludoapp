using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Ludo.Core;
using Ludo.Utils;

namespace Ludo.Networking
{
    /// <summary>
    /// Thin façade over a real Socket.IO library. We DO NOT directly depend on
    /// any specific package here; instead, we wire a pluggable
    /// <see cref="ISocketTransport"/> at runtime (e.g. the SocketIOClient
    /// NuGet package, or a custom Engine.IO transport).
    ///
    /// This keeps the rest of the codebase unit-testable and lets you swap
    /// libraries without touching gameplay code.
    /// </summary>
    public class SocketClient
    {
        private readonly AppConfig _cfg;
        private readonly object _gate = new object();
        private ISocketTransport _transport;
        private CancellationTokenSource _heartbeatCts;
        private bool _paused;
        private int _consecutiveMissed;

        public bool IsConnected => _transport != null && _transport.IsConnected;
        public string AuthToken { get; private set; }

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string, string> OnEvent;          // (eventName, jsonPayload)
        public event Action<string> OnReconnectFailed;        // last reason

        public SocketClient(AppConfig cfg) { _cfg = cfg; }

        /// <summary>Inject the concrete transport at startup (set up by a Unity component).</summary>
        public void AttachTransport(ISocketTransport transport)
        {
            _transport = transport;
            _transport.OnConnected += HandleConnected;
            _transport.OnDisconnected += HandleDisconnected;
            _transport.OnEvent += HandleEvent;
        }

        public async Task ConnectAsync(string token)
        {
            if (_transport == null)
                throw new InvalidOperationException("Socket transport not attached.");
            AuthToken = token;
            await _transport.ConnectAsync(_cfg.socketUrl, token);
        }

        public Task DisconnectAsync()
        {
            StopHeartbeat();
            return _transport?.DisconnectAsync() ?? Task.CompletedTask;
        }

        public void Emit(string eventName, object payload)
        {
            if (_transport == null || !_transport.IsConnected)
            {
                Debug.LogWarning($"[Socket] Drop emit {eventName} (not connected)");
                return;
            }
            try { _transport.Emit(eventName, payload); }
            catch (Exception e) { Debug.LogError($"[Socket] Emit failed: {e}"); }
        }

        public void OnAppPause(bool paused)
        {
            _paused = paused;
            if (paused) StopHeartbeat();
            else if (_transport != null && _transport.IsConnected) StartHeartbeat();
        }

        // ------------ heartbeat ------------

        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatCts = new CancellationTokenSource();
            _ = HeartbeatLoop(_heartbeatCts.Token);
        }

        private void StopHeartbeat()
        {
            try { _heartbeatCts?.Cancel(); } catch { }
            _heartbeatCts = null;
        }

        private async Task HeartbeatLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _transport != null && _transport.IsConnected)
            {
                try
                {
                    var sent = DateTime.UtcNow;
                    var pongTask = WaitForPong(ct);
                    Emit(ClientEvents.Ping, new { t = sent.Ticks });
                    var pong = await Task.WhenAny(pongTask, Task.Delay(_cfg.heartbeatIntervalMs, ct));
                    if (pong != pongTask)
                    {
                        _consecutiveMissed++;
                        if (_consecutiveMissed >= _cfg.maxMissedHeartbeats)
                        {
                            Debug.LogWarning("[Socket] Heartbeat missed → forcing reconnect");
                            await TriggerReconnectAsync("heartbeat_lost");
                            return;
                        }
                    }
                    else _consecutiveMissed = 0;

                    await Task.Delay(_cfg.heartbeatIntervalMs, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e) { Debug.LogError($"[Socket] heartbeat loop: {e}"); break; }
            }
        }

        private TaskCompletionSource<bool> _pongTcs;

        private Task WaitForPong(CancellationToken ct)
        {
            _pongTcs?.TrySetCanceled();
            _pongTcs = new TaskCompletionSource<bool>();
            ct.Register(() => _pongTcs?.TrySetCanceled());
            return _pongTcs.Task;
        }

        // ------------ reconnect ------------

        public async Task TriggerReconnectAsync(string reason)
        {
            if (_paused) return;
            try { await _transport?.DisconnectAsync(); } catch { }

            int delay = _cfg.reconnectInitialDelayMs;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                try
                {
                    await Task.Delay(delay);
                    await _transport.ConnectAsync(_cfg.socketUrl, AuthToken);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Socket] reconnect attempt {attempt+1} failed: {e.Message}");
                    delay = Math.Min(_cfg.reconnectMaxDelayMs, delay * 2);
                }
            }
            OnReconnectFailed?.Invoke(reason);
        }

        // ------------ transport callbacks ------------

        private void HandleConnected()
        {
            _consecutiveMissed = 0;
            StartHeartbeat();
            EventBus.Publish(new ConnectionStateEvent { Connected = true });
            OnConnected?.Invoke();
        }

        private void HandleDisconnected(string reason)
        {
            StopHeartbeat();
            EventBus.Publish(new ConnectionStateEvent { Connected = false, Reason = reason });
            OnDisconnected?.Invoke(reason);
            if (!_paused) _ = TriggerReconnectAsync(reason);
        }

        private void HandleEvent(string ev, string json)
        {
            if (ev == ServerEvents.Pong)
            {
                _pongTcs?.TrySetResult(true);
                return;
            }
            OnEvent?.Invoke(ev, json);
        }
    }

    public struct ConnectionStateEvent
    {
        public bool Connected;
        public string Reason;
    }

    /// <summary>
    /// Pluggable transport contract. Implement once with your chosen
    /// Socket.IO library and register via SocketClient.AttachTransport.
    /// </summary>
    public interface ISocketTransport
    {
        bool IsConnected { get; }
        event Action OnConnected;
        event Action<string> OnDisconnected;
        event Action<string, string> OnEvent; // event name, json payload
        Task ConnectAsync(string url, string token);
        Task DisconnectAsync();
        void Emit(string eventName, object payload);
    }
}
