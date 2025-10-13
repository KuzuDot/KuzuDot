using System;
using System.Runtime.InteropServices;

namespace KuzuDot
{
    // Minimal Apache Arrow C Data Interface structs for interop exposure.
    // Public (explicit exception) for consumers needing Arrow C Data Interface.
    [StructLayout(LayoutKind.Sequential)]
    public struct ArrowSchema : IEquatable<ArrowSchema>
    {
        public IntPtr format;      // const char*
        public IntPtr name;        // const char*
        public IntPtr metadata;    // const char*
        public long flags;
        public long n_children;
        public IntPtr children;    // ArrowSchema**
        public IntPtr dictionary;  // ArrowSchema*
        public IntPtr release;     // void (*release)(ArrowSchema*)
        public IntPtr private_data; // void*

        public readonly bool Equals(ArrowSchema other) =>
            format == other.format &&
            name == other.name &&
            metadata == other.metadata &&
            flags == other.flags &&
            n_children == other.n_children &&
            children == other.children &&
            dictionary == other.dictionary &&
            release == other.release &&
            private_data == other.private_data;
        public override readonly bool Equals(object? obj) => obj is ArrowSchema other && Equals(other);
        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + format.GetHashCode();
                hash = hash * 31 + name.GetHashCode();
                hash = hash * 31 + metadata.GetHashCode();
                hash = hash * 31 + flags.GetHashCode();
                hash = hash * 31 + n_children.GetHashCode();
                hash = hash * 31 + children.GetHashCode();
                hash = hash * 31 + dictionary.GetHashCode();
                hash = hash * 31 + release.GetHashCode();
                hash = hash * 31 + private_data.GetHashCode();
                return hash;
            }
        }
        public static bool operator ==(ArrowSchema left, ArrowSchema right) => left.Equals(right);
        public static bool operator !=(ArrowSchema left, ArrowSchema right) => !left.Equals(right);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ArrowArray : IEquatable<ArrowArray>
    {
        public long length;
        public long null_count;
        public long offset;
        public long n_buffers;
        public long n_children;
        public IntPtr buffers;     // const void**
        public IntPtr children;    // ArrowArray**
        public IntPtr dictionary;  // ArrowArray*
        public IntPtr release;     // void (*release)(ArrowArray*)
        public IntPtr private_data; // void*
        public readonly bool Equals(ArrowArray other) =>
            length == other.length &&
            null_count == other.null_count &&
            offset == other.offset &&
            n_buffers == other.n_buffers &&
            n_children == other.n_children &&
            buffers == other.buffers &&
            children == other.children &&
            dictionary == other.dictionary &&
            release == other.release &&
            private_data == other.private_data;
        public override readonly bool Equals(object? obj) => obj is ArrowArray other && Equals(other);
        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + length.GetHashCode();
                hash = hash * 31 + null_count.GetHashCode();
                hash = hash * 31 + offset.GetHashCode();
                hash = hash * 31 + n_buffers.GetHashCode();
                hash = hash * 31 + n_children.GetHashCode();
                hash = hash * 31 + buffers.GetHashCode();
                hash = hash * 31 + children.GetHashCode();
                hash = hash * 31 + dictionary.GetHashCode();
                hash = hash * 31 + release.GetHashCode();
                hash = hash * 31 + private_data.GetHashCode();
                return hash;
            }
        }
        public static bool operator ==(ArrowArray left, ArrowArray right) => left.Equals(right);
        public static bool operator !=(ArrowArray left, ArrowArray right) => !left.Equals(right);
    }
}
