# KuzuDot Performance Guide

This document provides comprehensive guidance on optimizing performance when using KuzuDot with KùzuDB.

## Table of Contents

- [Performance Overview](#performance-overview)
- [Connection Management](#connection-management)
- [Query Optimization](#query-optimization)
- [Prepared Statements](#prepared-statements)
- [Result Processing](#result-processing)
- [Memory Management](#memory-management)
- [Async Operations](#async-operations)
- [Monitoring and Profiling](#monitoring-and-profiling)
- [Benchmarking](#benchmarking)
- [Best Practices](#best-practices)

## Performance Overview

KuzuDot is designed for high-performance graph database operations. Key performance characteristics:

- **Native Performance**: Direct P/Invoke calls to KuzuDB C++ library
- **Memory Efficiency**: Proper resource management with IDisposable patterns
- **Concurrent Access**: Multiple connections per database instance
- **Type Safety**: Compile-time type checking with runtime optimization

### Performance Metrics

Typical performance characteristics (varies by hardware and data):

- **Simple Queries**: 10,000-100,000 rows/second
- **Complex Graph Traversals**: 1,000-10,000 paths/second
- **Prepared Statement Reuse**: 50,000-500,000 executions/second
- **POCO Mapping**: 5,000-50,000 objects/second

## Connection Management

### Connection Pooling

While KuzuDot doesn't provide built-in connection pooling, you can implement your own:

```csharp
public class DatabaseConnectionPool
{
    private readonly Database _database;
    private readonly ConcurrentQueue<Connection> _connections;
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConnections;

    public DatabaseConnectionPool(string databasePath, int maxConnections = 10)
    {
        _database = Database.FromPath(databasePath);
        _connections = new ConcurrentQueue<Connection>();
        _semaphore = new SemaphoreSlim(maxConnections, maxConnections);
        _maxConnections = maxConnections;
    }

    public async Task<T> ExecuteAsync<T>(Func<Connection, T> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            var connection = GetOrCreateConnection();
            return operation(connection);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private Connection GetOrCreateConnection()
    {
        if (_connections.TryDequeue(out var connection) && !connection.IsDisposed)
        {
            return connection;
        }
        return _database.Connect();
    }

    public void Dispose()
    {
        while (_connections.TryDequeue(out var connection))
        {
            connection.Dispose();
        }
        _database.Dispose();
    }
}
```

### Connection Configuration

Optimize connection settings for your workload:

```csharp
using var connection = database.Connect();

// Set maximum threads for query execution
connection.MaxNumThreadsForExecution = Environment.ProcessorCount;

// Set query timeout
connection.SetQueryTimeout(30000); // 30 seconds
```

## Query Optimization

### Efficient Query Patterns


#### Use LIMIT for Large Result Sets

```csharp
// ❌ Inefficient - loads all data into memory
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name");

// ✅ Efficient - limits result set size
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name LIMIT 1000");
```

#### Use Indexes and Constraints

```csharp
// Create indexes for frequently queried properties
connection.NonQuery("CREATE NODE TABLE Person(id INT64, name STRING, email STRING, PRIMARY KEY(id))");

// Use primary key lookups
using var stmt = connection.Prepare("MATCH (p:Person) WHERE p.id = $id RETURN p.name");
stmt.Bind("id", 123);
```

### Query Planning

#### Analyze Query Performance

```csharp
// Use EXPLAIN to understand query execution
using var explainResult = connection.Query("EXPLAIN MATCH (p:Person)-[:KNOWS]->(f:Person) RETURN p.name, f.name");
while (explainResult.HasNext())
{
    using var row = explainResult.GetNext();
    var plan = row.GetValueAs<string>(0);
    Console.WriteLine($"Query Plan: {plan}");
}
```

#### Optimize Graph Traversals

```csharp
// ❌ Inefficient - unbounded traversal
using var result = connection.Query("MATCH (p:Person)-[:KNOWS*]->(f:Person) RETURN p.name, f.name");

// ✅ Efficient - bounded traversal with depth limit
using var result = connection.Query("MATCH (p:Person)-[:KNOWS*1..3]->(f:Person) RETURN p.name, f.name");
```

## Prepared Statements

### Reuse Prepared Statements

Prepared statements provide significant performance benefits:

```csharp
// ✅ Efficient - prepare once, execute many times
using var stmt = connection.Prepare("MATCH (p:Person) WHERE p.age >= $minAge AND p.age <= $maxAge RETURN p.name");

var ageRanges = new[] { (18, 25), (26, 35), (36, 50), (51, 65) };

foreach (var (minAge, maxAge) in ageRanges)
{
    stmt.Bind("minAge", minAge);
    stmt.Bind("maxAge", maxAge);
    
    using var result = stmt.Execute();
    // Process results
}
```

### Batch Operations

```csharp
// ✅ Efficient batch insert
using var insertStmt = connection.Prepare("CREATE (:Person {id: $id, name: $name, age: $age})");

var persons = GetPersonsFromDataSource(); // 10,000 persons

foreach (var person in persons)
{
    insertStmt.Bind("id", person.Id);
    insertStmt.Bind("name", person.Name);
    insertStmt.Bind("age", person.Age);
    
    insertStmt.Execute();
}
```

### Object Binding Performance

```csharp
// ✅ Efficient - bind entire object at once
public class PersonData
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

using var stmt = connection.Prepare("CREATE (:Person {id: $id, name: $name, age: $age})");

var person = new PersonData { Id = 1, Name = "Alice", Age = 30 };
stmt.Bind(person); // Binds all properties at once
stmt.Execute();
```

## Result Processing

### Efficient Result Iteration

#### Use Streaming for Large Results

```csharp
// ✅ Efficient - streams results without loading all into memory
foreach (var row in connection.QueryEnumerable("MATCH (p:Person) RETURN p.id, p.name"))
{
    using (row)
    {
        var id = row.GetValueAs<long>(0);
        var name = row.GetValueAs<string>(1);
        // Process row
    }
}
```

#### Use POCO Mapping for Structured Data

```csharp
// ✅ Efficient - automatic mapping with caching
public class Person
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age");
// KuzuDot caches reflection metadata for better performance
```

### Value Access Optimization

#### Use GetValueAs for Simple Types

```csharp
// ✅ Efficient - direct type conversion
var id = row.GetValueAs<long>(0);
var name = row.GetValueAs<string>(1);
var age = row.GetValueAs<int>(2);
```

#### Avoid Unnecessary Type Checking

```csharp
// ❌ Inefficient - unnecessary type checking
using var value = row.GetValue(0);
if (value is KuzuInt64 intValue)
{
    long id = intValue.Value;
}

// ✅ Efficient - direct access
long id = row.GetValueAs<long>(0);
```

## Memory Management

### Resource Disposal Patterns

#### Proper Using Statements

```csharp
// ✅ Correct - proper resource disposal
using var database = Database.FromMemory();
using var connection = database.Connect();
using var result = connection.Query("MATCH (p:Person) RETURN p.name");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var nameValue = row.GetValue<KuzuString>(0);
    
    string name = nameValue.Value;
    // Resources automatically disposed
}
```

#### Batch Disposal for Large Operations

```csharp
// ✅ Efficient - batch disposal
var values = new List<KuzuValue>();

try
{
    using var result = connection.Query("MATCH (p:Person) RETURN p.name");
    while (result.HasNext())
    {
        using var row = result.GetNext();
        var value = row.GetValue<KuzuString>(0);
        values.Add(value);
    }
    
    // Process all values
    foreach (var value in values)
    {
        if (value is KuzuString stringValue)
        {
            Console.WriteLine(stringValue.Value);
        }
    }
}
finally
{
    // Dispose all values
    foreach (var value in values)
    {
        value.Dispose();
    }
}
```

### Memory-Efficient Processing

#### Process Results in Chunks

```csharp
// ✅ Memory-efficient - process in chunks
const int chunkSize = 1000;
var offset = 0;

while (true)
{
    using var result = connection.Query($"MATCH (p:Person) RETURN p.id, p.name SKIP {offset} LIMIT {chunkSize}");
    
    if (!result.HasNext()) break;
    
    var chunk = new List<(long Id, string Name)>();
    while (result.HasNext())
    {
        using var row = result.GetNext();
        var id = row.GetValueAs<long>(0);
        var name = row.GetValueAs<string>(1);
        chunk.Add((id, name));
    }
    
    // Process chunk
    ProcessChunk(chunk);
    
    offset += chunkSize;
}
```

## Async Operations

### Async Query Execution

```csharp
// ✅ Efficient - async execution for long-running queries
public async Task<List<Person>> GetPersonsAsync(int minAge, int maxAge)
{
    using var stmt = await connection.PrepareAsync("MATCH (p:Person) WHERE p.age >= $minAge AND p.age <= $maxAge RETURN p.id, p.name, p.age");
    stmt.Bind("minAge", minAge);
    stmt.Bind("maxAge", maxAge);
    
    using var result = await connection.ExecuteAsync(stmt);
    var persons = new List<Person>();
    
    while (result.HasNext())
    {
        using var row = result.GetNext();
        persons.Add(new Person
        {
            Id = row.GetValueAs<long>(0),
            Name = row.GetValueAs<string>(1),
            Age = row.GetValueAs<int>(2)
        });
    }
    
    return persons;
}
```

### Parallel Processing

```csharp
// ✅ Efficient - parallel processing of independent queries
public async Task<Dictionary<string, int>> GetStatisticsAsync()
{
    var tasks = new[]
    {
        GetPersonCountAsync(),
        GetCompanyCountAsync(),
        GetRelationshipCountAsync()
    };
    
    var results = await Task.WhenAll(tasks);
    
    return new Dictionary<string, int>
    {
        ["Persons"] = results[0],
        ["Companies"] = results[1],
        ["Relationships"] = results[2]
    };
}

private async Task<int> GetPersonCountAsync()
{
    using var result = await connection.QueryAsync("MATCH (p:Person) RETURN COUNT(p)");
    return result.HasNext() ? (int)result.GetNext().GetValueAs<long>(0) : 0;
}
```

## Monitoring and Profiling

### Built-in Interceptors

#### Timing Interceptor

```csharp
// Register timing interceptor
KuzuInterceptorRegistry.Register(new TimingLoggingInterceptor());

// All queries will be logged with timing information
using var result = connection.Query("MATCH (p:Person) RETURN p.name");
// Output: Query executed in 15.234ms, returned 1000 rows
```

#### Custom Performance Interceptor

```csharp
public class PerformanceInterceptor : IInterceptor
{
    private readonly ConcurrentDictionary<string, QueryStats> _stats = new();

    public void OnAfterQuery(Connection connection, string query, QueryResult? result, Exception? exception, TimeSpan elapsed)
    {
        var key = GetQueryKey(query);
        var stats = _stats.GetOrAdd(key, _ => new QueryStats());
        
        lock (stats)
        {
            stats.ExecutionCount++;
            stats.TotalTime += elapsed;
            stats.AverageTime = stats.TotalTime.TotalMilliseconds / stats.ExecutionCount;
            
            if (result != null)
            {
                stats.TotalRows += result.RowCount;
            }
        }
    }

    public void LogStatistics()
    {
        foreach (var (query, stats) in _stats)
        {
            Console.WriteLine($"Query: {query}");
            Console.WriteLine($"  Executions: {stats.ExecutionCount}");
            Console.WriteLine($"  Average Time: {stats.AverageTime:F2}ms");
            Console.WriteLine($"  Total Rows: {stats.TotalRows}");
        }
    }

    private string GetQueryKey(string query)
    {
        // Normalize query for grouping (remove specific values, etc.)
        return query.Trim().ToUpperInvariant();
    }

    private class QueryStats
    {
        public int ExecutionCount { get; set; }
        public TimeSpan TotalTime { get; set; }
        public double AverageTime { get; set; }
        public ulong TotalRows { get; set; }
    }
}
```

### Performance Counters

KuzuDot includes built-in performance counters:

```csharp
// Access performance counters
var tupleCount = PerfCounters.TupleCount;
var valueWrapperCount = PerfCounters.ValueWrapperCount;

Console.WriteLine($"Tuples processed: {tupleCount}");
Console.WriteLine($"Value wrappers created: {valueWrapperCount}");
```

## Benchmarking

### Using BenchmarkDotNet

KuzuDot includes comprehensive benchmarks. Run them to measure performance:

```bash
# Run all benchmarks
dotnet run -c Release --project KuzuDot.Benchmarks

# Run specific benchmark
dotnet run -c Release --project KuzuDot.Benchmarks -- --filter *PreparedReuseSingleBind*

# Run with specific parameters
dotnet run -c Release --project KuzuDot.Benchmarks -- --filter *QueryBenchmarks* --param IterationCount=10000
```

### Custom Benchmarks

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CustomBenchmark
{
    private Database _database = null!;
    private Connection _connection = null!;

    [GlobalSetup]
    public void Setup()
    {
        _database = Database.FromMemory();
        _connection = _database.Connect();
        
        // Setup test data
        _connection.NonQuery("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id))");
        for (int i = 0; i < 10000; i++)
        {
            _connection.NonQuery($"CREATE (:Person {{id: {i}, name: 'Person{i}'}})");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
        _database.Dispose();
    }

    [Benchmark]
    public void QueryAllPersons()
    {
        using var result = _connection.Query("MATCH (p:Person) RETURN p.id, p.name");
        while (result.HasNext())
        {
            using var row = result.GetNext();
            var id = row.GetValueAs<long>(0);
            var name = row.GetValueAs<string>(1);
        }
    }

    [Benchmark]
    public void PreparedStatementQuery()
    {
        using var stmt = _connection.Prepare("MATCH (p:Person) WHERE p.id = $id RETURN p.name");
        stmt.Bind("id", 5000);
        using var result = stmt.Execute();
        while (result.HasNext())
        {
            using var row = result.GetNext();
            var name = row.GetValueAs<string>(0);
        }
    }
}
```

## Best Practices

### General Performance Guidelines

1. **Use Prepared Statements**: Always use prepared statements for repeated queries
2. **Dispose Resources**: Always use `using` statements or explicitly dispose resources
3. **Limit Result Sets**: Use `LIMIT` and `SKIP` for large result sets
4. **Use Indexes**: Create appropriate indexes and constraints
5. **Monitor Performance**: Use interceptors to monitor query performance
6. **Batch Operations**: Group related operations together
7. **Async for Long Operations**: Use async methods for long-running queries

### Memory Management

1. **Prompt Disposal**: Dispose `KuzuValue` instances as soon as possible
2. **Batch Processing**: Process large result sets in chunks
3. **Avoid Memory Leaks**: Ensure all resources are properly disposed
4. **Monitor Memory Usage**: Use memory profilers to identify leaks

### Query Optimization

1. **Specific Columns**: Select only needed columns
2. **Efficient Filters**: Use indexed columns in WHERE clauses
3. **Bounded Traversals**: Limit graph traversal depth
4. **Query Planning**: Use EXPLAIN to understand query execution

### Concurrent Access

1. **Connection Per Thread**: Use one connection per thread
2. **Database Sharing**: Multiple connections can share one database
3. **Thread Safety**: Individual connections are not thread-safe
4. **Async Operations**: Use async methods for concurrent operations

### Error Handling

1. **Try Methods**: Use `TryQuery` and `TryPrepare` for safe execution
2. **Exception Handling**: Handle `KuzuException` appropriately
3. **Resource Cleanup**: Ensure cleanup in finally blocks
4. **Timeout Handling**: Set appropriate query timeouts

This performance guide provides comprehensive strategies for optimizing KuzuDot applications. Regular benchmarking and monitoring will help identify performance bottlenecks and optimization opportunities.
