using KuzuDot.Utils;
using KuzuDot.Value;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace KuzuDot.Native
{
    internal static class NativeUtil
    {
#if NETSTANDARD2_0
        private static string PtrToStringUtf8Internal(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return string.Empty;
            int len = 0;
            // Find null terminator
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            if (len == 0) return string.Empty;
            byte[] buffer = new byte[len];
            Marshal.Copy(ptr, buffer, 0, len);
            return Encoding.UTF8.GetString(buffer);
        }
#endif
        internal static string PtrToStringAndDestroy(IntPtr ptr, Action<IntPtr> destroy)
        {
            try 
            {
#if NET8_0_OR_GREATER
                return Marshal.PtrToStringUTF8(ptr) ?? String.Empty;
#else
                return PtrToStringUtf8Internal(ptr);
#endif
            }
            finally 
            { 
                destroy(ptr); 
            }
        }

        internal static string ToUpperInvariant(string str)
        {
#if NET6_0_OR_GREATER || NET8_0_OR_GREATER
            // avoid intermediate string allocation
            return string.Create(str.Length, str, (span, src) =>
            {
                for (int i = 0; i < span.Length; i++) span[i] = char.ToUpperInvariant(src[i]);
            });
#else
            return str.ToUpperInvariant();
#endif
        }

        internal static NativeKuzuInt128 BigIntegerToNative(BigInteger value)
        {
            var bytes = value.ToByteArray();
            if (bytes.Length > 16) throw new OverflowException("BigInteger does not fit into 128 bits");
            byte[] padded = new byte[16];
            byte fill = value.Sign < 0 ? (byte)0xFF : (byte)0x00;
            for (int i = 0; i < 16; i++) padded[i] = fill;
            Array.Copy(bytes, 0, padded, 0, bytes.Length);
            ulong low = BitConverter.ToUInt64(padded, 0);
            long high = BitConverter.ToInt64(padded, 8);
            return new NativeKuzuInt128(low, high);
        }

        internal static BigInteger NativeToBigInteger(NativeKuzuInt128 native)
        {
            byte[] bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(native.Low), 0, bytes, 0, 8);
            Array.Copy(BitConverter.GetBytes(native.High), 0, bytes, 8, 8);
            return new BigInteger(bytes);
        }
    }
}