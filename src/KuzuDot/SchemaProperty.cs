namespace KuzuDot
{
    /// <summary>
    /// Represents a property of an entity in the Kùzu database schema, including its identifier, name, type, default
    /// expression, and primary key status.
    /// </summary>
    /// <remarks>Use this class to describe the metadata of a property within a node or relationship type in
    /// the Kùzu database. Each instance encapsulates information about a single property, which can be used for schema
    /// inspection or dynamic data handling.</remarks>
    public class SchemaProperty
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string DefaultExpression { get; set; } = default!;
        public bool IsPrimaryKey { get; set; }
    }
}