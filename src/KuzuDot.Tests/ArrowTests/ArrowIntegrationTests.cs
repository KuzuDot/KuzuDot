using Apache.Arrow;
using Apache.Arrow.Types;
using System.Runtime.InteropServices;

namespace KuzuDot.Tests.ArrowTests
{
    /// <summary>
    /// These test that data from Kuzu can be materialized into managed Apache Arrow arrays.
    /// </summary>
    [TestClass]
    public class ArrowIntegrationTests
    {
        private static string PtrToCString(IntPtr p) => p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(p) ?? string.Empty);

        [TestMethod]
        public void QueryResult_CanMaterializeApacheArrow_Int64Column()
        {
            // Arrange: attempt to create in-memory database
            Database db;
            try
            {
                db = Database.FromMemory();
            }
            catch (KuzuException ex)
            {
                Assert.Inconclusive("Native Kuzu library not available: " + ex.Message);
                return;
            }

            using (db)
            using (var conn = db.Connect())
            {
                conn.Query("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));").Dispose();
                conn.Query("CREATE (:T {id: 1});").Dispose();
                conn.Query("CREATE (:T {id: 2});").Dispose();
                conn.Query("CREATE (:T {id: 3});").Dispose();

                using var result = conn.Query("MATCH (x:T) RETURN x.id ORDER BY x.id;");

                if (!result.TryGetArrowSchema(out ArrowSchema schema))
                {
                    Assert.Inconclusive("Arrow schema not available (feature disabled in this native build)");
                    return;
                }

                string topFormat = PtrToCString(schema.format);
                bool isStruct = topFormat == "+s"; // struct wrapper

                if (!result.TryGetNextArrowChunk(100, out ArrowArray topArray))
                {
                    Assert.Inconclusive("Arrow array chunk not available (feature disabled in this native build)");
                    return;
                }

                // Resolve to the primitive data array (might be child 0 of a struct)
                ArrowArray dataArray = topArray;
                if (isStruct || topArray.n_children > 0)
                {
                    Assert.AreNotEqual(IntPtr.Zero, topArray.children, "Struct children pointer null");
                    IntPtr firstChildPtr = Marshal.ReadIntPtr(topArray.children);
                    Assert.AreNotEqual(IntPtr.Zero, firstChildPtr, "First child ArrowArray pointer null");
                    dataArray = Marshal.PtrToStructure<ArrowArray>(firstChildPtr);
                }

                long rowCount = dataArray.length;
                Assert.AreEqual(3, rowCount, "Expected 3 rows");

                // Extract buffers
                Assert.AreNotEqual(IntPtr.Zero, dataArray.buffers, "buffers pointer must not be null");
                int bufferCount = (int)dataArray.n_buffers;
                var bufferPtrs = new IntPtr[bufferCount];
                Marshal.Copy(dataArray.buffers, bufferPtrs, 0, bufferCount);

                // Locate primitive values buffer (allowing optional validity buffer)
                IntPtr dataBuffer = IntPtr.Zero;
                if (bufferCount > 1 && bufferPtrs[1] != IntPtr.Zero)
                    dataBuffer = bufferPtrs[1];
                else if (bufferCount >= 1)
                    dataBuffer = bufferPtrs[0];

                Assert.AreNotEqual(IntPtr.Zero, dataBuffer, "Could not locate data buffer");

                // Copy unmanaged int64 values into managed array
                var values = new long[rowCount];
                for (int i = 0; i < rowCount; i++)
                {
                    values[i] = Marshal.ReadInt64(dataBuffer, i * 8);
                }

                // Build a managed Apache.Arrow Int64Array from the values (materialization step)
                var builder = new Int64Array.Builder();
                builder.AppendRange(values);
                Int64Array managedArray = builder.Build();

                // Wrap into a RecordBatch (simple schema with one field)
                var field = new Field("id", Int64Type.Default, nullable: dataArray.null_count != 0);
                var schemaArrow = new Schema.Builder().Field(field).Build();
                using var recordBatch = new RecordBatch(schemaArrow, [managedArray], managedArray.Length);

                // Assertions using Apache Arrow managed structures
                Assert.AreEqual(1L, ((Int64Array)recordBatch.Column(0)).GetValue(0));
                Assert.AreEqual(2L, ((Int64Array)recordBatch.Column(0)).GetValue(1));
                Assert.AreEqual(3L, ((Int64Array)recordBatch.Column(0)).GetValue(2));
            }
        }
    }
}
