using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ludo.Utils
{
    /// <summary>
    /// Wraps async work with try/catch + cancellation + timeout, so a failed
    /// background task never silently kills the app or leaks.
    /// </summary>
    public static class SafeAsync
    {
        public static async void Fire(Func<Task> work, string tag = null)
        {
            try { await work(); }
            catch (OperationCanceledException) { /* expected */ }
            catch (Exception e) { Debug.LogError($"[SafeAsync:{tag}] {e}"); }
        }

        public static async Task<T> WithTimeout<T>(Task<T> task, int timeoutMs, T fallback = default)
        {
            using var cts = new CancellationTokenSource();
            var delay = Task.Delay(timeoutMs, cts.Token);
            var done = await Task.WhenAny(task, delay);
            if (done == task)
            {
                cts.Cancel();
                return await task;
            }
            return fallback;
        }
    }
}
