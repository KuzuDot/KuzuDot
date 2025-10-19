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

        /// <summary>
        /// Gets the number of columns in the result set.
        /// </summary>
        public ulong ColumnCount
        {
            get
            {
                return NativeMethods.kuzu_query_result_get_num_columns(ref _handle.NativeStruct);
            }
        }

        // Method form kept (examples reference GetErrorMessage).
        /// <summary>
        /// Gets the error message if the query failed.
        /// </summary>
        public string? ErrorMessage
        {
            get
            {
                var ptr = NativeMethods.kuzu_query_result_get_error_message(ref _handle.NativeStruct);
                if (ptr == IntPtr.Zero) return null;
                return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the query was successful.
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return NativeMethods.kuzu_query_result_is_success(ref _handle.NativeStruct);
            }
        }

        /// <summary>
        /// Gets the number of rows in the result set.
        /// </summary>
        public ulong RowCount
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.kuzu_query_result_get_num_tuples(ref _handle.NativeStruct);
            }
        }

        internal QueryResult(NativeKuzuQueryResult native)
        {
            KuzuGuard.AssertNotZero(native.QueryResult, $"Native query result pointer is null {nameof(native)}");
            _handle = new QueryResultSafeHandle(native.QueryResult, native.IsOwnedByCpp);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="QueryResult"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

#if DATATYPE
        public DataType GetColumnDataType(ulong index)
        {
            ThrowIfDisposed();
            var state = NativeMethods.kuzu_query_result_get_column_data_type(ref _handle.NativeStruct, index, out var nativeType);
            KuzuGuard.CheckSuccess(state, "Failed to get column data type");
            return new DataType(nativeType);
        }
#endif

        /// <summary>
        /// Gets the name of the column at the specified index.
        /// </summary>
        /// <param name="index">The column index.</param>
        /// <returns>The column name.</returns>
        public string GetColumnName(ulong index)
        {
            var state = NativeMethods.kuzu_query_result_get_column_name(ref _handle.NativeStruct, index, out var namePtr);
            KuzuGuard.CheckSuccess(state, "Failed to get column name");
            return NativeUtil.PtrToStringAndDestroy(namePtr, NativeMethods.kuzu_destroy_string);
        }

        /// <summary>
        /// Gets the next row in the result set as a <see cref="FlatTuple"/>.
        /// </summary>
        /// <returns>The next <see cref="FlatTuple"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there are no more rows.</exception>
        public FlatTuple GetNext()
        {
            ThrowIfDisposed();
            if (!HasNext()) throw new InvalidOperationException("No more tuples available");
            var state = NativeMethods.kuzu_query_result_get_next(ref _handle.NativeStruct, out var tupleHandle);
            KuzuGuard.CheckSuccess(state, "Failed to get next tuple");
            var ft = new FlatTuple(tupleHandle, this) { Size = ColumnCount };
            KuzuDot.Utils.PerfCounters.IncTuple();
            return ft;
        }

        public QueryResult GetNextQueryResult()
        {
            ThrowIfDisposed();

            var state = NativeMethods.kuzu_query_result_get_next_query_result(ref _handle.NativeStruct, out var next);
            KuzuGuard.CheckSuccess(state, "Failed to get next query result");
            return new QueryResult(next);
        }

        public QuerySummary GetQuerySummary()
        {
            ThrowIfDisposed();

            var state = NativeMethods.kuzu_query_result_get_query_summary(ref _handle.NativeStruct, out var summaryHandle);
            KuzuGuard.CheckSuccess(state, "Failed to get query summary");
            return new QuerySummary(summaryHandle);
        }

        /// <summary>
        /// Determines whether there are more rows available in the result set.
        /// </summary>
        /// <returns>True if there are more rows; otherwise, false.</returns>
        public bool HasNext()
        {
            ThrowIfDisposed();
            return NativeMethods.kuzu_query_result_has_next(ref _handle.NativeStruct);
        }

        public bool HasNextQueryResult()
        {
            ThrowIfDisposed();

            return NativeMethods.kuzu_query_result_has_next_query_result(ref _handle.NativeStruct);
        }

        public void ResetIterator()
        {
            ThrowIfDisposed();
            NativeMethods.kuzu_query_result_reset_iterator(ref _handle.NativeStruct);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_handle.IsInvalid) return "QueryResult(Disposed)";
            try
            {
                var strPtr = NativeMethods.kuzu_query_result_to_string(ref _handle.NativeStruct);
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

            var state = NativeMethods.kuzu_query_result_get_arrow_schema(ref _handle.NativeStruct, out schema);
            return state == KuzuState.Success;
        }

        public bool TryGetNextArrowChunk(long chunkSize, out ArrowArray array)
        {
            ThrowIfDisposed();

            var state = NativeMethods.kuzu_query_result_get_next_arrow_chunk(ref _handle.NativeStruct, chunkSize, out array);
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
                if (_handle.IsInvalid) return;
                NativeMethods.kuzu_query_result_destroy(ref _handle.NativeStruct);
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
            internal NativeKuzuQueryResult NativeStruct;

            internal QueryResultSafeHandle(IntPtr ptr, bool isOwnedByCpp) : base("QueryResult")
            {
                NativeStruct = new NativeKuzuQueryResult(ptr, isOwnedByCpp);
                Initialize(ptr);
            }

            protected override void Release()
            {
            }
        }
    }
}