using KuzuDot.Value;
using System.Numerics;

namespace KuzuDot.Tests.KuzuValueTests
{
    [TestClass]
    public class KuzuValueUnitTests
    {
        [TestMethod]
        public void CreateNull_IsNullShouldBeTrue_ThenUnset()
        {
            using var v = KuzuValueFactory.CreateNull();
            Assert.IsTrue(v.IsNull());
            v.SetNull(false); // Should throw exception
            Assert.IsFalse(v.IsNull());
            v.SetNull(true);
            Assert.IsTrue(v.IsNull());
        }

        [TestMethod]
        public void PrimitiveBool_RoundTrip()
        {
            using var t = KuzuValueFactory.CreateBool(true);
            using var f = KuzuValueFactory.CreateBool(false);
            Assert.IsInstanceOfType<KuzuBool>(t);
            Assert.IsInstanceOfType<KuzuBool>(f);
            var tv = (KuzuBool)t; var fv = (KuzuBool)f;
            Assert.AreEqual(true, tv.Value);
            Assert.AreEqual(false, fv.Value);
        }

        [TestMethod]
        public void Integers_SignedUnsigned_RoundTrip()
        {
            using var i8 = KuzuValueFactory.CreateInt8(-5);
            using var i16 = KuzuValueFactory.CreateInt16(-32000);
            using var i32 = KuzuValueFactory.CreateInt32(-123456);
            using var i64 = KuzuValueFactory.CreateInt64(long.MaxValue);
            using var u8 = KuzuValueFactory.CreateUInt8(200);
            using var u16 = KuzuValueFactory.CreateUInt16(65000);
            using var u32 = KuzuValueFactory.CreateUInt32(4000000000u);
            using var u64 = KuzuValueFactory.CreateUInt64(18000000000000000000UL);
            Assert.IsInstanceOfType<KuzuInt8>(i8);
            Assert.IsInstanceOfType<KuzuInt16>(i16);
            Assert.IsInstanceOfType<KuzuInt32>(i32);
            Assert.IsInstanceOfType<KuzuInt64>(i64);
            Assert.IsInstanceOfType<KuzuUInt8>(u8);
            Assert.IsInstanceOfType<KuzuUInt16>(u16);
            Assert.IsInstanceOfType<KuzuUInt32>(u32);
            Assert.IsInstanceOfType<KuzuUInt64>(u64);
            Assert.AreEqual(-5, ((KuzuInt8)i8).Value);
            Assert.AreEqual(-32000, ((KuzuInt16)i16).Value);
            Assert.AreEqual(-123456, ((KuzuInt32)i32).Value);
            Assert.AreEqual(long.MaxValue, ((KuzuInt64)i64).Value);
            Assert.AreEqual((byte)200, ((KuzuUInt8)u8).Value);
            Assert.AreEqual((ushort)65000, ((KuzuUInt16)u16).Value);
            Assert.AreEqual(4000000000u, ((KuzuUInt32)u32).Value);
            Assert.AreEqual(18000000000000000000UL, ((KuzuUInt64)u64).Value);
        }

        [TestMethod]
        public void BigInteger_RoundTrip_Basic()
        {
            var big = BigInteger.Parse("123456789012345678901234567890", System.Globalization.CultureInfo.InvariantCulture);
            using var v = KuzuValueFactory.CreateInt128(big);
            Assert.IsInstanceOfType<KuzuInt128>(v);
            var back = ((KuzuInt128)v).Value;
            Assert.AreEqual(big, back);
        }

        [TestMethod]
        public void FloatDouble_RoundTrip_WithTolerance()
        {
            using var f = KuzuValueFactory.CreateFloat(3.14159f);
            using var d = KuzuValueFactory.CreateDouble(2.718281828459045);
            Assert.IsInstanceOfType<KuzuFloat>(f);
            Assert.IsInstanceOfType<KuzuDouble>(d);
            Assert.AreEqual(3.14159f, ((KuzuFloat)f).Value, 0.00001f);
            Assert.AreEqual(2.718281828459045, ((KuzuDouble)d).Value, 1e-12);
        }

