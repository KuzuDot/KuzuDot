using KuzuDot.Utils;

namespace KuzuDot
{
    /// <summary>
    /// Public snapshot of internal performance counters (experimental).
    /// </summary>
    public readonly struct KuzuPerformanceSnapshot : System.IEquatable<KuzuPerformanceSnapshot>
    {
        public long ValueWrappersCreated { get; }
        public long TuplesEnumerated { get; }
        public long StringDecodes { get; }
        public long BlobHexDecodes { get; }
        internal KuzuPerformanceSnapshot(long v, long t, long s, long b)
        { ValueWrappersCreated = v; TuplesEnumerated = t; StringDecodes = s; BlobHexDecodes = b; }
        public override string ToString() => $"KuzuPerformanceSnapshot(Values={ValueWrappersCreated}, Tuples={TuplesEnumerated}, Strings={StringDecodes}, Blobs={BlobHexDecodes})";
        public static KuzuPerformanceSnapshot Capture()
        {
            var snap = PerfCounters.Snapshot();
            return new KuzuPerformanceSnapshot(snap.ValueWrappersCreated, snap.TuplesEnumerated, snap.StringDecodes, snap.BlobHexDecodes);
        }
        public bool Equals(KuzuPerformanceSnapshot other) => ValueWrappersCreated == other.ValueWrappersCreated && TuplesEnumerated == other.TuplesEnumerated && StringDecodes == other.StringDecodes && BlobHexDecodes == other.BlobHexDecodes;
        public override bool Equals(object? obj) => obj is KuzuPerformanceSnapshot o && Equals(o);
        public override int GetHashCode()
        {
            unchecked
            {
                var h = ValueWrappersCreated.GetHashCode();
                h = (h * 397) ^ TuplesEnumerated.GetHashCode();
                h = (h * 397) ^ StringDecodes.GetHashCode();
                h = (h * 397) ^ BlobHexDecodes.GetHashCode();
                return h;
            }
        }
        public static bool operator ==(KuzuPerformanceSnapshot left, KuzuPerformanceSnapshot right) => left.Equals(right);
        public static bool operator !=(KuzuPerformanceSnapshot left, KuzuPerformanceSnapshot right) => !left.Equals(right);
    }
}
