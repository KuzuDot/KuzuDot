using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace KuzuDot
{
    /// <summary>
    /// Built-in interceptor that records timings for prepare/query/execute and optionally logs them.
    /// Intended for diagnostics and test harnesses; minimal overhead when unsubscribed.
    /// </summary>
    public sealed class TimingLoggingInterceptor : IConnectionInterceptor
    {
        private readonly Action<TimingLogEntry> _sink;
        private readonly ConcurrentDictionary<OperationKind, ConcurrentStack<long>> _stacks = new();
        private readonly ConcurrentQueue<TimingLogEntry> _entries = new();

        public TimingLoggingInterceptor(Action<TimingLogEntry>? sink = null)
        {
            _sink = sink ?? (e => Console.WriteLine($"[KuzuTiming] {e.Operation} Elapsed={e.Elapsed.TotalMilliseconds:F3}ms Success={e.Exception is null} Query={e.Query ?? "<prepared>"}"));
        }

        public enum OperationKind { Prepare, Query, ExecutePrepared }

    public IReadOnlyCollection<TimingLogEntry> SnapshotEntries => _entries.ToArray();

        private void Push(OperationKind k)
        {
            var stack = _stacks.GetOrAdd(k, _ => new ConcurrentStack<long>());
            stack.Push(Stopwatch.GetTimestamp());
        }
        private TimeSpan Pop(OperationKind k, TimeSpan elapsedFromConnection)
        {
            if (elapsedFromConnection != TimeSpan.Zero) return elapsedFromConnection;
            if (_stacks.TryGetValue(k, out var stack) && stack.TryPop(out var start))
            {
                long end = Stopwatch.GetTimestamp();
                return TimeSpan.FromSeconds((end - start) / (double)Stopwatch.Frequency);
            }
            return TimeSpan.Zero;
        }

        public void OnBeforePrepare(Connection connection, string query) => Push(OperationKind.Prepare);
        public void OnAfterPrepare(Connection connection, string query, PreparedStatement? statement, Exception? exception, TimeSpan elapsed)
        {
            var e = new TimingLogEntry(OperationKind.Prepare, query, Pop(OperationKind.Prepare, elapsed), exception);
            _entries.Enqueue(e); _sink(e);
        }

        public void OnBeforeQuery(Connection connection, string query) => Push(OperationKind.Query);
        public void OnAfterQuery(Connection connection, string query, QueryResult? result, Exception? exception, TimeSpan elapsed)
        {
            var e = new TimingLogEntry(OperationKind.Query, query, Pop(OperationKind.Query, elapsed), exception);
            _entries.Enqueue(e); _sink(e);
        }

        public void OnBeforeExecutePrepared(Connection connection, PreparedStatement statement) => Push(OperationKind.ExecutePrepared);
        public void OnAfterExecutePrepared(Connection connection, PreparedStatement statement, QueryResult? result, Exception? exception, TimeSpan elapsed)
        {
            var e = new TimingLogEntry(OperationKind.ExecutePrepared, null, Pop(OperationKind.ExecutePrepared, elapsed), exception);
            _entries.Enqueue(e); _sink(e);
        }
    }
}

namespace KuzuDot
{
    /// <summary>
    /// Public timing log entry for the built-in TimingLoggingInterceptor.
    /// </summary>
    public readonly struct TimingLogEntry : IEquatable<TimingLogEntry>
    {
        public TimingLoggingInterceptor.OperationKind Operation { get; }
        public string? Query { get; }
        public TimeSpan Elapsed { get; }
        public Exception? Exception { get; }
        public TimingLogEntry(TimingLoggingInterceptor.OperationKind operation, string? query, TimeSpan elapsed, Exception? exception)
        {
            Operation = operation;
            Query = query;
            Elapsed = elapsed;
            Exception = exception;
        }
        public override string ToString() => $"{Operation} {Elapsed.TotalMilliseconds:F3}ms {(Exception == null ? "OK" : "ERR" )}";
        public override bool Equals(object? obj) => obj is TimingLogEntry other && Equals(other);
        public bool Equals(TimingLogEntry other) => Operation == other.Operation && Query == other.Query && Elapsed.Equals(other.Elapsed) && Exception == other.Exception;
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ Operation.GetHashCode();
                hash = (hash * 16777619) ^ (Query is null ? 0 : StringComparer.Ordinal.GetHashCode(Query));
                hash = (hash * 16777619) ^ Elapsed.GetHashCode();
                hash = (hash * 16777619) ^ (Exception?.GetHashCode() ?? 0);
                return hash;
            }
        }
        public static bool operator ==(TimingLogEntry left, TimingLogEntry right) => left.Equals(right);
        public static bool operator !=(TimingLogEntry left, TimingLogEntry right) => !left.Equals(right);
    }
}
