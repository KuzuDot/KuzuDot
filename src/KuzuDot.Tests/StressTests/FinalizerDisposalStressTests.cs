using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot.Value;

namespace KuzuDot.Tests.StressTests
{
    /// <summary>
    /// Stress tests to verify that finalizers properly dispose of native resources for KuzuDot types (except QueryResult).
    /// These tests allocate many objects and rely on GC/finalizer cleanup, without explicit Dispose calls.
    /// QueryResult disposal during finalization causes access violations, so it is excluded here.
    /// </summary>
    [TestClass]
    [TestCategory("Stress")]
    public sealed class FinalizerDisposalStressTests
    {
        private const int Iterations = 500;
        private const int LargeIterations = 2000;

        [TestMethod]
        public void KuzuValue_FinalizerDisposal_ShouldNotLeakOrExhaust()
        {
            // Allocate many KuzuValue instances (various types) without Dispose
            for (int i = 0; i < LargeIterations; i++)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                _ = KuzuValueFactory.CreateInt64(i);
                _ = KuzuValueFactory.CreateString($"str_{i}");
                _ = KuzuValueFactory.CreateBool(i % 2 == 0);
                _ = KuzuValueFactory.CreateNull();
#pragma warning restore CA2000 // Dispose objects before losing scope
                // Do not call Dispose
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // If native resources are not released, subsequent allocations may fail or memory may grow excessively
            using var vTest = KuzuValueFactory.CreateInt32(123);
            Assert.IsInstanceOfType<KuzuInt32>(vTest);
        }

        [TestMethod]
        public void DataType_FinalizerDisposal_ShouldNotLeakOrExhaust()
        {
            for (int i = 0; i < Iterations; i++)
            {
                using var vInt = KuzuValueFactory.CreateInt32(i);
                var dt = vInt.DataTypeId;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestMethod]
        public void QuerySummary_FinalizerDisposal_ShouldNotLeakOrExhaust()
        {
            using var db = Database.FromMemory();
            using var conn = db.Connect();
            using var create = conn.Query("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));");
            using var insert = conn.Query("CREATE (:T {id: 1});");
            for (int i = 0; i < Iterations; i++)
            {
                using var result = conn.Query("MATCH (x:T) RETURN x.id;");
                var summary = result.GetQuerySummary();
                // Do not call Dispose on summary
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            using var result2 = conn.Query("MATCH (x:T) RETURN x.id;");
            using var summary2 = result2.GetQuerySummary();
            Assert.IsGreaterThanOrEqualTo(0, summary2.CompilingTimeMs);
            Assert.IsGreaterThanOrEqualTo(0, summary2.ExecutionTimeMs);
        }
    }
}
