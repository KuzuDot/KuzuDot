using KuzuDot.Utils;
using System;

namespace KuzuDot
{
    /// <summary>
    /// Specifies an explicit parameter or column name for POCO binding / materialization.
    /// Applied to public properties or fields. Overrides the member name when matching
    /// to prepared statement parameters or result set columns. Matching remains
    /// case-insensitive.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class KuzuNameAttribute : Attribute
    {
        public string Name { get; }
        public KuzuNameAttribute(string name)
        {
            KuzuGuard.NotNull(name, nameof(name));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("KuzuName cannot be empty", nameof(name));
            Name = name.Trim();
        }
        public override string ToString() => Name;
    }
}
