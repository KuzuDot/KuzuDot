using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using System;

namespace KuzuDot.Value
{
    public sealed class KuzuInternalId : KuzuTypedValue<InternalId>
    {
        internal KuzuInternalId(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuInternalId(IntPtr ptr) : base(ptr)
        {
        }

        protected override bool TryGetNativeValue(out InternalId v)
        {
            var st = NativeMethods.kuzu_value_get_internal_id(ref Handle.NativeStruct, out var temp);
            if (st == KuzuState.Success)
            {
                v = new InternalId(temp.TableId, temp.Offset);
                return true;
            }
            v = default;
            return false;
        }

        public static InternalId FromKuzuInternalId(KuzuInternalId v)
        {
            KuzuGuard.NotNull(v, nameof(v));
            return v.Value;
        }

        public static implicit operator InternalId(KuzuInternalId value) => FromKuzuInternalId(value);
    }
}