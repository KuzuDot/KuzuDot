using System;
using System.Threading;

namespace KuzuDot.Utils
{
    internal static class PerfCounters
    {
        private static long _valueWrappersCreated;
        private static long _tuplesEnumerated;
        private static long _stringDecodes;
        private static long _blobHexDecodes;

        public static void IncValueWrapper() => Interlocked.Increment(ref _valueWrappersCreated);
        public static void IncTuple() => Interlocked.Increment(ref _tuplesEnumerated);
        public static void IncStringDecode() => Interlocked.Increment(ref _stringDecodes);
        public static void IncBlobDecode() => Interlocked.Increment(ref _blobHexDecodes);

        public static PerfSnapshot Snapshot() => new(
            Interlocked.Read(ref _valueWrappersCreated),
            Interlocked.Read(ref _tuplesEnumerated),
            Interlocked.Read(ref _stringDecodes),
            Interlocked.Read(ref _blobHexDecodes));

        public readonly struct PerfSnapshot
        {
            public readonly long ValueWrappersCreated;
            public readonly long TuplesEnumerated;
            public readonly long StringDecodes;
            public readonly long BlobHexDecodes;
            public PerfSnapshot(long v, long t, long s, long b)
            { ValueWrappersCreated = v; TuplesEnumerated = t; StringDecodes = s; BlobHexDecodes = b; }
            public override string ToString() => $"PerfSnapshot(Values={ValueWrappersCreated}, Tuples={TuplesEnumerated}, Strings={StringDecodes}, Blobs={BlobHexDecodes})";
        }
    }
}
