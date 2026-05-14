using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Core
{
    /// <summary>
    /// Lightweight typed pub/sub. Use for cross-screen events
    /// (game state updates, ad lifecycle, connection changes).
    /// All callbacks are wrapped in try/catch so a buggy listener
    /// can't take the app down.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subs = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            if (!_subs.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>(4);
                _subs[typeof(T)] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            if (_subs.TryGetValue(typeof(T), out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) _subs.Remove(typeof(T));
            }
        }

        public static void Publish<T>(T evt)
        {
            if (!_subs.TryGetValue(typeof(T), out var list)) return;
            // copy to allow unsubscribe during dispatch
            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { ((Action<T>)snapshot[i])(evt); }
                catch (Exception e) { Debug.LogError($"[EventBus] Listener threw: {e}"); }
            }
        }

        public static void Clear() => _subs.Clear();
    }
}
