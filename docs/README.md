# KuzuDot Usage Guide

KuzuDot is a C# wrapper for the Kùzu graph database engine.

## Installation


## Basic Usage

### 1. Creating a Database and Connection
```csharp
using KuzuDot;

// Create or open a database
using var db = new Database("./mydb");

// Create a connection
using var conn = db.Connect();
```

### 2. Executing Queries
```csharp
using var db = new Database(":memory:");
using var conn = db.Connect();

conn.NonQuery("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id))");
conn.NonQuery("CREATE (:Person {id: 1, name:'Alice'})");
conn.NonQuery("CREATE (:Person {id: 2, name:'Bob'})");
conn.NonQuery("CREATE (:Person {id: 3, name:'Charlie'})");

using var result = conn.Query("MATCH (n) RETURN n LIMIT 10;");

while (result.HasNext())
{
    using var row = result.GetNext(); // Fet the KuzuFlatTuple
    using var node = row.GetValue<KuzuNode>(0) // Read result item 1 as a KuzuNode
    console.WriteLine("Node Label: {0}", node.Label);
    console.WriteLine("Node Properties:");
    foreach(var (key, value) in node.Properties) 
    {
        console.WriteLine("\t{0}: {1}", key, value);
        value.Dispose(); // Values are IDisposable
    }
}
```

### 3. Prepared Statements
```csharp
using var stmt = conn.Prepare("MATCH (n) WHERE n.id = $id RETURN n;");
stmt.Bind("id", 42);
using var result = stmt.Execute();
```

### 4. Working with Values
```csharp
using var value = result.GetValue(0);
if (value.IsInt64())
{
    long id = value.AsInt64();
}
if (value.IsString())
{
    string name = value.AsString();
}
```

## Advanced Features

- **Arrow Interop**: Use `ArrowInterop` for high-performance data exchange with Apache Arrow. (`experimental`)

- **Resource Management**: Dispose connections, results, and values promptly to avoid memory leaks.
- **Type Safety**: Use type-checking methods (`IsInt64`, `IsString`, etc.) before accessing values.
- **Interceptors**: Implement custom interceptors for logging and query monitoring.


## Disposal and Lifetime

- Always dispose of `Connection`, `QueryResult`, and `KuzuValue` objects when done.
- Use `using` statements or call `.Dispose()` explicitly.

## Example
```csharp
using (var db = new Database("./mydb"))
using (var conn = new Connection(db))
{
    using var result = conn.Query("MATCH (n) RETURN n LIMIT 1;");
    if (result.HasNext())
    {
        using var row = result.GetNext();
        // Note that calls that return managed data types don't need disposal
        Console.WriteLine(row.GetValue<string>(0));
    }
}
```

### Test Project

A typical test project (`KuzuDot.Tests.csproj`).

### Example Project

The example project (`KuzuDot.Examples.csproj`).


## Example: Comprehensive Usage

See `KuzuDot.Examples/ComprehensiveExample.cs` for a full-featured demonstration, including schema creation, data insertion, and aggregate calculations:

```csharp
using var database = new Database(":memory:");
using var connection = database.Connect();
connection.NonQuery("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id))");
using var stmt = connection.Prepare("CREATE (:Person {id: $id, name: $name})");
stmt.Bind("id", 1);
stmt.Bind("name", "Alice");
stmt.Execute();
using var result = connection.Query("MATCH (p:Person) RETURN p.*");
while (result.HasNext())
{
    using var tuple = result.GetNext();
    Console.WriteLine($"ID={tuple.GetValue(0).AsInt64()} Name={tuple.GetValue(1).AsString()}");
}
```

For more advanced scenarios, see `AdvancedExamples.cs` and `KuzuDot.Tests` for test-driven usage patterns.

---
For further help, open an issue on GitHub or consult the official Kùzu documentation.
