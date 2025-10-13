using System;

namespace KuzuDot.Native
{
    /// <summary>
    /// Utility class for working with Kuzu date and timestamp types.
    /// Public surface uses DateTime/TimeSpan; native structs are internal.
    /// </summary>
    internal static class DateTimeUtilities
    {
        private static readonly DateTime UnixEpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime DaysToDateTime(int days) => UnixEpochUtc.AddDays(days);

        public static int DateTimeToDays(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local) dateTime = dateTime.ToUniversalTime();
            else if (dateTime.Kind == DateTimeKind.Unspecified) dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return (int)(dateTime.Date - UnixEpochUtc).TotalDays;
        }

        /// <summary>
        /// Convert DateTime to microseconds since UNIX epoch (UTC assumed / converted).
        /// </summary>
        internal static long DateTimeToUnixMicroseconds(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local) dateTime = dateTime.ToUniversalTime();
            else if (dateTime.Kind == DateTimeKind.Unspecified) dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            var ticks = (dateTime - UnixEpochUtc).Ticks; // 100ns units
            return ticks / 10; // microseconds
        }

        /// <summary>
        /// Convert microseconds since UNIX epoch to DateTime (UTC).
        /// </summary>
        internal static DateTime UnixMicrosecondsToDateTime(long micros) => UnixEpochUtc.AddTicks(micros * 10);

        /// <summary>
        /// Creates internal native KuzuTimestamp from DateTime (microseconds since epoch).
        /// </summary>
        internal static NativeKuzuTimestamp DateTimeToNativeTimestamp(DateTime dt) => new(DateTimeToUnixMicroseconds(dt));

        /// <summary>
        /// Converts internal native KuzuTimestamp to DateTime.
        /// </summary>
        internal static DateTime NativeTimestampToDateTime(NativeKuzuTimestamp ts) => UnixMicrosecondsToDateTime(ts.Value);

        /// <summary>
        /// Convert TimeSpan to internal native interval (months set 0, split days + remaining micros).
        /// </summary>
        internal static NativeKuzuInterval TimeSpanToNativeInterval(TimeSpan span)
        {
            var totalMicros = (long)(span.TotalMilliseconds * 1000); // may overflow extremely large spans but acceptable
            var days = span.Days;
            var microsRemainder = totalMicros - days * 24L * 60L * 60L * 1_000_000L;
            return new NativeKuzuInterval( 0, days, microsRemainder);
        }

        /// <summary>
        /// Convert internal native interval to TimeSpan (ignores months).
        /// </summary>
        internal static TimeSpan NativeIntervalToTimeSpan(NativeKuzuInterval interval)
        {
            var totalMicros = interval.Days * 24L * 60L * 60L * 1_000_000L + interval.Micros;
            return TimeSpan.FromTicks(totalMicros * 10); // micro -> 100ns
        }

        // Date conversions (internal)
        internal static NativeKuzuDate DateTimeToKuzuDate(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local) dateTime = dateTime.ToUniversalTime();
            else if (dateTime.Kind == DateTimeKind.Unspecified) dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            var days = (int)(dateTime.Date - UnixEpochUtc).TotalDays;
            return new NativeKuzuDate(days);
        }

        internal static DateTime KuzuDateToDateTime(NativeKuzuDate kuzuDate) => UnixEpochUtc.AddDays(kuzuDate.Days);

    }
}