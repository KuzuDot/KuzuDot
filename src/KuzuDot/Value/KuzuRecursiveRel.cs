using KuzuDot.Native;
using KuzuDot.Utils;
using System.Diagnostics.CodeAnalysis;

namespace KuzuDot.Value
{
    /// <summary>
    /// Represents a recursive relationship value in KuzuDB, containing lists of nodes and relationships.
    /// </summary>
    public sealed class KuzuRecursiveRel : KuzuValue
    {
        internal KuzuRecursiveRel(NativeKuzuValue n) : base(n)
        {
        }

        /// <summary>
        /// Gets the list of nodes in the recursive relationship.
        /// </summary>
        /// <returns>A <see cref="KuzuList"/> containing the nodes.</returns>
        /// <exception cref="KuzuException">Thrown if the node list is empty.</exception>
        public KuzuList GetNodeList()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_recursive_rel_node_list(ref Handle.NativeStruct, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get recursive rel node list");
            var lv = (KuzuList)FromNativeStruct(h);
            if (lv.Count == 0) throw new KuzuException("Recursive relationship not supported: empty node list");
            return lv;
        }

        /// <summary>
        /// Gets the list of relationships in the recursive relationship.
        /// </summary>
        /// <returns>A <see cref="KuzuList"/> containing the relationships.</returns>
        /// <exception cref="KuzuException">Thrown if the relationship list is empty.</exception>
        public KuzuList GetRelList()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_recursive_rel_rel_list(ref Handle.NativeStruct, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get recursive rel rel list");
            var lv = (KuzuList)FromNativeStruct(h);
            if (lv.Count == 0) throw new KuzuException("Recursive relationship not supported: empty rel list");
            return lv;
        }
    }
}