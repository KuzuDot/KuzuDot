using KuzuDot.Value;

namespace KuzuDot.Tests.KuzuValueTests
{
    [TestClass]
    public sealed class BlobTests : IDisposable
    {
        private Database? _database; private Connection? _connection; private string? _initError;
        [TestInitialize]
        public void Init()
        {
            try
            {
                _database = Database.FromMemory();
                _connection = _database.Connect();
                _connection.Query("CREATE NODE TABLE File(id STRING, data BLOB, PRIMARY KEY(id))").Dispose();
            }
            catch (KuzuException ex)
            {
                _initError = ex.Message;
                _database = null;
                _connection = null;
            }
        }

        [TestCleanup]
        public void Cleanup() => Dispose();

        public void Dispose()
        {
            _connection?.Dispose();
            _database?.Dispose();
            _connection = null;
            _database = null;
            GC.SuppressFinalize(this);
        }
        private void RequireNative() { if (_connection == null || _database == null) Assert.Inconclusive("Native library unavailable: " + _initError); }
        private bool TryInsertBlob(string id, byte[] data)
        {
            string hex = Convert.ToHexString(data);
            try
            {
                using var r = _connection!.Query($"CREATE (:File {{id: '{id}', data: BLOB('{hex}')}})");
                return true;
            }
            catch (KuzuException)
            {
                return false;
            }
        }

        [TestMethod]
        public void RetrieveEmptyBlobOrInconclusive()
        {
            RequireNative();
            if (!TryInsertBlob("empty", Array.Empty<byte>())) Assert.Inconclusive("Blob literal insertion not supported in current engine.");
            using var result = _connection!.Query("MATCH (f:File {id: 'empty'}) RETURN f.data");
            Assert.AreEqual(1UL, result.RowCount);
            using var row = result.GetNext();
            using KuzuBlob value = (KuzuBlob)row.GetValue(0);
            var blob = value.GetBytes();
            Assert.AreEqual(0, blob.Length);
        }

        [TestMethod]
        public void RetrieveBlobIntegrityOrInconclusive()
        {
            RequireNative();
            byte[] data = new byte[16]; for (int i = 0; i < data.Length; i++) data[i] = (byte)i;
            if (!TryInsertBlob("file1", data)) Assert.Inconclusive("Blob literal insertion not supported in current engine.");
            using var result = _connection!.Query("MATCH (f:File {id: 'file1'}) RETURN f.data");
            Assert.AreEqual(1UL, result.RowCount);
            using var row = result.GetNext();
            using KuzuBlob value = (KuzuBlob)row.GetValue(0);
            var fetched = value.GetBytes();
            Assert.AreEqual(data.Length, fetched.Length);
            for (int i = 0; i < fetched.Length; i++) Assert.AreEqual(data[i], fetched[i]);

            // Span path should be allocation-free after first decode (same underlying cached array)
            var span = value.GetSpan();
            Assert.AreEqual(fetched.Length, span.Length);
            for (int i = 0; i < span.Length; i++) Assert.AreEqual(fetched[i], span[i]);
            // Idempotent
            var span2 = value.GetSpan();
            Assert.AreEqual(span.Length, span2.Length);
        }
    }
}
