using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuConnection(IntPtr connection)
    {
        public readonly IntPtr Connection = connection;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuDatabase(IntPtr database)
    {
        public readonly IntPtr Database = database;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuDate(int days)
    {
        public readonly int Days = days;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuFlatTuple(IntPtr handle, bool isOwnedByCpp)
    {
        public readonly IntPtr FlatTuple = handle;

        [MarshalAs(UnmanagedType.U1)]
        public readonly bool IsOwnedByCpp = isOwnedByCpp;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuInt128(ulong low, long high)
    {
        public readonly ulong Low = low; 
        public readonly long High = high;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuInternalId(ulong tableId, ulong offset)
    {
        public readonly ulong TableId = tableId;
        public readonly ulong Offset = offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuInterval(int months, int days, long micros)
    {
        public readonly int Months = months;
        public readonly int Days = days;
        public readonly long Micros = micros;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuLogicalType(IntPtr handle)
    {
        public readonly IntPtr DataType = handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuPreparedStatement(IntPtr handle, IntPtr boundValues)
    {
        public readonly IntPtr PreparedStatement = handle;
        public readonly IntPtr BoundValues = boundValues;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuQueryResult(IntPtr handle, bool isOwnedByCpp)
    {
        public readonly IntPtr QueryResult = handle;

        [MarshalAs(UnmanagedType.U1)]
        public readonly bool IsOwnedByCpp = isOwnedByCpp;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuQuerySummary(IntPtr handle)
    {
        public readonly IntPtr QuerySummary = handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeKuzuSystemConfig
    {
        public ulong BufferPoolSize;
        public ulong MaxNumThreads;

        [MarshalAs(UnmanagedType.U1)]
        public bool EnableCompression;

        [MarshalAs(UnmanagedType.U1)]
        public bool ReadOnly;

        public ulong MaxDbSize;

        [MarshalAs(UnmanagedType.U1)]
        public bool AutoCheckpoint;

        public ulong CheckpointThreshold;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuTimestamp(long value)
    {
        public readonly long Value = value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuTimestampMs(long value)
    {
        public readonly long Value = value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuTimestampNs(long value)
    {
        public readonly long Value = value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuTimestampSec(long value)
    {
        public readonly long Value = value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuTimestampTz(long value)
    {
        public readonly long Value = value;
    }

    // Portable subset of C's struct tm (platforms commonly expose at least these 9 int fields).
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeKuzuTm
    {
        public int tm_sec;   // Seconds [0,60]
        public int tm_min;   // Minutes [0,59]
        public int tm_hour;  // Hour [0,23]
        public int tm_mday;  // Day of month [1,31]
        public int tm_mon;   // Month of year [0,11]
        public int tm_year;  // Years since 1900
        public int tm_wday;  // Day of week [0,6] (Sunday = 0)
        public int tm_yday;  // Day of year [0,365]
        public int tm_isdst; // Daylight Savings flag
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeKuzuValue(IntPtr handle, bool isOwnedByCpp)
    {
        public readonly IntPtr Value = handle;

        [MarshalAs(UnmanagedType.U1)]
        public readonly bool IsOwnedByCpp = isOwnedByCpp;
    }
}