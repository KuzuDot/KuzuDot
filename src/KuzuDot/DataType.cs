using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Diagnostics;

#if INCLUDE_DATATYPE

namespace KuzuDot
{
    /// <summary>
    /// Managed wrapper around a logical (data) type in the native engine.
    /// </summary>
    public sealed class DataType : IDisposable, IEquatable<DataType>
    {
        private readonly DataTypeSafeHandle _handle;

        /// <summary>If this represents an ARRAY type returns element count, else null.</summary>
        public ulong? ArrayNumElements
        {
            get
            {
                ThrowIfDisposed();
                var state = NativeMethods.kuzu_data_type_get_num_elements_in_array(ref _handle.NativeStruct, out var n);
                return state == KuzuState.Success ? n : null;
            }
        }

        /// <summary>Raw underlying id (engine specific).</summary>
        public KuzuDataTypeId Id
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.kuzu_data_type_get_id(ref _handle.NativeStruct);
            }
        }

        internal DataType(NativeKuzuLogicalType native)
        { _handle = new(native); }

        public static bool operator !=(DataType? a, DataType? b) => !(a == b);

        public static bool operator ==(DataType? a, DataType? b) => ReferenceEquals(a, b) || a is not null && a.Equals(b);

        public DataType Clone()
        {
            ThrowIfDisposed();
            NativeMethods.kuzu_data_type_clone(ref _handle.NativeStruct, out var clone);
            return new DataType(clone);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        public bool Equals(DataType? other)
        {
            if (other is null) return false;
            ThrowIfDisposed();
            other.ThrowIfDisposed();
            return NativeMethods.kuzu_data_type_equals(ref _handle.NativeStruct, ref other._handle.NativeStruct);
        }

        public override bool Equals(object? obj) => obj is DataType dt && Equals(dt);

        public override int GetHashCode() => (int)Id;

        public override string ToString()
        {
            if (_handle.IsInvalid) return "DataType(Disposed)";
            return "DataType(Id=" + Id + (ArrayNumElements is ulong n ? ",Elements=" + n : string.Empty) + ")";
        }

        /// <summary>Creates a cloned copy of the underlying native logical type.</summary>
        internal static DataType FromBorrowed(in NativeKuzuLogicalType native)
        {
            var copySrc = native; // need mutable ref for P/Invoke
            NativeMethods.kuzu_data_type_clone(ref copySrc, out var clone);
            return new DataType(clone);
        }

        
        private void ThrowIfDisposed()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(DataType));
        }

        private sealed class DataTypeSafeHandle : KuzuSafeHandle
        {
            internal NativeKuzuLogicalType NativeStruct;

            internal DataTypeSafeHandle(NativeKuzuLogicalType native) : base("DataType")
            {
                NativeStruct = native;
                Initialize(native.DataType);
            }

            protected override void Release()
            {
                NativeMethods.kuzu_data_type_destroy(ref NativeStruct);
                NativeStruct = default;
            }
        }
    }
}

#endif