using System.Runtime.InteropServices;

namespace KuzuDot.Tests.ArrowTests
{
    /// <summary>
    /// Tests that directly access Arrow C Data Interface structs from a Kuzu query result
    /// These don't use Apache Arrow .NET types, just check the raw C Data Interface structs
    /// </summary>
    [TestClass]
    public class DirectArrowDataTests
    {
        private static string PtrToCString(IntPtr p) => p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(p) ?? string.Empty);

        [TestMethod]
        public void QueryResult_CanReturnArrowData_AndAccessValues()
        {
            // Arrange: create in-memory database (skip test if native library missing)
            using var db = Database.FromMemory();
            using var conn = db.Connect();
            {
                // Create table & insert a few rows
                conn.Query("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));").Dispose();
                conn.Query("CREATE (:T {id: 1});").Dispose();
                conn.Query("CREATE (:T {id: 2});").Dispose();
                conn.Query("CREATE (:T {id: 3});").Dispose();

                using var result = conn.Query("MATCH (x:T) RETURN x.id ORDER BY x.id;");

                // Attempt to fetch Arrow schema
                if (!result.TryGetArrowSchema(out ArrowSchema schema))
                {
                    Assert.Inconclusive("Arrow schema not available (feature disabled in this native build)");
                    return;
                }

                string topFormat = PtrToCString(schema.format);
                Assert.IsFalse(string.IsNullOrEmpty(topFormat), "Arrow format string should be present");
                // Accept primitive int64 or struct (+s) wrapping one child column
                bool isPrimitiveInt64 = topFormat == "l" || topFormat == "g" || topFormat == "L"; // various signed 64 representations
                bool isStruct = topFormat == "+s"; // Arrow C data interface format for struct
                Assert.IsTrue(isPrimitiveInt64 || isStruct, "Unexpected top-level Arrow format: " + topFormat);

                // Fetch one Arrow chunk (large chunkSize to get all rows)
                if (!result.TryGetNextArrowChunk(100, out ArrowArray topArray))
                {
                    Assert.Inconclusive("Arrow array chunk not available (feature disabled in this native build)");
                    return;
                }

                // Determine actual data array (may be topArray itself or its first child if struct)
                ArrowArray dataArray = topArray;
                if (isStruct || topArray.n_children > 0)
                {
                    Assert.IsTrue(topArray.n_children >= 1, "Struct array should have at least one child");
                    Assert.AreNotEqual(IntPtr.Zero, topArray.children, "Struct children pointer null");
                    // Read first child pointer (ArrowArray**)
                    IntPtr firstChildPtr = Marshal.ReadIntPtr(topArray.children); // offset 0
                    Assert.AreNotEqual(IntPtr.Zero, firstChildPtr, "First child ArrowArray pointer null");
                    dataArray = Marshal.PtrToStructure<ArrowArray>(firstChildPtr);
                }

                Assert.AreEqual(3, dataArray.length, "Expected 3 rows in data array");

                // For a primitive fixed-width numeric array, expect at least validity & data buffers.
                Assert.AreNotEqual(IntPtr.Zero, dataArray.buffers, "buffers pointer must not be null");
                int bufferCount = (int)dataArray.n_buffers;
                Assert.IsTrue(bufferCount >= 1, "Expected at least one buffer (validity)");
                var bufferPtrs = new IntPtr[bufferCount];
                Marshal.Copy(dataArray.buffers, bufferPtrs, 0, bufferCount);

                // Data buffer may be at index 1 (validity + data) or index 0 if no validity buffer (all non-null)
                IntPtr dataBuffer = IntPtr.Zero;
                if (bufferCount > 1 && bufferPtrs[1] != IntPtr.Zero)
                {
                    dataBuffer = bufferPtrs[1];
                }
                else if (bufferPtrs[0] != IntPtr.Zero && bufferCount == 1)
                {
                    // Some producers can omit validity when no nulls and still use index 0 for data (rare, but defensive)
                    dataBuffer = bufferPtrs[0];
                }

                Assert.AreNotEqual(IntPtr.Zero, dataBuffer, "Could not locate primitive data buffer");

                long v0 = Marshal.ReadInt64(dataBuffer, 0);
                long v1 = Marshal.ReadInt64(dataBuffer, 8);
                long v2 = Marshal.ReadInt64(dataBuffer, 16);

                Assert.AreEqual(1L, v0, "Row 0 mismatch");
                Assert.AreEqual(2L, v1, "Row 1 mismatch");
                Assert.AreEqual(3L, v2, "Row 2 mismatch");
            }
        }
    }
}
