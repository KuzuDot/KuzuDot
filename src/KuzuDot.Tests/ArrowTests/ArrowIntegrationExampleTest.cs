using Apache.Arrow;
using System.Runtime.InteropServices;

namespace KuzuDot.Tests.ArrowTests
{
    /// <summary>
    /// This test purposefully shows a "happy path" convenience style pattern for
    /// turning a Kuzu query result into an Apache Arrow managed array with minimal code.
    /// It contrasts with the lower-level interop heavy tests by wrapping the few native
    /// Arrow C Data Interface steps (struct unwrapping + data buffer extraction) into
    /// tiny local helpers so application code stays simple.
    /// </summary>
    [TestClass]
    public class ArrowIntegrationExampleTest
    {
        private static string CString(IntPtr p) => p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(p) ?? string.Empty);

        [TestMethod]
        public void Easy_Int64_Materialization_Demo()
        {
            // Skip gracefully if native library (and thus Arrow feature) unavailable.
            KuzuDot.Database db;
            try { db = Database.FromMemory(); }
            catch (KuzuDot.KuzuException ex)
            {
                Assert.Inconclusive("Native Kuzu library not available: " + ex.Message);
                return;
            }

            using (db)
            using (var conn = db.Connect())
            {
                conn.Query("CREATE NODE TABLE Num(id INT64, PRIMARY KEY(id));").Dispose();
                conn.Query("CREATE (:Num {id: 10});").Dispose();
                conn.Query("CREATE (:Num {id: 20});").Dispose();
                conn.Query("CREATE (:Num {id: 30});").Dispose();

                using var result = conn.Query("MATCH (n:Num) RETURN n.id ORDER BY n.id;");

                if (!result.TryGetArrowSchema(out var schema))
                {
                    Assert.Inconclusive("Arrow schema not available (feature disabled in this native build)");
                    return;
                }

                // Fetch a single chunk (all rows expected to fit)
                if (!result.TryGetNextArrowChunk(100, out var topArray))
                {
                    Assert.Inconclusive("Arrow array chunk not available (feature disabled in this native build)");
                    return;
                }

                // One-liner: get managed Int64Array using our small helper wrappers.
                var managed = BuildInt64Array(topArray, schema);

                // Show how easy value access is once in managed Arrow structures.
                CollectionAssert.AreEqual(new long[] { 10, 20, 30 }, managed.Values.ToArray()); // convert span to array
                Assert.AreEqual(3, managed.Length);
                Assert.AreEqual(20L, managed.GetValue(1));
            }
        }

        // --- Helper region (would normally live in a reusable utility class) ---

        private static Int64Array BuildInt64Array(KuzuDot.ArrowArray topArray, KuzuDot.ArrowSchema schema)
        {
            var dataArray = UnwrapIfStruct(topArray, schema);
            var values = ReadInt64Values(dataArray);
            var builder = new Int64Array.Builder();
            builder.AppendRange(values);
            return builder.Build();
        }

        private static KuzuDot.ArrowArray UnwrapIfStruct(KuzuDot.ArrowArray array, KuzuDot.ArrowSchema schema)
        {
            string fmt = CString(schema.format);
            bool isStruct = fmt == "+s"; // Arrow C data interface struct wrapper
            if (!isStruct && array.n_children == 0)
                return array; // already primitive

            // Read first child ArrowArray*
            if (array.children == IntPtr.Zero)
                throw new InvalidOperationException("Struct array children pointer was null");
            IntPtr firstChildPtr = Marshal.ReadIntPtr(array.children);
            if (firstChildPtr == IntPtr.Zero)
                throw new InvalidOperationException("First child pointer was null");
            return Marshal.PtrToStructure<KuzuDot.ArrowArray>(firstChildPtr);
        }

        private static long[] ReadInt64Values(KuzuDot.ArrowArray primitive)
        {
            long len = primitive.length;
            if (len <= 0) return [];
            if (primitive.buffers == IntPtr.Zero)
                throw new InvalidOperationException("Buffers pointer null");

            int bufferCount = (int)primitive.n_buffers;
            var ptrs = new IntPtr[bufferCount];
            Marshal.Copy(primitive.buffers, ptrs, 0, bufferCount);

            // For a fixed-width primitive: buffer[0] = validity (optional), buffer[1] = values
            IntPtr dataBuf = bufferCount > 1 && ptrs[1] != IntPtr.Zero ? ptrs[1] : ptrs[0];
            if (dataBuf == IntPtr.Zero)
                throw new InvalidOperationException("Data buffer pointer null");

            var values = new long[len];
            for (int i = 0; i < len; i++)
                values[i] = Marshal.ReadInt64(dataBuf, i * 8);
            return values;
        }
    }
}
