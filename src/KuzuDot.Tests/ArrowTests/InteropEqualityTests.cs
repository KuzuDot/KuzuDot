using KuzuDot.Value;
using System;

namespace KuzuDot.Tests.ArrowTests
{
    [TestClass]
    public class InteropEqualityTests
    {
        [TestMethod]
        public void ArrowSchema_EqualityAndHashCode()
        {
            var a = new KuzuDot.ArrowSchema
            {
                format = new IntPtr(1),
                name = new IntPtr(2),
                metadata = new IntPtr(3),
                flags = 4,
                n_children = 5,
                children = new IntPtr(6),
                dictionary = new IntPtr(7),
                release = new IntPtr(8),
                private_data = new IntPtr(9)
            };
            var b = new KuzuDot.ArrowSchema
            {
                format = new IntPtr(1),
                name = new IntPtr(2),
                metadata = new IntPtr(3),
                flags = 4,
                n_children = 5,
                children = new IntPtr(6),
                dictionary = new IntPtr(7),
                release = new IntPtr(8),
                private_data = new IntPtr(9)
            };

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            var c = new KuzuDot.ArrowSchema
            {
                format = new IntPtr(1),
                name = new IntPtr(2),
                metadata = new IntPtr(3),
                flags = 4,
                n_children = 5,
                children = new IntPtr(6),
                dictionary = new IntPtr(7),
                release = new IntPtr(8),
                private_data = new IntPtr(10)
            };

            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a != c);
        }

        [TestMethod]
        public void ArrowArray_EqualityAndHashCode()
        {
            var a = new KuzuDot.ArrowArray
            {
                length = 1,
                null_count = 2,
                offset = 3,
                n_buffers = 4,
                n_children = 5,
                buffers = new IntPtr(6),
                children = new IntPtr(7),
                dictionary = new IntPtr(8),
                release = new IntPtr(9),
                private_data = new IntPtr(10)
            };
            var b = new KuzuDot.ArrowArray
            {
                length = 1,
                null_count = 2,
                offset = 3,
                n_buffers = 4,
                n_children = 5,
                buffers = new IntPtr(6),
                children = new IntPtr(7),
                dictionary = new IntPtr(8),
                release = new IntPtr(9),
                private_data = new IntPtr(10)
            };

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            b.length = 99;
            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }
    }

    [TestClass]
    public class InternalIdEqualityTests
    {
        [TestMethod]
        public void InternalId_EqualsAndHashCodeAndOperators()
        {
            var a = new InternalId(5, 10);
            var b = new InternalId(5, 10);
            var c = new InternalId(6, 10);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            Assert.IsFalse(a.Equals(c));
            Assert.IsFalse(a == c);
            Assert.IsTrue(a != c);
        }
    }
}
