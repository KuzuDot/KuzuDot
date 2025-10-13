using System.Text.RegularExpressions;

namespace KuzuDot.Tests.VersionTests
{
    /// <summary>
    /// Tests for KuzuVersion utility class
    /// </summary>
    [TestClass]
    public sealed class VersionUnitTests : IDisposable
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

    [TestCleanup]
    public void Cleanup() => Dispose();

        public void Dispose()
        {
            _database?.Dispose();
            _connection?.Dispose();
            _database = null;
            _connection = null;
            GC.SuppressFinalize(this);
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null || _connection == null)
            {
                throw new InvalidOperationException($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void GetVersion_ShouldReturnNonEmptyString()
        {
            EnsureNativeLibraryAvailable();

            var version = KuzuDot.Version.GetVersion();

            Assert.IsNotNull(version);
            Assert.IsFalse(string.IsNullOrEmpty(version));
            // Version should typically contain numbers and dots
            Assert.IsTrue(Regex.IsMatch(version, @"^\d+\.\d+"));
        }

        [TestMethod]
        public void GetStorageVersion_ShouldReturnPositiveNumber()
        {
            EnsureNativeLibraryAvailable();

            var storageVersion = KuzuDot.Version.GetStorageVersion();

            Assert.IsTrue(storageVersion > 0, "Storage version should be a positive number");
        }

        [TestMethod]
        public void GetVersion_MultipleCallsShouldReturnSameResult()
        {
            EnsureNativeLibraryAvailable();

            var version1 = KuzuDot.Version.GetVersion();
            var version2 = KuzuDot.Version.GetVersion();

            Assert.AreEqual(version1, version2);
        }

        [TestMethod]
        public void GetStorageVersion_MultipleCallsShouldReturnSameResult()
        {
            EnsureNativeLibraryAvailable();

            var storageVersion1 = KuzuDot.Version.GetStorageVersion();
            var storageVersion2 = KuzuDot.Version.GetStorageVersion();

            Assert.AreEqual(storageVersion1, storageVersion2);
        }
    }
}