using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using KuzuDot.Value;
using System;


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

        public static string GetNameFromType(KuzuDataTypeId dataTypeId)
        {
            return dataTypeId switch
            {
                KuzuDataTypeId.KuzuAny => "ANY",
                KuzuDataTypeId.KuzuSerial => "SERIAL",
                KuzuDataTypeId.KuzuInternalId => "INTERNAL_ID", // Not sure?
                KuzuDataTypeId.KuzuUUID => "UUID",
                KuzuDataTypeId.KuzuBool => "BOOL",
                KuzuDataTypeId.KuzuInt64 => "INT64",
                KuzuDataTypeId.KuzuInt32 => "INT32",
                KuzuDataTypeId.KuzuInt16 => "INT16",
                KuzuDataTypeId.KuzuInt8 => "INT8",
                KuzuDataTypeId.KuzuUInt64 => "UINT64",
                KuzuDataTypeId.KuzuUInt32 => "UINT32",
                KuzuDataTypeId.KuzuUInt16 => "UINT16",
                KuzuDataTypeId.KuzuUInt8 => "UINT8",
                KuzuDataTypeId.KuzuFloat => "FLOAT",
                KuzuDataTypeId.KuzuDouble => "DOUBLE",
                KuzuDataTypeId.KuzuInt128 => "INT128",
                KuzuDataTypeId.KuzuDate => "DATE",
                KuzuDataTypeId.KuzuTimestamp => "TIMESTAMP",
                KuzuDataTypeId.KuzuInterval => "INTERVAL",
                KuzuDataTypeId.KuzuString => "STRING",
                KuzuDataTypeId.KuzuBlob => "BLOB",
                KuzuDataTypeId.KuzuNode => "NODE",
                KuzuDataTypeId.KuzuList => "LIST",
                KuzuDataTypeId.KuzuStruct => "STRUCT",
                KuzuDataTypeId.KuzuMap => "MAP",
                KuzuDataTypeId.KuzuArray => "ARRAY",
                KuzuDataTypeId.KuzuRel => "REL",
                KuzuDataTypeId.KuzuRecursiveRel => "RECURSIVE_REL", // Not sure
                KuzuDataTypeId.KuzuUnion => "UNION",
                KuzuDataTypeId.KuzuPointer => "POINTER",
                KuzuDataTypeId.KuzuTimestampSec => "TIMESTAMP_SEC", // Not sure
                KuzuDataTypeId.KuzuTimestampMs => "TIMESTAMP_MS",   // Not sure
                KuzuDataTypeId.KuzuTimestampNs => "TIMESTAMP_NS",   // Not sure
                KuzuDataTypeId.KuzuTimestampTz => "TIMESTAMP_TZ",   // Not sure
                _ => "UNKNOWN"
            };
        }

        public static KuzuDataTypeId GetDataTypeFromName(string name)
        {
            return name?.ToUpperInvariant() switch
            {
                "ANY" => KuzuDataTypeId.KuzuAny,
                "SERIAL" => KuzuDataTypeId.KuzuSerial,
                "INTERNAL_ID" => KuzuDataTypeId.KuzuInternalId,
                "UUID" => KuzuDataTypeId.KuzuUUID,
                "BOOL" => KuzuDataTypeId.KuzuBool,
                "INT64" => KuzuDataTypeId.KuzuInt64,
                "INT32" => KuzuDataTypeId.KuzuInt32,
                "INT16" => KuzuDataTypeId.KuzuInt16,
                "INT8" => KuzuDataTypeId.KuzuInt8,
                "UINT64" => KuzuDataTypeId.KuzuUInt64,
                "UINT32" => KuzuDataTypeId.KuzuUInt32,
                "UINT16" => KuzuDataTypeId.KuzuUInt16,
                "UINT8" => KuzuDataTypeId.KuzuUInt8,
                "FLOAT" => KuzuDataTypeId.KuzuFloat,
                "DOUBLE" => KuzuDataTypeId.KuzuDouble,
                "INT128" => KuzuDataTypeId.KuzuInt128,
                "DATE" => KuzuDataTypeId.KuzuDate,
                "TIMESTAMP" => KuzuDataTypeId.KuzuTimestamp,
                "INTERVAL" => KuzuDataTypeId.KuzuInterval,
                "STRING" => KuzuDataTypeId.KuzuString,
                "BLOB" => KuzuDataTypeId.KuzuBlob,
                "NODE" => KuzuDataTypeId.KuzuNode,
                "LIST" => KuzuDataTypeId.KuzuList,
                "STRUCT" => KuzuDataTypeId.KuzuStruct,
                "MAP" => KuzuDataTypeId.KuzuMap,
                "ARRAY" => KuzuDataTypeId.KuzuArray,
                "REL" => KuzuDataTypeId.KuzuRel,
                "RECURSIVE_REL" => KuzuDataTypeId.KuzuRecursiveRel,
                "UNION" => KuzuDataTypeId.KuzuUnion,
                "POINTER" => KuzuDataTypeId.KuzuPointer,
                "TIMESTAMP_SEC" => KuzuDataTypeId.KuzuTimestampSec,
                "TIMESTAMP_MS" => KuzuDataTypeId.KuzuTimestampMs,
                "TIMESTAMP_NS" => KuzuDataTypeId.KuzuTimestampNs,
                "TIMESTAMP_TZ" => KuzuDataTypeId.KuzuTimestampTz,
                _ => throw new ArgumentException($"Unknown data type name: {name}", nameof(name))
            };
        }

        internal static KuzuDataTypeId GetIdFromType(Type type)
        {
            return type switch 
            {
                Type t when t == typeof(bool) => KuzuDataTypeId.KuzuBool,
                Type t when t == typeof(sbyte) => KuzuDataTypeId.KuzuInt8,
                Type t when t == typeof(short) => KuzuDataTypeId.KuzuInt16,
                Type t when t == typeof(int) => KuzuDataTypeId.KuzuInt32,
                Type t when t == typeof(long) => KuzuDataTypeId.KuzuInt64,
                Type t when t == typeof(byte) => KuzuDataTypeId.KuzuUInt8,
                Type t when t == typeof(ushort) => KuzuDataTypeId.KuzuUInt16,
                Type t when t == typeof(uint) => KuzuDataTypeId.KuzuUInt32,
                Type t when t == typeof(ulong) => KuzuDataTypeId.KuzuUInt64,
                Type t when t == typeof(float) => KuzuDataTypeId.KuzuFloat,
                Type t when t == typeof(double) => KuzuDataTypeId.KuzuDouble,
                Type t when t == typeof(string) => KuzuDataTypeId.KuzuString,
                Type t when t == typeof(byte[]) => KuzuDataTypeId.KuzuBlob,
                Type t when t == typeof(UUID) => KuzuDataTypeId.KuzuUUID,
#if NET8_0_OR_GREATER
                Type t when t == typeof(DateOnly) => KuzuDataTypeId.KuzuDate,
#endif
                Type t when t == typeof(DateTime) => KuzuDataTypeId.KuzuTimestamp,
                _ => throw new ArgumentException($"Unsupported CLR type for mapping to Kuzu data type: {type.FullName}", nameof(type))
            };
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
