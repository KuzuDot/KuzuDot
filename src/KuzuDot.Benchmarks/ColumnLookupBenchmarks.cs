using BenchmarkDotNet.Attributes;
namespace KuzuDot.Benchmarks;

[Config(typeof(BenchConfig))]
[BenchmarkCategory("ColumnLookup")] 
public class ColumnLookupBenchmarks : System.IDisposable
{
    private Database? _db; private Connection? _conn;

    [Params(3,8,16)] public int ColumnCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _db = Database.FromMemory(); _conn = _db.Connect();
        // Build a RETURN clause with N generated properties.
        var cols = string.Join(", ", System.Linq.Enumerable.Range(0, ColumnCount).Select(i => $"p.name AS name{i}"));
        _conn.Query("CREATE NODE TABLE P(id INT64, name STRING, PRIMARY KEY(id))").Dispose();
        for (int i = 0; i < 50; i++) _conn.Query($"CREATE (:P {{id:{i}, name:'N{i}'}})").Dispose();
        _query = $"MATCH (p:P) RETURN {cols}";
    }
    private string _query = string.Empty;

    [Benchmark(Description="Lookup last column by name each row")]
    public int ColumnNameLookupLast()
    {
        var target = $"name{ColumnCount-1}";
        int len = 0;
        using var r = _conn!.Query(_query);
        while (r.HasNext())
        {
            using var row = r.GetNext();
            using var v = row.GetValue(target);
            len += v.ToString().Length;
        }
        return len;
    }

    [Benchmark(Description="Lookup last column by ordinal each row")]
    public int ColumnOrdinalLookupLast()
    {
        ulong ord = (ulong)(ColumnCount - 1);
        int len = 0;
        using var r = _conn!.Query(_query);
        while (r.HasNext())
        {
            using var row = r.GetNext();
            using var v = row.GetValue(ord);
            len += v.ToString().Length;
        }
        return len;
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _conn?.Dispose(); _conn=null; _db?.Dispose(); _db=null;
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
