using System;
using System.Collections.Generic;

namespace Ludo.Core
{
    /// <summary>
    /// Tiny service locator. Used to wire Managers without static singletons
    /// scattered across the codebase. Registered once in Bootstrap.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _services[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var v)) return (T)v;
            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public static bool TryGet<T>(out T value) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var v))
            {
                value = (T)v;
                return true;
            }
            value = null;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}
