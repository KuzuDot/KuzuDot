namespace KuzuDot
{
    /// <summary>
    /// Represents a mapping between source and target database tables, including their primary key columns.
    /// </summary>
    /// <remarks>This class represents the results of show_connection('table')</remarks>
    public class SchemaConnection
    {
        public string SourceTable { get; set; } = default!;
        public string TargetTable { get; set; } = default!;
        public string SourcePrimaryKey { get; set; } = default!;
        public string TargetPrimaryKey { get; set; } = default!;
    }
}