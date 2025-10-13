using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot.Value;
using System.Globalization;

namespace KuzuDot.Tests.PropertiesTests
{
    [TestClass]
    public class NodePropertiesTests
    {
        private static Database? _db;
        private static Connection? _conn;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            // Create in-memory KuzuDB and connection
            _db = Database.FromMemory();
            _conn = _db.Connect();
            // Create a table and insert a node
            _conn.NonQuery("CREATE NODE TABLE Person(name STRING, age INT64, country STRING, PRIMARY KEY(name))");
            _conn.NonQuery("CREATE (:Person {name: 'Alice', age: 42, country: 'Wonderland'})");
        }

        private static (PropertyDictionary Props, QueryResult Result) GetNodeProperties()
        {
            var result = _conn!.Query("MATCH (p:Person) WHERE p.name = 'Alice' RETURN p");
            Assert.AreEqual(1UL, result.RowCount);
            var nodeVal = result.GetNext().GetValue(0) as KuzuNode;
            Assert.IsNotNull(nodeVal);
            return (nodeVal.Properties, result);
        }

        [TestMethod]
        public void Indexer_ReturnsValue_ForExistingKey()
        {
            var (props, result) = GetNodeProperties();
            Assert.AreEqual("Alice", props.Get<string>("name"));
            Assert.AreEqual("42", props.Get<long>("age").ToString(CultureInfo.InvariantCulture));
            result.Dispose();
        }

        [TestMethod]
        public void Indexer_Throws_ForMissingKey()
        {
            var (props, result) = GetNodeProperties();
            Assert.ThrowsExactly<KeyNotFoundException>(() => props.Get("missing"));
            result.Dispose();
        }

        [TestMethod]
        public void GetOrDefault_ReturnsValueOrNull()
        {
            var (props, result) = GetNodeProperties();
            Assert.AreEqual("Wonderland", props.GetOrNull("country")?.ToString());
            Assert.IsNull(props.GetOrNull("missing"));
            result.Dispose();
        }

        [TestMethod]
        public void GetOrDefault_Generic_ReturnsValueOrNull()
        {
            var (props, result) = GetNodeProperties();
            // Existing key, correct type
            Assert.AreEqual(42L, props.GetOrNull<long>("age"));
            // Existing key, wrong type
            Assert.ThrowsExactly<InvalidCastException>(() => props.GetOrNull<int>("age"));
            // Missing key, should give null
            Assert.IsNull(props.GetOrNull<long>("missing"));
            Assert.AreEqual((long)default, props.GetOrDefault<long>("missing"));
            result.Dispose();
        }

        [TestMethod]
        public void Has_ReturnsTrueOrFalse()
        {
            var (props, result) = GetNodeProperties();
            Assert.IsTrue(props.Has("age"));
            Assert.IsFalse(props.Has("missing"));
            result.Dispose();
        }

        [TestMethod]
        public void TryGetValue_ReturnsTrueAndValue_OrFalseAndNull()
        {
            var (props, result) = GetNodeProperties();
            Assert.IsTrue(props.TryGetValue("name", out var val));
            Assert.AreEqual("Alice", val.ToString());
            Assert.IsFalse(props.TryGetValue("missing", out var missing));
            Assert.IsNull(missing);
            result.Dispose();
        }

        [TestMethod]
        public void Keys_And_Values_EnumerateCorrectly()
        {
            var (props, result) = GetNodeProperties();
            var keys = new HashSet<string>(props.Keys);
            var values = new HashSet<string>();
            foreach (var v in props.Values)
                values.Add(v.ToString());
            Assert.Contains("name", keys);
            Assert.Contains("age", keys);
            Assert.Contains("country", keys);
            Assert.Contains("Alice", values);
            Assert.Contains("42", values);
            Assert.Contains("Wonderland", values);
            result.Dispose();
        }

        [TestMethod]
        public void AsDictionary_ReturnsDictionaryWithAllProperties()
        {
            var (props, result) = GetNodeProperties();
            var asDict = props.AsDictionary();
            Assert.HasCount(3, asDict);
            Assert.AreEqual("Alice", asDict["name"].ToString());
            Assert.AreEqual("42", asDict["age"].ToString());
            Assert.AreEqual("Wonderland", asDict["country"].ToString());
            result.Dispose();
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            _conn!.Dispose();
            _db!.Dispose();
        }
    }
}
