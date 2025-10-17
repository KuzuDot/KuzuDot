using KuzuDot.Native;
using KuzuDot.Utils;
using KuzuDot.Value;
using System;

namespace KuzuDot
{
    /// <summary>
    /// Represents a flat tuple (row) from a query result.
    /// </summary>
    public sealed class FlatTuple : IDisposable
    {
        private readonly FlatTupleSafeHandle _handle;

        private readonly QueryResult? _owner;

        /// <summary>Number of values in this tuple.</summary>
        public ulong Size { get; internal set; }

        internal FlatTuple(NativeKuzuFlatTuple native, QueryResult? owner = null)
        {
            _handle = new FlatTupleSafeHandle(native);
            _owner = owner;
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        /// <summary>
        /// Gets the value at the specified index (returned instance is an appropriate concrete subclass of KuzuValue).
        /// </summary>
        public KuzuValue GetValue(ulong index)
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(FlatTuple));
            var state = NativeMethods.kuzu_flat_tuple_get_value(ref _handle.NativeStruct, index, out var kuzuValue);
            KuzuGuard.CheckSuccess(state, $"Failed to get value at index {index}. Native result: {state}");
            KuzuGuard.AssertNotZero(kuzuValue.Value, $"Retrieved null handle for value at index {index}");
            return KuzuValue.FromNativeStruct(kuzuValue);
        }

        /// <summary>
        /// Gets a disposable KuzuValue by column name (case-insensitive). Caller must dispose the returned value.
        /// </summary>
        public KuzuValue GetValue(string columnName)
        {
            if (_owner == null) throw new InvalidOperationException("Name-based access requires owner QueryResult context.");
            if (!_owner.TryGetOrdinal(columnName, out var ord)) throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            return GetValue(ord);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP017:Prefer using", Justification = "Disposal handled by caller")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP016:Don't use disposed instance", Justification = "Disposal handled by caller")]
        public TValue GetValue<TValue>(ulong index) where TValue : KuzuValue
        {
            var val = GetValue(index);
            if (val is TValue typed)
                return typed; // Caller must dispose
            val.Dispose();
            throw new InvalidCastException($"Value at index {index} is not of type {typeof(TValue).Name} (actual type: {val.GetType().Name})");
        }

        public TValue GetValue<TValue>(string columnName) where TValue : KuzuValue
        {
            if (_owner == null) throw new InvalidOperationException("Name-based access requires owner QueryResult context.");
            if (!_owner.TryGetOrdinal(columnName, out var ord)) throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            return GetValue<TValue>(ord);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP017:Prefer using", Justification = "Disposal handled by caller unless there's an exception")]
        public T GetValueAs<T>(ulong index)
        {
            using var val = GetValue(index);
            if (val is KuzuTypedValue<T> scalar)
                return scalar.Value;
            throw new InvalidCastException($"Value at index {index} is not a scalar of type {typeof(T).Name}.");
        }

        public T GetValueAs<T>(string columnName)
        {
            if (_owner == null) throw new InvalidOperationException("Name-based access requires owner QueryResult context.");
            if (!_owner.TryGetOrdinal(columnName, out var ord)) throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            return GetValueAs<T>(ord);
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "FlatTuple(Disposed)";
            var strPtr = NativeMethods.kuzu_flat_tuple_to_string(ref _handle.NativeStruct);
            var row = NativeUtil.PtrToStringAndDestroy(strPtr, NativeMethods.kuzu_destroy_string);
            return $"FlatTuple(Size={Size}) " + row;
        }

        /// <summary>
        /// Attempts to get a value by column name; returns false if not found.
        /// </summary>
        public bool TryGetValue(string columnName, out KuzuValue? value)
        {
            value = null;
            if (_owner == null) return false;
            if (!_owner.TryGetOrdinal(columnName, out var ord)) return false;
            value = GetValue(ord);
            return true;
        }

        public bool TryGetValueAs<T>(string columnName, out T value)
        {
            value = default!;
            if (_owner == null) return false;
            if (!_owner.TryGetOrdinal(columnName, out var ord)) return false;
            try
            {
                value = GetValueAs<T>(ord);
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        private sealed class FlatTupleSafeHandle : KuzuSafeHandle
        {
            internal NativeKuzuFlatTuple NativeStruct;

            internal FlatTupleSafeHandle(NativeKuzuFlatTuple native) : base("FlatTuple")
            {
                NativeStruct = native;
                Initialize(native.FlatTuple);
            }

            protected override void Release()
            {
                if (!NativeStruct.IsOwnedByCpp)
                {
                    NativeMethods.kuzu_flat_tuple_destroy(ref NativeStruct);
                    NativeStruct = default;
                }
            }
        }
    }
}