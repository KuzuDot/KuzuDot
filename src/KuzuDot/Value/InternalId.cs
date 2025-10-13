using KuzuDot.Native;
using System;

namespace KuzuDot.Value
{
    /// <summary>
    /// Managed wrapper representing an internal identifier (table id + offset) in Kuzu.
    /// This hides the underlying mutable native struct and provides value semantics.
    /// </summary>
    public readonly struct InternalId : IEquatable<InternalId>
    {
        public InternalId(ulong tableId, ulong offset)
        {
            TableId = tableId;
            Offset = offset;
        }

        internal InternalId(NativeKuzuInternalId native)
        {
            TableId = native.TableId;
            Offset = native.Offset;
        }

        /// <summary>Offset/row identifier within table.</summary>
        public ulong Offset { get; }

        /// <summary>Table identifier.</summary>
        public ulong TableId { get; }

        public static bool operator !=(InternalId left, InternalId right) => !left.Equals(right);

        public static bool operator ==(InternalId left, InternalId right) => left.Equals(right);

        public bool Equals(InternalId other) => TableId == other.TableId && Offset == other.Offset;

        public override bool Equals(object? obj) => obj is InternalId o && Equals(o);

        public override int GetHashCode()
        {
#if NET8_0_OR_GREATER
            return HashCode.Combine(TableId, Offset);
#else
            unchecked
            {
                // simple combination suitable for dictionary usage
                ulong combined = TableId * 397UL ^ Offset;
                return (int)(combined >> 32) ^ (int)combined;
            }
#endif
        }

        public override string ToString() => $"InternalId(Table={TableId}, Offset={Offset})";

        internal NativeKuzuInternalId ToNative() => new(TableId, Offset);
    }
}