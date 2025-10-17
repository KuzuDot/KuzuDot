using KuzuDot.Native;
using System;

namespace KuzuDot.Value
{
    public sealed class KuzuAny : KuzuValue
    {
        // Create Null creates a pointer to a KuzuAny value that is null.
        internal KuzuAny(IntPtr ptr) : base(ptr)
        {
        }

        internal KuzuAny(NativeKuzuValue nativeStruct) : base(nativeStruct)
        {
        }
    }
}