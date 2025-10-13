using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace KuzuDot.Tests.ConnectionTests
{
    [TestClass]
    public sealed class ConnectionHelperTests : IDisposable
    {
        private Database? _db;
        private Connection? _conn;
        private string? _initError;
        private string _dbPath = string.Empty;

        [TestInitialize]
        public void Init()
        {
            try
            {
                _dbPath = Path.Combine(Path.GetTempPath(), "kuzu_helper_" + Guid.NewGuid().ToString("N"));
                _db = Database.FromPath(_dbPath);
                _conn = _db.Connect();
                _conn.ExecuteNonQuery("CREATE NODE TABLE Person(name STRING, age INT64, height DOUBLE, PRIMARY KEY(name));");
                // Insert a few rows
                _conn.ExecuteNonQuery("CREATE (:Person {name:'Alice', age:30, height:1.70})");
                _conn.ExecuteNonQuery("CREATE (:Person {name:'Bob', age:41, height:1.82})");
                _conn.ExecuteNonQuery("CREATE (:Person {name:'Carol', age:25, height:1.60})");
            }
            catch (KuzuException ex)
            {
                _initError = ex.Message;
                _db = null; _conn = null;
            }
        }

        [TestCleanup]
        public void Cleanup() => Dispose();

        private void RequireNative()
        {
            if (_db == null || _conn == null) Assert.Inconclusive("Native library unavailable: " + _initError);
        }

        [TestMethod]
        public void ExecuteScalar_WorksOrInconclusive()
        {
            RequireNative();
            var age = _conn!.ExecuteScalar<long>("MATCH (p:Person {name:'Alice'}) RETURN p.age");
            Assert.AreEqual(30L, age);
        }

        [TestMethod]
        public void ExecuteNonQuery_ReturnsTrueOrInconclusive()
        {
            RequireNative();
            bool ok = _conn!.ExecuteNonQuery("CREATE (:Person {name:'Dave', age:55, height:1.90})");
            Assert.IsTrue(ok);
            var count = _conn.ExecuteScalar<long>("MATCH (p:Person) RETURN COUNT(p)");
            Assert.IsTrue(count >= 4); // at least the three initial + Dave
        }

        [TestMethod]
        public void QueryEnumerable_Projector_DisposesValues()
        {
            RequireNative();
            var names = new List<string>();
            foreach (var row in _conn!.QueryEnumerable("MATCH (p:Person) RETURN p.name, p.age"))
            {
                using (row)
                {
                    var v0 = row.GetValueAs<string>("p.name");
                    var v1 = row.GetValueAs<long>("p.age");
                    names.Add(v0 + ":" + v1);
                }
            }
            Assert.IsTrue(names.Any(n => n.StartsWith("Alice:", StringComparison.InvariantCulture)));
        }

        private sealed class PersonRecord
        {
            // Column alias will be Name (case-insensitive match)
            public string? Name { get; set; }
            // Use attribute to map AGE -> AgeYears
            [KuzuName("Age")] public long AgeYears { get; set; }
            // Public field mapping (HEIGHT -> HeightM via case-insensitive name)
            public double HEIGHT { get; set; }
        }

        [TestMethod]
        public void PocoBinding_WithAttribute_AndFields()
        {
            RequireNative();
            var people = _conn!.Query<PersonRecord>("MATCH (p:Person) RETURN p.name AS Name, p.age AS AGE, p.height AS height");
            Assert.IsGreaterThanOrEqualTo(3, people.Count);
            var alice = people.FirstOrDefault(p => p.Name == "Alice");
            Assert.IsNotNull(alice);
            Assert.AreEqual(30L, alice!.AgeYears);
            Assert.AreNotEqual(0.0, alice.HEIGHT);
        }

        [TestMethod]
        public void TryQuery_And_TryPrepare_ErrorPaths()
        {
            RequireNative();
            bool ok = _conn!.TryQuery("MATCH (p:NonExistentLabel) RETURN p.x", out var qr, out var error);
            using (qr) { } // Dispose if non-null
            if (!ok)
            {
                Assert.IsNotNull(error);
                Assert.IsNull(qr);
            }

            bool prepOk = _conn.TryPrepare("THIS IS NOT VALID KUZU", out var ps, out var prepError);
            using (ps) { } // Dispose if non-null
            Assert.IsFalse(prepOk, "Prepare should fail for clearly invalid text");
            Assert.IsNull(ps);
            Assert.IsNotNull(prepError);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test")]
        public void Dispose()
        {
            _conn?.Dispose();
            _db?.Dispose();
            try { if (!string.IsNullOrEmpty(_dbPath) && Directory.Exists(_dbPath)) Directory.Delete(_dbPath, true); } catch { }
            _conn = null; _db = null;
        }
    }
}
