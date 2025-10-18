using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace KuzuDot.Value
{
    /// <summary>
    /// Abstract base class for all Kuzu values. Provides common lifetime, null handling,
    /// cloning and string conversion. Individual concrete subclasses expose only the
    /// operations valid for their logical type.
    /// </summary>
    public abstract class KuzuValue : IDisposable
    {
        private readonly KuzuValueSafeHandle _handle = new();

        private readonly object _lockObject = new();

        /// <summary>
        /// Kuzu logical type id of this value.
        /// </summary>
        public KuzuDataTypeId DataTypeId
        {
            get
            {
                ThrowIfDisposed();
                NativeMethods.kuzu_value_get_data_type(ref Handle.NativeStruct, out var t);
                return NativeMethods.kuzu_data_type_get_id(ref t);
            }
        }

        internal KuzuValueSafeHandle Handle => _handle;

        internal IntPtr NativePtr => _handle.DangerousGetHandle();

        internal KuzuValue(IntPtr ptr)
        {
            _handle.Init(ptr);
        }

        internal KuzuValue(NativeKuzuValue nativeStruct)
        {
            _handle.Init(nativeStruct);
        }

        public KuzuValue Clone()
        {
            lock (_lockObject)
            {
                ThrowIfDisposed();
                var clonePtr = NativeMethods.kuzu_value_clone(ref Handle.NativeStruct);
                KuzuGuard.AssertNotZero(clonePtr, "Failed to clone value");
                return WrapOwned(clonePtr);
            }
        }

        public void CopyFrom(KuzuValue other)
        {
            KuzuGuard.NotNull(other, nameof(other));
            lock (_lockObject)
            {
                ThrowIfDisposed();
                other.ThrowIfDisposed();
                if (DataTypeId != other.DataTypeId)
                    throw new ArgumentException("Cannot copy from value of different type", nameof(other));
                NativeMethods.kuzu_value_copy(ref Handle.NativeStruct, other.NativePtr);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsNull()
        {
            ThrowIfDisposed();
            return NativeMethods.kuzu_value_is_null(ref Handle.NativeStruct);
        }

        public void SetNull(bool isNull)
        {
            lock (_lockObject)
            {
                ThrowIfDisposed();
                NativeMethods.kuzu_value_set_null(ref Handle.NativeStruct, isNull);
            }
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "[Invalid KuzuValue]";
            ThrowIfDisposed();
            var ptr = NativeMethods.kuzu_value_to_string(ref Handle.NativeStruct);
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        internal static object ConvertKuzuValue(Type target, KuzuValue value)
        {
            // Handle nullable types first
            var underlyingType = System.Nullable.GetUnderlyingType(target);
            if (underlyingType != null)
            {
                // Convert to the underlying type first
                var converted = ConvertKuzuValue(underlyingType, value);
                // If the value is null, return null for nullable type
                if (value.IsNull())
                    return null!;
                return converted;
            }

            // Order common primitives first; extend as needed
            switch (value)
            {
                case KuzuInt8 v when target == typeof(sbyte) || target == typeof(object): return v.Value;
                case KuzuInt16 v when target == typeof(short) || target == typeof(object): return v.Value;
                case KuzuInt32 v when target == typeof(int) || target == typeof(object): return v.Value;
                case KuzuInt64 v when target == typeof(long) || target == typeof(object): return v.Value;
                case KuzuUInt32 v when target == typeof(uint) || target == typeof(object): return v.Value;
                case KuzuUInt64 v when target == typeof(ulong) || target == typeof(object): return v.Value;
                case KuzuDouble v when target == typeof(double) || target == typeof(object): return v.Value;
                case KuzuFloat v when target == typeof(float) || target == typeof(object): return v.Value;
                case KuzuBool v when target == typeof(bool) || target == typeof(object): return v.Value;
                case KuzuString v when target == typeof(string) || target == typeof(object): return v.Value;
                case KuzuDate v when target == typeof(DateTime) || target == typeof(object): return v.AsDateTime();
#if NET8_0_OR_GREATER
                case KuzuDate v when target == typeof(DateOnly) || target == typeof(object): return v.AsDateOnly();
#endif
                case KuzuTimestamp v when target == typeof(DateTime) || target == typeof(object): return v.Value;
                case KuzuInterval v when target == typeof(TimeSpan) || target == typeof(object): return v.Value;
            }
            // Fallback: attempt ChangeType on string/primitive representation
            var str = value.ToString();
            return Convert.ChangeType(str, target, System.Globalization.CultureInfo.InvariantCulture)!;
        }

        //private static KuzuValue FromBorrowed(NativeKuzuValue raw)
        //{
        //    ////Make a shallow copy of the kuzu_value; C++ will destroy this
        //    //int size = Marshal.SizeOf<NativeKuzuValue>();
        //    //var wrapperPtr = Marshal.AllocHGlobal(size);
        //    //Marshal.StructureToPtr(raw, wrapperPtr, false);
        //    return WrapNativeStruct(raw); // new(wrapperPtr, false));
        //}

        internal static KuzuValue FromNativePtr(IntPtr raw)
        {
            return WrapOwned(raw);
        }

        internal static KuzuValue FromNativeStruct(NativeKuzuValue raw)
        {
            return WrapNativeStruct(raw);
            //return (!raw.IsOwnedByCpp) ? WrapOwned(raw) : FromBorrowed(raw);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handle.Dispose();
            }
        }

        ////public bool Is<T>() where T : struct
        ////{
        ////    return this.DataTypeId switch
        ////    {
        ////        KuzuDataTypeId.KuzuBool when typeof(T) == typeof(bool) => true,
        ////        KuzuDataTypeId.KuzuInt8 when typeof(T) == typeof(sbyte) => true,
        ////        KuzuDataTypeId.KuzuInt16 when typeof(T) == typeof(short) => true,
        ////        KuzuDataTypeId.KuzuInt32 when typeof(T) == typeof(int) => true,
        ////        KuzuDataTypeId.KuzuInt64 when typeof(T) == typeof(long) => true,
        ////        KuzuDataTypeId.KuzuUInt8 when typeof(T) == typeof(byte) => true,
        ////        KuzuDataTypeId.KuzuUInt16 when typeof(T) == typeof(ushort) => true,
        ////        KuzuDataTypeId.KuzuUInt32 when typeof(T) == typeof(uint) => true,
        ////        KuzuDataTypeId.KuzuUInt64 when typeof(T) == typeof(ulong) => true,
        ////        KuzuDataTypeId.KuzuFloat when typeof(T) == typeof(float) => true,
        ////        KuzuDataTypeId.KuzuDouble when typeof(T) == typeof(double) => true,
        ////        KuzuDataTypeId.KuzuDecimal when typeof(T) == typeof(decimal) => true,
        ////        KuzuDataTypeId.KuzuInt128 when typeof(T) == typeof(System.Numerics.BigInteger) => true,
        ////        KuzuDataTypeId.KuzuDate when typeof(T) == typeof(DateTime) => true, // TODO: if .net 2.0 vs 8 logic
        ////        KuzuDataTypeId.KuzuTimestamp when typeof(T) == typeof(DateTime) => true,
        ////        KuzuDataTypeId.KuzuInterval when typeof(T) == typeof(TimeSpan) => true,
        ////        KuzuDataTypeId.KuzuString when typeof(T) == typeof(string) => true,
        ////        KuzuDataTypeId.KuzuUUID when typeof(T) == typeof(UUID) => true,
        ////        _ => false,
        ////    };
        ////}

        protected void ThrowIfDisposed()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, GetType().Name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller must dispose")]
        private static KuzuValue WrapNativeStruct(NativeKuzuValue nativeStruct)
        {
            if (nativeStruct.Value == IntPtr.Zero) return new KuzuAny(nativeStruct); // fallback
            NativeMethods.kuzu_value_get_data_type(ref nativeStruct, out var tNative);
            var id = NativeMethods.kuzu_data_type_get_id(ref tNative);
            KuzuValue v = id switch
            {
                KuzuDataTypeId.KuzuBool => new KuzuBool(nativeStruct),
                KuzuDataTypeId.KuzuInt8 => new KuzuInt8(nativeStruct),
                KuzuDataTypeId.KuzuInt16 => new KuzuInt16(nativeStruct),
                KuzuDataTypeId.KuzuInt32 => new KuzuInt32(nativeStruct),
                KuzuDataTypeId.KuzuInt64 => new KuzuInt64(nativeStruct),
                KuzuDataTypeId.KuzuUInt8 => new KuzuUInt8(nativeStruct),
                KuzuDataTypeId.KuzuUInt16 => new KuzuUInt16(nativeStruct),
                KuzuDataTypeId.KuzuUInt32 => new KuzuUInt32(nativeStruct),
                KuzuDataTypeId.KuzuUInt64 => new KuzuUInt64(nativeStruct),
                KuzuDataTypeId.KuzuFloat => new KuzuFloat(nativeStruct),
                KuzuDataTypeId.KuzuDouble => new KuzuDouble(nativeStruct),
                KuzuDataTypeId.KuzuInt128 => new KuzuInt128(nativeStruct),
                KuzuDataTypeId.KuzuDate => new KuzuDate(nativeStruct),
                KuzuDataTypeId.KuzuTimestamp => new KuzuTimestamp(nativeStruct),
                KuzuDataTypeId.KuzuTimestampNs => new KuzuTimestampNs(nativeStruct),
                KuzuDataTypeId.KuzuTimestampMs => new KuzuTimestampMs(nativeStruct),
                KuzuDataTypeId.KuzuTimestampSec => new KuzuTimestampSec(nativeStruct),
                KuzuDataTypeId.KuzuTimestampTz => new KuzuTimestampTz(nativeStruct),
                KuzuDataTypeId.KuzuInterval => new KuzuInterval(nativeStruct),
                KuzuDataTypeId.KuzuInternalId => new KuzuInternalId(nativeStruct),
                KuzuDataTypeId.KuzuString => new KuzuString(nativeStruct),
                KuzuDataTypeId.KuzuBlob => new KuzuBlob(nativeStruct),
                KuzuDataTypeId.KuzuList => new KuzuList(nativeStruct),
                KuzuDataTypeId.KuzuStruct => new KuzuStruct(nativeStruct),
                KuzuDataTypeId.KuzuMap => new KuzuMap(nativeStruct),
                KuzuDataTypeId.KuzuArray => new KuzuArray(nativeStruct),
                KuzuDataTypeId.KuzuNode => new KuzuNode(nativeStruct),
                KuzuDataTypeId.KuzuRel => new KuzuRel(nativeStruct),
                KuzuDataTypeId.KuzuRecursiveRel => new KuzuRecursiveRel(nativeStruct),
                KuzuDataTypeId.KuzuUUID => new KuzuUUID(nativeStruct),
                _ => new KuzuAny(nativeStruct)
            };
            PerfCounters.IncValueWrapper();
            return v;
        }

        private static KuzuValue WrapOwned(IntPtr structPtr)
        {
            KuzuGuard.AssertNotZero(structPtr, "Failed to create value (null native pointer)");
            var temp = Marshal.PtrToStructure<NativeKuzuValue>(structPtr);
            Marshal.FreeHGlobal(structPtr);
            return WrapNativeStruct(temp);
        }

        internal sealed class KuzuValueSafeHandle : KuzuSafeHandle
        {
            internal NativeKuzuValue NativeStruct;

            public override bool IsInvalid => NativeStruct.Value == IntPtr.Zero;

            internal KuzuValueSafeHandle() : base("KuzuValue")
            {
            }

            public void Init(IntPtr nativePtr)
            {
                if (!IsInvalid) throw new InvalidOperationException("Handle is already initialized");
                NativeStruct = Marshal.PtrToStructure<NativeKuzuValue>(nativePtr);
                Initialize(nativePtr);
            }

            public void Init(NativeKuzuValue nativeStruct)
            {
                if (!IsInvalid) throw new InvalidOperationException("Handle is already initialized");
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeKuzuValue>());
                Marshal.StructureToPtr(nativeStruct, ptr, false);
                NativeStruct = Marshal.PtrToStructure<NativeKuzuValue>(ptr);
                Initialize(ptr);
            }

            protected override void Release()
            {
                if (!NativeStruct.IsOwnedByCpp)
                {
                    NativeMethods.kuzu_value_destroy(handle);
                }
                NativeStruct = default;
            }
        }
    }
}