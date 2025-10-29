using KuzuDot.Enums;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace KuzuDot.Utils
{
    internal static class KuzuGuard
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull<T>(T value, string paramName)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(value, paramName);
#else
            if (value == null) throw new ArgumentNullException(paramName);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrEmpty(string? value, string paramName)
        {
#if NET8_0_OR_GREATER
            ArgumentException.ThrowIfNullOrEmpty(value, paramName);
#else
            if (string.IsNullOrEmpty(value)) throw new ArgumentException($"Parameter '{paramName}' cannot be null or empty.", paramName);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotDisposed(bool disposed, string objectName)
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, objectName);
#else
            if (disposed) throw new ObjectDisposedException(objectName);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertNotZero(IntPtr ptr, string message)
        {
            if (ptr == IntPtr.Zero) throw new ArgumentNullException(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckSuccess(KuzuState state, string? message = null)
        {
            if (state != KuzuState.Success)
                throw new KuzuException(message ?? "KuzuDB operation failed");
        }

        internal static void StringContainsNull(string? value, string v)
        {
            var hasNull =
                value!.Contains('\0', StringComparison.Ordinal);
            if (hasNull)
                throw new ArgumentException($"String parameter '{v}' contains null character(s)", v);
        }

    }
}