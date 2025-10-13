namespace KuzuDot.Tests.DatabaseTests
{
    [TestClass]
    public class DatabaseUnitTests
    {
        private string _testDbPath = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _testDbPath = Path.GetTempFileName();
            File.Delete(_testDbPath); // Remove the temp file so we can create a directory
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testDbPath))
            {
                Directory.Delete(_testDbPath, true);
            }
        }

        [TestMethod]
        public void Constructor_WithValidPath_ShouldCreateDatabase()
        {
            // Arrange & Act
            using var database = Database.FromPath(_testDbPath);

            // Assert
            Assert.IsNotNull(database);
        }

        [TestMethod]
        public void Constructor_WithInvalidPath_ShouldThrowException()
        {
            // Arrange
            string invalidPath = "/invalid/path/that/does/not/exist";

            // Act & Assert
            Assert.ThrowsExactly<KuzuException>(() => Database.FromPath(invalidPath));
        }

        [TestMethod]
        public void Constructor_WithInMemoryPath_ShouldCreateInMemoryDatabase()
        {
            // Arrange & Act
            using var database = Database.FromMemory();

            // Assert
            Assert.IsNotNull(database);
        }

        [TestMethod]
        public void Connect_ShouldReturnValidConnection()
        {
            // Arrange
            using var database = Database.FromMemory();

            // Act
            using var connection = database.Connect();

            // Assert
            Assert.IsNotNull(connection);
        }

        [TestMethod]
        public void Connect_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var database = Database.FromMemory();
            database.Dispose();

            // Act & Assert
            Assert.ThrowsExactly<ObjectDisposedException>(() => database.Connect());
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var database = Database.FromMemory();

            // Act & Assert
            database.Dispose(); // Should not throw
            database.Dispose(); // Second call should also not throw
        }
    }
}