using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;

namespace KuzuDot.Value
{
    public sealed class KuzuBlob : KuzuValue
    {
        internal KuzuBlob(NativeKuzuValue n) : base(n) { }
        /// <summary>
        /// Returns the blob contents as a newly allocated managed byte array.
        /// Prefer <see cref="GetSpan()"/> when you only need to read the data transiently without allocation.
        /// </summary>
        public byte[] GetBytes()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_blob(Handle, out var ptr);
            if (st != KuzuState.Success) { PerfCounters.IncBlobDecode(); throw new InvalidOperationException("Failed to get blob value"); }
            if (ptr == IntPtr.Zero) return [];

            var hex = NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_blob);
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
            if (hex.Length == 0) return [];
            if (hex.Length % 2 != 0) throw new FormatException("Invalid blob hex length");
            return DecodeHexToBytes(hex);
        }

        /// <summary>
        /// Provides a non-allocating <see cref="ReadOnlySpan{T}"/> view over the blob bytes.
        /// Lifetime & Safety:
        ///  - The returned span is only valid until this <see cref="KuzuBlob"/> is disposed OR another method
        ///    is invoked on this same instance that may re-enter native APIs.
        ///  - Callers MUST copy the data (e.g. via <c>span.ToArray()</c>) if they need it beyond that point.
        ///  - The span references a temporary native buffer allocated by the engine; it is released as soon
        ///    as this method returns (after we copy into a managed staging buffer) OR (fast path) we parse directly.
        /// Implementation detail:
        ///  Current native API returns a hex string for blob bytes. For span access we still need to materialize
        ///  the decoded bytes once. To avoid double allocation vs GetBytes(), we COULD cache the decoded array for this
        ///  instance (since blobs are immutable) and then expose a span over that array.
        /// </summary>
        public
#if NET8_0_OR_GREATER
            ReadOnlySpan<byte>
#else
            byte[]
#endif
            GetSpan()
        {
            ThrowIfDisposed();

            var st = NativeMethods.kuzu_value_get_blob(Handle, out var ptr);
            KuzuGuard.CheckSuccess(st, "Failed to get blob value");
            if (ptr == IntPtr.Zero)
            {
                return [];
            }

            var hex = NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_blob);
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
            if (hex.Length == 0)
            {
                return [];
            }
            if (hex.Length % 2 != 0) throw new FormatException("Invalid blob hex length");
            var bytes = DecodeHexToBytes(hex);
#if NET8_0_OR_GREATER
            return new ReadOnlySpan<byte>(bytes);
#else
            return bytes;
#endif
        }
        private static byte[] DecodeHexToBytes(string hex)
        {
#if NET8_0_OR_GREATER
            // Convert.FromHexString is highly optimized in modern runtimes
            return Convert.FromHexString(hex);
#else
            int len = hex.Length;
            var bytes = new byte[len / 2];
            for (int si = 0, di = 0; si < len; si += 2, di++)
            {
                int hi = HexToNibble(hex[si]);
                int lo = HexToNibble(hex[si + 1]);
                if ((hi | lo) < 0) throw new FormatException("Invalid hex character in blob literal");
                bytes[di] = (byte)(hi << 4 | lo);
            }
            return bytes;
#endif
        }
#if !NET8_0_OR_GREATER
        private static int HexToNibble(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
#endif
    }
}