using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;

namespace KuzuDot.Value
{
    public sealed class KuzuTimestamp : KuzuTypedValue<DateTime>
    {
        internal KuzuTimestamp(NativeKuzuValue n) : base(n) { }
        internal KuzuTimestamp(IntPtr ptr) : base(ptr) { }
        protected override bool TryGetNativeValue(out DateTime value)
        {
            var st = NativeMethods.kuzu_value_get_timestamp(ref Handle.NativeStruct, out var ts);
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

        internal KuzuTimestampNs(IntPtr ptr) : base(ptr)
        {
        }

        public long UnixNanoseconds
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_ns(ref Handle.NativeStruct, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_ns"); return ts.Value; } }
    }

    public sealed class KuzuTimestampMs : KuzuValue
    {
        internal KuzuTimestampMs(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuTimestampMs(IntPtr ptr) : base(ptr)
        {
        }

        public long UnixMilliseconds
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_ms(ref Handle.NativeStruct, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_ms"); return ts.Value; } }
    }

    public sealed class KuzuTimestampSec : KuzuValue
    {
        internal KuzuTimestampSec(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuTimestampSec(IntPtr ptr) : base(ptr)
        {
        }

        public long UnixSeconds
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_sec(ref Handle.NativeStruct, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_sec"); return ts.Value; } }
    }

    public sealed class KuzuTimestampTz : KuzuValue
    {
        internal KuzuTimestampTz(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuTimestampTz(IntPtr ptr) : base(ptr)
        {
        }

        public long UnixMicrosUtc
        { get { ThrowIfDisposed(); var st = NativeMethods.kuzu_value_get_timestamp_tz(ref Handle.NativeStruct, out var ts); KuzuGuard.CheckSuccess(st, "Failed to get timestamp_tz"); return ts.Value; } }
    }
}