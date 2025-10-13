using KuzuDot.Value;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Numerics;

namespace KuzuDot.Tests.KuzuValueTests
{
    [TestClass]
    public class KuzuTypedValuesCastingTests
    {
        [TestMethod]
        public void KuzuUInt64_CastingOperators_WorkCorrectly()
        {
            ulong ul = 12345678901234567890UL;
            using var v = KuzuValueFactory.CreateUInt64(ul);
            var kuzu = (KuzuUInt64)v;
            Assert.AreEqual(ul, (ulong)kuzu);
            Assert.AreEqual(ul, KuzuUInt64.FromKuzuUInt64(kuzu));
        }

        [TestMethod]
        public void KuzuFloat_CastingOperators_WorkCorrectly()
        {
            float f = 3.14f;
            using var v = KuzuValueFactory.CreateFloat(f);
            var kuzu = (KuzuFloat)v;
            Assert.AreEqual(f, (float)kuzu);
            Assert.AreEqual(f, KuzuFloat.FromKuzuFloat(kuzu));
        }

        [TestMethod]
        public void KuzuDouble_CastingOperators_WorkCorrectly()
        {
            double d = 2.718281828459045;
            using var v = KuzuValueFactory.CreateDouble(d);
            var kuzu = (KuzuDouble)v;
            Assert.AreEqual(d, (double)kuzu, 1e-12);
            Assert.AreEqual(d, KuzuDouble.FromKuzuDouble(kuzu), 1e-12);
        }

        [TestMethod]
        public void KuzuInt128_CastingOperators_WorkCorrectly()
        {
            var big = BigInteger.Parse("123456789012345678901234567890", CultureInfo.InvariantCulture);
            using var v = KuzuValueFactory.CreateInt128(big);
            var kuzu = (KuzuInt128)v;
            Assert.AreEqual(big, (BigInteger)kuzu);
            Assert.AreEqual(big, KuzuInt128.FromKuzuInt128(kuzu));
        }

        [TestMethod]
        public void KuzuInterval_CastingOperators_WorkCorrectly()
        {
            var span = TimeSpan.FromDays(2) + TimeSpan.FromMinutes(30);
            using var v = KuzuValueFactory.CreateInterval(span);
            var kuzu = (KuzuInterval)v;
            Assert.AreEqual(span, (TimeSpan)kuzu);
            Assert.AreEqual(span, KuzuInterval.FromKuzuInterval(kuzu));
        }
    }
}
