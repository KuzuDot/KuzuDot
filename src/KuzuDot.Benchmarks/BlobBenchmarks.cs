using BenchmarkDotNet.Attributes;
using KuzuDot.Value;
using System;
using System.Linq;

namespace KuzuDot.Benchmarks;

[Config(typeof(BenchConfig))]
public class BlobBenchmarks : IDisposable
{
    private Database? _db;
    private Connection? _conn;
    private byte[] _small = null!;
    private byte[] _medium = null!;
    private byte[] _large = null!;

    [GlobalSetup]
    public void Setup()
    {
        _db = Database.FromMemory();
        _conn = _db.Connect();
        _conn.Query("CREATE NODE TABLE Bin(id INT64, data BLOB, PRIMARY KEY(id))").Dispose();
        _small = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        _medium = Enumerable.Range(0, 512).Select(i => (byte)(i % 256)).ToArray();
        _large = Enumerable.Range(0, 8192).Select(i => (byte)(i % 256)).ToArray();
        Insert(1, _small);
        Insert(2, _medium);
        Insert(3, _large);
    }

    private void Insert(long id, byte[] data)
    {
        string hex = Convert.ToHexString(data);
        _conn!.Query($"CREATE (:Bin {{id: {id}, data: BLOB('{hex}')}})").Dispose();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _conn?.Dispose();
        _conn = null;
        _db?.Dispose();
        _db = null;
    }

    // NOTE: We intentionally do not factor out a helper that returns BlobValue alone because
    // BlobValue instances are borrowed and depend on the lifetime of the owning row/result.
    // Each benchmark keeps the query result and row alive until after blob access completes.

    [Benchmark(Description="GetBytes small (16)")]
    public int GetBytesSmall()
    {
        using var r = _conn!.Query("MATCH (b:Bin {id:1}) RETURN b.data");
        using var row = r.GetNext();
        using var v = (KuzuBlob)row.GetValue(0);
        return v.GetBytes().Length;
    }

    [Benchmark(Description="GetSpan small (16)")]
    public int GetSpanSmall()
    {
        using var r = _conn!.Query("MATCH (b:Bin {id:1}) RETURN b.data");
        using var row = r.GetNext();
        using var v = (KuzuBlob)row.GetValue(0);
        return v.GetSpan().Length;
    }

    [Benchmark(Description="GetBytes medium (512)")]
    public int GetBytesMedium()
    {
        using var r = _conn!.Query("MATCH (b:Bin {id:2}) RETURN b.data");
        using var row = r.GetNext();
        using var v = (KuzuBlob)row.GetValue(0);
        return v.GetBytes().Length;
    }

    [Benchmark(Description="GetSpan medium (512)")]
    public int GetSpanMedium()
    {
        using var r = _conn!.Query("MATCH (b:Bin {id:2}) RETURN b.data");
        using var row = r.GetNext();
        using var v = (KuzuBlob)row.GetValue(0);
        return v.GetSpan().Length;
    }

    [Benchmark(Description="GetBytes large (8192)")]
    public int GetBytesLarge()
    {
        using var r = _conn!.Query("MATCH (b:Bin {id:3}) RETURN b.data");
        using var row = r.GetNext();
        using var v = (KuzuBlob)row.GetValue(0);
        return v.GetBytes().Length;
    }

    [Benchmark(Description="GetSpan large (8192)")]
    public int GetSpanLarge()
    {
        using var r = _conn!.Query("MATCH (b:Bin {id:3}) RETURN b.data");
        using var row = r.GetNext();
        using var v = (KuzuBlob)row.GetValue(0);
        return v.GetSpan().Length;
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
}
