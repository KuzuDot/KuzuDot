using KuzuDot.Value;
using System;
using System.Collections.Generic;

namespace KuzuDot.Tests.QueryResultTests
{
    [TestClass]
    public sealed class QueryResultNodeFlatTupleTests : IDisposable
    {
        private Database? _database;
        private Connection? _connection;
        private string? _initializationError;

        [TestInitialize]
        public void Init()
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
            _connection?.Dispose();
            _database?.Dispose();
            _connection = null;
            _database = null;
            GC.SuppressFinalize(this);
        }

        private void EnsureNativeAvailable()
        {
            if (_database == null || _connection == null)
            {
                Assert.Inconclusive($"Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void QueryResult_ToString_And_ResetIterator_Works()
        {
            EnsureNativeAvailable();

            using var create = _connection!.Query("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id));");
            using var insert1 = _connection.Query("CREATE (:Person {id: 1, name: 'Alice'});");
            using var insert2 = _connection.Query("CREATE (:Person {id: 2, name: 'Bob'});");

            using var result = _connection.Query("MATCH (p:Person) RETURN p.name AS name, p.id AS id ORDER BY id;");
            var text = result.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(text));
            Assert.IsTrue(text.StartsWith("QueryResult(Rows=", StringComparison.Ordinal));

            // Iterate a single row then reset and ensure we can read again
            if (!result.HasNext()) Assert.Fail("Expected rows");
            using (var row = result.GetNext()) { /* consume first row */ }
            // Reset iterator
            result.ResetIterator();
            int count = 0;
            while (result.HasNext()) { using var r = result.GetNext(); count++; }
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void QueryResult_RowLoop_And_FlatTuple_GetValue_ByIndexAndName()
        {
            EnsureNativeAvailable();

            using var create = _connection!.Query("CREATE NODE TABLE Item(name STRING, qty INT64, PRIMARY KEY(name));");
            using var insert1 = _connection.Query("CREATE (:Item {name: 'W', qty: 5});");
            using var insert2 = _connection.Query("CREATE (:Item {name: 'X', qty: 7});");

            using var result = _connection.Query("MATCH (i:Item) RETURN i.name AS name, i.qty AS qty ORDER BY name;");

            int seen = 0;
            while (result.HasNext())
            {
                using var row = result.GetNext();
                // by index
                using var nameVal = row.GetValue(0);
                using var qtyVal = row.GetValue(1);
                Assert.IsInstanceOfType<KuzuString>(nameVal);
                Assert.IsInstanceOfType<KuzuInt64>(qtyVal);

                // by name
                using var nameByName = row.GetValue("name");
                Assert.IsInstanceOfType<KuzuString>(nameByName);

                // TryGetValue
                var got = row.TryGetValue("qty", out var maybeQty);
                Assert.IsTrue(got);
                using (maybeQty)
                {
                    Assert.IsInstanceOfType<KuzuInt64>(maybeQty);
                }

                var gotQtyTyped = row.TryGetValueAs<long>("qty", out var qtyTyped);
                Assert.IsTrue(gotQtyTyped);
                Assert.AreEqual(((KuzuInt64)qtyVal).Value, qtyTyped);

                seen++;
            }
            Assert.AreEqual(2, seen);
        }

        [TestMethod]
        public void NodeValue_PropertyAccess_Label_Id_And_AsString()
        {
            EnsureNativeAvailable();

            using var create = _connection!.Query("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id));");
            using var insert = _connection.Query("CREATE (:Person {id: 42, name: 'Zoe'});");

            using var result = _connection.Query("MATCH (p:Person) RETURN p ORDER BY p.id;");
            Assert.IsTrue(result.HasNext());
            using var row = result.GetNext();
            using var nodeVal = row.GetValue(0);
            Assert.IsInstanceOfType<KuzuNode>(nodeVal);
            var node = (KuzuNode)nodeVal;

            // Label
            var label = node.Label;
            Assert.IsInstanceOfType<string>(label);
            Assert.Contains("Person", label, StringComparison.OrdinalIgnoreCase);

            // Id
            var idVal = node.Id;
            Assert.IsInstanceOfType<InternalId>(idVal);

            // Properties - build dictionary
            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var pc = node.PropertyCount;
            for (ulong i = 0; i < pc; i++)
            {
                var name = node.GetPropertyNameAt(i);
                using var val = node.GetPropertyValueAt(i);
                props[name] = val.ToString();
            }

            Assert.IsTrue(props.Count >= 1);
            // expect name present
            Assert.IsTrue(props.ContainsKey("name"));
            Assert.Contains("Zoe", props["name"], StringComparison.OrdinalIgnoreCase);

            // AsString should return a non-empty representation
            var asStr = node.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(asStr));
            Assert.Contains("Zoe", asStr, StringComparison.OrdinalIgnoreCase);
        }
    }
}
