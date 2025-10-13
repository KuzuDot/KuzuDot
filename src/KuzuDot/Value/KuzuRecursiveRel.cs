using KuzuDot.Native;
using KuzuDot.Utils;
using System.Diagnostics.CodeAnalysis;

namespace KuzuDot.Value
{
    public sealed class KuzuRecursiveRel : KuzuValue
    {
        internal KuzuRecursiveRel(NativeKuzuValue n) : base(n)
        {
        }

        public KuzuList GetNodeList()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_recursive_rel_node_list(Handle, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get recursive rel node list");
            var lv = (KuzuList)FromNative(h);
            if (lv.Count == 0) throw new KuzuException("Recursive relationship not supported: empty node list");
            return lv;
        }

        public KuzuList GetRelList()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_recursive_rel_rel_list(Handle, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get recursive rel rel list");
            var lv = (KuzuList)FromNative(h);
            if (lv.Count == 0) throw new KuzuException("Recursive relationship not supported: empty rel list");
            return lv;
        }
    }
}