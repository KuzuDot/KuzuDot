using System;

namespace KuzuDot.Value
{
    public readonly record struct UUID(string Value)
    {
        public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
    }
}