namespace KuzuDot.Value
{
    public interface IHasProperties
    {
        ulong PropertyCount { get; }
        string GetPropertyNameAt(ulong index);
        KuzuValue GetPropertyValueAt(ulong index);
    }
}