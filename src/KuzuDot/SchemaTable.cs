namespace KuzuDot
{
    /// <summary>
    /// Represents metadata information for a database table schema
    /// </summary>
    public class SchemaTable
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Comment { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
    }
}