using BenchmarkDotNet.Attributes;

namespace KuzuDot.Benchmarks;

[Config(typeof(BenchConfig))]
[BenchmarkCategory("PerfSnapshot")] 
public class PerfSnapshotBenchmarks
{
    private int _dummy; // ensure instance state so method not flagged static
    [Benchmark(Description="Capture performance snapshot")]
    public KuzuPerformanceSnapshot Capture() { _dummy++; return KuzuPerformanceSnapshot.Capture(); }
}
