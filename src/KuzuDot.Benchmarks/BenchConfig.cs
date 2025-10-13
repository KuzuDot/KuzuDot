using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;

namespace KuzuDot.Benchmarks;

internal sealed class BenchConfig : ManualConfig
{
    public BenchConfig()
    {
        AddJob(Job.Default.WithId("Default"));
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        AddDiagnoser(MemoryDiagnoser.Default);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method));
        Options |= ConfigOptions.DisableOptimizationsValidator; // allow DEBUG runs if needed
    }
}
