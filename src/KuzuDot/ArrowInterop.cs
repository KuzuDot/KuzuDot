using System;
using System.Runtime.InteropServices;

namespace KuzuDot
{
    /// <summary>
    /// Minimal Apache Arrow C Data Interface structs for interop exposure.
    /// Public (explicit exception) for consumers needing Arrow C Data Interface.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// Represents the ArrowSchema struct from the Apache Arrow C Data Interface.
    /// </summary>
    public struct ArrowSchema : IEquatable<ArrowSchema>
    {
        /// <summary>Pointer to format string (const char*)</summary>
        public IntPtr format;
        /// <summary>Pointer to name string (const char*)</summary>
        public IntPtr name;
        /// <summary>Pointer to metadata string (const char*)</summary>
        public IntPtr metadata;
        /// <summary>Flags for the schema</summary>
        public long flags;
        /// <summary>Number of child schemas</summary>
        public long n_children;
        /// <summary>Pointer to array of child schemas (ArrowSchema**)</summary>
        public IntPtr children;
        /// <summary>Pointer to dictionary schema (ArrowSchema*)</summary>
        public IntPtr dictionary;
        /// <summary>Pointer to release callback (void (*release)(ArrowSchema*))</summary>
        public IntPtr release;
        /// <summary>Pointer to private data (void*)</summary>
        public IntPtr private_data;

        /// <summary>
        /// Determines whether the specified <see cref="ArrowSchema"/> is equal to the current <see cref="ArrowSchema"/>.
        /// </summary>
        /// <param name="other">The <see cref="ArrowSchema"/> to compare with the current <see cref="ArrowSchema"/>.</param>
        /// <returns>true if the specified <see cref="ArrowSchema"/> is equal to the current <see cref="ArrowSchema"/>; otherwise, false.</returns>
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
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is ArrowSchema other && Equals(other);
        /// <inheritdoc/>
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
        /// <summary>
        /// Determines whether two specified <see cref="ArrowSchema"/> instances are equal.
        /// </summary>
        public static bool operator ==(ArrowSchema left, ArrowSchema right) => left.Equals(right);
        /// <summary>
        /// Determines whether two specified <see cref="ArrowSchema"/> instances are not equal.
        /// </summary>
        public static bool operator !=(ArrowSchema left, ArrowSchema right) => !left.Equals(right);
    }

    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// Represents the ArrowArray struct from the Apache Arrow C Data Interface.
    /// </summary>
    public struct ArrowArray : IEquatable<ArrowArray>
    {
        /// <summary>Length of the array</summary>
        public long length;
        /// <summary>Number of null values</summary>
        public long null_count;
        /// <summary>Offset into the array</summary>
        public long offset;
        /// <summary>Number of buffers</summary>
        public long n_buffers;
        /// <summary>Number of child arrays</summary>
        public long n_children;
        /// <summary>Pointer to array of buffers (const void**)</summary>
        public IntPtr buffers;
        /// <summary>Pointer to array of child arrays (ArrowArray**)</summary>
        public IntPtr children;
        /// <summary>Pointer to dictionary array (ArrowArray*)</summary>
        public IntPtr dictionary;
        /// <summary>Pointer to release callback (void (*release)(ArrowArray*))</summary>
        public IntPtr release;
        /// <summary>Pointer to private data (void*)</summary>
        public IntPtr private_data;
        /// <summary>
        /// Determines whether the specified <see cref="ArrowArray"/> is equal to the current <see cref="ArrowArray"/>.
        /// </summary>
        /// <param name="other">The <see cref="ArrowArray"/> to compare with the current <see cref="ArrowArray"/>.</param>
        /// <returns>true if the specified <see cref="ArrowArray"/> is equal to the current <see cref="ArrowArray"/>; otherwise, false.</returns>
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
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is ArrowArray other && Equals(other);
        /// <inheritdoc/>
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
        /// <summary>
        /// Determines whether two specified <see cref="ArrowArray"/> instances are equal.
        /// </summary>
        public static bool operator ==(ArrowArray left, ArrowArray right) => left.Equals(right);
        /// <summary>
        /// Determines whether two specified <see cref="ArrowArray"/> instances are not equal.
        /// </summary>
        public static bool operator !=(ArrowArray left, ArrowArray right) => !left.Equals(right);
    }
}
