using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    /// <summary>
    /// Factory methods for creating KuzuDB values.
    /// </summary>
    public static class KuzuValueFactory
    {
        /// <summary>
        /// Creates a null value.
        /// </summary>
        /// <returns>A <see cref="KuzuAny"/> representing a null value.</returns>
        public static KuzuAny CreateNull() => new (NativeMethods.kuzu_value_create_null());

        /// <summary>
        /// Creates a boolean value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A <see cref="KuzuBool"/> representing the boolean value.</returns>
        public static KuzuBool CreateBool(bool value) => new (NativeMethods.kuzu_value_create_bool(value));

        /// <summary>
        /// Creates an 8-bit signed integer value.
        /// </summary>
        /// <param name="value">The 8-bit signed integer value.</param>
        /// <returns>A <see cref="KuzuInt8"/> representing the value.</returns>
        public static KuzuInt8 CreateInt8(sbyte value) => new (NativeMethods.kuzu_value_create_int8(value));

        /// <summary>
        /// Creates a 16-bit signed integer value.
        /// </summary>
        /// <param name="value">The 16-bit signed integer value.</param>
        /// <returns>A <see cref="KuzuInt16"/> representing the value.</returns>
        public static KuzuInt16 CreateInt16(short value) => new (NativeMethods.kuzu_value_create_int16(value));

        /// <summary>
        /// Creates a 32-bit signed integer value.
        /// </summary>
        /// <param name="value">The 32-bit signed integer value.</param>
        /// <returns>A <see cref="KuzuInt32"/> representing the value.</returns>
        public static KuzuInt32 CreateInt32(int value) => new (NativeMethods.kuzu_value_create_int32(value));

        /// <summary>
        /// Creates a 64-bit signed integer value.
        /// </summary>
        /// <param name="value">The 64-bit signed integer value.</param>
        /// <returns>A <see cref="KuzuInt64"/> representing the value.</returns>
        public static KuzuInt64 CreateInt64(long value) => new(NativeMethods.kuzu_value_create_int64(value));

        /// <summary>
        /// Creates an 8-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The 8-bit unsigned integer value.</param>
        /// <returns>A <see cref="KuzuUInt8"/> representing the value.</returns>
        public static KuzuUInt8 CreateUInt8(byte value) => new(NativeMethods.kuzu_value_create_uint8(value));

        /// <summary>
        /// Creates a 16-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The 16-bit unsigned integer value.</param>
        /// <returns>A <see cref="KuzuUInt16"/> representing the value.</returns>
        public static KuzuUInt16 CreateUInt16(ushort value) => new(NativeMethods.kuzu_value_create_uint16(value));

        /// <summary>
        /// Creates a 32-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The 32-bit unsigned integer value.</param>
        /// <returns>A <see cref="KuzuUInt32"/> representing the value.</returns>
        public static KuzuUInt32 CreateUInt32(uint value) => new(NativeMethods.kuzu_value_create_uint32(value));

        /// <summary>
        /// Creates a 64-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The 64-bit unsigned integer value.</param>
        /// <returns>A <see cref="KuzuUInt64"/> representing the value.</returns>
        public static KuzuUInt64 CreateUInt64(ulong value) => new(NativeMethods.kuzu_value_create_uint64(value));

        internal static KuzuInt128 CreateInt128Internal(NativeKuzuInt128 value) => new(NativeMethods.kuzu_value_create_int128(value));

        /// <summary>
        /// Creates a single-precision floating-point value.
        /// </summary>
        /// <param name="value">The float value.</param>
        /// <returns>A <see cref="KuzuFloat"/> representing the value.</returns>
        public static KuzuFloat CreateFloat(float value) => new(NativeMethods.kuzu_value_create_float(value));

        /// <summary>
        /// Creates a double-precision floating-point value.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>A <see cref="KuzuDouble"/> representing the value.</returns>
        public static KuzuDouble CreateDouble(double value) => new(NativeMethods.kuzu_value_create_double(value));

        /// <summary>
        /// Creates an internal ID value.
        /// </summary>
        /// <param name="value">The internal ID value.</param>
        /// <returns>A <see cref="KuzuInternalId"/> representing the value.</returns>
        public static KuzuInternalId CreateInternalId(InternalId value) => new (NativeMethods.kuzu_value_create_internal_id(value.ToNative()));

        internal static KuzuDate CreateDateInternal(NativeKuzuDate value) => new (NativeMethods.kuzu_value_create_date(value));

        /// <summary>
        /// Creates a date value from a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The date and time value.</param>
        /// <returns>A <see cref="KuzuDate"/> representing the date.</returns>
        public static KuzuDate CreateDate(DateTime dateTime) => CreateDateInternal(DateTimeUtilities.DateTimeToKuzuDate(dateTime));

        /// <summary>
        /// Creates a timestamp value from a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The date and time value.</param>
        /// <returns>A <see cref="KuzuTimestamp"/> representing the timestamp.</returns>
        public static KuzuTimestamp CreateTimestamp(DateTime dateTime) => new (NativeMethods.kuzu_value_create_timestamp(DateTimeUtilities.DateTimeToNativeTimestamp(dateTime)));

        /// <summary>
        /// Creates a timestamp value from Unix microseconds.
        /// </summary>
        /// <param name="micros">The number of microseconds since Unix epoch.</param>
        /// <returns>A <see cref="KuzuTimestamp"/> representing the timestamp.</returns>
        public static KuzuTimestamp CreateTimestampFromUnixMicros(long micros) => new (NativeMethods.kuzu_value_create_timestamp(new NativeKuzuTimestamp(micros)));

        /// <summary>
        /// Creates a timestamp value from nanoseconds.
        /// </summary>
        /// <param name="nanos">The number of nanoseconds since Unix epoch.</param>
        /// <returns>A <see cref="KuzuTimestampNs"/> representing the timestamp.</returns>
        public static KuzuTimestampNs CreateTimestampNanoseconds(long nanos) => new (NativeMethods.kuzu_value_create_timestamp_ns(new NativeKuzuTimestampNs(nanos)));

        /// <summary>
        /// Creates a timestamp value from milliseconds.
        /// </summary>
        /// <param name="millis">The number of milliseconds since Unix epoch.</param>
        /// <returns>A <see cref="KuzuTimestampMs"/> representing the timestamp.</returns>
        public static KuzuTimestampMs CreateTimestampMilliseconds(long millis) => new (NativeMethods.kuzu_value_create_timestamp_ms(new NativeKuzuTimestampMs(millis)));

        /// <summary>
        /// Creates a timestamp value from seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds since Unix epoch.</param>
        /// <returns>A <see cref="KuzuTimestampSec"/> representing the timestamp.</returns>
        public static KuzuTimestampSec CreateTimestampSeconds(long seconds) => new (NativeMethods.kuzu_value_create_timestamp_sec(new NativeKuzuTimestampSec(seconds)));

        /// <summary>
        /// Creates a timestamp with time zone value from microseconds.
        /// </summary>
        /// <param name="microsUtc">The number of microseconds since Unix epoch (UTC).</param>
        /// <returns>A <see cref="KuzuTimestampTz"/> representing the timestamp with time zone.</returns>
        public static KuzuTimestampTz CreateTimestampWithTimeZoneMicros(long microsUtc) => new (NativeMethods.kuzu_value_create_timestamp_tz(new NativeKuzuTimestampTz(microsUtc)));

        /// <summary>
        /// Creates an interval value from a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="span">The time span value.</param>
        /// <returns>A <see cref="KuzuInterval"/> representing the interval.</returns>
        public static KuzuInterval CreateInterval(TimeSpan span) => new (NativeMethods.kuzu_value_create_interval(DateTimeUtilities.TimeSpanToNativeInterval(span)));

        public static KuzuString CreateString(string? value)
        {
            KuzuGuard.NotNull(value, nameof(value));
            KuzuGuard.StringContainsNull(value, nameof(value));

            // Convert to null terminated string
            var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value + '\0');
            IntPtr unmanaged = Marshal.AllocHGlobal(utf8Bytes.Length);
            Marshal.Copy(utf8Bytes, 0, unmanaged, utf8Bytes.Length);
            // Pass pointer to KuzuDB to create a native string
            IntPtr nativePtr = IntPtr.Zero;
            try
            {
                nativePtr = NativeMethods.kuzu_value_create_string_from_utf8(unmanaged);
                KuzuGuard.AssertNotZero(nativePtr, "Failed to create string value");
            }
            finally { Marshal.FreeHGlobal(unmanaged); }

            return new (nativePtr);
        }

        public static KuzuList CreateList(params KuzuValue?[] elements)
        {
            KuzuGuard.NotNull(elements, nameof(elements));

            IntPtr outValPtr;
            IntPtr elemsPtr = IntPtr.Zero;

            try
            {
                if (elements.Length == 0)
                {
                    var nativeState = NativeMethods.kuzu_value_create_list(0, IntPtr.Zero, out outValPtr);
                    KuzuGuard.CheckSuccess(nativeState, "Failed to create empty list");
                }
                else
                {
                    elemsPtr = Marshal.AllocHGlobal(IntPtr.Size * elements.Length);
                    for (int i = 0; i < elements.Length; i++)
                    {
                        if (elements[i] == null) throw new ArgumentNullException($"elements[{i}]");
                        Marshal.WriteIntPtr(elemsPtr, i * IntPtr.Size, elements[i]!.NativePtr);
                    }
                    var st = NativeMethods.kuzu_value_create_list((ulong)elements.Length, elemsPtr, out outValPtr);
                    KuzuGuard.CheckSuccess(st, "Failed to create list value");
                    KuzuGuard.AssertNotZero(outValPtr, "Failed to create list value");
                }
            }
            finally { if (elemsPtr != IntPtr.Zero) Marshal.FreeHGlobal(elemsPtr); }

            return new (outValPtr);
        }

        public static KuzuStruct CreateStruct(params (string Name, KuzuValue? Value)[] fields)
        {
            KuzuGuard.NotNull(fields, nameof(fields));

            IntPtr outValPtr;
            IntPtr namesPtr = IntPtr.Zero;
            IntPtr valuesPtr = IntPtr.Zero;
            var allocatedNames = new IntPtr[fields.Length];
            try
            {
                if (fields.Length == 0)
                {
                    var nativeState = NativeMethods.kuzu_value_create_struct(0, IntPtr.Zero, IntPtr.Zero, out outValPtr);
                    if (nativeState != KuzuState.Success || outValPtr == IntPtr.Zero)
                    {
                        throw new KuzuException("KuzuDB does not support zero-field structs");
                    }
                }
                else
                {
                    namesPtr = Marshal.AllocHGlobal(IntPtr.Size * fields.Length);
                    valuesPtr = Marshal.AllocHGlobal(IntPtr.Size * fields.Length);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.IsNullOrEmpty(fields[i].Name)) throw new ArgumentException("Field name cannot be null or empty", nameof(fields));
                        KuzuGuard.NotNull(fields[i].Value, $"fields[{i}].Value");
                        var namePtr = Marshal.StringToHGlobalAnsi(fields[i].Name);
                        allocatedNames[i] = namePtr;
                        Marshal.WriteIntPtr(namesPtr, i * IntPtr.Size, namePtr);
                        Marshal.WriteIntPtr(valuesPtr, i * IntPtr.Size, fields[i].Value!.NativePtr);
                    }
                    var st = NativeMethods.kuzu_value_create_struct((ulong)fields.Length, namesPtr, valuesPtr, out outValPtr);
                    if (st != KuzuState.Success || outValPtr == IntPtr.Zero)
                        throw new KuzuException("Failed to create struct value");
                }
            }
            finally
            {
                for (int i = 0; i < allocatedNames.Length; i++) if (allocatedNames[i] != IntPtr.Zero) Marshal.FreeHGlobal(allocatedNames[i]);
                if (namesPtr != IntPtr.Zero) Marshal.FreeHGlobal(namesPtr);
                if (valuesPtr != IntPtr.Zero) Marshal.FreeHGlobal(valuesPtr);
            }

            return new (outValPtr);
        }

        public static KuzuMap CreateMap(KuzuValue?[] keys, KuzuValue?[] values)
        {
            KuzuGuard.NotNull(keys, nameof(keys));
            KuzuGuard.NotNull(values, nameof(values));

            if (keys.Length != values.Length) throw new ArgumentException("Keys and values length mismatch");
            IntPtr outValPtr;
            IntPtr keysPtr = IntPtr.Zero;
            IntPtr valuesPtr = IntPtr.Zero;

            try
            {
                if (keys.Length == 0)
                {
                    var nativeState = NativeMethods.kuzu_value_create_map(0, IntPtr.Zero, IntPtr.Zero, out outValPtr);
                    if (nativeState != KuzuState.Success || outValPtr == IntPtr.Zero)
                    {
                        throw new KuzuException("Failed to create empty map");
                    }
                }
                else
                {
                    keysPtr = Marshal.AllocHGlobal(IntPtr.Size * keys.Length);
                    valuesPtr = Marshal.AllocHGlobal(IntPtr.Size * values.Length);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (keys[i] == null) throw new ArgumentNullException($"keys[{i}]");
                        if (values[i] == null) throw new ArgumentNullException($"values[{i}]");
                        Marshal.WriteIntPtr(keysPtr, i * IntPtr.Size, keys[i]!.NativePtr);
                        Marshal.WriteIntPtr(valuesPtr, i * IntPtr.Size, values[i]!.NativePtr);
                    }
                    var st = NativeMethods.kuzu_value_create_map((ulong)keys.Length, keysPtr, valuesPtr, out outValPtr);
                    if (st != KuzuState.Success || outValPtr == IntPtr.Zero)
                        throw new KuzuException("Failed to create map value");
                }
            }
            finally
            {
                if (keysPtr != IntPtr.Zero) Marshal.FreeHGlobal(keysPtr);
                if (valuesPtr != IntPtr.Zero) Marshal.FreeHGlobal(valuesPtr);
            }

            return new KuzuMap(outValPtr);
        }

        public static KuzuInt128 CreateInt128(BigInteger value)
        {
            var native = NativeUtil.BigIntegerToNative(value);
            return CreateInt128Internal(native);
        }

        public static KuzuDate CreateDateFromString(string dateString)
        {
            KuzuGuard.NotNullOrEmpty(dateString, nameof(dateString));
            var state = NativeMethods.kuzu_date_from_string(dateString, out var d);
            KuzuGuard.CheckSuccess(state, $"Failed to parse date from string: {dateString}");
            return CreateDateInternal(d);
        }

        public static KuzuInt128 CreateInt128FromString(string int128String)
        {
            KuzuGuard.NotNullOrEmpty(int128String, nameof(int128String));
            var state = NativeMethods.kuzu_int128_t_from_string(int128String, out var v);
            KuzuGuard.CheckSuccess(state, $"Failed to parse int128 from string: {int128String}");
            return CreateInt128Internal(v);
        }
    }
}
