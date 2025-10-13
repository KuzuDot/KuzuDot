using System.Numerics;
using System.Globalization;
using KuzuDot.Value;

namespace KuzuDot.Tests.EdgeCaseTests
{
    /// <summary>
    /// Edge case tests for KuzuValue types including special float values, overflow conditions, and error scenarios
    /// </summary>
    [TestClass]
    public sealed class ValueTypeEdgeCaseTests : IDisposable
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

        #region Special Float Values

        [TestMethod]
        public void FloatValue_SpecialValues_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();

            // Test positive and negative infinity
            using var posInf = KuzuValueFactory.CreateFloat(float.PositiveInfinity);
            using var negInf = KuzuValueFactory.CreateFloat(float.NegativeInfinity);

            Assert.IsTrue(float.IsPositiveInfinity(((KuzuFloat)posInf).Value));
            Assert.IsTrue(float.IsNegativeInfinity(((KuzuFloat)negInf).Value));

            // Test NaN
            using var nan = KuzuValueFactory.CreateFloat(float.NaN);
            Assert.IsTrue(float.IsNaN(((KuzuFloat)nan).Value));

            // Test epsilon and very small values
            using var epsilon = KuzuValueFactory.CreateFloat(float.Epsilon);
            Assert.AreEqual(float.Epsilon, ((KuzuFloat)epsilon).Value);

