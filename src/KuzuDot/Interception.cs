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
        /// <summary>
        /// Called before a statement is prepared.
        /// </summary>
        void OnBeforePrepare(Connection connection, string query);
        /// <summary>
        /// Called after a statement is prepared.
        /// </summary>
        void OnAfterPrepare(Connection connection, string query, PreparedStatement? statement, Exception? exception, TimeSpan elapsed);
        /// <summary>
        /// Called before a query is executed.
        /// </summary>
        void OnBeforeQuery(Connection connection, string query);
        /// <summary>
        /// Called after a query is executed.
        /// </summary>
        void OnAfterQuery(Connection connection, string query, QueryResult? result, Exception? exception, TimeSpan elapsed);
        /// <summary>
        /// Called before a prepared statement is executed.
        /// </summary>
        void OnBeforeExecutePrepared(Connection connection, PreparedStatement statement);
        /// <summary>
        /// Called after a prepared statement is executed.
        /// </summary>
        void OnAfterExecutePrepared(Connection connection, PreparedStatement statement, QueryResult? result, Exception? exception, TimeSpan elapsed);
    }

    /// <summary>
    /// Global interceptor registry (applies to all new and existing connections). Thread-safe.
    /// </summary>
    public static class KuzuInterceptorRegistry
    {
        private static readonly ConcurrentDictionary<IConnectionInterceptor, byte> _global = new();
        internal static IEnumerable<IConnectionInterceptor> Snapshot() => _global.Keys;
        /// <summary>
        /// Registers a global connection interceptor.
        /// </summary>
        /// <param name="interceptor">The interceptor to register.</param>
        public static void Register(IConnectionInterceptor interceptor)
        {
            KuzuGuard.NotNull(interceptor, nameof(interceptor));
            _global[interceptor] = 0;
        }
        /// <summary>
        /// Unregisters a global connection interceptor.
        /// </summary>
        /// <param name="interceptor">The interceptor to unregister.</param>
        /// <returns>True if the interceptor was removed; otherwise, false.</returns>
        public static bool Unregister(IConnectionInterceptor interceptor)
        {
            if (interceptor == null) return false;
            return _global.TryRemove(interceptor, out _);
        }
        /// <summary>
        /// Removes all registered global interceptors.
        /// </summary>
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