using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot
{
    /// <summary>
    /// Managed wrapper for Kuzu query summary (compilation & execution timing info).
    /// </summary>
    public sealed class QuerySummary : IDisposable
    {
        private sealed class QuerySummarySafeHandle : KuzuSafeHandle
        {
            internal QuerySummarySafeHandle() : base("QuerySummary") { }
            internal void Init(IntPtr ptr) => Initialize(ptr);
            protected override void Release()
            {
                var native = new NativeKuzuQuerySummary(handle);
                NativeMethods.kuzu_query_summary_destroy(ref native);
            }
        }

        private readonly QuerySummarySafeHandle _handle = new QuerySummarySafeHandle();

        internal QuerySummary(NativeKuzuQuerySummary native)
        {
            _handle.Init(native.QuerySummary);
        }

        public double CompilingTimeMs
        {
            get
            {
                KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(QuerySummary));
                var native = new NativeKuzuQuerySummary(_handle.RawHandle);
                return NativeMethods.kuzu_query_summary_get_compiling_time(ref native);
            }
        }

        public double ExecutionTimeMs
        {
            get
            {
                KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(QuerySummary));
                var native = new NativeKuzuQuerySummary (_handle.RawHandle);
                return NativeMethods.kuzu_query_summary_get_execution_time(ref native);
            }
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "QuerySummary(Disposed)";
            return $"QuerySummary(CompileMs={CompilingTimeMs:F2}, ExecMs={ExecutionTimeMs:F2})";
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}
