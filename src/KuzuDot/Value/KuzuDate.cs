using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;

namespace KuzuDot.Value
{
    public sealed class KuzuDate : KuzuValue
    {
        internal KuzuDate(NativeKuzuValue n) : base(n)
        {
            if (!TryGetNativeValue(out var days))
                throw new KuzuException("Failed to get date value");
            Days = days;
        }

        public int Days { get; }

        private bool TryGetNativeValue(out int days)
        {
            var st = NativeMethods.kuzu_value_get_date(Handle, out var native);
            if (st == KuzuState.Success)
            {
                days = native.Days;
                return true;
            }
            days = 0;
            return false;
        }

        public override string ToString()
        {
            var st = NativeMethods.kuzu_date_to_string(new NativeKuzuDate(Days), out var ptr);
            KuzuGuard.CheckSuccess(st, "Failed to convert date to string");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }


#if NET8_0_OR_GREATER
        public DateOnly AsDateOnly() => DateOnly.FromDateTime(FromKuzuDate(this));

        public static implicit operator DateOnly(KuzuDate v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.AsDateOnly();
        }
#endif

        public DateTime AsDateTime() => FromKuzuDate(this);

        public static DateTime FromKuzuDate(KuzuDate v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return DateTimeUtilities.DaysToDateTime(v.Days);
        }

        public static implicit operator DateTime(KuzuDate v) => FromKuzuDate(v);
    }
}