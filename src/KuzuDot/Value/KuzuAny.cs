using KuzuDot.Native;

namespace KuzuDot.Value
{
    public sealed class KuzuAny : KuzuValue
    {
        internal KuzuAny(NativeKuzuValue n) : base(n)
        {
        }
    }
}