        [TestMethod]
        public void DateTimestampInterval_RoundTrip()
        {
            var date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var ts = DateTime.UtcNow;
            var span = TimeSpan.FromHours(49) + TimeSpan.FromMilliseconds(123);
            using var dVal = KuzuValueFactory.CreateDate(date);
            using var tsVal = KuzuValueFactory.CreateTimestamp(ts);
            using var intVal = KuzuValueFactory.CreateInterval(span);
            Assert.IsInstanceOfType<KuzuDate>(dVal);
            Assert.IsInstanceOfType<KuzuTimestamp>(tsVal);
            Assert.IsInstanceOfType<KuzuInterval>(intVal);
            Assert.AreEqual(date.Date, ((KuzuDate)dVal).AsDateTime());
            Assert.AreEqual(ts.ToLongTimeString(), ((KuzuTimestamp)tsVal).Value.ToLongTimeString());
            var backSpan = ((KuzuInterval)intVal).Value;
            Assert.AreEqual(span.Days, backSpan.Days);
            Assert.AreEqual(span.Hours, backSpan.Hours);
        }

        [TestMethod]
        public void InternalId_RoundTrip()
        {
            var id = new InternalId(7, 999);
            using var v = KuzuValueFactory.CreateInternalId(id);
            Assert.IsInstanceOfType<KuzuInternalId>(v);
            var back = ((KuzuInternalId)v).Value;
            Assert.AreEqual(id.TableId, back.TableId);
            Assert.AreEqual(id.Offset, back.Offset);
        }

        [TestMethod]
        public void String_EmptyAndRegular_RoundTrip()
        {
            using var empty = KuzuValueFactory.CreateString(string.Empty);
            using var hello = KuzuValueFactory.CreateString("Hello World");
            Assert.IsInstanceOfType<KuzuString>(empty);
            Assert.IsInstanceOfType<KuzuString>(hello);
            Assert.AreEqual(string.Empty, ((KuzuString)empty).Value);
            Assert.AreEqual("Hello World", ((KuzuString)hello).Value);
        }

        [TestMethod]
        public void Clone_ShouldProduceIndependentCopy()
        {
            using var original = KuzuValueFactory.CreateInt32(123);
            using var clone = original.Clone();
            Assert.IsInstanceOfType<KuzuInt32>(original);
            Assert.IsInstanceOfType<KuzuInt32>(clone);
            Assert.AreEqual(123, ((KuzuInt32)original).Value);
            Assert.AreEqual(123, ((KuzuInt32)clone).Value);
            using var newVal = KuzuValueFactory.CreateInt32(456);
            clone.CopyFrom(newVal);
            Assert.AreEqual(123, ((KuzuInt32)original).Value);
            Assert.AreEqual(456, ((KuzuInt32)clone).Value);
        }

        [TestMethod]
        public void CopyFrom_ShouldOverwriteTargetValue()
        {
            using var target = KuzuValueFactory.CreateInt64(100);
            using var source = KuzuValueFactory.CreateInt64(200);
            Assert.IsInstanceOfType<KuzuInt64>(target);
            Assert.IsInstanceOfType<KuzuInt64>(source);
            target.CopyFrom(source);
            Assert.AreEqual(200L, ((KuzuInt64)target).Value);
        }

        [TestMethod]
        public void TypeMismatch_Getter_ShouldThrow()
        {
            using KuzuValue v = KuzuValueFactory.CreateInt32(10); // ensure static type is base class
            Assert.ThrowsExactly<InvalidCastException>(() =>
            {
                var _ = (KuzuDouble)v; // invalid cast from Int32Value to DoubleValue at runtime
            });
        }

        [TestMethod]
        public void ToString_ShouldReturnNonEmpty_ForPrimitive()
        {
            using var v = KuzuValueFactory.CreateInt16(42);
            Assert.IsInstanceOfType<KuzuInt16>(v);
            var s = v.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(s));
        }

        [TestMethod]
        public void NullHandling_SetNullTrueThenFalse()
        {
            using var v = KuzuValueFactory.CreateInt8(5);
            Assert.IsInstanceOfType<KuzuInt8>(v);
            Assert.IsFalse(v.IsNull());
            v.SetNull(true);
            Assert.IsTrue(v.IsNull());
            v.SetNull(false);
            Assert.IsFalse(v.IsNull());
        }

        [TestMethod]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            var v = KuzuValueFactory.CreateUInt32(99);
            Assert.IsInstanceOfType<KuzuUInt32>(v);
            v.Dispose();
            v.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => { var _ = ((KuzuUInt32)v).Value; });
        }
    }
}
