# KuzuDot

KuzuDot is a comprehensive .NET client library for interacting with the [KùzuDB](https://kuzudb.com/) graph database. It provides a modern, type-safe, and performant interface for executing Cypher queries, managing connections, and working with graph data in .NET applications.

## Features

- **Graph Database Operations**: Connect to Kùzu graph database instances
- **Cypher Query Support**: Execute Cypher queries and retrieve results
- **Parameterized Queries**: Support for prepared statements with parameter binding
- **POCO Mapping**: Map query results to Plain Old CLR Objects (POCOs)
- **Rich Data Types**: Support for Lists, Structs, Maps, Arrays, Nodes, Relationships, and more
- **Apache Arrow Integration**: High-performance data exchange with Apache Arrow format
- **Async Support**: Asynchronous query execution with cancellation support
- **Resource Management**: Proper disposal patterns for native resource management
- **Performance Monitoring**: Built-in interceptors for query monitoring and logging
- **Multi-targeting**: Supports .NET Standard 2.0 and .NET 8.0+

## Quick Start

### Prerequisites

- .NET 8.0 or later (recommended)
- .NET Standard 2.0 compatible runtime (.NET Framework 4.7.2+)
- KuzuDB native library (`kuzu_shared.dll` for Windows)

### Installation

1. **Add KuzuDot to your project**:
   ```bash
   dotnet add package KuzuDot
   ```

2. **Ensure native library is available**:
   The `kuzu_shared.dll` should be automatically included in your output directory.

### Basic Usage

```csharp
using KuzuDot;

// Create an in-memory database
using var database = Database.FromMemory();
using var connection = database.Connect();

// Create a node table
connection.NonQuery("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id))");

// Insert data
connection.NonQuery("CREATE (:Person {id: 1, name: 'Alice'})");
connection.NonQuery("CREATE (:Person {id: 2, name: 'Bob'})");

// Query data
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name");
while (result.HasNext())
{
    using var row = result.GetNext();
    var id = row.GetValueAs<long>(0);
    var name = row.GetValueAs<string>(1);
    Console.WriteLine($"ID: {id}, Name: {name}");
}
```

## Documentation

- **[API Reference](docs/API_REFERENCE.md)** - Complete API documentation
- **[Examples](docs/EXAMPLES.md)** - Comprehensive usage examples
- **[Data Types](docs/DATA_TYPES.md)** - Working with KuzuDB data types
- **[Performance Guide](docs/PERFORMANCE.md)** - Performance optimization tips
- **[Migration Guide](docs/MIGRATION.md)** - Upgrading from previous versions

## Key Concepts

### Database and Connections

```csharp
// File-based database
using var database = Database.FromPath("./mygraph.db");

// In-memory database
using var database = Database.FromMemory();

// Create connection
using var connection = database.Connect();
```

### Query Execution

```csharp
// Simple query
using var result = connection.Query("MATCH (n) RETURN n LIMIT 10");

// Prepared statements
using var stmt = connection.Prepare("MATCH (n) WHERE n.id = $id RETURN n");
stmt.Bind("id", 42);
using var result = stmt.Execute();

// Scalar queries
long count = connection.ExecuteScalar<long>("MATCH (n) RETURN COUNT(n)");
```

### Working with Results

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.*");

while (result.HasNext())
{
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
}
```

### POCO Mapping

```csharp
public class Person
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

// Query and map to POCOs
var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age");
foreach (var person in people)
{
    Console.WriteLine($"{person.Name} (ID: {person.Id}, Age: {person.Age})");
}
```

## Advanced Features

### Async Operations

```csharp
// Async query execution
using var result = await connection.QueryAsync("MATCH (n) RETURN n");

// Async prepared statements
using var stmt = await connection.PrepareAsync("MATCH (n) WHERE n.id = $id RETURN n");
stmt.Bind("id", 42);
using var result = await connection.ExecuteAsync(stmt);
```

### Interceptors for Monitoring

```csharp
// Register a timing interceptor
KuzuInterceptorRegistry.Register(new TimingLoggingInterceptor());

// All queries will now be logged with timing information
using var result = connection.Query("MATCH (n) RETURN n");
```

### Complex Data Types

```csharp
// Working with nodes
using var result = connection.Query("MATCH (n:Person) RETURN n");
while (result.HasNext())
{
    using var row = result.GetNext();
    using var nodeValue = row.GetValue<KuzuNode>(0);
    
    Console.WriteLine($"Node Label: {nodeValue.Label}");
    foreach (var (key, value) in nodeValue.Properties)
    {
        Console.WriteLine($"  {key}: {value}");
        value.Dispose();
    }
}
```

## Performance Considerations

- **Use prepared statements** for repeated queries
- **Dispose resources promptly** using `using` statements
- **Consider async operations** for long-running queries
- **Use POCO mapping** for better performance with large result sets
- **Monitor query performance** with interceptors

## Thread Safety

KuzuDot supports concurrent access patterns:
- Multiple connections can be created from a single database
- Connections can be used concurrently across threads
- Individual connections are not thread-safe - use one connection per thread

## Error Handling

```csharp
try
{
    using var result = connection.Query("INVALID QUERY");
}
catch (KuzuException ex)
{
    Console.WriteLine($"KuzuDB Error: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid Operation: {ex.Message}");
}
```

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Performance Benchmarks

The repository includes comprehensive benchmarks using BenchmarkDotNet:

```bash
# Run all benchmarks
dotnet run -c Release --project KuzuDot.Benchmarks

# Run specific benchmark
dotnet run -c Release --project KuzuDot.Benchmarks -- --filter *PreparedReuseSingleBind*
```

Benchmark results are available in the `KuzuDot.Benchmarks/BenchmarkDotNet.Artifacts/results` directory.

## Support

- **Documentation**: [docs/](docs/)
- **Examples**: [src/KuzuDot.Examples/](src/KuzuDot.Examples/)
- **Issues**: [GitHub Issues](https://github.com/your-repo/kuzudot/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/kuzudot/discussions)