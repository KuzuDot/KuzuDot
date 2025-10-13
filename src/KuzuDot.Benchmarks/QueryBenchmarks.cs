using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Threading;

namespace KuzuDot.Benchmarks;

[Config(typeof(BenchConfig))]
public class QueryBenchmarks : IDisposable
{
    private Database? _db;
    private Connection? _conn;
    private PreparedStatement? _prepared;

    [GlobalSetup]
    public void Setup()
    {
        _db = Database.FromMemory();
        _conn = _db.Connect();
        _conn.Query("CREATE NODE TABLE Person(id INT64, name STRING, age INT64, PRIMARY KEY(id))").Dispose();
        for (int i = 0; i < 500; i++)
        {
            _conn.Query($"CREATE (:Person {{id: {i}, name: 'Name{i}', age: {20 + (i % 50)}}})").Dispose();
        }
        _prepared = _conn.Prepare("MATCH (p:Person) WHERE p.id=$id RETURN p.name, p.age");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _prepared?.Dispose();
        _prepared = null;
        _conn?.Dispose();
        _conn = null;
        _db?.Dispose();
        _db = null;
    }

    [Benchmark(Description="Simple full scan materialize all rows")]
    public int SimpleQueryMaterialize()
    {
        using var result = _conn!.Query("MATCH (p:Person) RETURN p.id, p.name, p.age");
        int rows = 0;
        while (result.HasNext())
        {
            using var row = result.GetNext();
            rows++;
        }
        return rows;
    }

    [Benchmark(Description="Scalar COUNT(*) query")] 
    public long ScalarCount()
    {
        return _conn!.ExecuteScalar<long>("MATCH (p:Person) RETURN COUNT(*)");
    }

    [Benchmark(Description="Prepared bind + execute single row")]
    public string PreparedSingleBind()
    {
        using var ps = _conn!.Prepare("MATCH (p:Person) WHERE p.id=$id RETURN p.name");
        ps.Bind("id", 123);
        using var r = ps.Execute();
        if (!r.HasNext()) return string.Empty;
        using var row = r.GetNext();
        using var val = row.GetValue(0);
        return val.ToString();
    }

    [Benchmark(Description="Reused prepared: bind + execute single row")]
    public string PreparedReuseSingleBind()
    {
        _prepared!.Bind("id", 321);
        using var r = _prepared.Execute();
        using var row = r.GetNext();
        using var val = row.GetValue(0);
        return val.ToString();
    }

    [Benchmark(Description="Enumerate mapped POCOs")]
    public int PocoEnumeration()
    {
        int count = 0;
        foreach (var p in _conn!.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age"))
        {
            count++;
        }
        return count;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Cleanup();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private sealed class Person { public long id { get; set; } public string? name { get; set; } public long age { get; set; } }
}
