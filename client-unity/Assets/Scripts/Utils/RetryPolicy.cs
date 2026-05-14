using System;
using System.Threading.Tasks;
using UnityEngine;
using Ludo.Networking;

namespace Ludo.Utils
{
    /// <summary>
    /// Exponential backoff + jitter with a "retryable" predicate.
    /// Used by ApiClient and elsewhere.
    /// </summary>
    public static class RetryPolicy
    {
        public static async Task<T> RunAsync<T>(
            Func<Task<T>> fn,
            int attempts = 3,
            int baseMs = 200,
            int maxMs = 4000)
        {
            Exception last = null;
            for (int i = 0; i < attempts; i++)
            {
                try { return await fn(); }
                catch (ApiException ae) when (!ae.Retryable) { throw; }
                catch (Exception e)
                {
                    last = e;
                    if (i == attempts - 1) break;
                    int wait = (int)(Math.Min(maxMs, baseMs * Math.Pow(2, i)) * (0.5 + UnityEngine.Random.value));
                    await Task.Delay(wait);
                }
            }
            Debug.LogError($"[Retry] gave up after {attempts}: {last?.Message}");
            throw last ?? new Exception("retry_failed");
        }
    }
}
