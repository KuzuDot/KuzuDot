using KuzuDot.Native;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot
{
    /// <summary>
    /// Provides version information for the Kuzu library
    /// </summary>
    public static class Version
    {
        /// <summary>
        /// Gets the version string of the Kuzu library
        /// </summary>
        /// <returns>The version string</returns>
        public static string GetVersion()
        {
            var versionPtr = NativeMethods.kuzu_get_version();
            return NativeUtil.PtrToStringAndDestroy(versionPtr, NativeMethods.kuzu_destroy_string);
        }

        /// <summary>
        /// Gets the storage version of the Kuzu library
        /// </summary>
        /// <returns>The storage version number</returns>
        public static ulong GetStorageVersion()
        {
            return NativeMethods.kuzu_get_storage_version();
        }
    }
}