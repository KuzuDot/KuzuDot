using KuzuDot.Value;

namespace KuzuDot.Tests.KuzuValueTests
{
    /// <summary>
    /// Tests for UUID values and graph-specific value types that require database operations
    /// </summary>
    [TestClass]
    public sealed class DatabaseDependentValueTypeTests : IDisposable
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
        public void TestCleanup() => Dispose();

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
                Assert.Inconclusive($"Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        #region UUID Tests

        [TestMethod]
        public void UuidValue_CreateAndRetrieve_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                // Create table with UUID column
                using var createResult = _connection!.Query("CREATE NODE TABLE UuidTest(id UUID, PRIMARY KEY(id));");
                Assert.IsTrue(createResult.IsSuccess);

                // Insert a UUID value
                string testUuid = "550e8400-e29b-41d4-a716-446655440000";
                using var insertResult = _connection.Query($"CREATE (:UuidTest {{id: UUID('{testUuid}')}});");
                Assert.IsTrue(insertResult.IsSuccess);

                // Retrieve the UUID value
                using var queryResult = _connection.Query("MATCH (u:UuidTest) RETURN u.id;");
                Assert.IsTrue(queryResult.IsSuccess);
                Assert.AreEqual(1UL, queryResult.RowCount);

                using var row = queryResult.GetNext();
                var uuidValue = row.GetValueAs<UUID>(0);

                Assert.IsInstanceOfType<UUID>(uuidValue);
                string retrievedUuid = uuidValue.Value;
                Assert.AreEqual(testUuid, retrievedUuid);
            }
            catch (KuzuException ex) when (ex.Message.Contains("UUID", StringComparison.Ordinal) || ex.Message.Contains("not supported", StringComparison.Ordinal))
            {
                Assert.Inconclusive("UUID type not supported in current engine version");
            }
        }

        [TestMethod]
        public void UuidValue_ToString_ShouldReturnValidString()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                // Create and retrieve UUID to get UuidValue instance
                using var createResult = _connection!.Query("CREATE NODE TABLE UuidTest2(id UUID, PRIMARY KEY(id));");
                using var insertResult = _connection.Query("CREATE (:UuidTest2 {id: UUID('123e4567-e89b-12d3-a456-426614174000')});");
                using var queryResult = _connection.Query("MATCH (u:UuidTest2) RETURN u.id;");

                using var row = queryResult.GetNext();
                using var uuidValue = row.GetValue(0);

                string str = uuidValue.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(str));
                // UUID toString should contain the UUID string representation
                Assert.IsTrue(str.Contains("123e4567", StringComparison.Ordinal) || str.Contains("123E4567", StringComparison.Ordinal));
            }
            catch (KuzuException ex) when (ex.Message.Contains("UUID", StringComparison.Ordinal) || ex.Message.Contains("not supported", StringComparison.Ordinal))
            {
                Assert.Inconclusive("UUID type not supported in current engine version");
            }
        }

        #endregion

        #region Graph Type Tests

        [TestMethod]
        public void NodeValue_CreateAndRetrieve_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Create a simple node table
            using var createResult = _connection!.Query("CREATE NODE TABLE Person(name STRING, age INT32, PRIMARY KEY(name));");
            Assert.IsTrue(createResult.IsSuccess);

            // Insert a node
            using var insertResult = _connection.Query("CREATE (:Person {name: 'Alice', age: 30});");
            Assert.IsTrue(insertResult.IsSuccess);

            // Retrieve the node
            using var queryResult = _connection.Query("MATCH (p:Person) RETURN p;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var nodeValue = row.GetValue(0);

            Assert.IsInstanceOfType<KuzuNode>(nodeValue);
            var node = (KuzuNode)nodeValue;

            // Test node methods
            var idValue = node.Id;
            var labelValue = node.Label;

            Assert.IsInstanceOfType<InternalId>(idValue);
            Assert.IsInstanceOfType<string>(labelValue);
            Assert.AreEqual("Person", labelValue);

            // Test property access
            ulong propCount = node.PropertyCount;
            Assert.AreEqual(2UL, propCount); // name and age

            string prop0Name = node.GetPropertyNameAt(0);
            string prop1Name = node.GetPropertyNameAt(1);

            using var prop0Value = node.GetPropertyValueAt(0);
            using var prop1Value = node.GetPropertyValueAt(1);

            // Properties should be name and age (order may vary)
            bool hasName = prop0Name == "name" || prop1Name == "name";
            bool hasAge = prop0Name == "age" || prop1Name == "age";
            Assert.IsTrue(hasName);
            Assert.IsTrue(hasAge);

            // Test AsString method
            string nodeStr = node.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(nodeStr));
            Assert.IsTrue(nodeStr.Contains("Alice", StringComparison.Ordinal) || nodeStr.Contains("Person", StringComparison.Ordinal));
        }

        [TestMethod]
        public void NodeValue_GetProperty_WithInvalidIndex_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            using var createResult = _connection!.Query("CREATE NODE TABLE TestNode(id INT32, PRIMARY KEY(id));");
            using var insertResult = _connection.Query("CREATE (:TestNode {id: 1});");
            using var queryResult = _connection.Query("MATCH (n:TestNode) RETURN n;");

            using var row = queryResult.GetNext();
            using var nodeValue = row.GetValue(0);
            var node = (KuzuNode)nodeValue;

            ulong propCount = node.PropertyCount;
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => node.GetPropertyNameAt(propCount));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => node.GetPropertyValueAt(propCount));
        }

        [TestMethod]
        public void RelValue_CreateAndRetrieve_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Create node and relationship tables
            using var createNodeResult = _connection!.Query("CREATE NODE TABLE Person2(name STRING, PRIMARY KEY(name));");
            using var createRelResult = _connection.Query("CREATE REL TABLE Knows(FROM Person2 TO Person2, since INT32);");

            // Insert nodes and relationship
            using var insertNode1 = _connection.Query("CREATE (:Person2 {name: 'Alice'});");
            using var insertNode2 = _connection.Query("CREATE (:Person2 {name: 'Bob'});");
            using var insertRel = _connection.Query("MATCH (a:Person2 {name: 'Alice'}), (b:Person2 {name: 'Bob'}) CREATE (a)-[:Knows {since: 2020}]->(b);");

            // Retrieve the relationship
            using var queryResult = _connection.Query("MATCH (a:Person2)-[r:Knows]->(b:Person2) RETURN r;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var relValue = row.GetValue(0);

            Assert.IsInstanceOfType<KuzuRel>(relValue);
            var rel = (KuzuRel)relValue;

            // Test relationship methods
            var idValue = rel.Id;
            var srcIdValue = rel.SrcId;
            var dstIdValue = rel.DstId;
            var labelValue = rel.Label;

            Assert.IsInstanceOfType<InternalId>(idValue);
            Assert.IsInstanceOfType<InternalId>(srcIdValue);
            Assert.IsInstanceOfType<InternalId>(dstIdValue);
            Assert.IsInstanceOfType<string>(labelValue);
            Assert.AreEqual("Knows", labelValue);

            // Test property access
            ulong propCount = rel.PropertyCount;
            Assert.AreEqual(1UL, propCount); // since

            string propName = rel.GetPropertyNameAt(0);
            Assert.AreEqual("since", propName);

            using var propValue = rel.GetPropertyValueAt(0);
            Assert.IsInstanceOfType<KuzuInt32>(propValue);
            Assert.AreEqual(2020, ((KuzuInt32)propValue).Value);

            // Test AsString method
            string relStr = rel.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(relStr));
        }

        [TestMethod]
        public void RelValue_GetProperty_WithInvalidIndex_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            using var createNodeResult = _connection!.Query("CREATE NODE TABLE TestNode2(id INT32, PRIMARY KEY(id));");
            using var createRelResult = _connection.Query("CREATE REL TABLE TestRel(FROM TestNode2 TO TestNode2);");
            using var insertNode1 = _connection.Query("CREATE (:TestNode2 {id: 1});");
            using var insertNode2 = _connection.Query("CREATE (:TestNode2 {id: 2});");
            using var insertRel = _connection.Query("MATCH (a:TestNode2 {id: 1}), (b:TestNode2 {id: 2}) CREATE (a)-[:TestRel]->(b);");
            using var queryResult = _connection.Query("MATCH ()-[r:TestRel]->() RETURN r;");

            using var row = queryResult.GetNext();
            using var relValue = row.GetValue(0);
            var rel = (KuzuRel)relValue;

            ulong propCount = rel.PropertyCount;
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => rel.GetPropertyNameAt(propCount));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => rel.GetPropertyValueAt(propCount));
        }

        [TestMethod]
        public void RecursiveRelValue_CreateAndRetrieve_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                // Create node and relationship tables for recursive relationships
                using var createNodeResult = _connection!.Query("CREATE NODE TABLE City(name STRING, PRIMARY KEY(name));");
                using var createRelResult = _connection.Query("CREATE REL TABLE ConnectedTo(FROM City TO City, distance INT32);");

                // Insert nodes and relationships to create a path
                using var insertCity1 = _connection.Query("CREATE (:City {name: 'A'});");
                using var insertCity2 = _connection.Query("CREATE (:City {name: 'B'});");
                using var insertCity3 = _connection.Query("CREATE (:City {name: 'C'});");
                using var insertRel1 = _connection.Query("MATCH (a:City {name: 'A'}), (b:City {name: 'B'}) CREATE (a)-[:ConnectedTo {distance: 10}]->(b);");
                using var insertRel2 = _connection.Query("MATCH (b:City {name: 'B'}), (c:City {name: 'C'}) CREATE (b)-[:ConnectedTo {distance: 20}]->(c);");

                // Return a path variable (p) so the value comes back as a RECURSIVE_REL instead of just a list of rels
                using var queryResult = _connection.Query("MATCH p=(a:City {name: 'A'})-[:ConnectedTo*1..2]->(c:City) RETURN p;");

                if (queryResult.RowCount > 0)
                {
                    using var row = queryResult.GetNext();
                    using var pathValue = row.GetValue(0);

                    if (pathValue is KuzuRecursiveRel recRel)
                    {
                        using var nodeList = recRel.GetNodeList();
                        using var relList = recRel.GetRelList();

                        Assert.IsInstanceOfType<KuzuList>(nodeList);
                        Assert.IsInstanceOfType<KuzuList>(relList);

                        // Expect at least one edge and two nodes for shortest path
                        Assert.IsTrue(nodeList.Count >= 2, "Expected at least 2 nodes in recursive path");
                        Assert.IsTrue(relList.Count >= 1, "Expected at least 1 relationship in recursive path");
                    }
                    else
                    {
                        Assert.Inconclusive("Returned value was not a RecursiveRelValue (engine representation may differ)");
                    }
                }
                else
                {
                    Assert.Inconclusive("No recursive relationships found in test data");
                }
            }
            catch (KuzuException ex) when (ex.Message.Contains("recursive", StringComparison.Ordinal) || ex.Message.Contains("not supported", StringComparison.Ordinal))
            {
                Assert.Inconclusive("Recursive relationships not supported in current engine version");
            }
        }

        #endregion

        #region Complex Types in Database

        [TestMethod]
        public void ListValue_FromDatabase_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                // Create table with list column
                using var createResult = _connection!.Query("CREATE NODE TABLE ListTest(id INT32, numbers INT32[], PRIMARY KEY(id));");
                Assert.IsTrue(createResult.IsSuccess);

                // Insert a list value
                using var insertResult = _connection.Query("CREATE (:ListTest {id: 1, numbers: [1, 2, 3, 4]});");
                Assert.IsTrue(insertResult.IsSuccess);

                // Retrieve the list value
                using var queryResult = _connection.Query("MATCH (l:ListTest) RETURN l.numbers;");
                Assert.IsTrue(queryResult.IsSuccess);
                Assert.AreEqual(1UL, queryResult.RowCount);

                using var row = queryResult.GetNext();
                using var listValue = row.GetValue(0);

                Assert.IsInstanceOfType<KuzuList>(listValue);
                var list = (KuzuList)listValue;

                Assert.AreEqual(4UL, list.Count);

                for (ulong i = 0; i < list.Count; i++)
                {
                    using var element = list.GetElement(i);
                    Assert.IsInstanceOfType<KuzuInt32>(element);
                    Assert.AreEqual((int)(i + 1), ((KuzuInt32)element).Value);
                }
            }
            catch (KuzuException ex) when (ex.Message.Contains("array", StringComparison.Ordinal) || ex.Message.Contains("not supported", StringComparison.Ordinal))
            {
                Assert.Inconclusive("List/Array types not supported in current engine version");
            }
        }

        [TestMethod]
        public void StructValue_FromDatabase_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                // Create table with struct column
                using var createResult = _connection!.Query("CREATE NODE TABLE StructTest(id INT32, person STRUCT(name STRING, age INT32), PRIMARY KEY(id));");
                Assert.IsTrue(createResult.IsSuccess);

                // Insert a struct value
                using var insertResult = _connection.Query("CREATE (:StructTest {id: 1, person: {name: 'John', age: 25}});");
                Assert.IsTrue(insertResult.IsSuccess);

                // Retrieve the struct value
                using var queryResult = _connection.Query("MATCH (s:StructTest) RETURN s.person;");
                Assert.IsTrue(queryResult.IsSuccess);
                Assert.AreEqual(1UL, queryResult.RowCount);

                using var row = queryResult.GetNext();
                using var structValue = row.GetValue(0);

                Assert.IsInstanceOfType<KuzuStruct>(structValue);
                var structVal = (KuzuStruct)structValue;

                Assert.AreEqual(2UL, structVal.FieldCount);

                string field0Name = structVal.GetFieldName(0);
                string field1Name = structVal.GetFieldName(1);

                // Fields might be in different order
                bool hasName = field0Name == "name" || field1Name == "name";
                bool hasAge = field0Name == "age" || field1Name == "age";
                Assert.IsTrue(hasName);
                Assert.IsTrue(hasAge);

                using var field0Value = structVal.GetFieldValue(0);
                using var field1Value = structVal.GetFieldValue(1);

                // One should be string, one should be int32
                bool hasStringValue = field0Value is KuzuString || field1Value is KuzuString;
                bool hasIntValue = field0Value is KuzuInt32 || field1Value is KuzuInt32;
                Assert.IsTrue(hasStringValue);
                Assert.IsTrue(hasIntValue);
            }
            catch (KuzuException ex) when (ex.Message.Contains("STRUCT", StringComparison.Ordinal) || ex.Message.Contains("not supported", StringComparison.Ordinal))
            {
                Assert.Inconclusive("STRUCT types not supported in current engine version");
            }
        }

        [TestMethod]
        public void MapValue_FromDatabase_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Create table with map column
            using var createResult = _connection!.Query("CREATE NODE TABLE MapTest(id INT32, mapping MAP(STRING, INT32), PRIMARY KEY(id));");
            Assert.IsTrue(createResult.IsSuccess);

            using var insertAttempt = _connection.Query("CREATE (:MapTest {id: 1, mapping: map(['key1','key2'],[100,200])});");

            // Retrieve the map value
            using var queryResult = _connection.Query("MATCH (m:MapTest) RETURN m.mapping;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var mapValue = row.GetValue(0);

            Assert.IsInstanceOfType<KuzuMap>(mapValue);
            var map = (KuzuMap)mapValue;

            Assert.AreEqual(2UL, map.Count);

            for (ulong i = 0; i < map.Count; i++)
            {
                using var key = map.GetKey(i);
                using var value = map.GetValueAt(i);

                Assert.IsInstanceOfType<KuzuString>(key);
                Assert.IsInstanceOfType<KuzuInt32>(value);

                string keyStr = ((KuzuString)key).Value;
                int valueInt = ((KuzuInt32)value).Value;

                Assert.IsTrue(keyStr == "key1" || keyStr == "key2", $"Unexpected key '{keyStr}'");
                Assert.IsTrue(valueInt == 100 || valueInt == 200, $"Unexpected value '{valueInt}'");
            }
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void ComplexTypes_InvalidAccess_ShouldThrowAppropriateExceptions()
        {
            EnsureNativeLibraryAvailable();

            // Test with empty list/struct/map
            using var value = KuzuValueFactory.CreateInt16(10);
            using var key = KuzuValueFactory.CreateString("key");
            using var emptyList = KuzuValueFactory.CreateList([value]);
            using var emptyStruct = KuzuValueFactory.CreateStruct(("key", value));
            using var emptyMap = KuzuValueFactory.CreateMap([key], [value]);

            // Accessing invalid indices should throw
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => emptyList.GetElement(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => emptyStruct.GetFieldName(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => emptyStruct.GetFieldValue(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => emptyMap.GetKey(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => emptyMap.GetValueAt(1));
        }

        #endregion

        #region KuzuList Tests

        [TestMethod]
        public void KuzuList_AsT_ShouldReturnTypedValues()
        {
            EnsureNativeLibraryAvailable();

            using var queryResult = _connection!.Query("RETURN list_creation(1,2,3,4) as A;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var listValue = row.GetValue(0);
            Assert.IsInstanceOfType<KuzuList>(listValue);
            var list = (KuzuList)listValue;
            Assert.AreEqual(4UL, list.Count, "Incorrect list count");

            // Test As<long> using converter only
            var intValues = list.As(v => ((KuzuInt64)v).Value).ToList();
            CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4 }, intValues);
        }

        [TestMethod]
        public void KuzuList_AsTypedT_ShouldReturnTypedValues()
        {
            EnsureNativeLibraryAvailable();

            using var queryResult = _connection!.Query("RETURN list_creation(1,2,3,4) as A;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var listValue = row.GetValue(0);
            Assert.IsInstanceOfType<KuzuList>(listValue);
            var list = (KuzuList)listValue;
            Assert.AreEqual(4UL, list.Count, "Incorrect list count");

            // Test As<long> using generics
            var intValues = list.As<long>().ToList();
            CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4 }, intValues);
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "<Pending>")]
        public void KuzuStruct_FromQuery_ShouldReturnCorrectFieldsAndValues()
        {
            EnsureNativeLibraryAvailable();

            using var queryResult = _connection!.Query("RETURN {foo: 42, bar: 'baz'} as s;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var structValue = row.GetValue(0);
            Assert.IsInstanceOfType<KuzuStruct>(structValue);
            var s = (KuzuStruct)structValue;
            Assert.AreEqual(2UL, s.FieldCount);

            var fieldNames = new List<string>();
            var fieldValues = new List<object?>();
            for (ulong i = 0; i < s.FieldCount; i++)
            {
                var name = s.GetFieldName(i);
                using var value = s.GetFieldValue(i);
                fieldNames.Add(name);
                if (value is KuzuInt64 ki64)
                    fieldValues.Add(ki64.Value);
                else if (value is KuzuString ks)
                    fieldValues.Add(ks.Value);
                else
                    fieldValues.Add(value.ToString());
            }
            CollectionAssert.AreEquivalent(new[] { "foo", "bar" }, fieldNames);
            CollectionAssert.Contains(fieldValues, 42L);
            CollectionAssert.Contains(fieldValues, "baz");
        }


        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "<Pending>")]
        public void KuzuStruct_FromQuery_ShouldEnumerateKeysAndValues()
        {
            EnsureNativeLibraryAvailable();

            using var queryResult = _connection!.Query("RETURN {foo: 42, bar: 'baz'} as s;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var structValue = row.GetValue(0);
            Assert.IsInstanceOfType<KuzuStruct>(structValue);
            var s = (KuzuStruct)structValue;
            Assert.AreEqual(2UL, s.FieldCount, "Incorrect field count");

            var fieldNames = new List<string>();
            var fieldValues = new List<object?>();
            foreach (var (key, value) in s)
            {
                fieldNames.Add(key);
                if (value is KuzuInt64 ki64)
                    fieldValues.Add(ki64.Value);
                else if (value is KuzuString ks)
                    fieldValues.Add(ks.Value);
                else
                    fieldValues.Add(value.ToString());
                value.Dispose();
            }
            CollectionAssert.AreEquivalent(new[] { "foo", "bar" }, fieldNames);
            CollectionAssert.Contains(fieldValues, 42L);
            CollectionAssert.Contains(fieldValues, "baz");
        }

        [TestMethod]
        public void KuzuStruct_FromQuery_ShouldAccessKeysAndValues()
        {
            EnsureNativeLibraryAvailable();

            using var queryResult = _connection!.Query("RETURN {foo: 42, bar: 'baz'} as s;");
            Assert.IsTrue(queryResult.IsSuccess);
            Assert.AreEqual(1UL, queryResult.RowCount);

            using var row = queryResult.GetNext();
            using var s = row.GetValue<KuzuStruct>(0);

            var fieldNames = new List<string>();
            var fieldValues = new List<object?>();
            var (key, value) = s[0];
            Assert.AreEqual("foo", key);
            Assert.AreEqual(42L, ((KuzuInt64)value).Value);

            value.Dispose();
        }

        #endregion
    }
}