using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KuzuDot
{
    /// <summary>
    /// Managed wrapper for a native Kuzu query result.
    /// </summary>
    public sealed class QueryResult : IDisposable
    {
        private readonly QueryResultSafeHandle _handle;

        private string[]? _colNamesOriginal;

        // preserve original case for ToString or external usage
        private string[]? _colNamesUpper;

        // uppercase for case-insensitive match
        private bool _columnNameCacheBuilt;

        public ulong ColumnCount
        {
            get
            {
                var s = AsStruct();
                return NativeMethods.kuzu_query_result_get_num_columns(ref s);
            }
        }

        // Method form kept (examples reference GetErrorMessage).
        public string? ErrorMessage
        {
            get
            {
                var s = AsStruct();
                var ptr = NativeMethods.kuzu_query_result_get_error_message(ref s);
                if (ptr == IntPtr.Zero) return null;
                return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
            }
        }

        public bool IsSuccess
        {
            get
            {
                var s = AsStruct();
                return NativeMethods.kuzu_query_result_is_success(ref s);
            }
        }

        public ulong RowCount
        {
            get
            {
                ThrowIfDisposed();
                var s = AsStruct();
                return NativeMethods.kuzu_query_result_get_num_tuples(ref s);
            }
        }

        internal QueryResult(NativeKuzuQueryResult native)
        {
            KuzuGuard.AssertNotZero(native.QueryResult, $"Native query result pointer is null {nameof(native)}");
            _handle = new QueryResultSafeHandle(native.QueryResult, native.IsOwnedByCpp);
        }

        public void Dispose()
        {
            Dispose(true);
        }

#if DATATYPE
        public DataType GetColumnDataType(ulong index)
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_column_data_type(ref s, index, out var nativeType);
            KuzuGuard.CheckSuccess(state, "Failed to get column data type");
            return new DataType(nativeType);
        }
#endif

        public string GetColumnName(ulong index)
        {
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_column_name(ref s, index, out var namePtr);
            KuzuGuard.CheckSuccess(state, "Failed to get column name");
            return NativeUtil.PtrToStringAndDestroy(namePtr, NativeMethods.kuzu_destroy_string);
        }

        public FlatTuple GetNext()
        {
            ThrowIfDisposed();
            if (!HasNext()) throw new InvalidOperationException("No more tuples available");
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_next(ref s, out var tupleHandle);
            KuzuGuard.CheckSuccess(state, "Failed to get next tuple");
            var ft = new FlatTuple(tupleHandle, this) { Size = ColumnCount };
            KuzuDot.Utils.PerfCounters.IncTuple();
            return ft;
        }

        public QueryResult GetNextQueryResult()
        {
            ThrowIfDisposed();

            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_next_query_result(ref s, out var next);
            KuzuGuard.CheckSuccess(state, "Failed to get next query result");
            return new QueryResult(next);
        }

        public QuerySummary GetQuerySummary()
        {
            ThrowIfDisposed();

            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_query_summary(ref s, out var summaryHandle);
            KuzuGuard.CheckSuccess(state, "Failed to get query summary");
            return new QuerySummary(summaryHandle);
        }

        public bool HasNext()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            return NativeMethods.kuzu_query_result_has_next(ref s);
        }

        public bool HasNextQueryResult()
        {
            ThrowIfDisposed();

            var s = AsStruct();
            return NativeMethods.kuzu_query_result_has_next_query_result(ref s);
        }

        public void ResetIterator()
        {
            ThrowIfDisposed();

            var s = AsStruct();
            NativeMethods.kuzu_query_result_reset_iterator(ref s);
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "QueryResult(Disposed)";
            try
            {
                var s = AsStruct();
                var strPtr = NativeMethods.kuzu_query_result_to_string(ref s);
                var basic = NativeUtil.PtrToStringAndDestroy(strPtr, NativeMethods.kuzu_destroy_string);
                return $"QueryResult(Rows={RowCount}, Cols={ColumnCount})\n" + basic;
            }
            catch (KuzuException)
            {
                return "QueryResult(Error)";
            }
        }

        public bool TryGetArrowSchema(out ArrowSchema schema)
        {
            ThrowIfDisposed();

            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_arrow_schema(ref s, out schema);
            return state == KuzuState.Success;
        }

        public bool TryGetNextArrowChunk(long chunkSize, out ArrowArray array)
        {
            ThrowIfDisposed();

            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_next_arrow_chunk(ref s, chunkSize, out array);
            return state == KuzuState.Success;
        }

        /// <summary>
        /// Returns a lightweight forward-only reader over the rows in this result. Each call to
        /// <see cref="RowReader.MoveNext(out FlatTuple)"/> produces a <see cref="FlatTuple"/> that the caller must dispose.
        /// This is a prototype API intended to enable future pooling / reuse optimizations without changing callers again.
        /// </summary>
        internal RowReader CreateRowReader() => new(this);

        internal bool TryGetOrdinal(string columnName, out ulong ordinal)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(columnName)) { ordinal = 0; return false; }
            BuildColumnNameCache();
            if (_colNamesUpper == null) { ordinal = 0; return false; }

            var target = columnName.ToUpperInvariant();

            for (int i = 0; i < _colNamesUpper.Length; i++)
            {
                if (_colNamesUpper[i] == target)
                {
                    ordinal = (ulong)i; return true;
                }
            }
            ordinal = 0; return false;
        }

        private NativeKuzuQueryResult AsStruct()
        { 
            // TODO: move struct to safehandle.
            return new NativeKuzuQueryResult(_handle.RawHandle, _handle.IsOwnedByCpp);
        }

        private void BuildColumnNameCache()
        {
            if (_columnNameCacheBuilt) return;
            var count = ColumnCount;
            if (count == 0)
            {
                _colNamesOriginal = [];
                _colNamesUpper = [];
                _columnNameCacheBuilt = true; return;
            }
            var originals = new string[count];
            var uppers = new string[count];
            for (ulong i = 0; i < count; i++)
            {
                try
                {
                    var name = GetColumnName(i);
                    originals[i] = name;
                    uppers[i] = name.ToUpperInvariant();
                }
                catch (KuzuException)
                {
                    originals[i] = string.Empty;
                    uppers[i] = string.Empty;
                }
            }
            _colNamesOriginal = originals;
            _colNamesUpper = uppers;
            _columnNameCacheBuilt = true;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                var native = AsStruct();
                NativeMethods.kuzu_query_result_destroy(ref native);
                _handle.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(QueryResult));
        }

        /// <summary>
        /// A lightweight forward-only row reader that avoids allocating an enumerator or capturing lambdas.
        /// Prototype: uses existing <see cref="HasNext"/> and <see cref="GetNext"/> calls under the hood.
        /// Usage:
        /// <code>
        /// var reader = result.GetRowReader();
        /// while (reader.MoveNext(out var row))
        /// {
        ///     using (row) { /* consume values */ }
        /// }
        /// </code>
        /// Rows MUST be disposed by the caller each iteration (mirrors direct GetNext usage).
        /// </summary>
        internal readonly struct RowReader
        {
            private readonly QueryResult _owner;

            internal RowReader(QueryResult owner)
            { _owner = owner; }

            public bool MoveNext(out FlatTuple row)
            {
                if (!_owner.HasNext()) { row = null!; return false; }
                row = _owner.GetNext();
                return true;
            }
        }

        private sealed class QueryResultSafeHandle : KuzuSafeHandle
        {
            internal bool IsOwnedByCpp;

            internal QueryResultSafeHandle(IntPtr ptr, bool isOwnedByCpp) : base("QueryResult")
            {
                IsOwnedByCpp = isOwnedByCpp;
                Initialize(ptr);
            }

            protected override void Release()
            {
            }
        }
    }
}