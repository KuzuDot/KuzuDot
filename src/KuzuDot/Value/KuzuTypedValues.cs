using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace KuzuDot.Value
{
    /// <summary>
    /// Represents a Kuzu value that can be cast to a specific native type <typeparamref name="T"/>.
    /// Materializes values on creation since they're immutable and relatively small.
    /// </summary>
    /// <typeparam name="T">The native type this value represents.</typeparam>
    public abstract class KuzuTypedValue<T> : KuzuValue
    {
        internal KuzuTypedValue(IntPtr ptr) : base(ptr)
        {
        }

        internal KuzuTypedValue(NativeKuzuValue n) : base(n)
        {
        }

        protected abstract bool TryGetNativeValue(out T value);

        /// <summary>
        /// Gets the value as the native type <typeparamref name="T"/>.
        /// </summary>
        public T Value
        {
            get
            {
                ThrowIfDisposed();
                if (!TryGetNativeValue(out var v))
                    throw new InvalidOperationException($"Failed to get {typeof(T).Name} value (type mismatch)");
                return v;
            }
        }

        public override string ToString() => base.ToString();
    }

    public sealed class KuzuBool : KuzuTypedValue<bool>
    {
        internal KuzuBool(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuBool(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out bool v)
        {
            return NativeMethods.kuzu_value_get_bool(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static bool FromKuzuBool(KuzuBool v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator bool(KuzuBool value)
        {
            KuzuGuard.NotNull(value, nameof(value));
            return value.Value;
        }
    }

    public sealed class KuzuInt8 : KuzuTypedValue<sbyte>
    {
        internal KuzuInt8(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInt8(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out sbyte v)
        {
            return NativeMethods.kuzu_value_get_int8(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static sbyte FromKuzuInt8(KuzuInt8 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator sbyte(KuzuInt8 value) => FromKuzuInt8(value);
    }

    public sealed class KuzuInt16 : KuzuTypedValue<short>
    {
        internal KuzuInt16(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInt16(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out short v)
        {
            return NativeMethods.kuzu_value_get_int16(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static short FromKuzuInt16(KuzuInt16 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator short(KuzuInt16 value) => FromKuzuInt16(value);
    }

    public sealed class KuzuInt32 : KuzuTypedValue<int>
    {
        internal KuzuInt32(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInt32(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out int v)
        {
            return NativeMethods.kuzu_value_get_int32(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static int FromKuzuInt32(KuzuInt32 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator int(KuzuInt32 value) => FromKuzuInt32(value);
    }

    public sealed class KuzuInt64 : KuzuTypedValue<long>
    {
        internal KuzuInt64(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInt64(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out long v)
        {
            return NativeMethods.kuzu_value_get_int64(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static long FromKuzuInt64(KuzuInt64 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator long(KuzuInt64 value) => FromKuzuInt64(value);
    }

    public sealed class KuzuUInt8 : KuzuTypedValue<byte>
    {
        internal KuzuUInt8(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuUInt8(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out byte v)
        {
            return NativeMethods.kuzu_value_get_uint8(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static byte FromKuzuUInt8(KuzuUInt8 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator byte(KuzuUInt8 value) => FromKuzuUInt8(value);
    }

    public sealed class KuzuUInt16 : KuzuTypedValue<ushort>
    {
        internal KuzuUInt16(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuUInt16(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out ushort v)
        {
            return NativeMethods.kuzu_value_get_uint16(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static ushort FromKuzuUInt16(KuzuUInt16 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator ushort(KuzuUInt16 value) => FromKuzuUInt16(value);
    }

    public sealed class KuzuUInt32 : KuzuTypedValue<uint>
    {
        internal KuzuUInt32(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuUInt32(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out uint v)
        {
            return NativeMethods.kuzu_value_get_uint32(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static uint FromKuzuUInt32(KuzuUInt32 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator uint(KuzuUInt32 value) => FromKuzuUInt32(value);
    }

    public sealed class KuzuUInt64 : KuzuTypedValue<ulong>
    {
        internal KuzuUInt64(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuUInt64(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out ulong v)
        {
            return NativeMethods.kuzu_value_get_uint64(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static ulong FromKuzuUInt64(KuzuUInt64 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator ulong(KuzuUInt64 value) => FromKuzuUInt64(value);
    }

    public sealed class KuzuFloat : KuzuTypedValue<float>
    {
        internal KuzuFloat(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuFloat(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out float v)
        {
            return NativeMethods.kuzu_value_get_float(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static float FromKuzuFloat(KuzuFloat v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator float(KuzuFloat value) => FromKuzuFloat(value);
    }

    public sealed class KuzuDouble : KuzuTypedValue<double>
    {
        internal KuzuDouble(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuDouble(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out double v)
        {
            return NativeMethods.kuzu_value_get_double(ref Handle.NativeStruct, out v) == KuzuState.Success;
        }

        public static double FromKuzuDouble(KuzuDouble v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator double(KuzuDouble value) => FromKuzuDouble(value);
    }

    public sealed class KuzuString : KuzuTypedValue<string>
    {
        internal KuzuString(IntPtr ptr) : base(ptr)
        {
        }

        internal KuzuString(NativeKuzuValue n) : base(n)
        {
        }

        protected override bool TryGetNativeValue(out string v)
        {
            var st = NativeMethods.kuzu_value_get_string(ref Handle.NativeStruct, out var ptr);
            if (st == KuzuState.Success)
            {
                v = NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
                return true;
            }
            v = string.Empty;
            return false;
        }

        public static string FromKuzuString(KuzuString v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator string(KuzuString value) => FromKuzuString(value);
    }

    public sealed class KuzuInt128 : KuzuTypedValue<BigInteger>
    {
        internal KuzuInt128(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInt128(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out BigInteger v)
        {
            var st = NativeMethods.kuzu_value_get_int128(ref Handle.NativeStruct, out var native);
            if (st == KuzuState.Success)
            {
                v = NativeUtil.NativeToBigInteger(native);
                return v != null;
            }
            v = default;
            return false;
        }

        public static BigInteger FromKuzuInt128(KuzuInt128 v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator BigInteger(KuzuInt128 value) => FromKuzuInt128(value);
    }

    public sealed class KuzuInterval : KuzuTypedValue<TimeSpan>
    {
        internal KuzuInterval(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInterval(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out TimeSpan value)
        {
            var st = NativeMethods.kuzu_value_get_interval(ref Handle.NativeStruct, out var iv);
            if (st == KuzuState.Success)
            {
                value = DateTimeUtilities.NativeIntervalToTimeSpan(iv);
                return true;
            }
            value = default;
            return false;
        }

        public static TimeSpan FromKuzuInterval(KuzuInterval v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator TimeSpan(KuzuInterval value) => FromKuzuInterval(value);
    }
}