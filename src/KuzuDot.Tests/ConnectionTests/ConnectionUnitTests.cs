namespace KuzuDot.Tests.ConnectionTests
{
    /// <summary>
    /// Comprehensive tests for Connection functionality including basic operations, 
    /// configuration, prepared statements, and resource management.
    /// </summary>
    [TestClass]
    public sealed class ConnectionUnitTests : IDisposable
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
                // Native library not available in test environment
                _database = null;
                _initializationError = ex.Message;
            }
        }

        [TestCleanup]
        public void TestCleanup() => Dispose();

        public void Dispose()
        {
            _database?.Dispose();
            _database = null;
            GC.SuppressFinalize(this);

            // Force garbage collection to ensure native resources are cleaned up
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null)
            {
                throw new InvalidOperationException($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        #region Basic Query Operations

        [TestMethod]
        public void Query_WithValidQuery_ShouldReturnQueryResult()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            string query = "CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));";

            using var result = connection.Query(query);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void Query_WithInvalidQuery_ShouldThrowException()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            string invalidQuery = "INVALID SQL QUERY";

            Assert.ThrowsExactly<KuzuException>(() => connection.NonQuery(invalidQuery));
        }

        [TestMethod]
        public void Query_AfterDispose_ShouldThrowObjectDisposedException()
        {
            EnsureNativeLibraryAvailable();

            var connection = _database!.Connect();
            connection.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => connection.NonQuery("SELECT 1;"));
        }

        #endregion

        #region Schema/Table Info

        [TestMethod]
        public void GetTables_ShouldReturnAllTables()
        {
            EnsureNativeLibraryAvailable();
            using var connection = _database!.Connect();
            // Create tables
            connection.NonQuery("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            connection.NonQuery("CREATE NODE TABLE Company(name STRING, PRIMARY KEY(name));");
            connection.NonQuery("CREATE REL TABLE WorksAt(FROM Person TO Company, since INT64);");

            var tables = connection.GetTables();
            Assert.IsNotNull(tables);
            Assert.IsGreaterThanOrEqualTo(3, tables.Count, "Should have at least 3 tables");
            Assert.IsTrue(tables.Any(t => t.Name == "Person"));
            Assert.IsTrue(tables.Any(t => t.Name == "Company"));
            Assert.IsTrue(tables.Any(t => t.Name == "WorksAt"));
        }

        [TestMethod]
        public void GetNodeTables_ShouldReturnOnlyNodeTables()
        {
            EnsureNativeLibraryAvailable();
            using var connection = _database!.Connect();
            connection.NonQuery("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            connection.NonQuery("CREATE NODE TABLE Company(name STRING, PRIMARY KEY(name));");
            connection.NonQuery("CREATE REL TABLE WorksAt(FROM Person TO Company, since INT64);");

            var nodeTables = connection.GetNodeTables();
            Assert.IsNotNull(nodeTables);
            Assert.IsTrue(nodeTables.All(t => t.Type == "NODE"));
            Assert.IsTrue(nodeTables.Any(t => t.Name == "Person"));
            Assert.IsTrue(nodeTables.Any(t => t.Name == "Company"));
            Assert.IsFalse(nodeTables.Any(t => t.Name == "WorksAt"));
        }

        [TestMethod]
        public void GetRelTables_ShouldReturnOnlyRelTables()
        {
            EnsureNativeLibraryAvailable();
            using var connection = _database!.Connect();
            connection.NonQuery("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            connection.NonQuery("CREATE NODE TABLE Company(name STRING, PRIMARY KEY(name));");
            connection.NonQuery("CREATE REL TABLE WorksAt(FROM Person TO Company, since INT64);");

            var relTables = connection.GetRelTables();
            Assert.IsNotNull(relTables);
            Assert.IsTrue(relTables.All(t => t.Type == "REL"));
            Assert.IsTrue(relTables.Any(t => t.Name == "WorksAt"));
            Assert.IsFalse(relTables.Any(t => t.Name == "Person"));
            Assert.IsFalse(relTables.Any(t => t.Name == "Company"));
        }

        [TestMethod]
        public void GetTableInfo_ShouldReturnSchemaProperties()
        {
            EnsureNativeLibraryAvailable();
            using var connection = _database!.Connect();
            connection.NonQuery("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            var tables = connection.GetTables();
            var personTable = tables.First(t => t.Name == "Person");
            var info = connection.GetTableInfo(personTable.Id);
            Assert.IsNotNull(info);
            Assert.IsTrue(info.Any(p => p.Name == "name" && p.Type == "STRING"));
            Assert.IsTrue(info.Any(p => p.Name == "age" && p.Type == "INT64"));
            Assert.IsTrue(info.Any(p => p.IsPrimaryKey));
        }

        #endregion

        #region Prepared Statement Operations

        [TestMethod]
        public void Prepare_WithValidQuery_ShouldReturnPreparedStatement()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // First create a table
            using var createResult = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");

            string query = "CREATE (:Person {name: $name, age: $age});";

            using var preparedStatement = connection.Prepare(query);

            Assert.IsNotNull(preparedStatement);
            Assert.IsTrue(preparedStatement.IsSuccess);
        }

        [TestMethod]
        public void Prepare_WithInvalidQuery_ShouldThrowException()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            string invalidQuery = "INVALID PREPARED QUERY";
            var ex = Assert.ThrowsExactly<KuzuException>(() => connection.Prepare(invalidQuery));
            Assert.IsTrue(ex.Message.StartsWith("Prepare failed:", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Prepare_AfterDispose_ShouldThrowObjectDisposedException()
        {
            EnsureNativeLibraryAvailable();

            var connection = _database!.Connect();
            connection.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => connection.Prepare("SELECT 1;"));
        }

        [TestMethod]
        public void Execute_WithValidPreparedStatement_ShouldReturnQueryResult()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // Create table
            using var createResult = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");

            using var preparedStatement = connection.Prepare("CREATE (:Person {name: $name, age: $age});");
            preparedStatement.BindString("name", "Alice");
            preparedStatement.BindInt64("age", 30);

            using var result = preparedStatement.Execute();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
        }

        #endregion

        #region Connection Configuration

        [TestMethod]
        public void MaxNumThreadsForExecution_GetAndSet_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // Get initial value
            var initialThreads = connection.MaxNumThreadsForExecution;
            Assert.IsGreaterThan<ulong>(0, initialThreads, "Initial thread count should be positive");

            // Set new value
            var newThreadCount = initialThreads == 1 ? 2UL : 1UL;
            connection.MaxNumThreadsForExecution = newThreadCount;

            // Verify it was set
            Assert.AreEqual(newThreadCount, connection.MaxNumThreadsForExecution);

            // Set back to original
            connection.MaxNumThreadsForExecution = initialThreads;
            Assert.AreEqual(initialThreads, connection.MaxNumThreadsForExecution);
        }

        [TestMethod]
        public void SetQueryTimeout_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // Test setting various timeout values
            connection.SetQueryTimeout(1000); // 1 second
            connection.SetQueryTimeout(5000); // 5 seconds
            connection.SetQueryTimeout(0);    // No timeout
        }

        [TestMethod]
        public void Interrupt_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // Test that interrupt doesn't throw even when no query is running
            connection.Interrupt();
        }

        [TestMethod]
        public void MaxNumThreadsForExecution_WithZero_ShouldHandleGracefully()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // Setting thread count to 0 should either throw or be handled gracefully
            try
            {
                connection.MaxNumThreadsForExecution = 0;
                // If it doesn't throw, verify that it's been set to some valid value
                Assert.IsGreaterThanOrEqualTo<ulong>(1, connection.MaxNumThreadsForExecution);
            }
            catch (KuzuException)
            {
                // It's also valid for this to throw an exception
            }
        }

        [TestMethod]
        public void MaxNumThreadsForExecution_WithVeryLargeValue_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            var originalThreads = connection.MaxNumThreadsForExecution;

            try
            {
                // Try setting a very large value
                connection.MaxNumThreadsForExecution = 1000;

                // The implementation might cap this to a reasonable value
                var actualThreads = connection.MaxNumThreadsForExecution;
                Assert.IsGreaterThan<ulong>(0, actualThreads);
                Assert.IsLessThanOrEqualTo<ulong>(1000, actualThreads);
            }
            finally
            {
                // Restore original value
                connection.MaxNumThreadsForExecution = originalThreads;
            }
        }

        #endregion

        #region Resource Management

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            var connection = _database!.Connect();

            connection.Dispose(); // Should not throw
            connection.Dispose(); // Second call should also not throw
        }

        [TestMethod]
        public async Task Interrupt_DuringQuery_ShouldNotCrash()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();

            // Create a simple table for testing
