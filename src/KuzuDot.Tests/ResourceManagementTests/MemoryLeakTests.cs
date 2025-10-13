using KuzuDot.Value;

namespace KuzuDot.Tests.ResourceManagement
{
    /// <summary>
    /// Tests to detect memory leaks and verify proper native resource cleanup.
    /// These tests focus on explicit disposal without relying on finalizers.
    /// </summary>
    [TestClass]
    public sealed class MemoryLeakTests : IDisposable
    {
        private Database? _database;
        private string? _initializationError;

        [TestInitialize]
        public void TestInitialize()
        {
            try
            {
                _database = Database.FromMemory();
                _initializationError = null;
            }
            catch (KuzuException ex)
            {
                _database = null;
                _initializationError = ex.Message;
            }
        }

        public void Dispose()
        {
            _database?.Dispose();
            _database = null;
            GC.SuppressFinalize(this);
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null)
            {
                throw new InvalidOperationException($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void RepetitiveConnectionCreation_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();
            var initialMemory = GC.GetTotalMemory(true);
            const int iterations = 100;
            for (int i = 0; i < iterations; i++)
            {
                using var connection = _database!.Connect();
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _ = GC.GetTotalMemory(false);
                }
            }
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;
            Assert.IsTrue(memoryGrowth < 1_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void RepetitiveQueryExecution_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();
            using var connection = _database!.Connect();
            using (var setupResult = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));")) { Assert.IsTrue(setupResult.IsSuccess); }
            var initialMemory = GC.GetTotalMemory(true);
            const int iterations = 50;
            for (int i = 0; i < iterations; i++)
            {
                string query = $"CREATE (:Person {{name: 'Person{i}', age: {20 + (i % 50)}}});";
                using var result = connection.Query(query);
                Assert.IsTrue(result.IsSuccess);
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _ = GC.GetTotalMemory(false);
                }
            }
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;
            Assert.IsTrue(memoryGrowth < 2_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void RepetitivePreparedStatementExecution_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();
            using var connection = _database!.Connect();
            using (var setupResult = connection.Query("CREATE NODE TABLE TestNode(id INT64, value STRING, PRIMARY KEY(id));")) { Assert.IsTrue(setupResult.IsSuccess); }
            var initialMemory = GC.GetTotalMemory(true);
            const int iterations = 50;
            for (int i = 0; i < iterations; i++)
            {
                using var statement = connection.Prepare("CREATE (:TestNode {id: $id, value: $value});");
                statement.BindInt64("id", i);
                statement.BindString("value", $"TestValue{i}");
                using var result = statement.Execute();
                Assert.IsTrue(result.IsSuccess);
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _ = GC.GetTotalMemory(false);
                }
            }
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;
            Assert.IsTrue(memoryGrowth < 2_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void KuzuValueCreation_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();
            var initialMemory = GC.GetTotalMemory(true);
            const int iterations = 1000;
            for (int i = 0; i < iterations; i++)
            {
                using (var stringValue = KuzuValueFactory.CreateString($"TestString{i}"))
                using (var intValue = KuzuValueFactory.CreateInt64(i))
                using (var boolValue = KuzuValueFactory.CreateBool(i % 2 == 0))
                using (var nullValue = KuzuValueFactory.CreateNull())
                {
                    Assert.IsInstanceOfType<KuzuString>(stringValue);
                    Assert.IsInstanceOfType<KuzuInt64>(intValue);
                    Assert.IsInstanceOfType<KuzuBool>(boolValue);
                    Assert.AreEqual($"TestString{i}", ((KuzuString)stringValue).Value);
                    Assert.AreEqual(i, ((KuzuInt64)intValue).Value);
                    Assert.AreEqual(i % 2 == 0, ((KuzuBool)boolValue).Value);
                    Assert.IsTrue(nullValue.IsNull());
                }
                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _ = GC.GetTotalMemory(false);
                }
            }
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;
            Assert.IsTrue(memoryGrowth < 1_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void NestedResourceUsage_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();
            var initialMemory = GC.GetTotalMemory(true);
            const int iterations = 20;
            for (int i = 0; i < iterations; i++)
            {
                using var tempDb = Database.FromMemory();
                using var connection = tempDb.Connect();
                using (var schemaResult = connection.Query("CREATE NODE TABLE TempNode(id INT64, PRIMARY KEY(id));")) { Assert.IsTrue(schemaResult.IsSuccess); }
                for (int j = 0; j < 10; j++)
                {
                    using var statement = connection.Prepare("CREATE (:TempNode {id: $id});");
                    statement.BindInt64("id", j);
                    using var result = statement.Execute();
                    Assert.IsTrue(result.IsSuccess);
                }
                using (var queryResult = connection.Query("MATCH (n:TempNode) RETURN COUNT(n);"))
                {
                    Assert.IsTrue(queryResult.IsSuccess);
                    Assert.AreEqual(1UL, queryResult.ColumnCount);
                }
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _ = GC.GetTotalMemory(false);
                }
            }
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;
            Assert.IsTrue(memoryGrowth < 3_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }
    }
}