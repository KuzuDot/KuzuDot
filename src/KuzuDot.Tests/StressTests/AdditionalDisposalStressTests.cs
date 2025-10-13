using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KuzuDot.Tests.StressTests;

[TestClass]
[TestCategory("Stress")]
public sealed class AdditionalDisposalStressTests
{
    private static int GetWorkers(int cap)
    {
        var env = Environment.GetEnvironmentVariable("KUZU_STRESS_WORKERS");
        if (int.TryParse(env, out var parsed) && parsed > 0) return Math.Min(parsed, cap);
        return Math.Min(Environment.ProcessorCount, cap);
    }

    [TestMethod]
    public void PreparedStatementCreateBindDisposeParallel()
    {
        using var db = Database.FromMemory();
        using var conn = db.Connect();
        conn.NonQuery("CREATE NODE TABLE PS(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 32; i++) conn.NonQuery($"CREATE (:PS {{id: {i}}});");

        int workers = GetWorkers(8);
        int iterationsPerWorker = 200;
        var errors = new ConcurrentQueue<Exception>();

        Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, w =>
        {
            try
            {
                using var local = db.Connect();
                for (int i = 0; i < iterationsPerWorker; i++)
                {
                    using var ps = local.Prepare("MATCH (p:PS) WHERE p.id=$id RETURN p.id;");
                    ps.Bind("id", i % 32);
                    using var result = ps.Execute();
                    if (result.HasNext())
                    {
                        using var row = result.GetNext();
                        using var v = row.GetValue(0);
                        var _ignore = v.ToString();
                    }
                }
            }
            catch (KuzuException ex) { errors.Enqueue(ex); }
            catch (ObjectDisposedException ex) { errors.Enqueue(ex); }
            catch (InvalidOperationException ex) { errors.Enqueue(ex); }
        });

        if (!errors.IsEmpty)
        {
            Assert.Fail($"Errors: {errors.Count}\n{string.Join(Environment.NewLine, errors.Take(5))}");
        }
    }

    [TestMethod]
    public void QuerySummaryRapidAcquireDispose()
    {
        using var db = Database.FromMemory();
        using var conn = db.Connect();
        conn.NonQuery("CREATE NODE TABLE QS(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 8; i++) conn.NonQuery($"CREATE (:QS {{id: {i}}});");

        int workers = GetWorkers(6);
        int iterations = 300;
        var errors = new ConcurrentQueue<Exception>();

        Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, _ =>
        {
            try
            {
                using var local = db.Connect();
                for (int i = 0; i < iterations; i++)
                {
                    using var r = local.Query("MATCH (q:QS) RETURN q.id;");
                    // Partially consume one value (if any) to ensure some iteration.
                    if (r.HasNext()) { using var row = r.GetNext(); using var v = row.GetValue(0); var _ignore1 = v.ToString(); }
                    // Acquire summary and dispose rapidly.
                    var summary = r.GetQuerySummary();
                    // Access a property to exercise native call.
                    var _execMs = summary.ExecutionTimeMs; // force native call
                    summary.Dispose();
                }
            }
            catch (KuzuException ex) { errors.Enqueue(ex); }
            catch (ObjectDisposedException ex) { errors.Enqueue(ex); }
            catch (InvalidOperationException ex) { errors.Enqueue(ex); }
        });

        if (!errors.IsEmpty)
        {
            Assert.Fail($"Errors: {errors.Count}\n{string.Join(Environment.NewLine, errors.Take(5))}");
        }
    }

    [TestMethod]
    public void FlatTupleValueDisposeChurn()
    {
        using var db = Database.FromMemory();
        using var conn = db.Connect();
        conn.NonQuery("CREATE NODE TABLE FT(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 128; i++) conn.NonQuery($"CREATE (:FT {{id: {i}}});");

        int workers = GetWorkers(6);
        int loops = 250;
        var errors = new ConcurrentQueue<Exception>();

        Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, _ =>
        {
            try
            {
                using var local = db.Connect();
                for (int i = 0; i < loops; i++)
                {
                    using var r = local.Query("MATCH (f:FT) RETURN f.id;");
                    int consumed = 0;
                    while (r.HasNext() && consumed < 5)
                    {
                        using var row = r.GetNext();
                        using var v = row.GetValue(0);
                        var _ignore2 = v.ToString();
                        consumed++;
                    }
                }
            }
            catch (KuzuException ex) { errors.Enqueue(ex); }
            catch (ObjectDisposedException ex) { errors.Enqueue(ex); }
            catch (InvalidOperationException ex) { errors.Enqueue(ex); }
        });

        if (!errors.IsEmpty)
        {
            Assert.Fail($"Errors: {errors.Count}\n{string.Join(Environment.NewLine, errors.Take(5))}");
        }
    }
}
