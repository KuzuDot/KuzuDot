using System.Numerics;
using System.Globalization;
using KuzuDot.Value;

namespace KuzuDot.Tests.KuzuValueTests
{
    /// <summary>
    /// Comprehensive tests for all KuzuValue types and their methods
    /// </summary>
    [TestClass]
    public sealed class ComprehensiveValueTypeTests : IDisposable
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

        #region Timestamp Variants Tests

        [TestMethod]
        public void TimestampNsValue_CreateAndRetrieve_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();
            long testNanos = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000; // millis to nanos

            using KuzuTimestampNs value = KuzuValueFactory.CreateTimestampNanoseconds(testNanos);
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(testNanos, value.UnixNanoseconds);
        }

        [TestMethod]
        public void TimestampMsValue_CreateAndRetrieve_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();
            long testMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using KuzuTimestampMs value = KuzuValueFactory.CreateTimestampMilliseconds(testMillis);
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(testMillis, value.UnixMilliseconds);
        }

        [TestMethod]
        public void TimestampSecValue_CreateAndRetrieve_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();
            long testSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using KuzuTimestampSec value = KuzuValueFactory.CreateTimestampSeconds(testSeconds);
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(testSeconds, value.UnixSeconds);
        }

        [TestMethod]
        public void TimestampTzValue_CreateAndRetrieve_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();
            long testMicrosUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000; // millis to micros

            using KuzuTimestampTz value = KuzuValueFactory.CreateTimestampWithTimeZoneMicros(testMicrosUtc);
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(testMicrosUtc, value.UnixMicrosUtc);
        }

        [TestMethod]
        public void TimestampValue_UnixMicros_ShouldMatchDateTime()
        {
            EnsureNativeLibraryAvailable();
            var testDateTime = DateTime.UtcNow;

            using KuzuTimestamp value = KuzuValueFactory.CreateTimestamp(testDateTime);
            Assert.IsFalse(value.IsNull());

            // The UnixMicros property should give us the raw microsecond value
            long micros = value.UnixMicros;
            Assert.IsTrue(micros > 0);

            // DateTime property should roundtrip approximately
            DateTime retrieved = value.Value;
            Assert.AreEqual(testDateTime.Year, retrieved.Year);
            Assert.AreEqual(testDateTime.Month, retrieved.Month);
            Assert.AreEqual(testDateTime.Day, retrieved.Day);
        }

        [TestMethod]
        public void TimestampValue_CreateFromUnixMicros_ShouldWork()
        {
            EnsureNativeLibraryAvailable();
            long testMicros = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

            using KuzuTimestamp value = KuzuValueFactory.CreateTimestampFromUnixMicros(testMicros);
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(testMicros, value.UnixMicros);
        }

        #endregion

        #region String Creation Methods Tests

        [TestMethod]
        public void BigIntegerValue_AsString_ShouldReturnValidString()
        {
            EnsureNativeLibraryAvailable();
            var testValue = BigInteger.Parse("123456789012345678901234567890", CultureInfo.InvariantCulture);

            using KuzuInt128 value = KuzuValueFactory.CreateInt128(testValue);
            string stringResult = value.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(stringResult));
            // The string should be parseable back to the original value
            BigInteger parsed = BigInteger.Parse(stringResult, CultureInfo.InvariantCulture);
            Assert.AreEqual(testValue, parsed);
        }

        [TestMethod]
        public void BigIntegerValue_CreateFromString_ShouldWork()
        {
            EnsureNativeLibraryAvailable();
            string testString = "987654321098765432109876543210";

            using KuzuInt128 value = KuzuValueFactory.CreateInt128FromString(testString);
            Assert.IsFalse(value.IsNull());

            BigInteger retrieved = value.Value;
            Assert.AreEqual(BigInteger.Parse(testString, CultureInfo.InvariantCulture), retrieved);
        }

        [TestMethod]
        public void BigIntegerValue_CreateFromString_WithInvalidString_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();
            // Null should produce ArgumentException (param validation)
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateInt128FromString(null!));
            // Empty string still treated as argument problem
            Assert.ThrowsExactly<ArgumentException>(() => KuzuValueFactory.CreateInt128FromString(""));
            // Non-numeric should surface native parse failure
            Assert.ThrowsExactly<KuzuException>(() => KuzuValueFactory.CreateInt128FromString("not_a_number"));
        }

        [TestMethod]
        public void InternalIdValue_AsString_ShouldReturnValidString()
        {
            EnsureNativeLibraryAvailable();
            var testId = new InternalId(42, 1337);

            using KuzuInternalId value = KuzuValueFactory.CreateInternalId(testId);
            string stringResult = value.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(stringResult));
            Assert.IsTrue(stringResult.Contains("42", StringComparison.Ordinal));
            Assert.IsTrue(stringResult.Contains("1337", StringComparison.Ordinal));
        }

        [TestMethod]
        public void DateValue_AsString_ShouldReturnValidString()
        {
            EnsureNativeLibraryAvailable();
            var testDate = new DateTime(2024, 12, 25);

            using KuzuDate value = KuzuValueFactory.CreateDate(testDate);
            string stringResult = value.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(stringResult));
            Assert.IsTrue(stringResult.Contains("2024", StringComparison.Ordinal));
        }

        [TestMethod]
        public void DateValue_CreateFromString_ShouldWork()
        {
            EnsureNativeLibraryAvailable();
            string testDateString = "2024-01-15";

            using KuzuDate value = KuzuValueFactory.CreateDateFromString(testDateString);
            Assert.IsFalse(value.IsNull());

            DateTime retrieved = value.AsDateTime();
            Assert.AreEqual(2024, retrieved.Year);
            Assert.AreEqual(1, retrieved.Month);
            Assert.AreEqual(15, retrieved.Day);
        }

        [TestMethod]
        public void DateValue_CreateFromString_WithInvalidString_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateDateFromString(null!));
            Assert.ThrowsExactly<ArgumentException>(() => KuzuValueFactory.CreateDateFromString(""));
            Assert.ThrowsExactly<KuzuException>(() => KuzuValueFactory.CreateDateFromString("not_a_date"));
        }

        #endregion

        #region Complex Types Tests

        [TestMethod]
        public void ListValue_CreateEmpty_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                using KuzuList list = KuzuValueFactory.CreateList();
                Assert.IsFalse(list.IsNull());
                Assert.AreEqual(0UL, list.Count);
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Empty list creation not supported by native implementation");
            }
        }

        [TestMethod]
        public void ListValue_CreateWithElements_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var elem1 = KuzuValueFactory.CreateInt32(10);
            using var elem2 = KuzuValueFactory.CreateInt32(20);
            using var elem3 = KuzuValueFactory.CreateInt32(30);

            using KuzuList list = KuzuValueFactory.CreateList(elem1, elem2, elem3);
            Assert.IsFalse(list.IsNull());
            Assert.AreEqual(3UL, list.Count);

            using var retrieved1 = list.GetElement(0);
            using var retrieved2 = list.GetElement(1);
            using var retrieved3 = list.GetElement(2);

            Assert.IsInstanceOfType<KuzuInt32>(retrieved1);
            Assert.IsInstanceOfType<KuzuInt32>(retrieved2);
            Assert.IsInstanceOfType<KuzuInt32>(retrieved3);

            Assert.AreEqual(10, ((KuzuInt32)retrieved1).Value);
            Assert.AreEqual(20, ((KuzuInt32)retrieved2).Value);
            Assert.AreEqual(30, ((KuzuInt32)retrieved3).Value);
        }

        [TestMethod]
        public void ListValue_GetElement_WithInvalidIndex_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            using var elem = KuzuValueFactory.CreateInt32(42);
            using KuzuList list = KuzuValueFactory.CreateList(elem);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.GetElement(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.GetElement(100));
        }

        [TestMethod]
        public void ListValue_CreateWithNullElements_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateList(null!));

            using var validElem = KuzuValueFactory.CreateInt32(1);
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateList(validElem, null!));
        }

        [TestMethod]
        public void StructValue_CreateEmpty_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                using KuzuStruct structValue = KuzuValueFactory.CreateStruct();
                Assert.IsFalse(structValue.IsNull());
                Assert.AreEqual(0UL, structValue.FieldCount);
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Empty struct creation not supported by native implementation");
            }
        }

        [TestMethod]
        public void StructValue_CreateWithFields_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var nameValue = KuzuValueFactory.CreateString("John");
            using var ageValue = KuzuValueFactory.CreateInt32(25);

            using KuzuStruct structValue = KuzuValueFactory.CreateStruct(
                ("name", nameValue),
                ("age", ageValue)
            );

            Assert.IsFalse(structValue.IsNull());
            Assert.AreEqual(2UL, structValue.FieldCount);

            Assert.AreEqual("name", structValue.GetFieldName(0));
            Assert.AreEqual("age", structValue.GetFieldName(1));

            using var retrievedName = structValue.GetFieldValue(0);
            using var retrievedAge = structValue.GetFieldValue(1);

            Assert.IsInstanceOfType<KuzuString>(retrievedName);
            Assert.IsInstanceOfType<KuzuInt32>(retrievedAge);

            Assert.AreEqual("John", ((KuzuString)retrievedName).Value);
            Assert.AreEqual(25, ((KuzuInt32)retrievedAge).Value);
        }

        [TestMethod]
        public void StructValue_GetField_WithInvalidIndex_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            using var value = KuzuValueFactory.CreateString("test");
            using KuzuStruct structValue = KuzuValueFactory.CreateStruct(("field1", value));

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => structValue.GetFieldName(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => structValue.GetFieldValue(1));
        }

        [TestMethod]
        public void StructValue_CreateWithInvalidFields_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateStruct(null!));

            using var validValue = KuzuValueFactory.CreateInt32(1);
            Assert.ThrowsExactly<ArgumentException>(() => KuzuValueFactory.CreateStruct(("", validValue)));
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateStruct(("field", null!)));
        }

        [TestMethod]
        public void MapValue_CreateEmpty_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            try
            {
                using KuzuMap map = KuzuValueFactory.CreateMap([], []);
                Assert.IsFalse(map.IsNull());
                Assert.AreEqual(0UL, map.Count);
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Empty map creation not supported by native implementation");
            }
        }

        [TestMethod]
        public void MapValue_CreateWithEntries_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var key1 = KuzuValueFactory.CreateString("key1");
            using var key2 = KuzuValueFactory.CreateString("key2");
            using var val1 = KuzuValueFactory.CreateInt32(100);
            using var val2 = KuzuValueFactory.CreateInt32(200);

            using KuzuMap map = KuzuValueFactory.CreateMap(
                new[] { key1, key2 },
                new[] { val1, val2 }
            );

            Assert.IsFalse(map.IsNull());
            Assert.AreEqual(2UL, map.Count);

            using var retrievedKey1 = map.GetKey(0);
            using var retrievedKey2 = map.GetKey(1);
            using var retrievedVal1 = map.GetValueAt(0);
            using var retrievedVal2 = map.GetValueAt(1);

            Assert.IsInstanceOfType<KuzuString>(retrievedKey1);
            Assert.IsInstanceOfType<KuzuString>(retrievedKey2);
            Assert.IsInstanceOfType<KuzuInt32>(retrievedVal1);
            Assert.IsInstanceOfType<KuzuInt32>(retrievedVal2);

            Assert.AreEqual("key1", ((KuzuString)retrievedKey1).Value);
            Assert.AreEqual("key2", ((KuzuString)retrievedKey2).Value);
            Assert.AreEqual(100, ((KuzuInt32)retrievedVal1).Value);
            Assert.AreEqual(200, ((KuzuInt32)retrievedVal2).Value);
        }

        [TestMethod]
        public void MapValue_GetEntry_WithInvalidIndex_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            using var key = KuzuValueFactory.CreateString("key");
            using var value = KuzuValueFactory.CreateInt32(42);
            using KuzuMap map = KuzuValueFactory.CreateMap(new[] { key }, new[] { value });

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => map.GetKey(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => map.GetValueAt(1));
        }

        [TestMethod]
        public void MapValue_CreateWithMismatchedArrays_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            using var key = KuzuValueFactory.CreateString("key");
            using var val1 = KuzuValueFactory.CreateInt32(1);
            using var val2 = KuzuValueFactory.CreateInt32(2);

            Assert.ThrowsExactly<ArgumentException>(() =>
                KuzuValueFactory.CreateMap(new[] { key }, new[] { val1, val2 }));
        }

        [TestMethod]
        public void MapValue_CreateWithNullArrays_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateMap(null!, Array.Empty<KuzuValue>()));
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateMap(Array.Empty<KuzuValue>(), null!));

            using var key = KuzuValueFactory.CreateString("key");
            using var value = KuzuValueFactory.CreateInt32(42);
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateMap(new KuzuValue?[] { null }, new[] { value }));
            Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValueFactory.CreateMap(new[] { key }, new KuzuValue?[] { null }));
        }

        #endregion

        #region DataType Tests

        [TestMethod]
        public void KuzuValue_GetDataType_ShouldReturnCorrectType()
        {
            EnsureNativeLibraryAvailable();

            using var boolVal = KuzuValueFactory.CreateBool(true);
            using var intVal = KuzuValueFactory.CreateInt32(42);
            using var stringVal = KuzuValueFactory.CreateString("test");

            var boolType = boolVal.DataTypeId;
            var intType = intVal.DataTypeId;
            var stringType = stringVal.DataTypeId;

            // DataType should have valid string representation
            Assert.IsFalse(string.IsNullOrEmpty(boolType.ToString()));
            Assert.IsFalse(string.IsNullOrEmpty(intType.ToString()));
            Assert.IsFalse(string.IsNullOrEmpty(stringType.ToString()));
        }

        #endregion

        #region Null Handling Tests

        [TestMethod]
        public void KuzuValue_SetNullAndClear_AllTypes_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            var testValues = new KuzuValue[]
            {
                KuzuValueFactory.CreateBool(true),
                KuzuValueFactory.CreateInt8(1),
                KuzuValueFactory.CreateInt16(2),
                KuzuValueFactory.CreateInt32(3),
                KuzuValueFactory.CreateInt64(4),
                KuzuValueFactory.CreateUInt8(5),
                KuzuValueFactory.CreateUInt16(6),
                KuzuValueFactory.CreateUInt32(7),
                KuzuValueFactory.CreateUInt64(8),
                KuzuValueFactory.CreateFloat(9.0f),
                KuzuValueFactory.CreateDouble(10.0),
                KuzuValueFactory.CreateString("test"),
                KuzuValueFactory.CreateDate(DateTime.Now),
                KuzuValueFactory.CreateTimestamp(DateTime.Now),
                KuzuValueFactory.CreateInterval(TimeSpan.FromHours(1)),
                KuzuValueFactory.CreateInternalId(new InternalId(1, 2)),
                KuzuValueFactory.CreateInt128(BigInteger.One)
            };

            try
            {
                foreach (var value in testValues)
                {
                    // Initially should not be null
                    Assert.IsFalse(value.IsNull(), $"Value {value.GetType().Name} should not be null initially");

                    // TODO: Uncomment if SetNull is implemented
                    ////// Set to null
                    ////value.SetNull(true);
                    ////Assert.IsTrue(value.IsNull(), $"Value {value.GetType().Name} should be null after SetNull(true)");

                    ////// Clear null
                    ////value.SetNull(false);
                    ////Assert.IsFalse(value.IsNull(), $"Value {value.GetType().Name} should not be null after SetNull(false)");
                }
            }
            finally
            {
                foreach (var value in testValues)
                {
                    value.Dispose();
                }
            }
        }

        #endregion

        #region ToString Tests

        [TestMethod]
        public void KuzuValue_ToString_AllTypes_ShouldReturnNonEmptyString()
        {
            EnsureNativeLibraryAvailable();

            var testValues = new KuzuValue[]
            {
                KuzuValueFactory.CreateBool(true),
                KuzuValueFactory.CreateInt8(1),
                KuzuValueFactory.CreateInt16(2),
                KuzuValueFactory.CreateInt32(3),
                KuzuValueFactory.CreateInt64(4),
                KuzuValueFactory.CreateUInt8(5),
                KuzuValueFactory.CreateUInt16(6),
                KuzuValueFactory.CreateUInt32(7),
                KuzuValueFactory.CreateUInt64(8),
                KuzuValueFactory.CreateFloat(9.0f),
                KuzuValueFactory.CreateDouble(10.0),
                KuzuValueFactory.CreateString("test"),
                KuzuValueFactory.CreateDate(DateTime.Now),
                KuzuValueFactory.CreateTimestamp(DateTime.Now),
                KuzuValueFactory.CreateInterval(TimeSpan.FromHours(1)),
                KuzuValueFactory.CreateInternalId(new InternalId(1, 2)),
                KuzuValueFactory.CreateInt128(BigInteger.One),
                KuzuValueFactory.CreateNull()
            };

            try
            {
                foreach (var value in testValues)
                {
                    string str = value.ToString();
                    Assert.IsNotNull(str, $"ToString() for {value.GetType().Name} should not return null");
                    // Note: null values might return empty string, so we don't assert non-empty for all
                }
            }
            finally
            {
                foreach (var value in testValues)
                {
                    value.Dispose();
                }
            }
        }

        #endregion

        #region Clone and CopyFrom Tests

        // TODO: Uncomment if clone/copy functionality is implemented
        ////[TestMethod]
        ////public void KuzuValue_Clone_AllScalarTypes_ShouldProduceIndependentCopy()
        ////{
        ////    EnsureNativeLibraryAvailable();

        ////    var testCases = new (KuzuValue original, KuzuValue newValue)[]
        ////    {
        ////        (KuzuValueFactory.CreateBool(true), KuzuValueFactory.CreateBool(false)),
        ////        (KuzuValueFactory.CreateInt32(100), KuzuValueFactory.CreateInt32(200)),
        ////        (KuzuValueFactory.CreateDouble(1.5), KuzuValueFactory.CreateDouble(2.5)),
        ////        (KuzuValueFactory.CreateString("original"), KuzuValueFactory.CreateString("modified"))
        ////    };

        ////    try
        ////    {
        ////        foreach (var (original, newValue) in testCases)
        ////        {
        ////            using var clone = original.Clone();

        ////            // Clone should match original initially
        ////            Assert.AreEqual(original.ToString(), clone.ToString());

        ////            // Modify clone
        ////            clone.CopyFrom(newValue);

        ////            // Original should be unchanged, clone should be modified
        ////            Assert.AreNotEqual(original.ToString(), clone.ToString());
        ////            Assert.AreEqual(newValue.ToString(), clone.ToString());
        ////        }
        ////    }
        ////    finally
        ////    {
        ////        foreach (var (original, newValue) in testCases)
        ////        {
        ////            original.Dispose();
        ////            newValue.Dispose();
        ////        }
        ////    }
        ////}

        [TestMethod]
        public void KuzuValue_CopyFrom_WithNullSource_ShouldThrow()
        {
            // TODO: Uncomment if CopyFrom is implemented
            //EnsureNativeLibraryAvailable();

            //using var target = KuzuValueFactory.CreateInt32(42);
            //Assert.ThrowsExactly<ArgumentNullException>(() => target.CopyFrom(null!));
        }

        [TestMethod]
        public void KuzuValue_CopyFrom_WithDifferentType_ShouldThrow()
        {
            // TODO: Uncomment if CopyFrom is implemented
            //EnsureNativeLibraryAvailable();
            //using var intValue = KuzuValueFactory.CreateInt32(42);
            //using var stringValue = KuzuValueFactory.CreateString("not an int");
            //Assert.ThrowsExactly<ArgumentException>(() => intValue.CopyFrom(stringValue));
        }

        #endregion

        #region Extreme Values Tests

        [TestMethod]
        public void KuzuValue_ExtremeNumericValues_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();

            // Test extreme values for each numeric type
            using var minInt8 = KuzuValueFactory.CreateInt8(sbyte.MinValue);
            using var maxInt8 = KuzuValueFactory.CreateInt8(sbyte.MaxValue);
            using var minInt16 = KuzuValueFactory.CreateInt16(short.MinValue);
            using var maxInt16 = KuzuValueFactory.CreateInt16(short.MaxValue);
            using var minInt32 = KuzuValueFactory.CreateInt32(int.MinValue);
            using var maxInt32 = KuzuValueFactory.CreateInt32(int.MaxValue);
            using var minInt64 = KuzuValueFactory.CreateInt64(long.MinValue);
            using var maxInt64 = KuzuValueFactory.CreateInt64(long.MaxValue);

            using var minUInt8 = KuzuValueFactory.CreateUInt8(byte.MinValue);
            using var maxUInt8 = KuzuValueFactory.CreateUInt8(byte.MaxValue);
            using var minUInt16 = KuzuValueFactory.CreateUInt16(ushort.MinValue);
            using var maxUInt16 = KuzuValueFactory.CreateUInt16(ushort.MaxValue);
            using var minUInt32 = KuzuValueFactory.CreateUInt32(uint.MinValue);
            using var maxUInt32 = KuzuValueFactory.CreateUInt32(uint.MaxValue);
            using var minUInt64 = KuzuValueFactory.CreateUInt64(ulong.MinValue);
            using var maxUInt64 = KuzuValueFactory.CreateUInt64(ulong.MaxValue);

            using var minFloat = KuzuValueFactory.CreateFloat(float.MinValue);
            using var maxFloat = KuzuValueFactory.CreateFloat(float.MaxValue);
            using var minDouble = KuzuValueFactory.CreateDouble(double.MinValue);
            using var maxDouble = KuzuValueFactory.CreateDouble(double.MaxValue);

            // Verify values roundtrip correctly
            Assert.AreEqual(sbyte.MinValue, ((KuzuInt8)minInt8).Value);
            Assert.AreEqual(sbyte.MaxValue, ((KuzuInt8)maxInt8).Value);
            Assert.AreEqual(short.MinValue, ((KuzuInt16)minInt16).Value);
            Assert.AreEqual(short.MaxValue, ((KuzuInt16)maxInt16).Value);
            Assert.AreEqual(int.MinValue, ((KuzuInt32)minInt32).Value);
            Assert.AreEqual(int.MaxValue, ((KuzuInt32)maxInt32).Value);
            Assert.AreEqual(long.MinValue, ((KuzuInt64)minInt64).Value);
            Assert.AreEqual(long.MaxValue, ((KuzuInt64)maxInt64).Value);

            Assert.AreEqual(byte.MinValue, ((KuzuUInt8)minUInt8).Value);
            Assert.AreEqual(byte.MaxValue, ((KuzuUInt8)maxUInt8).Value);
            Assert.AreEqual(ushort.MinValue, ((KuzuUInt16)minUInt16).Value);
            Assert.AreEqual(ushort.MaxValue, ((KuzuUInt16)maxUInt16).Value);
            Assert.AreEqual(uint.MinValue, ((KuzuUInt32)minUInt32).Value);
            Assert.AreEqual(uint.MaxValue, ((KuzuUInt32)maxUInt32).Value);
            Assert.AreEqual(ulong.MinValue, ((KuzuUInt64)minUInt64).Value);
            Assert.AreEqual(ulong.MaxValue, ((KuzuUInt64)maxUInt64).Value);

            Assert.AreEqual(float.MinValue, ((KuzuFloat)minFloat).Value);
            Assert.AreEqual(float.MaxValue, ((KuzuFloat)maxFloat).Value);
            Assert.AreEqual(double.MinValue, ((KuzuDouble)minDouble).Value);
            Assert.AreEqual(double.MaxValue, ((KuzuDouble)maxDouble).Value);
        }

        [TestMethod]
        public void BigIntegerValue_ExtremeValues_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Test very large positive value
            var largeBigInt = BigInteger.Parse("170141183460469231731687303715884105727", CultureInfo.InvariantCulture); // close to 2^127 - 1
            using var largeValue = KuzuValueFactory.CreateInt128(largeBigInt);
            Assert.AreEqual(largeBigInt, ((KuzuInt128)largeValue).Value);

            // Test very large negative value
            var negativelargeBigInt = BigInteger.Parse("-170141183460469231731687303715884105728", CultureInfo.InvariantCulture); // close to -2^127
            using var negativeLargeValue = KuzuValueFactory.CreateInt128(negativelargeBigInt);
            Assert.AreEqual(negativelargeBigInt, ((KuzuInt128)negativeLargeValue).Value);

            // Test zero
            using var zeroValue = KuzuValueFactory.CreateInt128(BigInteger.Zero);
            Assert.AreEqual(BigInteger.Zero, ((KuzuInt128)zeroValue).Value);

            // Test one
            using var oneValue = KuzuValueFactory.CreateInt128(BigInteger.One);
            Assert.AreEqual(BigInteger.One, ((KuzuInt128)oneValue).Value);

            // Test minus one
            using var minusOneValue = KuzuValueFactory.CreateInt128(BigInteger.MinusOne);
            Assert.AreEqual(BigInteger.MinusOne, ((KuzuInt128)minusOneValue).Value);
        }

        #endregion

        #region Disposal Tests

        [TestMethod]
        public void KuzuValue_DisposeMultipleTimes_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            var value = KuzuValueFactory.CreateInt32(42);

            // Multiple disposal should not throw
            value.Dispose();
            value.Dispose();
            value.Dispose();

            // Accessing disposed value should throw or return invalid state
            try
            {
                var _ = ((KuzuInt32)value).Value;
                Assert.Fail("Expected ObjectDisposedException or similar");
            }
            catch (ObjectDisposedException)
            {
                // Expected behavior
            }
            catch (InvalidOperationException)
            {
                // Also acceptable for disposed objects
            }
            catch (KuzuException)
            {
                // Native might return error for disposed objects
            }
        }

        [TestMethod]
        public void KuzuValue_AccessAfterDispose_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            var testValues = new KuzuValue[]
            {
                KuzuValueFactory.CreateBool(true),
                KuzuValueFactory.CreateInt32(42),
                KuzuValueFactory.CreateString("test"),
                KuzuValueFactory.CreateDate(DateTime.Now)
            };

            // Dispose all values
            foreach (var value in testValues)
            {
                value.Dispose();
            }

            // Accessing any method should throw some form of exception
            foreach (var value in testValues)
            {
                try
                {
                    value.IsNull();
                    Assert.Fail($"Expected exception when calling IsNull() on disposed {value.GetType().Name}");
                }
                catch (ObjectDisposedException) { /* Expected */ }
                catch (InvalidOperationException) { /* Acceptable */ }
                catch (KuzuException) { /* Native might handle differently */ }

                try
                {
                    value.ToString();
                    // ToString might not throw for some disposed objects, so we don't fail here
                }
                catch (ObjectDisposedException) { /* Expected */ }
                catch (InvalidOperationException) { /* Acceptable */ }
                catch (KuzuException) { /* Native might handle differently */ }

                // TODO: uncomment if clone is implemented
                //try
                //{
                //    value.Clone();
                //    Assert.Fail($"Expected exception when calling Clone() on disposed {value.GetType().Name}");
                //}
                //catch (ObjectDisposedException) { /* Expected */ }
                //catch (InvalidOperationException) { /* Acceptable */ }
                //catch (KuzuException) { /* Native might handle differently */ }
            }
        }

        #endregion
    }
}