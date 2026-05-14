using System;
using UnityEngine;

namespace Ludo.Error
{
    /// <summary>
    /// Catches unhandled exceptions and Unity log errors so the app never
    /// silently dies. Forward to your crash reporter (Firebase Crashlytics,
    /// Sentry, BackTrace, etc.) inside <see cref="Report"/>.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceivedThreaded += OnLogMessage;

            Debug.Log("[CrashGuard] installed");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                Report("UnhandledException", ex?.ToString() ?? "unknown");
            } catch { /* never throw from a handler */ }
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            try { Report(type.ToString(), $"{condition}\n{stackTrace}"); }
            catch { /* swallow */ }
        }

        private static void Report(string kind, string message)
        {
            // TODO: forward to crash reporter (Crashlytics, Sentry, etc.).
            // Keep this lightweight — never block the main thread.
            // Example:
            //   FirebaseCrashlytics.Instance.Log(kind);
            //   FirebaseCrashlytics.Instance.LogException(new Exception(message));
            Debug.LogWarning($"[CrashGuard] {kind}: {message}");
        }
    }
}