#pragma warning disable CA1849 // Call async methods when in an async method
            using (var createResult = connection.Query("CREATE NODE TABLE TestNode(id INT64, PRIMARY KEY(id))"))
            {
                Assert.IsTrue(createResult.IsSuccess);
            }
#pragma warning restore CA1849 // Call async methods when in an async method

            // Insert some data
#pragma warning disable CA1849 // Call async methods when in an async method
            using (var insertResult = connection.Query("CREATE (n:TestNode {id: 1})"))
            {
                Assert.IsTrue(insertResult.IsSuccess);
            }
#pragma warning restore CA1849 // Call async methods when in an async method

            // Test that interrupt doesn't crash the connection
            var interruptTask = Task.Run(async () =>
            {
                await Task.Delay(50, TestContext.CancellationToken).ConfigureAwait(false); // Wait a bit, then interrupt
                connection.Interrupt();
            }, TestContext.CancellationToken);

            try
            {
                // Run a simple query (in a real scenario, this would be a long-running query)
#pragma warning disable CA1849 // Call async methods when in an async method
                using var queryResult = connection.Query("MATCH (n:TestNode) RETURN n.id");
                // The query might succeed or be interrupted, both are valid
#pragma warning restore CA1849 // Call async methods when in an async method
            }
            catch (KuzuException)
            {
                // Interruption might cause an exception, which is expected
            }

            await interruptTask.ConfigureAwait(false);
        }

        public TestContext TestContext { get; set; }

        #endregion
    }
}