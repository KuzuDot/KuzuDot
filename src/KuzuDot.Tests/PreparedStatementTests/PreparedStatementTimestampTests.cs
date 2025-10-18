namespace KuzuDot.Tests.PreparedStatementTests
{
    [TestClass]
    public sealed class PreparedStatementTimestampTests : IDisposable
    {
        private Database? _database;
        private Connection? _connection;
        private string? _initializationError;

        [TestInitialize]
        public void TestInitialize()
        {
            try
            {
                _database = Database.FromMemory();
                _connection = _database.Connect();
                _initializationError = null;
            }
            catch (KuzuException ex)
            {
                _database = null;
                _connection = null;
                _initializationError = ex.Message;
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _database?.Dispose();
            _connection = null;
            _database = null;
            GC.SuppressFinalize(this);
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null || _connection == null)
            {
                Assert.Inconclusive($"Native library unavailable: {_initializationError}");
            }
        }

        private static long NowMicro() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

        [TestMethod]
        public void BindTimestampVariants_ShouldSucceed()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE EVT(
                id INT64,
                ts_base TIMESTAMP,
                ts_ns TIMESTAMP_NS,
                ts_ms TIMESTAMP_MS,
                ts_sec TIMESTAMP_SEC,
                ts_tz TIMESTAMP_TZ,
                PRIMARY KEY(id)
            )");

            long baseMicros = NowMicro();
            long ns = baseMicros * 1000; // micros -> nanos
            long ms = baseMicros / 1000; // micros -> millis
            long sec = baseMicros / 1_000_000; // micros -> seconds
            long tzMicros = baseMicros; // microseconds (assume UTC)
            var baseTs = DateTime.UtcNow;

            using var stmt = _connection.Prepare(@"CREATE (:EVT {id: $id, ts_base: $b, ts_ns: $n, ts_ms: $m, ts_sec: $s, ts_tz: $z})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            stmt.BindInt64("id", 1);
            stmt.BindTimestamp("b", baseTs);
            stmt.BindTimestampNanoseconds("n", ns);
            stmt.BindTimestampMilliseconds("m", ms);
            stmt.BindTimestampSeconds("s", sec);
            stmt.BindTimestampWithTimeZone("z", DateTimeOffset.FromUnixTimeMilliseconds(tzMicros / 1000));

            using var insertRes = stmt.Execute();
            Assert.IsTrue(insertRes.IsSuccess);
        }

        [TestMethod]
        public void BindComplexObjectToSingleParameter_ShouldThrowArgumentException()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE Test(
                id INT64,
                name STRING,
                PRIMARY KEY(id)
            )");

            using var stmt = _connection.Prepare(@"CREATE (:Test {id: $id, name: $name})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            // This should throw an ArgumentException
            var complexObject = new { Id = 1, Name = "Test" };
            
            try
            {
                stmt.Bind("id", complexObject);
                Assert.Fail("Expected ArgumentException was not thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Cannot bind complex object", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(ex.Message.Contains("to parameter 'id'", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public void BindPrimitiveTypesToSingleParameter_ShouldSucceed()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE Test(
                id INT64,
                name STRING,
                is_active BOOL,
                PRIMARY KEY(id)
            )");

            using var stmt = _connection.Prepare(@"CREATE (:Test {id: $id, name: $name, is_active: $is_active})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            // These should all succeed
            stmt.Bind("id", 1L);
            stmt.Bind("name", "Test");
            stmt.Bind("is_active", true);
            
            using var result = stmt.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindAndExecuteBatch_ShouldProcessMultipleItems()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE BatchTest(
                id INT64,
                name STRING,
                value DOUBLE,
                PRIMARY KEY(id)
            )");

            using var stmt = _connection.Prepare(@"CREATE (:BatchTest {id: $id, name: $name, value: $value})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            var items = new[]
            {
                new { Id = 1L, Name = "Item1", Value = 1.5 },
                new { Id = 2L, Name = "Item2", Value = 2.5 },
                new { Id = 3L, Name = "Item3", Value = 3.5 }
            };

            var processedCount = stmt.BindAndExecuteBatch(items);
            Assert.AreEqual(3, processedCount);

            // Verify the data was inserted
            using var verify = _connection.Query("MATCH (b:BatchTest) RETURN COUNT(b) as count");
            using var verifyRow = verify.GetNext();
            Assert.AreEqual(3L, verifyRow.GetValueAs<long>(0));
        }

        [TestMethod]
        public void BindAndExecuteBatchWithErrorHandling_ShouldContinueOnErrors()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE ErrorTest(
                id INT64,
                name STRING,
                PRIMARY KEY(id)
            )");

            using var stmt = _connection.Prepare(@"CREATE (:ErrorTest {id: $id, name: $name})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            var items = new[]
            {
                new { Id = 1L, Name = "Valid1" },
                new { Id = 1L, Name = "Duplicate" }, // This will fail due to duplicate primary key
                new { Id = 2L, Name = "Valid2" }
            };

            var errors = new List<(object item, int index, Exception ex)>();
            var successCount = stmt.BindAndExecuteBatchWithErrorHandling(items, (item, index, ex) => 
            {
                errors.Add((item, index, ex));
            });

            Assert.AreEqual(2, successCount); // Only 2 should succeed
            Assert.AreEqual(1, errors.Count); // 1 should fail
            Assert.AreEqual(1, errors[0].index); // The duplicate should be at index 1
        }

        [TestMethod]
        public void BindAndExecute_SingleItem_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE SingleTest(
                id INT64,
                name STRING,
                PRIMARY KEY(id)
            )");

            using var stmt = _connection.Prepare(@"CREATE (:SingleTest {id: $id, name: $name})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            var item = new { Id = 1L, Name = "TestItem" };
            using var result = stmt.BindAndExecute(item);
            Assert.IsTrue(result.IsSuccess);

            // Verify the data was inserted
            using var verify = _connection.Query("MATCH (s:SingleTest) RETURN COUNT(s) as count");
            using var verifyRow = verify.GetNext();
            Assert.AreEqual(1L, verifyRow.GetValueAs<long>(0));
        }
    }
}
