using KuzuDot.Value;

namespace KuzuDot.Tests.ResourceManagement
{
    /// <summary>
    /// Tests to verify that the memory management fixes prevent protected memory access errors
    /// </summary>
    [TestClass]
    public sealed class MemoryManagementFixTests : IDisposable
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
                Assert.Inconclusive($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void KuzuVersion_GetVersion_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            var v1 = Version.GetVersion();
            var v2 = Version.GetVersion();
            Assert.IsFalse(string.IsNullOrEmpty(v1));
            Assert.AreEqual(v1, v2);
        }

        [TestMethod]
        public void QueryResult_ToString_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            using var result = _connection!.Query("RETURN 1");
            var s1 = result.ToString();
            var s2 = result.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(s1));
            Assert.AreEqual(s1, s2);
        }

        [TestMethod]
        public void PreparedStatement_ErrorMessage_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            try
            {
                using var stmt = _connection!.Prepare("INVALID SYNTAX HERE");
                var e1 = stmt.ErrorMessage;
                var e2 = stmt.ErrorMessage;
                Assert.AreEqual(e1, e2);
            }
            catch (KuzuException)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void KuzuValue_ToString_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            using var value = KuzuValueFactory.CreateString("Test Value");
            var s1 = value.ToString();
            var s2 = value.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(s1));
            Assert.AreEqual(s1, s2);
        }

        [TestMethod]
        public void KuzuValue_GetString_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            using var value = KuzuValueFactory.CreateString("Test Value");
            var s1 = value.Value;
            var s2 = value.Value;
            Assert.AreEqual("Test Value", s1);
            Assert.AreEqual(s1, s2);
        }

        [TestMethod]
        public void MultipleStringOperations_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            for (int i = 0; i < 5; i++)
            {
                var version = Version.GetVersion();
                using var value = KuzuValueFactory.CreateString($"Test{i}");
                var strVal = value.Value;
                var toStringVal = value.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(version));
                Assert.AreEqual($"Test{i}", strVal);
                Assert.IsFalse(string.IsNullOrEmpty(toStringVal));
            }
        }
    }
}