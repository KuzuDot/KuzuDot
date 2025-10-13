using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;

namespace KuzuDot.Value
{
    public sealed class KuzuTimestamp : KuzuTypedValue<DateTime>
    {
        internal KuzuTimestamp(NativeKuzuValue n) : base(n) { }
        protected override bool TryGetNativeValue(out DateTime value)
        {
            var st = NativeMethods.kuzu_value_get_timestamp(Handle, out var ts);
            if (st == KuzuState.Success)
            {
                value = DateTimeUtilities.NativeTimestampToDateTime(ts);
                return true;
            }
            value = default;
            return false;
        }

        public long UnixMicros
        {
            get
            {
                return DateTimeUtilities.DateTimeToUnixMicroseconds(Value);
            }
        }
    }


    public sealed class KuzuTimestampNs : KuzuValue
    {
        internal KuzuTimestampNs(NativeKuzuValue n) : base(n)
        {
        }

        public long UnixNanoseconds
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_ns(Handle, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_ns"); return ts.Value; } }
    }

    public sealed class KuzuTimestampMs : KuzuValue
    {
        internal KuzuTimestampMs(NativeKuzuValue n) : base(n)
        {
        }

        public long UnixMilliseconds
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_ms(Handle, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_ms"); return ts.Value; } }
    }

    public sealed class KuzuTimestampSec : KuzuValue
    {
        internal KuzuTimestampSec(NativeKuzuValue n) : base(n)
        {
        }

        public long UnixSeconds
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_sec(Handle, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_sec"); return ts.Value; } }
    }

    public sealed class KuzuTimestampTz : KuzuValue
    {
        internal KuzuTimestampTz(NativeKuzuValue n) : base(n)
        {
        }

        public long UnixMicrosUtc
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_tz(Handle, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_tz"); return ts.Value; } }
    }
}