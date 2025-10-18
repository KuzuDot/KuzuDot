using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace KuzuDot.Tests.PocoBinderTests
{
    [TestClass]
    public sealed class PocoBinderUnitTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly Database _db;
        private readonly Connection _conn;

        public PocoBinderUnitTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _db = Database.FromPath(_dbPath);
            _conn = _db.Connect();
            _conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
        }

        [TestMethod]
        public void BindObject_InsertsRow()
        {
            using var ps = _conn.Prepare("CREATE (:Person {name: $name, age: $age});");
            var poco = new PersonInsert
            {
                Name = "Alice",
                Age = 42
            };
            ps.Bind(poco);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void QueryT_Materializes()
        {
            // Seed
            using (var ps = _conn.Prepare("CREATE (:Person {name: $name, age: $age});"))
            {
                ps.Bind(new PersonInsert { Name = "Bob", Age = 30 });
                using var r = ps.Execute();
            }
            var rows = _conn.Query<PersonRow>("MATCH (p:Person) RETURN p.name AS name, p.age AS age;");
            Assert.IsTrue(rows.Count > 0);
            var first = rows[0];
            Assert.AreEqual("Bob", first.Name);
            Assert.AreEqual(30L, first.Age);
        }

        [TestMethod]
        public void BindObject_GuidAndDecimal_StringFallback()
        {
            // Extend schema minimally (decimal represented via string fallback binding)
            _conn.TryQuery("CREATE NODE TABLE Extra(id STRING, amount STRING, PRIMARY KEY(id));", out _, out _); // ignore if already exists
            var guid = Guid.NewGuid();
            using (var ps = _conn.Prepare("CREATE (:Extra {id: $id, amount: $amount});"))
            {
                ps.Bind(new ExtraInsert { Id = guid, Amount = 12.345m });
                using var r = ps.Execute();
                Assert.IsTrue(r.IsSuccess);
            }
        }

        [TestMethod]
        public void AttributeNameMapping_BindsCustomNames()
        {
            _conn.TryQuery("CREATE NODE TABLE Mapped(customName STRING, customAge INT64, PRIMARY KEY(customName));", out _, out _); // ignore if exists
            using (var ps = _conn.Prepare("CREATE (:Mapped {customName: $name_alias, customAge: $years});"))
            {
                ps.Bind(new AliasInsert { Name = "Dana", Years = 55 });
                using var r = ps.Execute();
                Assert.IsTrue(r.IsSuccess);
            }
        }

        [TestMethod]
        public void QueryAsyncT_Cancellation()
        {
            using (var ps = _conn.Prepare("CREATE (:Person {name: $name, age: $age});"))
            {
                ps.Bind(new PersonInsert { Name = "Carol", Age = 10 });
                using var r = ps.Execute();
            }
            using var cts = new System.Threading.CancellationTokenSource();
            var task = _conn.QueryAsync<PersonRow>("MATCH (p:Person) RETURN p.name AS name, p.age AS age;", cts.Token);
            cts.Cancel();
            try
            {
                task.GetAwaiter().GetResult();
                Assert.Fail("Expected cancellation");
            }
            catch (OperationCanceledException) { }
        }

        [TestMethod]
        public void QueryT_MatchStrippedPrefixes_MapsToPoco()
        {
            // Seed data with specific values
            using (var ps = _conn.Prepare("CREATE (:Person {name: $name, age: $age});"))
            {
                ps.Bind(new PersonInsert { Name = "test", Age = 34 });
                using var r = ps.Execute();
                Assert.IsTrue(r.IsSuccess);
            }

            // Query with MATCH using specific values and map to POCO
            var people = _conn.Query<Person>("MATCH (p:Person) RETURN p.name, p.age");
            
            Assert.IsNotEmpty(people, "Expected at least one person to be returned");
            var person = people[0];
            Assert.AreEqual("test", person.Name);
            Assert.AreEqual(34L, person.Age);
            Assert.AreEqual(string.Empty, person.EyeColor); // Default value for unmapped property
        }

        private sealed class PersonInsert
        {
            public string Name { get; set; } = string.Empty;
            public long Age { get; set; }
        }

        private sealed class PersonRow
        {
            public string Name { get; set; } = string.Empty;
            public long Age { get; set; }
        }

        private sealed class ExtraInsert
        {
            public Guid Id { get; set; }
            public decimal Amount { get; set; }
        }

        private sealed class AliasInsert
        {
            [KuzuName("name_alias")] public string Name { get; set; } = string.Empty;
            [KuzuName("years")] public long Years { get; set; }
        }

        private sealed class Person
        {
            public string Name { get; set; } = string.Empty;
            public long Age { get; set; }

            public string EyeColor { get; set; } = string.Empty;
        }

        public void Dispose()
        {
            _conn?.Dispose();
            _db?.Dispose();
            try { if (Directory.Exists(_dbPath)) Directory.Delete(_dbPath, recursive: true); } catch (System.IO.IOException) { } catch (System.UnauthorizedAccessException) { }
        }
    }
}
