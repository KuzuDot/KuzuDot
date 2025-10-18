# KuzuDot Examples

This directory contains practical examples demonstrating various use cases and features of KuzuDot.

## Quick Start Example

### Basic Database Operations

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

## Example Categories

### 1. Basic Operations
- [Hello World](basic/hello-world/) - Simple database creation and querying
- [CRUD Operations](basic/crud-operations/) - Create, Read, Update, Delete operations
- [Data Types](basic/data-types/) - Working with different KuzuDB data types

### 2. Graph Operations
- [Social Network](graph/social-network/) - Users, posts, and relationships
- [Corporate Hierarchy](graph/corporate-hierarchy/) - Employee management structure
- [Product Catalog](graph/product-catalog/) - E-commerce product relationships

### 3. Advanced Features
- [Prepared Statements](advanced/prepared-statements/) - Parameterized queries
- [POCO Mapping](advanced/poco-mapping/) - Object-relational mapping
- [Async Operations](advanced/async-operations/) - Asynchronous query execution

### 4. Performance
- [Batch Operations](performance/batch-operations/) - Efficient bulk operations
- [Connection Pooling](performance/connection-pooling/) - Managing multiple connections
- [Query Optimization](performance/query-optimization/) - Optimizing query performance

### 5. Real-World Scenarios
- [Recommendation System](real-world/recommendation-system/) - Movie recommendation engine
- [Fraud Detection](real-world/fraud-detection/) - Detecting suspicious patterns
- [Network Analysis](real-world/network-analysis/) - Analyzing network topologies

## Running Examples

### Prerequisites
- .NET 8.0 or later
- KuzuDB native library (`kuzu_shared.dll`)

### Command Line
```bash
# Run individual examples
dotnet run --project basic/hello-world
dotnet run --project basic/crud-operations
dotnet run --project basic/data-types

dotnet run --project graph/social-network
dotnet run --project graph/corporate-hierarchy
dotnet run --project graph/product-catalog

dotnet run --project advanced/prepared-statements
dotnet run --project advanced/poco-mapping
dotnet run --project advanced/async-operations

dotnet run --project performance/batch-operations
dotnet run --project performance/connection-pooling
dotnet run --project performance/query-optimization

dotnet run --project real-world/recommendation-system
dotnet run --project real-world/fraud-detection
dotnet run --project real-world/network-analysis
```

### Visual Studio
1. Open the solution in Visual Studio
2. Set the example project as startup project
3. Press F5 to run

## Example Structure

Each example follows this structure:

```csharp
using KuzuDot;

namespace KuzuDot.Examples
{
    public class ExampleName
    {
        public static void Main(string[] args)
        {
            try
            {
                RunExample();
            }
            catch (KuzuException ex)
            {
                Console.WriteLine($"KuzuDB Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void RunExample()
        {
            // Example implementation
        }
    }
}
```

## Contributing Examples

When adding new examples:

1. Follow the established naming conventions
2. Include proper error handling
3. Use `using` statements for resource management
4. Add comments explaining key concepts
5. Include expected output in comments

## Example Index

| Example | Description | Complexity |
|---------|-------------|------------|
| [Hello World](basic/hello-world/) | Basic database operations | Beginner |
| [CRUD Operations](basic/crud-operations/) | Create, read, update, delete | Beginner |
| [Data Types](basic/data-types/) | Working with KuzuDB types | Beginner |
| [Social Network](graph/social-network/) | Graph relationships | Intermediate |
| [Corporate Hierarchy](graph/corporate-hierarchy/) | Management structure | Intermediate |
| [Product Catalog](graph/product-catalog/) | E-commerce relationships | Intermediate |
| [Prepared Statements](advanced/prepared-statements/) | Parameterized queries | Intermediate |
| [POCO Mapping](advanced/poco-mapping/) | Object mapping | Intermediate |
| [Async Operations](advanced/async-operations/) | Asynchronous execution | Advanced |
| [Batch Operations](performance/batch-operations/) | Bulk operations | Advanced |
| [Connection Pooling](performance/connection-pooling/) | Multiple connections | Advanced |
| [Query Optimization](performance/query-optimization/) | Performance tuning | Advanced |
| [Recommendation System](real-world/recommendation-system/) | Real-world application | Advanced |
| [Fraud Detection](real-world/fraud-detection/) | Pattern detection | Advanced |
| [Network Analysis](real-world/network-analysis/) | Graph analysis | Advanced |

## Getting Help

- **Documentation**: See [docs/](../docs/) for comprehensive guides
- **API Reference**: See [docs/API_REFERENCE.md](../docs/API_REFERENCE.md)
- **Performance Guide**: See [docs/PERFORMANCE.md](../docs/PERFORMANCE.md)
