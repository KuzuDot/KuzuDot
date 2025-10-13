using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KuzuDot.Value;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KuzuDot.Tests.StressTests;

[TestClass]
[TestCategory("Stress")]
public sealed class ConcurrencyStressTests
{
    private static readonly int MaxSuggestedWorkers = Math.Max(1, Math.Min(Environment.ProcessorCount, 8));

    private static int GetWorkerCount(int defaultCap)
    {
        // Allow KUZU_STRESS_WORKERS to override. Clamp to a safe ceiling (MaxSuggestedWorkers).
        var env = Environment.GetEnvironmentVariable("KUZU_STRESS_WORKERS");
        if (int.TryParse(env, out var parsed) && parsed > 0)
            return Math.Min(parsed, MaxSuggestedWorkers);
        return Math.Min(defaultCap, MaxSuggestedWorkers);
    }

    [ClassInitialize]
    public static void Init(TestContext _) { /* No gating; always run */ }

    [TestMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test")]
    public void ParallelSimpleSelectsMultiConnection()
    {
        using var db = Database.FromMemory();
        using var conn = db.Connect();
        conn.NonQuery("CREATE NODE TABLE Item(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 64; i++) conn.NonQuery($"CREATE (:Item {{id: {i}}});");
        int workers = GetWorkerCount(8);
        int iterationsPerWorker = 200;
        var errors = new ConcurrentQueue<Exception>();

        Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, w =>
        {
            try
            {
                using var local = db.Connect();
                for (int i = 0; i < iterationsPerWorker; i++)
                {
                    using var result = local.Query("MATCH (x:Item) RETURN x.id;");
                    while (result.HasNext())
                    {
                        using var row = result.GetNext();
                        var v = row.GetValue(0);
                        if (v is KuzuInt64 i64) _ = i64.Value; else _ = v.ToString();
                    }
                }
            }
            catch (Exception ex) { errors.Enqueue(ex); }
        });

        if (!errors.IsEmpty)
        {
            Assert.Fail($"Errors: {errors.Count}\n{string.Join(Environment.NewLine, errors.Take(5))}");
        }
    }

    [TestMethod]
    public void CancellationInterruptStorm()
    {
        using var db = Database.FromMemory();
        using var setup = db.Connect();
        setup.NonQuery("CREATE NODE TABLE Spin(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 8; i++) setup.NonQuery($"CREATE (:Spin {{id: {i}}});");
        int workers = GetWorkerCount(6);
        var errors = new ConcurrentQueue<Exception>();
        using var interruptConn = db.Connect();

        var interruptTask = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(2))
            {
                try { interruptConn.Interrupt(); } catch (KuzuException ex) { errors.Enqueue(ex); }
                await Task.Delay(10).ConfigureAwait(false);
            }
        });

        Task[] queryTasks = Enumerable.Range(0, workers).Select(i => Task.Run(() =>
        {
            using var c = db.Connect();
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (DateTime.UtcNow < end)
            {
                try
                {
                    using var r = c.Query("MATCH (s:Spin) RETURN s.id;");
                    if (r.HasNext())
                    {
                        using var row = r.GetNext();
                        using var v = row.GetValue(0);
                        if (v is KuzuInt64 i64) _ = i64.Value; else _ = v.ToString();
                    }
                }
                catch (KuzuException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException ex) { errors.Enqueue(ex); }
            }
        })).ToArray();

        Task.WaitAll(queryTasks);
        interruptTask.Wait();

        if (!errors.IsEmpty)
        {
            Assert.Fail($"Errors: {errors.Count}\n{string.Join(Environment.NewLine, errors.Take(5))}");
        }
    }

    [TestMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "<Pending>")]
    public async Task AsyncQueryRoundsWithCancellation()
    {
        using var db = Database.FromMemory();
        using var setup = db.Connect();
#pragma warning disable CA1849 // Call async methods when in an async method
        setup.NonQuery("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 128; i++) setup.NonQuery($"CREATE (:T {{id: {i}}});");
#pragma warning restore CA1849 // Call async methods when in an async method
        int workers = GetWorkerCount(4);
        using var cts = new CancellationTokenSource();
        var tasks = Enumerable.Range(0, workers).Select(i => Task.Run(async () =>
        {
            using var local = db.Connect();
            int completed = 0;
            while (!cts.IsCancellationRequested && completed < 80)
            {
                try
                {
                    using var result = await local.QueryAsync("MATCH (x:T) RETURN x.id;").ConfigureAwait(false);
                    int consumed = 0;
                    while (result.HasNext() && consumed < 3)
                    {
                        using var row = result.GetNext();
                        // Consume a value to exercise materialization; ignore content.
                        var _val = row.GetValue(0);
                        consumed++;
                    }
                    completed++;
                }
                catch (KuzuException) { }
                catch (ObjectDisposedException) { }
            }
        }, cts.Token)).ToArray();

        await Task.Delay(250).ConfigureAwait(false);
        cts.Cancel();
        try { await Task.WhenAll(tasks).ConfigureAwait(false); } catch (OperationCanceledException) { }
    }

    [TestMethod]
    public void SingleThreadLongIterationSmoke()
    {
        using var db = Database.FromMemory();
        using var c = db.Connect();
        c.NonQuery("CREATE NODE TABLE X(id INT64, PRIMARY KEY(id));");
        for (int i = 0; i < 8; i++) c.NonQuery($"CREATE (:X {{id: {i}}});");
        for (int iter = 0; iter < 2000; iter++)
        {
            using var r = c.Query("MATCH (x:X) RETURN x.id;");
            while (r.HasNext()) { using var row = r.GetNext(); using var v = row.GetValue(0); _ = v.ToString(); }
        }
    }

    [TestMethod]
    public void HighIterationParallelHarness()
    {
        // Intense loop over simple parallel select scenario to surface rare races.
        int iterations =  int.TryParse(Environment.GetEnvironmentVariable("KUZU_STRESS_HARNESS_ITERS"), out var iters) && iters > 0 ? Math.Min(iters, 10_000) : 500;
        int workersEnvCap = int.TryParse(Environment.GetEnvironmentVariable("KUZU_STRESS_WORKERS"), out var w) && w > 0 ? w : Environment.ProcessorCount;
        int workers = Math.Max(2, Math.Min(workersEnvCap, Environment.ProcessorCount * 2)); // allow slight oversubscription

        using var db = Database.FromMemory();
        using (var setup = db.Connect())
        {
            setup.NonQuery("CREATE NODE TABLE Y(id INT64, PRIMARY KEY(id));");
            for (int i = 0; i < 128; i++) setup.NonQuery($"CREATE (:Y {{id: {i}}});");
        }

        var errors = new ConcurrentQueue<Exception>();
        for (int outer = 0; outer < iterations; outer++)
        {
            Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, idx =>
            {
                try
                {
                    using var local = db.Connect();
                    using var result = local.Query("MATCH (y:Y) RETURN y.id;");
                    int count = 0;
                    while (result.HasNext() && count < 4) // partial consume keeps variety
                    {
                        using var row = result.GetNext();
                        var v = row.GetValue(0);
                        if (v is KuzuInt64 i64) _ = i64.Value; else _ = v.ToString();
                        count++;
                    }
                }
                catch (KuzuException ex) { errors.Enqueue(ex); }
                catch (ObjectDisposedException ex) { errors.Enqueue(ex); }
                catch (InvalidOperationException ex) { errors.Enqueue(ex); }
            });

            if (!errors.IsEmpty)
            {
                Assert.Fail($"Errors encountered at iteration {outer}. First: {errors.FirstOrDefault()} (total {errors.Count})");
            }
        }
    }
}
