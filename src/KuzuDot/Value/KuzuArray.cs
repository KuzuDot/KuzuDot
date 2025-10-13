using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    public sealed class KuzuArray : KuzuList
    {
        internal KuzuArray(NativeKuzuValue n) : base(n)
        {
        }

                

        protected override ulong FetchCount()
        {
            ThrowIfDisposed();
            NativeMethods.kuzu_value_get_data_type(NativePtr, out var dt);
            var state = NativeMethods.kuzu_data_type_get_num_elements_in_array(ref dt, out var n);
            KuzuGuard.CheckSuccess(state, "Failed to get number of elements in array");
            return n;
        }
    }
}