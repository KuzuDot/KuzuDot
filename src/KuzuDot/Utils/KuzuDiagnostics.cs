using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace KuzuDot.Utils
{
    /// <summary>
    /// Lightweight diagnostics helper for tracking native handle lifetimes.
    /// Intended for tests and optional leak detection; not thread-hot.
    /// </summary>
    internal static class KuzuDiagnostics
    {
        private static readonly ConcurrentDictionary<string, int> _counts = new();

        internal static void HandleCreated(string kind) => _counts.AddOrUpdate(kind, 1, (_, v) => v + 1);
        internal static void HandleDestroyed(string kind) => _counts.AddOrUpdate(kind, 0, (_, v) => v > 0 ? v - 1 : 0);

        /// <summary>
        /// Snapshot of current active handle counts keyed by handle kind.
        /// </summary>
        internal static IReadOnlyDictionary<string, int> GetActiveHandleCounts()
        {
            return new Dictionary<string, int>(_counts);
        }
    }
}
