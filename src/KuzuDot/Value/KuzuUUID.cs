using KuzuDot.Native;
using KuzuDot.Utils;
using System.Diagnostics.CodeAnalysis;

namespace KuzuDot.Value
{
    public sealed class KuzuUUID : KuzuTypedValue<UUID>
    {
        internal KuzuUUID(NativeKuzuValue n) : base(n) { }
        protected override bool TryGetNativeValue(out UUID value)
        {
            var st = NativeMethods.kuzu_value_get_uuid(Handle, out var ptr);
            if (st == Enums.KuzuState.Success) {
                value = new UUID(NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string));
                return true;
            }
            value = default;
            return false;
        }
    }
}