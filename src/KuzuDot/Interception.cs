using KuzuDot.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace KuzuDot
{
    /// <summary>
    /// Interceptors can observe and instrument core connection lifecycle events (prepare/query/execute).
    /// Exceptions thrown by interceptors are swallowed to avoid impacting primary execution flow.
    /// </summary>
    public interface IConnectionInterceptor
    {
        void OnBeforePrepare(Connection connection, string query);
        void OnAfterPrepare(Connection connection, string query, PreparedStatement? statement, Exception? exception, TimeSpan elapsed);
        void OnBeforeQuery(Connection connection, string query);
        void OnAfterQuery(Connection connection, string query, QueryResult? result, Exception? exception, TimeSpan elapsed);
        void OnBeforeExecutePrepared(Connection connection, PreparedStatement statement);
        void OnAfterExecutePrepared(Connection connection, PreparedStatement statement, QueryResult? result, Exception? exception, TimeSpan elapsed);
    }

    /// <summary>
    /// Global interceptor registry (applies to all new and existing connections). Thread-safe.
    /// </summary>
    public static class KuzuInterceptorRegistry
    {
        private static readonly ConcurrentDictionary<IConnectionInterceptor, byte> _global = new();
        internal static IEnumerable<IConnectionInterceptor> Snapshot() => _global.Keys;
        public static void Register(IConnectionInterceptor interceptor)
        {
            KuzuGuard.NotNull(interceptor, nameof(interceptor));
            _global[interceptor] = 0;
        }
        public static bool Unregister(IConnectionInterceptor interceptor)
        {
            if (interceptor == null) return false;
            return _global.TryRemove(interceptor, out _);
        }
        public static void Clear() => _global.Clear();
        internal static bool HasGlobal => !_global.IsEmpty;
    }

    internal static class InterceptorSafeExecutor
    {
#pragma warning disable CA1031 // Intentional: interceptor failures must not affect main execution path
        internal static void Invoke(Action action)
        {
            try { action(); } catch (Exception) { }
        }
#pragma warning restore CA1031
    }
}