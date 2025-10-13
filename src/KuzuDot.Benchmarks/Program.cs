using BenchmarkDotNet.Running;

namespace KuzuDot.Benchmarks;

internal static class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Benchmark")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Benchmark")]
    public static void Main(string[] args)
    {
        // Explicitly reference benchmark types to ensure they are retained for reflection discovery
        _ = typeof(BlobBenchmarks);
        _ = typeof(QueryBenchmarks);
        _ = typeof(ScalarAccessBenchmarks);
        _ = typeof(ColumnLookupBenchmarks);
        _ = typeof(PreparedBindBenchmarks);
        _ = typeof(PerfSnapshotBenchmarks);
        // Force JIT / construction to avoid any trimming edge (should not be needed, defensive)
        using (_ = new ScalarAccessBenchmarks()) { }
        using (_ = new ColumnLookupBenchmarks()) { }
        using (_ = new PreparedBindBenchmarks()) { }
        _ = new PerfSnapshotBenchmarks();

        // Diagnostic: list types with methods carrying [Benchmark] attribute
        var asm = typeof(Program).Assembly;
        Console.WriteLine("== Diagnostic: Candidate benchmark types ==");
        foreach (var t in asm.GetTypes())
        {
            try
            {
                var benchMethods = t.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly);
                bool any = false;
                foreach (var m in benchMethods)
                {
                    if (m.GetCustomAttributes(typeof(BenchmarkDotNet.Attributes.BenchmarkAttribute), false).Length > 0)
                    {
                        if (!any)
                        {
                            Console.WriteLine($" Type: {t.FullName}");
                            any = true;
                        }
                        Console.WriteLine($"    Method: {m.Name}");
                    }
                }
            }
            catch { }
        }
        Console.WriteLine("== End Diagnostic ==");
        if (args.Length > 0 && args.Any(a => a.Equals("phase1", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("Running Phase1 explicit benchmark set (manual invocation workaround)...");
            BenchmarkRunner.Run<ScalarAccessBenchmarks>();
            BenchmarkRunner.Run<ColumnLookupBenchmarks>();
            BenchmarkRunner.Run<PreparedBindBenchmarks>();
            BenchmarkRunner.Run<PerfSnapshotBenchmarks>();
        }
        else
        {
            var switcher = new BenchmarkSwitcher(new []
            {
                typeof(BlobBenchmarks),
                typeof(QueryBenchmarks),
                typeof(ScalarAccessBenchmarks),
                typeof(ColumnLookupBenchmarks),
                typeof(PreparedBindBenchmarks),
                typeof(PerfSnapshotBenchmarks)
            });
            switcher.Run(args);
        }
    }
}
