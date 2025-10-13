using BenchmarkDotNet.Attributes;
using KuzuDot.Value;

namespace KuzuDot.Benchmarks;

[Config(typeof(BenchConfig))]
[BenchmarkCategory("PreparedBind")] 
public class PreparedBindBenchmarks : System.IDisposable
{
    private Database? _db; private Connection? _conn; private PreparedStatement? _ps;
    private readonly PreparedBindParams _tp = new();

    [GlobalSetup]
    public void Setup()
    {
        _db = Database.FromMemory();
        _conn = _db.Connect();
        // Schema
        _conn.Query("CREATE NODE TABLE X(id INT64, name STRING, score INT64, PRIMARY KEY(id))").Dispose();
        // Seed one matching row for the benchmark so the prepared statement returns exactly one row.
        // Use the same values as in _tp to ensure the WHERE clause matches.
        _conn.Query($"CREATE (:X {{id:{_tp.Id}, name:'{_tp.Name}', score:{_tp.Score}}});").Dispose();
        // Prepared statement that references all three parameters so both benchmark variants bind identical sets.
        _ps = _conn.Prepare("MATCH (x:X) WHERE x.id=$id AND x.name=$name AND x.score=$score RETURN x.id");
    }

    [Benchmark(Description="Bind object (cached normalization)")]
    public long PreparedBindObjectAndExec()
    {
        _ps!.Bind(_tp); using var r = _ps.Execute(); using var row = r.GetNext(); using var v = row.GetValue(0); return ((KuzuInt64)v).Value;
    }

    [Benchmark(Description="Bind primitives individually")]
    public long PreparedBindIndividual()
    {
        _ps!.Bind("id", _tp.Id).Bind("name", _tp.Name).Bind("score", _tp.Score); using var r = _ps.Execute(); using var row = r.GetNext(); using var v = row.GetValue(0); return ((KuzuInt64)v).Value;
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ps?.Dispose(); _ps=null; _conn?.Dispose(); _conn=null; _db?.Dispose(); _db=null;
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

internal sealed class PreparedBindParams
{
    // Use long for Id to align with table INT64 schema (avoid implicit widening in binder path)
    public long Id { get; set; } = 55;
    public string? Name { get; set; } = "Alpha";
    public long Score { get; set; } = 1234;
}
