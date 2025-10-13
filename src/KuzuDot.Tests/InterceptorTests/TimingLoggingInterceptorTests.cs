using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KuzuDot.Tests.InterceptorTests
{
    [TestClass]
    public sealed class TimingLoggingInterceptorTests : IDisposable
    {
        private Database? _db;
        private Connection? _conn;
        private TimingLoggingInterceptor? _timing;
    private readonly List<TimingLogEntry> _captured = new();
        private string _dbPath = string.Empty;

        [TestInitialize]
        public void Init()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "kuzu_timelog_" + Guid.NewGuid().ToString("N"));
            _db = Database.FromPath(_dbPath);
            _conn = _db.Connect();
            _timing = new TimingLoggingInterceptor(e => _captured.Add(e));
            KuzuInterceptorRegistry.Register(_timing);
            _conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
        }

        [TestCleanup]
        public void Cleanup() => Dispose();

        [TestMethod]
        public void Events_Fire_For_Prepare_Execute_Query()
        {
            using var ps = _conn!.Prepare("CREATE (:Person {name: $name, age: $age})").Bind("name", "Alice").Bind("age", 30);
            using var execResult = ps.Execute();
            using var queryResult = _conn.Query("MATCH (p:Person) RETURN p.name, p.age");

            // Snapshot
            var entries = _timing!.SnapshotEntries.ToList();
            // We expect at least one prepare, one execute, one query
            Assert.IsTrue(entries.Any(e => e.Operation == TimingLoggingInterceptor.OperationKind.Prepare));
            Assert.IsTrue(entries.Any(e => e.Operation == TimingLoggingInterceptor.OperationKind.ExecutePrepared));
            Assert.IsTrue(entries.Any(e => e.Operation == TimingLoggingInterceptor.OperationKind.Query));
            Assert.IsTrue(entries.All(e => e.Elapsed >= TimeSpan.Zero));
        }

        [TestMethod]
        public void Unregister_Stops_Emission()
        {
            // initial action
            using var ps = _conn!.Prepare("CREATE (:Person {name: $name, age: $age})").Bind("name", "Bob").Bind("age", 41);
            using var execResult = ps.Execute();
            var before = _timing!.SnapshotEntries.Count;
            Assert.IsTrue(before >= 2);

            // unregister
            KuzuInterceptorRegistry.Unregister(_timing);

            // further actions should not increase internal queue for this instance
            using var ps2 = _conn.Prepare("CREATE (:Person {name: $name, age: $age})").Bind("name", "Carol").Bind("age", 50);
            using var execResult2 = ps2.Execute();
            var after = _timing.SnapshotEntries.Count;
            Assert.AreEqual(before, after);
        }

        public void Dispose()
        {
            if (_timing is not null)
            {
                KuzuInterceptorRegistry.Unregister(_timing);
                _timing = null;
            }
            _conn?.Dispose();
            _db?.Dispose();
            try { if (Directory.Exists(_dbPath)) Directory.Delete(_dbPath, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