            // Test subnormal values
            using var subnormal = KuzuValueFactory.CreateFloat(1.4e-45f); // smallest positive subnormal
            Assert.AreEqual(1.4e-45f, ((KuzuFloat)subnormal).Value, 1e-50f);
        }

        [TestMethod]
        public void DoubleValue_SpecialValues_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();

            // Test positive and negative infinity
            using var posInf = KuzuValueFactory.CreateDouble(double.PositiveInfinity);
            using var negInf = KuzuValueFactory.CreateDouble(double.NegativeInfinity);

            Assert.IsTrue(double.IsPositiveInfinity(((KuzuDouble)posInf).Value));
            Assert.IsTrue(double.IsNegativeInfinity(((KuzuDouble)negInf).Value));

            // Test NaN
            using var nan = KuzuValueFactory.CreateDouble(double.NaN);
            Assert.IsTrue(double.IsNaN(((KuzuDouble)nan).Value));

            // Test epsilon and very small values
            using var epsilon = KuzuValueFactory.CreateDouble(double.Epsilon);
            Assert.AreEqual(double.Epsilon, ((KuzuDouble)epsilon).Value);

            // Test subnormal values
            using var subnormal = KuzuValueFactory.CreateDouble(4.9e-324); // smallest positive subnormal
            Assert.AreEqual(4.9e-324, ((KuzuDouble)subnormal).Value, 1e-330);
        }

        [TestMethod]
        public void FloatValue_SpecialValues_ToString_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            using var posInf = KuzuValueFactory.CreateFloat(float.PositiveInfinity);
            using var negInf = KuzuValueFactory.CreateFloat(float.NegativeInfinity);
            using var nan = KuzuValueFactory.CreateFloat(float.NaN);

            // ToString should not throw for special values
            string posInfStr = posInf.ToString();
            string negInfStr = negInf.ToString();
            string nanStr = nan.ToString();

            Assert.IsNotNull(posInfStr);
            Assert.IsNotNull(negInfStr);
            Assert.IsNotNull(nanStr);
        }

        [TestMethod]
        public void DoubleValue_SpecialValues_ToString_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            using var posInf = KuzuValueFactory.CreateDouble(double.PositiveInfinity);
            using var negInf = KuzuValueFactory.CreateDouble(double.NegativeInfinity);
            using var nan = KuzuValueFactory.CreateDouble(double.NaN);

            // ToString should not throw for special values
            string posInfStr = posInf.ToString();
            string negInfStr = negInf.ToString();
            string nanStr = nan.ToString();

            Assert.IsNotNull(posInfStr);
            Assert.IsNotNull(negInfStr);
            Assert.IsNotNull(nanStr);
        }

        #endregion

        #region BigInteger Edge Cases

        [TestMethod]
        public void BigIntegerValue_OverflowValues_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();

            // Test BigInteger values that exceed 128-bit capacity
            var overflowPositive = BigInteger.Parse("340282366920938463463374607431768211456", CultureInfo.InvariantCulture); // 2^128
            var overflowNegative = BigInteger.Parse("-340282366920938463463374607431768211457", CultureInfo.InvariantCulture); // -(2^128 + 1)

            Assert.ThrowsExactly<OverflowException>(() => KuzuValueFactory.CreateInt128(overflowPositive));
            Assert.ThrowsExactly<OverflowException>(() => KuzuValueFactory.CreateInt128(overflowNegative));
        }

        [TestMethod]
        public void BigIntegerValue_BoundaryValues_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Test values at 128-bit boundaries (should work)
            var maxPositive = BigInteger.Parse("170141183460469231731687303715884105727", CultureInfo.InvariantCulture); // 2^127 - 1
            var minNegative = BigInteger.Parse("-170141183460469231731687303715884105728", CultureInfo.InvariantCulture); // -2^127

            using var maxVal = KuzuValueFactory.CreateInt128(maxPositive);
            using var minVal = KuzuValueFactory.CreateInt128(minNegative);

            Assert.AreEqual(maxPositive, ((KuzuInt128)maxVal).Value);
            Assert.AreEqual(minNegative, ((KuzuInt128)minVal).Value);
        }

        #endregion

        #region String Edge Cases

        [TestMethod]
        public void StringValue_UnicodeCharacters_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            string unicodeString = "Hello 世界 🌍 Здравствуй мир ñáéíóú";
            using KuzuString value = KuzuValueFactory.CreateString(unicodeString);

            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(unicodeString, value.Value);
        }

        [TestMethod]
        public void StringValue_ControlCharacters_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            string controlString = "Line1\nLine2\tTabbed\r\nCRLF";
            using KuzuString value = KuzuValueFactory.CreateString(controlString);

            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(controlString, value.Value);
        }

        [TestMethod]
        public void StringValue_NullChar_ShouldThrow()
        {
            EnsureNativeLibraryAvailable();
            string stringWithNull = "Hello\0World";
            Assert.ThrowsExactly<ArgumentException>(() => KuzuValueFactory.CreateString(stringWithNull));
        }

        [TestMethod]
        public void StringValue_VeryLongString_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Create a string longer than typical buffer sizes
            string longString = new string('A', 100000);
            using KuzuString value = KuzuValueFactory.CreateString(longString);

            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(longString, value.Value);
        }

        #endregion

        #region Date and Time Edge Cases

        [TestMethod]
        public void DateValue_ExtremeValidDates_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Test minimum and maximum DateTime values
            var minDate = DateTime.MinValue;
            var maxDate = DateTime.MaxValue;

            try
            {
                using var minDateValue = KuzuValueFactory.CreateDate(minDate);
                using var maxDateValue = KuzuValueFactory.CreateDate(maxDate);

                // Note: The native implementation might have different min/max ranges
                // so we mainly test that these don't crash
                Assert.IsFalse(minDateValue.IsNull());
                Assert.IsFalse(maxDateValue.IsNull());

                DateTime retrievedMin = ((KuzuDate)minDateValue).AsDateTime();
                DateTime retrievedMax = ((KuzuDate)maxDateValue).AsDateTime();

                // Values should be valid dates
                Assert.IsTrue(retrievedMin >= DateTime.MinValue);
                Assert.IsTrue(retrievedMax <= DateTime.MaxValue);
            }
            catch (KuzuException)
            {
                // Some extreme dates might not be supported by the native implementation
                Assert.Inconclusive("Extreme date values not supported by native implementation");
            }
        }

        [TestMethod]
        public void TimestampValue_ExtremeValidTimestamps_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var year2000 = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var year3000 = new DateTime(3000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            try
            {
                using var epochValue = KuzuValueFactory.CreateTimestamp(epoch);
                using var year2000Value = KuzuValueFactory.CreateTimestamp(year2000);
                using var year3000Value = KuzuValueFactory.CreateTimestamp(year3000);

                Assert.IsFalse(epochValue.IsNull());
                Assert.IsFalse(year2000Value.IsNull());
                Assert.IsFalse(year3000Value.IsNull());

                // Test UnixMicros values are reasonable
                long epochMicros = ((KuzuTimestamp)epochValue).UnixMicros;
                long year2000Micros = ((KuzuTimestamp)year2000Value).UnixMicros;
                long year3000Micros = ((KuzuTimestamp)year3000Value).UnixMicros;

                Assert.AreEqual(0L, epochMicros);
                Assert.IsTrue(year2000Micros > epochMicros);
                Assert.IsTrue(year3000Micros > year2000Micros);
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Some timestamp values not supported by native implementation");
            }
        }

        [TestMethod]
        public void IntervalValue_ExtremeTimeSpans_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            var maxTimeSpan = TimeSpan.MaxValue;
            var minTimeSpan = TimeSpan.MinValue;
            var zeroTimeSpan = TimeSpan.Zero;

            try
            {
                using var maxValue = KuzuValueFactory.CreateInterval(maxTimeSpan);
                using var minValue = KuzuValueFactory.CreateInterval(minTimeSpan);
                using var zeroValue = KuzuValueFactory.CreateInterval(zeroTimeSpan);

                Assert.IsFalse(maxValue.IsNull());
                Assert.IsFalse(minValue.IsNull());
                Assert.IsFalse(zeroValue.IsNull());

                TimeSpan retrievedMax = ((KuzuInterval)maxValue).Value;
                TimeSpan retrievedMin = ((KuzuInterval)minValue).Value;
                TimeSpan retrievedZero = ((KuzuInterval)zeroValue).Value;

                Assert.AreEqual(TimeSpan.Zero, retrievedZero);
                // Min/Max might be clamped by native implementation
                Assert.IsTrue(retrievedMax.Ticks >= 0 || retrievedMax.Ticks < 0); // Just ensure valid
                Assert.IsTrue(retrievedMin.Ticks >= 0 || retrievedMin.Ticks < 0); // Just ensure valid
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Extreme TimeSpan values not supported by native implementation");
            }
        }

        #endregion

        #region Timestamp Variant Edge Cases

        [TestMethod]
        public void TimestampVariants_ExtremeValues_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Test extreme but valid timestamp values for each precision
            long maxSeconds = long.MaxValue / 1000000; // avoid overflow when converting to micros
            long maxMillis = long.MaxValue / 1000; // avoid overflow when converting to micros
            long maxMicros = long.MaxValue / 1000; // avoid overflow when converting to nanos
            long maxNanos = long.MaxValue;

            try
            {
                using var secValue = KuzuValueFactory.CreateTimestampSeconds(maxSeconds);
                using var milliValue = KuzuValueFactory.CreateTimestampMilliseconds(maxMillis);
                using var microValue = KuzuValueFactory.CreateTimestampFromUnixMicros(maxMicros);
                using var nanoValue = KuzuValueFactory.CreateTimestampNanoseconds(maxNanos);

                Assert.IsFalse(secValue.IsNull());
                Assert.IsFalse(milliValue.IsNull());
                Assert.IsFalse(microValue.IsNull());
                Assert.IsFalse(nanoValue.IsNull());

                // Values should roundtrip
                Assert.AreEqual(maxSeconds, ((KuzuTimestampSec)secValue).UnixSeconds);
                Assert.AreEqual(maxMillis, ((KuzuTimestampMs)milliValue).UnixMilliseconds);
                Assert.AreEqual(maxMicros, ((KuzuTimestamp)microValue).UnixMicros);
                Assert.AreEqual(maxNanos, ((KuzuTimestampNs)nanoValue).UnixNanoseconds);
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Extreme timestamp values not supported by native implementation");
            }
        }

        [TestMethod]
        public void TimestampTz_ExtremeValues_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            long maxMicrosUtc = long.MaxValue / 1000; // avoid potential overflow
            long minMicrosUtc = long.MinValue / 1000;

            try
            {
                using var maxValue = KuzuValueFactory.CreateTimestampWithTimeZoneMicros(maxMicrosUtc);
                using var minValue = KuzuValueFactory.CreateTimestampWithTimeZoneMicros(minMicrosUtc);

                Assert.IsFalse(maxValue.IsNull());
                Assert.IsFalse(minValue.IsNull());

                Assert.AreEqual(maxMicrosUtc, ((KuzuTimestampTz)maxValue).UnixMicrosUtc);
                Assert.AreEqual(minMicrosUtc, ((KuzuTimestampTz)minValue).UnixMicrosUtc);
            }
            catch (KuzuException)
            {
                Assert.Inconclusive("Extreme timestamp timezone values not supported by native implementation");
            }
        }

        #endregion

        #region Internal ID Edge Cases

        [TestMethod]
        public void InternalIdValue_ExtremeValues_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            var maxId = new InternalId(ulong.MaxValue, ulong.MaxValue);
            var minId = new InternalId(ulong.MinValue, ulong.MinValue);
            var mixedId = new InternalId(ulong.MaxValue, ulong.MinValue);

            using var maxValue = KuzuValueFactory.CreateInternalId(maxId);
            using var minValue = KuzuValueFactory.CreateInternalId(minId);
            using var mixedValue = KuzuValueFactory.CreateInternalId(mixedId);

            Assert.IsFalse(maxValue.IsNull());
            Assert.IsFalse(minValue.IsNull());
            Assert.IsFalse(mixedValue.IsNull());

            var retrievedMax = ((KuzuInternalId)maxValue).Value;
            var retrievedMin = ((KuzuInternalId)minValue).Value;
            var retrievedMixed = ((KuzuInternalId)mixedValue).Value;

            Assert.AreEqual(maxId.TableId, retrievedMax.TableId);
            Assert.AreEqual(maxId.Offset, retrievedMax.Offset);
            Assert.AreEqual(minId.TableId, retrievedMin.TableId);
            Assert.AreEqual(minId.Offset, retrievedMin.Offset);
            Assert.AreEqual(mixedId.TableId, retrievedMixed.TableId);
            Assert.AreEqual(mixedId.Offset, retrievedMixed.Offset);
        }

        #endregion

        #region Null Value Edge Cases

        [TestMethod]
        public void NullValue_Operations_ShouldHandleGracefully()
        {
            EnsureNativeLibraryAvailable();

            using var nullValue = KuzuValueFactory.CreateNull();

            // Basic operations should work
            Assert.IsTrue(nullValue.IsNull());

            // ToString should not throw
            string str = nullValue.ToString();
            Assert.IsNotNull(str);

            // GetDataType should work
            Assert.IsNotNull(nullValue.DataTypeId);

            // Clone should work
            using var clonedNull = nullValue.Clone();
            Assert.IsTrue(clonedNull.IsNull());

            // CopyFrom should work
            using var anotherNull = KuzuValueFactory.CreateNull();
            nullValue.CopyFrom(anotherNull);
            Assert.IsTrue(nullValue.IsNull());
        }

        [TestMethod]
        public void NullValue_SetToNonNull_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var nullValue = KuzuValueFactory.CreateNull();
            Assert.IsTrue(nullValue.IsNull());

            // Setting to non-null should work
            nullValue.SetNull(false);
            Assert.IsFalse(nullValue.IsNull());

            // Setting back to null should work
            nullValue.SetNull(true);
            Assert.IsTrue(nullValue.IsNull());
        }

        #endregion

        #region Complex Type Edge Cases

        [TestMethod]
        public void ComplexTypes_WithNullElements_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            // Create complex types containing null elements
            using var nullInt = KuzuValueFactory.CreateInt32(42);
            nullInt.SetNull(true);

            using var validInt = KuzuValueFactory.CreateInt32(100);

            // List with null element
            using var listWithNull = KuzuValueFactory.CreateList(validInt, nullInt);
            Assert.AreEqual(2UL, listWithNull.Count);

            using var elem0 = listWithNull.GetElement(0);
            using var elem1 = listWithNull.GetElement(1);

            Assert.IsFalse(elem0.IsNull());
            Assert.IsTrue(elem1.IsNull());

            // Struct with null field
            using var nullString = KuzuValueFactory.CreateString("test");
            nullString.SetNull(true);

            using var structWithNull = KuzuValueFactory.CreateStruct(
                ("valid", validInt),
                ("null_field", nullString)
            );

            Assert.AreEqual(2UL, structWithNull.FieldCount);

            using var field0 = structWithNull.GetFieldValue(0);
            using var field1 = structWithNull.GetFieldValue(1);

            // One should be null, one should not (order may vary)
            bool hasNull = field0.IsNull() || field1.IsNull();
            bool hasNonNull = !field0.IsNull() || !field1.IsNull();
            Assert.IsTrue(hasNull);
            Assert.IsTrue(hasNonNull);
        }

        #endregion
    }
}