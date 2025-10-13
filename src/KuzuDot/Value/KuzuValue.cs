using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
                NativeMethods.kuzu_value_get_data_type(NativePtr, out var t);
                return NativeMethods.kuzu_data_type_get_id(ref t);
            }
        }

        internal KuzuValueSafeHandle Handle => _handle;

        internal IntPtr NativePtr => _handle.DangerousGetHandle();

        internal KuzuValue(NativeKuzuValue n)
        {
            _handle.Init(n);
        }

        public KuzuValue Clone()
        {
            lock (_lockObject)
            {
                ThrowIfDisposed();
                var clonePtr = NativeMethods.kuzu_value_clone(_handle);
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
                NativeMethods.kuzu_value_copy(_handle, other.Handle);
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
            return NativeMethods.kuzu_value_is_null(_handle);
        }

        public void SetNull(bool isNull)
        {
            lock (_lockObject)
            {
                ThrowIfDisposed();
                NativeMethods.kuzu_value_set_null(_handle, isNull);
            }
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "[Invalid KuzuValue]";
            ThrowIfDisposed();
            var ptr = NativeMethods.kuzu_value_to_string(_handle);
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        internal static object ConvertKuzuValue(Type target, KuzuValue value)
        {
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

        private static KuzuValue FromBorrowed(NativeKuzuValue raw)
        {
            //Make a deep copy of the value to make sure we don't hold a borrowed reference
            int size = Marshal.SizeOf<NativeKuzuValue>();
            var wrapperPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(raw, wrapperPtr, false);
            return WrapFromHandle(new(wrapperPtr, false));

            // Not sure why this doesn't work
            //var outPtr = NativeMethods.kuzu_value_clone(raw.Value);
            //return WrapFromHandle(new KuzuValueSafeHandle(new(outPtr, true)));
        }

        internal static KuzuValue FromNative(IntPtr raw)
        {
            return WrapOwned(raw);
        }

        internal static KuzuValue FromNative(NativeKuzuValue raw)
        {
            return (!raw.IsOwnedByCpp) ? WrapOwned(raw.Value) : FromBorrowed(raw);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handle.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, GetType().Name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller must dispose")]
        private static KuzuValue WrapFromHandle(NativeKuzuValue nativeStruct)
        {
            if (nativeStruct.Value == IntPtr.Zero) return new KuzuAny(nativeStruct); // fallback
            NativeMethods.kuzu_value_get_data_type(nativeStruct.Value, out var tNative);
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

        private static KuzuValue WrapOwned(IntPtr ptr)
        {
            KuzuGuard.AssertNotZero(ptr, "Failed to create value (null native pointer)");
            return WrapFromHandle(new(ptr, false));
        }

        internal sealed class KuzuValueSafeHandle : KuzuSafeHandle
        {
            internal NativeKuzuValue NativeStruct;

            public override bool IsInvalid => NativeStruct.Value == IntPtr.Zero;

            internal KuzuValueSafeHandle() : base("KuzuValue")
            {
            }

            internal KuzuValueSafeHandle(NativeKuzuValue nativeStruct) : base("KuzuValue")
            {
                NativeStruct = nativeStruct;
                Initialize(NativeStruct.Value);
            }

            public void Init(NativeKuzuValue nativeStruct)
            {
                if (!IsInvalid) throw new InvalidOperationException("Handle is already initialized");
                NativeStruct = nativeStruct;
                Initialize(NativeStruct.Value);
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