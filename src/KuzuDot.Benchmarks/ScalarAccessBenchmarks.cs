using BenchmarkDotNet.Attributes;
using KuzuDot.Value;
using System;

namespace KuzuDot.Benchmarks;

[Config(typeof(BenchConfig))]
[BenchmarkCategory("ScalarAccess")] 
public class ScalarAccessBenchmarks : System.IDisposable
{
    private Database? _db; private Connection? _conn; private PreparedStatement? _ps;

    [GlobalSetup]
    public void Setup()
    {
        _db = Database.FromMemory();
        _conn = _db.Connect();
        _conn.Query("CREATE NODE TABLE Num(id INT64, i32 INT32, dbl DOUBLE, flag BOOL, txt STRING, PRIMARY KEY(id))").Dispose();
        for (int i = 0; i < 200; i++)
        {
            // Use uppercase TRUE/FALSE to satisfy analyzer preference (Invariant casing rule)
            _conn.Query($"CREATE (:Num {{id:{i}, i32:{i}, dbl:{i * 0.5}, flag:{(i % 2 == 0).ToString().ToUpperInvariant()}, txt:'T{i}'}})").Dispose();
        }
        _ps = _conn.Prepare("MATCH (n:Num) WHERE n.id=$id RETURN n.i32, n.dbl, n.flag, n.txt");
    }

    [Benchmark(Description="Iterate & read typed values (lock-free path)")]
    public int ScalarIterateTyped()
    {
        int sum = 0;
        using var r = _conn!.Query("MATCH (n:Num) RETURN n.i32, n.dbl, n.flag, n.txt");
        while (r.HasNext())
        {
            using var row = r.GetNext();
            using var v0 = row.GetValue(0); sum += ((KuzuInt32)v0).Value;
            using var v1 = row.GetValue(1); _ = ((KuzuDouble)v1).Value;
            using var v2 = row.GetValue(2); _ = ((KuzuBool)v2).Value;
            using var v3 = row.GetValue(3); _ = ((KuzuString)v3).Value.Length;
        }
        return sum;
    }

    [Benchmark(Description="Prepared single-row bind + scalar reads")]
    public int ScalarPreparedSingle()
    {
        _ps!.Bind("id", 42);
        using var r = _ps.Execute();
        using var row = r.GetNext();
        using var v0 = row.GetValue(0); return ((KuzuInt32)v0).Value;
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ps?.Dispose(); _ps = null; _conn?.Dispose(); _conn = null; _db?.Dispose(); _db = null;
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
