using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Utils
{
    /// <summary>
    /// Base SafeHandle with unified diagnostics accounting. Derived classes implement Release().
    /// </summary>
    internal abstract class KuzuSafeHandle(string kind) : SafeHandle(IntPtr.Zero, ownsHandle: true)
    {
        private readonly string _kind = kind;

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected void Initialize(IntPtr ptr)
        {
            KuzuGuard.AssertNotZero(ptr, $"Native pointer '{nameof(ptr)}' is null");
            SetHandle(ptr);
            KuzuDiagnostics.HandleCreated(_kind);
        }

        internal IntPtr RawHandle => handle;

        protected abstract void Release();

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                Release();
            }
            KuzuDiagnostics.HandleDestroyed(_kind);
            handle = IntPtr.Zero;
            return true; // never retry
        }
    }
}
