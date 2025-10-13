using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KuzuDot.Tests.QueryResultTests;

/*
 * Regression: QueryResult concurrent destruction AccessViolation
 * --------------------------------------------------------------
 * Historical Failure (Pre-Fix):
 *   Under parallel query execution + rapid disposal, the native call
 *   kuzu_query_result_destroy intermittently triggered a fatal
 *   AccessViolationException (AV) in the finalizer or during explicit Dispose.
 *
 * Preconditions that increased likelihood:
 *   - Multiple threads each creating short-lived QueryResult instances.
 *   - Partial consumption (or no consumption) of result rows.
 *   - High iteration counts (hundreds to thousands) across several worker threads.
 *   - GC/finalizer pressure (fast churn + some results falling out of scope quickly).
 *
 * Root Cause (Observed Behavior):
 *   Not definitively diagnosed on native side, but strongly correlated with
 *   concurrent native destroy activity. Managed mitigations (lifetime guard,
 *   serialization via global lock) reduced but did not eliminate the crash.
 *
 * Resolution Implemented:
 *   QueryResult now enqueues native destroy operations to a dedicated single
 *   background thread, ensuring native destruction is serialized AND temporal
 *   separation from the allocating thread reduces race windows. Also retains a
 *   global pointer guard preventing double-destroy across wrappers.
 *
 * Purpose of This Test:
 *   Provide a focused, reproducible stress harness that used to surface the AV
 *   within seconds, so future refactors to destruction logic can be validated.
 *   If this test ever begins failing with an AV again, it indicates a regression
 *   in the queued destroy safety or a newly introduced native lifetime hazard.
 *
 * Guidance:
 *   - Keep execution time modest (< ~10s) so it stays in normal CI cycles.
 *   - Dial iteration/worker counts via env vars if deeper soak is needed.
 *   - Do NOT disable this test unless accompanied by an upstream native fix
 *     and a replacement stress methodology.
 */

[TestClass]
public sealed class QueryResultDestroyRegressionTests
{
    private static int GetWorkers()
    {
        int logical = Environment.ProcessorCount;
        int cap = Math.Max(2, Math.Min(logical, 8));
        var env = Environment.GetEnvironmentVariable("KUZU_STRESS_WORKERS");
        if (int.TryParse(env, out var parsed) && parsed > 0) return Math.Min(parsed, cap);
        return cap;
    }

    [TestMethod]
    public void QueryResultConcurrentCreateConsumeDisposeDoesNotCrash()
    {
        // Tunable knobs via env vars (allow escalation in ad-hoc runs)
        int workers = GetWorkers();
        int iterationsPerWorker = int.TryParse(Environment.GetEnvironmentVariable("KUZU_QR_REGRESSION_ITERS"), out var it) && it > 0 ? Math.Min(it, 4000) : 1200;
        int maxConsume = 3; // partial consumption exercises early-release patterns

        using var db = Database.FromMemory();
        using (var setup = db.Connect())
        {
            setup.NonQuery("CREATE NODE TABLE R(id INT64, PRIMARY KEY(id));");
            for (int i = 0; i < 256; i++) setup.Query($"CREATE (:R {{id: {i}}});");
        }

        var errors = new ConcurrentQueue<Exception>();
        var sw = Stopwatch.StartNew();

        Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, w =>
        {
            try
            {
                using var local = db.Connect();
                for (int i = 0; i < iterationsPerWorker; i++)
                {
                    // Alternate between full and partial consumption to vary lifecycle.
                    bool partial = (i & 1) == 0;
                    using var result = local.Query("MATCH (r:R) RETURN r.id;");
                    int consumed = 0;
                    while (result.HasNext() && (!partial || consumed < maxConsume))
                    {
                        using var row = result.GetNext();
                        using var v = row.GetValue(0);
                        _ = v.ToString();
                        consumed++;
                    }
                    //TestContext.WriteLine($"Completed one run: {w}");
                    // Some iterations deliberately avoid calling HasNext() again to
                    // leave internal iterator state in varied positions.
                }
            }
            catch (KuzuException ex) { errors.Enqueue(ex); }
            catch (ObjectDisposedException ex) { errors.Enqueue(ex); }
            catch (InvalidOperationException ex) { errors.Enqueue(ex); }
        });

        sw.Stop();

        if (!errors.IsEmpty)
        {
            Assert.Fail($"Unexpected managed exceptions encountered: {errors.Count}\nFirst: {errors.FirstOrDefault()}");
        }

        // If an AccessViolation were to occur it would have already aborted the test host.
        TestContext.WriteLine($"Regression run completed in {sw.Elapsed.TotalSeconds:F2}s with workers={workers} iters/worker={iterationsPerWorker}");
    }

    public TestContext TestContext { get; set; } = default!;
}
