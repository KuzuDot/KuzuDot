# KuzuDot API Reference

This document provides comprehensive API documentation for KuzuDot, the .NET client library for KùzuDB.

## Table of Contents

- [Core Classes](#core-classes)
- [Database Management](#database-management)
- [Connection Management](#connection-management)
- [Query Execution](#query-execution)
- [Prepared Statements](#prepared-statements)
- [Result Handling](#result-handling)
- [Data Types](#data-types)
- [Value Types](#value-types)
- [Async Operations](#async-operations)
- [Interceptors](#interceptors)
- [Error Handling](#error-handling)

## Core Classes

### Database

The `Database` class represents a KuzuDB database instance.

#### Static Factory Methods

```csharp
// Create a file-based database
public static Database FromPath(string path)
public static Database FromPath(string path, DatabaseConfig config)

// Create an in-memory database
public static Database FromMemory()
public static Database FromMemory(DatabaseConfig config)
```

#### Instance Methods

```csharp
// Create a new connection
public Connection Connect()

// Dispose resources
public void Dispose()
```

#### Example Usage

```csharp
// File-based database
using var database = Database.FromPath("./mygraph.db");

// In-memory database
using var database = Database.FromMemory();

// Create connection
using var connection = database.Connect();
```

### Connection

The `Connection` class manages database connections and query execution.

#### Properties

```csharp
// Maximum number of threads for query execution
public ulong MaxNumThreadsForExecution { get; set; }
```

#### Query Execution Methods

```csharp
// Execute a query and return results
public QueryResult Query(string query)

// Execute a query that doesn't return results (DDL/DML)
public bool NonQuery(string query)
public bool ExecuteNonQuery(string query) // Alias for NonQuery

// Execute a scalar query returning a single value
public T ExecuteScalar<T>(string query)

// Execute a prepared statement
public QueryResult ExecutePrepared(PreparedStatement statement)
```

#### Prepared Statement Methods

```csharp
// Prepare a statement for execution
public PreparedStatement Prepare(string query)

// Try to prepare a statement without throwing exceptions
public bool TryPrepare(string query, out PreparedStatement? statement, out string? errorMessage)
```

#### POCO Mapping Methods

```csharp
// Query and map results to POCOs
public IReadOnlyList<T> Query<T>(string query) where T : new()

// Query with custom projector function
public IReadOnlyList<T> Query<T>(string query, Func<FlatTuple, T> projector)
```

#### Enumerable Methods

```csharp
// Stream query results
public IEnumerable<FlatTuple> QueryEnumerable(string query)
public IEnumerable<T> QueryEnumerable<T>(string query, Func<FlatTuple, T> projector)
```

#### Configuration Methods

```csharp
// Set query timeout in milliseconds
public void SetQueryTimeout(ulong timeoutMs)

// Interrupt current query execution
public void Interrupt()
```

#### Error Handling Methods

```csharp
// Try to execute a query without throwing exceptions
public bool TryQuery(string query, out QueryResult? result, out string? errorMessage)
```

#### Example Usage

```csharp
using var connection = database.Connect();

// Simple query
using var result = connection.Query("MATCH (n) RETURN n LIMIT 10");

// Prepared statement
using var stmt = connection.Prepare("MATCH (n) WHERE n.id = $id RETURN n");
stmt.Bind("id", 42);
using var result = stmt.Execute();

// Scalar query
long count = connection.ExecuteScalar<long>("MATCH (n) RETURN COUNT(n)");

// POCO mapping
var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age");
```

### PreparedStatement

The `PreparedStatement` class represents a prepared query with parameter binding.

#### Properties

```csharp
// Whether the statement was prepared successfully
public bool IsSuccess { get; }

// Error message if preparation failed
public string ErrorMessage { get; }
```

#### Parameter Binding Methods

```csharp
// Bind primitive types
public PreparedStatement Bind(string paramName, bool value)
public PreparedStatement Bind(string paramName, sbyte value)
public PreparedStatement Bind(string paramName, short value)
public PreparedStatement Bind(string paramName, int value)
public PreparedStatement Bind(string paramName, long value)
public PreparedStatement Bind(string paramName, byte value)
public PreparedStatement Bind(string paramName, ushort value)
public PreparedStatement Bind(string paramName, uint value)
public PreparedStatement Bind(string paramName, ulong value)
public PreparedStatement Bind(string paramName, float value)
public PreparedStatement Bind(string paramName, double value)
public PreparedStatement Bind(string paramName, string? value)
public PreparedStatement Bind(string paramName, DateTime value)
public PreparedStatement Bind(string paramName, TimeSpan value)
public PreparedStatement Bind(string paramName, KuzuValue value)

// Bind with specific types
public PreparedStatement BindBool(string paramName, bool value)
public PreparedStatement BindInt8(string paramName, sbyte value)
public PreparedStatement BindInt16(string paramName, short value)
public PreparedStatement BindInt32(string paramName, int value)
public PreparedStatement BindInt64(string paramName, long value)
public PreparedStatement BindUInt8(string paramName, byte value)
public PreparedStatement BindUInt16(string paramName, ushort value)
public PreparedStatement BindUInt32(string paramName, uint value)
public PreparedStatement BindUInt64(string paramName, ulong value)
public PreparedStatement BindFloat(string paramName, float value)
public PreparedStatement BindDouble(string paramName, double value)
public PreparedStatement BindString(string paramName, string? value)

// Bind date/time types
public PreparedStatement BindDate(string paramName, DateTime value)
public PreparedStatement BindTimestamp(string paramName, DateTime value)
public PreparedStatement BindTimestampMicros(string paramName, long unixMicros)
public PreparedStatement BindTimestampMilliseconds(string paramName, long unixMillis)
public PreparedStatement BindTimestampNanoseconds(string paramName, long unixNanos)
public PreparedStatement BindTimestampSeconds(string paramName, long unixSeconds)
public PreparedStatement BindTimestampWithTimeZone(string paramName, DateTimeOffset dto)
public PreparedStatement BindInterval(string paramName, TimeSpan value)

// Bind KuzuValue
public PreparedStatement BindValue(string paramName, KuzuValue value)

// Bind object properties/fields
public PreparedStatement Bind(object parameters)
```

#### Execution Methods

```csharp
// Execute the prepared statement
public QueryResult Execute()
```

#### Example Usage

```csharp
using var stmt = connection.Prepare("CREATE (:Person {id: $id, name: $name, age: $age})");

// Bind parameters individually
stmt.Bind("id", 1);
stmt.Bind("name", "Alice");
stmt.Bind("age", 30);

// Or bind from an object
var person = new { Id = 1, Name = "Alice", Age = 30 };
stmt.Bind(person);

// Execute
using var result = stmt.Execute();
```

### QueryResult

The `QueryResult` class represents the result of a query execution.

#### Properties

```csharp
// Number of columns in the result
public ulong ColumnCount { get; }

// Number of rows in the result
public ulong RowCount { get; }

// Whether the query executed successfully
public bool IsSuccess { get; }

// Error message if query failed
public string? ErrorMessage { get; }
```

#### Navigation Methods

```csharp
// Check if there are more rows
public bool HasNext()

// Get the next row
public FlatTuple GetNext()

// Reset the iterator to the beginning
public void ResetIterator()

// Check if there are more query results (for multiple statements)
public bool HasNextQueryResult()

// Get the next query result
public QueryResult GetNextQueryResult()
```

#### Column Information Methods

```csharp
// Get column name by index
public string GetColumnName(ulong index)

// Try to get column ordinal by name (case-insensitive)
internal bool TryGetOrdinal(string columnName, out ulong ordinal)
```

#### Arrow Integration Methods

```csharp
// Try to get Arrow schema
public bool TryGetArrowSchema(out ArrowSchema schema)

// Try to get next Arrow chunk
public bool TryGetNextArrowChunk(long chunkSize, out ArrowArray array)
```

#### Utility Methods

```csharp
// Get query execution summary
public QuerySummary GetQuerySummary()

// Convert to string representation
public override string ToString()
```

#### Example Usage

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name, p.age");

Console.WriteLine($"Query returned {result.RowCount} rows with {result.ColumnCount} columns");

while (result.HasNext())
{
    using var row = result.GetNext();
    var id = row.GetValueAs<long>(0);
    var name = row.GetValueAs<string>(1);
    var age = row.GetValueAs<int>(2);
    
    Console.WriteLine($"Person: {name} (ID: {id}, Age: {age})");
}
```

### FlatTuple

The `FlatTuple` class represents a single row from a query result.

#### Properties

```csharp
// Number of values in this tuple
public ulong Size { get; }
```

#### Value Access Methods

```csharp
// Get value by index
public KuzuValue GetValue(ulong index)

// Get value by column name
public KuzuValue GetValue(string columnName)

// Get typed value by index
public TValue GetValue<TValue>(ulong index) where TValue : KuzuValue

// Get typed value by column name
public TValue GetValue<TValue>(string columnName) where TValue : KuzuValue

// Get value as specific CLR type
public T GetValueAs<T>(ulong index)
public T GetValueAs<T>(string columnName)

// Try to get value by column name
public bool TryGetValue(string columnName, out KuzuValue? value)
public bool TryGetValueAs<T>(string columnName, out T value)
```

#### Example Usage

```csharp
using var row = result.GetNext();

// Access by index
var id = row.GetValueAs<long>(0);
var name = row.GetValueAs<string>(1);

// Access by column name
var personId = row.GetValueAs<long>("id");
var personName = row.GetValueAs<string>("name");

// Get typed values
using var idValue = row.GetValue<KuzuInt64>(0);
using var nameValue = row.GetValue<KuzuString>(1);
```

## Data Types

### KuzuValue

The abstract base class for all KuzuDB values.

#### Properties

```csharp
// The KuzuDB data type ID
public KuzuDataTypeId DataTypeId { get; }
```

#### Methods

```csharp
// Check if the value is null
public bool IsNull()

// Set the null state
public void SetNull(bool isNull)

// Clone the value
public KuzuValue Clone()

// Copy from another value
public void CopyFrom(KuzuValue other)

// Convert to string
public override string ToString()
```

### Typed Value Classes

KuzuDot provides strongly-typed value classes for each KuzuDB data type:

#### Scalar Types

```csharp
KuzuBool      // Boolean values
KuzuInt8      // 8-bit signed integer
KuzuInt16     // 16-bit signed integer
KuzuInt32     // 32-bit signed integer
KuzuInt64     // 64-bit signed integer
KuzuUInt8     // 8-bit unsigned integer
KuzuUInt16    // 16-bit unsigned integer
KuzuUInt32    // 32-bit unsigned integer
KuzuUInt64    // 64-bit unsigned integer
KuzuFloat     // 32-bit floating point
KuzuDouble    // 64-bit floating point
KuzuString    // String values
```

#### Date/Time Types

```csharp
KuzuDate           // Date values
KuzuTimestamp      // Timestamp values
KuzuTimestampNs    // Nanosecond timestamps
KuzuTimestampMs    // Millisecond timestamps
KuzuTimestampSec   // Second timestamps
KuzuTimestampTz    // Timezone-aware timestamps
KuzuInterval       // Time interval values
```

#### Complex Types

```csharp
KuzuNode           // Graph nodes
KuzuRel            // Graph relationships
KuzuRecursiveRel   // Recursive relationships
KuzuList           // List values
KuzuStruct         // Struct values
KuzuMap            // Map values
KuzuArray          // Array values
KuzuBlob           // Binary data
KuzuUUID           // UUID values
KuzuInternalId     // Internal ID values
```

#### Example Usage

```csharp
using var value = row.GetValue(0);

// Type checking
if (value is KuzuString stringValue)
{
    Console.WriteLine($"String value: {stringValue.Value}");
}

// Direct casting
if (value is KuzuInt64 intValue)
{
    long id = intValue.Value;
}

// Using GetValueAs for convenience
var name = row.GetValueAs<string>("name");
var age = row.GetValueAs<int>("age");
```

## Async Operations

KuzuDot provides async wrappers for all major operations.

### Async Query Methods

```csharp
// Async query execution
public Task<QueryResult> QueryAsync(string query, CancellationToken cancellationToken = default)

// Async POCO mapping
public Task<IReadOnlyList<T>> QueryAsync<T>(string query, CancellationToken cancellationToken = default) where T : new()

// Async query with projector
public Task<IReadOnlyList<T>> QueryAsync<T>(string query, Func<FlatTuple, T> projector, CancellationToken cancellationToken = default)
```

### Async Prepared Statement Methods

```csharp
// Async statement preparation
public Task<PreparedStatement> PrepareAsync(string query, CancellationToken cancellationToken = default)

// Async statement execution
public Task<QueryResult> ExecuteAsync(PreparedStatement preparedStatement, CancellationToken cancellationToken = default)
```

#### Example Usage

```csharp
// Async query execution
using var result = await connection.QueryAsync("MATCH (n) RETURN n");

// Async prepared statement
using var stmt = await connection.PrepareAsync("MATCH (n) WHERE n.id = $id RETURN n");
stmt.Bind("id", 42);
using var result = await connection.ExecuteAsync(stmt);

// Async POCO mapping
var people = await connection.QueryAsync<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age");
```

## Interceptors

KuzuDot supports interceptors for monitoring and logging query execution.

### IInterceptor Interface

```csharp
public interface IInterceptor
{
    void OnBeforeQuery(Connection connection, string query);
    void OnAfterQuery(Connection connection, string query, QueryResult? result, Exception? exception, TimeSpan elapsed);
    void OnBeforePrepare(Connection connection, string query);
    void OnAfterPrepare(Connection connection, string query, PreparedStatement statement, Exception? exception, TimeSpan elapsed);
    void OnBeforeExecutePrepared(Connection connection, PreparedStatement statement);
    void OnAfterExecutePrepared(Connection connection, PreparedStatement statement, QueryResult? result, Exception? exception, TimeSpan elapsed);
}
```

### Built-in Interceptors

#### TimingLoggingInterceptor

```csharp
// Register timing interceptor
KuzuInterceptorRegistry.Register(new TimingLoggingInterceptor());

// All queries will now be logged with timing information
using var result = connection.Query("MATCH (n) RETURN n");
```

### Custom Interceptors

```csharp
public class CustomInterceptor : IInterceptor
{
    public void OnBeforeQuery(Connection connection, string query)
    {
        Console.WriteLine($"Executing query: {query}");
    }

    public void OnAfterQuery(Connection connection, string query, QueryResult? result, Exception? exception, TimeSpan elapsed)
    {
        if (exception != null)
        {
            Console.WriteLine($"Query failed after {elapsed.TotalMilliseconds}ms: {exception.Message}");
        }
        else
        {
            Console.WriteLine($"Query completed in {elapsed.TotalMilliseconds}ms, returned {result?.RowCount} rows");
        }
    }

    // Implement other methods...
}

// Register custom interceptor
KuzuInterceptorRegistry.Register(new CustomInterceptor());
```

## Error Handling

### KuzuException

The main exception type for KuzuDB-related errors.

```csharp
try
{
    using var result = connection.Query("INVALID QUERY");
}
catch (KuzuException ex)
{
    Console.WriteLine($"KuzuDB Error: {ex.Message}");
}
```

### Common Error Scenarios

#### Connection Errors

```csharp
try
{
    using var database = Database.FromPath("./nonexistent.db");
}
catch (KuzuException ex)
{
    Console.WriteLine($"Failed to open database: {ex.Message}");
}
```

#### Query Errors

```csharp
try
{
    using var result = connection.Query("MATCH (n:NonExistentTable) RETURN n");
}
catch (KuzuException ex)
{
    Console.WriteLine($"Query failed: {ex.Message}");
}
```

#### Resource Disposal Errors

```csharp
try
{
    using var result = connection.Query("MATCH (n) RETURN n");
    // Use result...
}
catch (ObjectDisposedException ex)
{
    Console.WriteLine($"Resource was disposed: {ex.Message}");
}
```

### Safe Error Handling Patterns

```csharp
// Using Try methods
if (connection.TryQuery("MATCH (n) RETURN n", out var result, out var errorMessage))
{
    using (result)
    {
        // Process result
    }
}
else
{
    Console.WriteLine($"Query failed: {errorMessage}");
}

// Using prepared statements safely
if (connection.TryPrepare("MATCH (n) WHERE n.id = $id RETURN n", out var stmt, out var prepareError))
{
    using (stmt)
    {
        stmt.Bind("id", 42);
        using var result = stmt.Execute();
        // Process result
    }
}
else
{
    Console.WriteLine($"Prepare failed: {prepareError}");
}
```

## Performance Considerations

### Resource Management

Always use `using` statements for proper resource disposal:

```csharp
using var database = Database.FromMemory();
using var connection = database.Connect();
using var result = connection.Query("MATCH (n) RETURN n");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var value = row.GetValue(0);
    // Process value
}
```

### Prepared Statements

Use prepared statements for repeated queries:

```csharp
using var stmt = connection.Prepare("MATCH (n) WHERE n.id = $id RETURN n");

foreach (var id in ids)
{
    stmt.Bind("id", id);
    using var result = stmt.Execute();
    // Process result
}
```

### POCO Mapping

Use POCO mapping for better performance with large result sets:

```csharp
// More efficient than manual value extraction
var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age");
```

### Async Operations

Use async operations for long-running queries:

```csharp
using var result = await connection.QueryAsync("MATCH (n) RETURN n");
```

This completes the comprehensive API reference for KuzuDot. For more examples and usage patterns, see the [Examples](EXAMPLES.md) documentation.
