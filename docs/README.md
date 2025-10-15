# KuzuDot Documentation

Welcome to the KuzuDot documentation! KuzuDot is a C# wrapper for the Kùzu graph database engine, providing a native .NET experience for working with graph databases.

## Getting Started

### Prerequisites
- .NET 8.0 or later
- KuzuDB native library (`kuzu_shared.dll`)

### Quick Start
```csharp
using KuzuDot;

// Create an in-memory database
using var database = Database.FromMemory();
using var connection = database.Connect();

// Create a simple table
connection.NonQuery("CREATE NODE TABLE Person(name STRING, age INT32, PRIMARY KEY(name))");

// Insert data
connection.NonQuery("CREATE (:Person {name: 'Alice', age: 30})");
connection.NonQuery("CREATE (:Person {name: 'Bob', age: 25})");

// Query data
using var result = connection.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age");
while (result.HasNext())
{
    using var row = result.GetNext();
    var name = row.GetValueAs<string>(0);
    var age = row.GetValueAs<int>(1);
    Console.WriteLine($"{name} is {age} years old");
}
```

## Documentation

### Core Documentation
- **[API Reference](API_REFERENCE.md)** - Complete API documentation for all classes and methods
- **[Data Types](DATA_TYPES.md)** - KuzuDB data types and their C# equivalents
- **[Examples](EXAMPLES.md)** - Comprehensive examples and use cases

### Additional Guides
- **[Performance Guide](PERFORMANCE.md)** - Optimization tips and best practices

## Key Features

- **Type Safety**: Strongly-typed access to KuzuDB data types
- **Resource Management**: Proper disposal patterns with `IDisposable`
- **Prepared Statements**: Parameterized queries for security and performance
- **Asynchronous Operations**: `Task`-based async methods
- **POCO Mapping**: Map query results to Plain Old CLR Objects
- **Apache Arrow Interop**: High-performance data exchange (experimental)

## Examples

Check out the [examples directory](../examples/) for practical demonstrations:

- **[Basic Examples](../examples/basic/)** - Hello world, CRUD operations, data types
- **[Graph Examples](../examples/graph/)** - Social networks, hierarchies, product catalogs
- **[Advanced Examples](../examples/advanced/)** - Prepared statements, POCO mapping, async operations
- **[Performance Examples](../examples/performance/)** - Batch operations, connection pooling, optimization
- **[Real-World Examples](../examples/real-world/)** - Recommendation systems, fraud detection, network analysis

## Running Examples

```bash
# From the examples directory
dotnet run --project basic/hello-world
dotnet run --project graph/social-network
dotnet run --project advanced/prepared-statements
```

## Resource Management

Always dispose of KuzuDot objects to prevent memory leaks:

```csharp
using var database = Database.FromMemory();
using var connection = database.Connect();
using var result = connection.Query("MATCH (n) RETURN n LIMIT 1");
while (result.HasNext())
{
    using var row = result.GetNext();
    using var value = row.GetValue(0);
    // Process value...
}
```

## Getting Help

- **Issues**: Report bugs and request features on the project repository
- **Documentation**: Browse the comprehensive guides in this directory
- **Examples**: Run the example projects to see KuzuDot in action

## License

See the main project [LICENSE](../LICENSE) file for licensing information.
