# KuzuDot Examples

This document provides comprehensive examples demonstrating various usage patterns and features of KuzuDot.

## Table of Contents

- [Basic Examples](#basic-examples)
- [Database Setup](#database-setup)
- [Query Execution](#query-execution)
- [Prepared Statements](#prepared-statements)
- [Working with Results](#working-with-results)
- [POCO Mapping](#poco-mapping)
- [Complex Data Types](#complex-data-types)
- [Graph Operations](#graph-operations)
- [Async Operations](#async-operations)
- [Error Handling](#error-handling)
- [Performance Patterns](#performance-patterns)
- [Real-World Scenarios](#real-world-scenarios)

## Basic Examples

### Hello World

```csharp
using KuzuDot;

// Create an in-memory database
using var database = Database.FromMemory();
using var connection = database.Connect();

// Create a simple table
connection.NonQuery("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name))");

// Insert data
connection.NonQuery("CREATE (:Person {name: 'Alice'})");
connection.NonQuery("CREATE (:Person {name: 'Bob'})");

// Query data
using var result = connection.Query("MATCH (p:Person) RETURN p.name ORDER BY p.name");
while (result.HasNext())
{
    using var row = result.GetNext();
    var name = row.GetValueAs<string>(0);
    Console.WriteLine($"Hello, {name}!");
}
```

### Version Information

```csharp
using KuzuDot;

Console.WriteLine($"KuzuDB Version: {Version.GetVersion()}");
Console.WriteLine($"Storage Version: {Version.GetStorageVersion()}");
```

## Database Setup

### File-Based Database

```csharp
using KuzuDot;

// Create or open a file-based database
using var database = Database.FromPath("./mygraph.db");
using var connection = database.Connect();

// Database will persist data to disk
connection.NonQuery("CREATE NODE TABLE User(id INT64, name STRING, PRIMARY KEY(id))");
```

### In-Memory Database

```csharp
using KuzuDot;

// Create an in-memory database (data is not persisted)
using var database = Database.FromMemory();
using var connection = database.Connect();

// Perfect for testing and temporary data
connection.NonQuery("CREATE NODE TABLE Temp(id INT64, data STRING, PRIMARY KEY(id))");
```

### Database Configuration

```csharp
using KuzuDot;

// Create database with custom configuration
var config = new DatabaseConfig();
// Configure as needed

using var database = Database.FromPath("./mygraph.db", config);
using var connection = database.Connect();
```

## Query Execution

### Simple Queries

```csharp
using var connection = database.Connect();

// Create schema
connection.NonQuery(@"
    CREATE NODE TABLE Person(
        id INT64, 
        name STRING, 
        age INT32, 
        email STRING,
        PRIMARY KEY(id)
    )");

// Insert data
connection.NonQuery("CREATE (:Person {id: 1, name: 'Alice', age: 30, email: 'alice@example.com'})");
connection.NonQuery("CREATE (:Person {id: 2, name: 'Bob', age: 25, email: 'bob@example.com'})");

// Query all persons
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name, p.age, p.email");
while (result.HasNext())
{
    using var row = result.GetNext();
    var id = row.GetValueAs<long>(0);
    var name = row.GetValueAs<string>(1);
    var age = row.GetValueAs<int>(2);
    var email = row.GetValueAs<string>(3);
    
    Console.WriteLine($"Person: {name} (ID: {id}, Age: {age}, Email: {email})");
}
```

### Scalar Queries

```csharp
// Get count of records
long count = connection.ExecuteScalar<long>("MATCH (p:Person) RETURN COUNT(p)");
Console.WriteLine($"Total persons: {count}");

// Get maximum age
int maxAge = connection.ExecuteScalar<int>("MATCH (p:Person) RETURN MAX(p.age)");
Console.WriteLine($"Maximum age: {maxAge}");

// Check if person exists
bool exists = connection.ExecuteScalar<bool>("MATCH (p:Person) WHERE p.name = 'Alice' RETURN COUNT(p) > 0");
Console.WriteLine($"Alice exists: {exists}");
```

### Non-Query Operations

```csharp
// DDL operations
connection.NonQuery("CREATE NODE TABLE Company(id INT64, name STRING, PRIMARY KEY(id))");
connection.NonQuery("CREATE REL TABLE WorksFor(FROM Person TO Company, start_date STRING)");

// DML operations
connection.NonQuery("CREATE (:Company {id: 1, name: 'TechCorp'})");
connection.NonQuery("CREATE (:Company {id: 2, name: 'DataSys'})");

// Update operations
connection.NonQuery("MATCH (p:Person) WHERE p.name = 'Alice' SET p.age = 31");
```

## Prepared Statements

### Basic Prepared Statements

```csharp
// Prepare a statement for inserting persons
using var insertStmt = connection.Prepare("CREATE (:Person {id: $id, name: $name, age: $age, email: $email})");

// Insert multiple persons
var persons = new[]
{
    new { Id = 3L, Name = "Charlie", Age = 35, Email = "charlie@example.com" },
    new { Id = 4L, Name = "Diana", Age = 28, Email = "diana@example.com" },
    new { Id = 5L, Name = "Eve", Age = 32, Email = "eve@example.com" }
};

foreach (var person in persons)
{
    insertStmt.Bind("id", person.Id);
    insertStmt.Bind("name", person.Name);
    insertStmt.Bind("age", person.Age);
    insertStmt.Bind("email", person.Email);
    
    using var result = insertStmt.Execute();
    Console.WriteLine($"Inserted: {person.Name}");
}
```

### Object Binding

```csharp
public class PersonData
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

// Prepare statement
using var stmt = connection.Prepare("CREATE (:Person {id: $id, name: $name, age: $age, email: $email})");

// Bind from object
var person = new PersonData
{
    Id = 6,
    Name = "Frank",
    Age = 29,
    Email = "frank@example.com"
};

stmt.Bind(person);
using var result = stmt.Execute();
```

### Query with Parameters

```csharp
// Prepare a query with parameters
using var queryStmt = connection.Prepare("MATCH (p:Person) WHERE p.age >= $minAge AND p.age <= $maxAge RETURN p.name, p.age ORDER BY p.age");

// Execute with different age ranges
queryStmt.Bind("minAge", 25);
queryStmt.Bind("maxAge", 35);

using var result = queryStmt.Execute();
Console.WriteLine("Persons aged 25-35:");
while (result.HasNext())
{
    using var row = result.GetNext();
    var name = row.GetValueAs<string>(0);
    var age = row.GetValueAs<int>(1);
    Console.WriteLine($"  {name} ({age} years old)");
}
```

## Working with Results

### Basic Result Processing

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name, p.age");

Console.WriteLine($"Query returned {result.RowCount} rows with {result.ColumnCount} columns");

// Get column names
for (ulong i = 0; i < result.ColumnCount; i++)
{
    var columnName = result.GetColumnName(i);
    Console.WriteLine($"Column {i}: {columnName}");
}

// Process rows
while (result.HasNext())
{
    using var row = result.GetNext();
    
    // Access by index
    var id = row.GetValueAs<long>(0);
    var name = row.GetValueAs<string>(1);
    var age = row.GetValueAs<int>(2);
    
    Console.WriteLine($"Person: {name} (ID: {id}, Age: {age})");
}
```

### Column Name Access

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.id AS person_id, p.name AS person_name, p.age AS person_age");

while (result.HasNext())
{
    using var row = result.GetNext();
    
    // Access by column name
    var id = row.GetValueAs<long>("person_id");
    var name = row.GetValueAs<string>("person_name");
    var age = row.GetValueAs<int>("person_age");
    
    Console.WriteLine($"Person: {name} (ID: {id}, Age: {age})");
}
```

### Typed Value Access

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name, p.age");

while (result.HasNext())
{
    using var row = result.GetNext();
    
    // Get typed values
    using var idValue = row.GetValue<KuzuInt64>(0);
    using var nameValue = row.GetValue<KuzuString>(1);
    using var ageValue = row.GetValue<KuzuInt32>(2);
    
    Console.WriteLine($"Person: {nameValue.Value} (ID: {idValue.Value}, Age: {ageValue.Value})");
}
```

### Safe Value Access

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.name, p.age");

while (result.HasNext())
{
    using var row = result.GetNext();
    
    // Safe access with TryGetValue
    if (row.TryGetValueAs<long>("id", out var id) &&
        row.TryGetValueAs<string>("name", out var name) &&
        row.TryGetValueAs<int>("age", out var age))
    {
        Console.WriteLine($"Person: {name} (ID: {id}, Age: {age})");
    }
}
```

## POCO Mapping

### Basic POCO Mapping

```csharp
public class Person
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

// Query and map to POCOs
var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age, p.email");

foreach (var person in people)
{
    Console.WriteLine($"Person: {person.Name} (ID: {person.Id}, Age: {person.Age}, Email: {person.Email})");
}
```

### POCO with Custom Column Names

```csharp
public class PersonWithAttributes
{
    [KuzuName("person_id")]
    public long Id { get; set; }
    
    [KuzuName("person_name")]
    public string Name { get; set; } = string.Empty;
    
    [KuzuName("person_age")]
    public int Age { get; set; }
}

// Query with aliased columns
var people = connection.Query<PersonWithAttributes>(
    "MATCH (p:Person) RETURN p.id AS person_id, p.name AS person_name, p.age AS person_age");

foreach (var person in people)
{
    Console.WriteLine($"Person: {person.Name} (ID: {person.Id}, Age: {person.Age})");
}
```

### Custom Projector Function

```csharp
// Define a custom projection
var personSummaries = connection.Query(
    "MATCH (p:Person) RETURN p.id, p.name, p.age",
    row => new
    {
        Id = row.GetValueAs<long>(0),
        Name = row.GetValueAs<string>(1),
        Age = row.GetValueAs<int>(2),
        IsAdult = row.GetValueAs<int>(2) >= 18
    });

foreach (var summary in personSummaries)
{
    Console.WriteLine($"{summary.Name} is {(summary.IsAdult ? "an adult" : "a minor")}");
}
```

## Complex Data Types

### Working with Nodes

```csharp
// Create nodes with relationships
connection.NonQuery(@"
    CREATE NODE TABLE Company(id INT64, name STRING, industry STRING, PRIMARY KEY(id))
    CREATE REL TABLE WorksFor(FROM Person TO Company, start_date STRING, salary INT64)
");

connection.NonQuery("CREATE (:Company {id: 1, name: 'TechCorp', industry: 'Technology'})");
connection.NonQuery("CREATE (:Company {id: 2, name: 'DataSys', industry: 'Technology'})");

// Create relationships
connection.NonQuery("MATCH (p:Person), (c:Company) WHERE p.name = 'Alice' AND c.name = 'TechCorp' CREATE (p)-[:WorksFor {start_date: '2023-01-01', salary: 75000}]->(c)");

// Query nodes
using var result = connection.Query("MATCH (p:Person)-[r:WorksFor]->(c:Company) RETURN p, r, c");

while (result.HasNext())
{
    using var row = result.GetNext();
    
    using var personNode = row.GetValue<KuzuNode>(0);
    using var relationship = row.GetValue<KuzuRel>(1);
    using var companyNode = row.GetValue<KuzuNode>(2);
    
    Console.WriteLine($"Person: {personNode.Label}");
    foreach (var (key, value) in personNode.Properties)
    {
        Console.WriteLine($"  {key}: {value}");
        value.Dispose();
    }
    
    Console.WriteLine($"Company: {companyNode.Label}");
    foreach (var (key, value) in companyNode.Properties)
    {
        Console.WriteLine($"  {key}: {value}");
        value.Dispose();
    }
    
    Console.WriteLine($"Relationship: {relationship.Label}");
    foreach (var (key, value) in relationship.Properties)
    {
        Console.WriteLine($"  {key}: {value}");
        value.Dispose();
    }
}
```

### Working with Lists

```csharp
// Create a table with list properties
connection.NonQuery("CREATE NODE TABLE Project(id INT64, name STRING, tags LIST(STRING), PRIMARY KEY(id))");

// Insert with list values
connection.NonQuery("CREATE (:Project {id: 1, name: 'Web App', tags: ['web', 'javascript', 'react']})");
connection.NonQuery("CREATE (:Project {id: 2, name: 'Mobile App', tags: ['mobile', 'ios', 'android']})");

// Query lists
using var result = connection.Query("MATCH (p:Project) RETURN p.id, p.name, p.tags");

while (result.HasNext())
{
    using var row = result.GetNext();
    var id = row.GetValueAs<long>(0);
    var name = row.GetValueAs<string>(1);
    
    using var tagsValue = row.GetValue<KuzuList>(2);
    var tags = new List<string>();
    
    for (ulong i = 0; i < tagsValue.Size; i++)
    {
        using var tagValue = tagsValue.GetValue(i);
        if (tagValue is KuzuString tag)
        {
            tags.Add(tag.Value);
        }
    }
    
    Console.WriteLine($"Project: {name} (ID: {id})");
    Console.WriteLine($"  Tags: {string.Join(", ", tags)}");
}
```

### Working with Maps

```csharp
// Create a table with map properties
connection.NonQuery("CREATE NODE TABLE Product(id INT64, name STRING, metadata MAP(STRING, STRING), PRIMARY KEY(id))");

// Insert with map values
connection.NonQuery("CREATE (:Product {id: 1, name: 'Laptop', metadata: {brand: 'TechCorp', model: 'TC-1000', warranty: '2 years'}})");

// Query maps
using var result = connection.Query("MATCH (p:Product) RETURN p.id, p.name, p.metadata");

while (result.HasNext())
{
    using var row = result.GetNext();
    var id = row.GetValueAs<long>(0);
    var name = row.GetValueAs<string>(1);
    
    using var metadataValue = row.GetValue<KuzuMap>(2);
    
    Console.WriteLine($"Product: {name} (ID: {id})");
    Console.WriteLine("  Metadata:");
    
    foreach (var (key, value) in metadataValue.Properties)
    {
        if (value is KuzuString stringValue)
        {
            Console.WriteLine($"    {key}: {stringValue.Value}");
        }
        value.Dispose();
    }
}
```

## Graph Operations

### Social Network Example

```csharp
// Create social network schema
connection.NonQuery(@"
    CREATE NODE TABLE User(id INT64, name STRING, email STRING, PRIMARY KEY(id))
    CREATE NODE TABLE Post(id INT64, title STRING, content STRING, created_at TIMESTAMP, PRIMARY KEY(id))
    CREATE REL TABLE Follows(FROM User TO User, since STRING)
    CREATE REL TABLE Authored(FROM User TO Post)
    CREATE REL TABLE Likes(FROM User TO Post, timestamp TIMESTAMP)
");

// Insert users
var users = new[]
{
    new { Id = 1L, Name = "Alice", Email = "alice@example.com" },
    new { Id = 2L, Name = "Bob", Email = "bob@example.com" },
    new { Id = 3L, Name = "Charlie", Email = "charlie@example.com" }
};

using var userStmt = connection.Prepare("CREATE (:User {id: $id, name: $name, email: $email})");
foreach (var user in users)
{
    userStmt.Bind(user);
    userStmt.Execute();
}

// Insert posts
var posts = new[]
{
    new { Id = 1L, Title = "Hello World", Content = "My first post!", CreatedAt = DateTime.UtcNow },
    new { Id = 2L, Title = "Graph Databases", Content = "Learning about graph databases", CreatedAt = DateTime.UtcNow }
};

using var postStmt = connection.Prepare("CREATE (:Post {id: $id, title: $title, content: $content, created_at: $created_at})");
foreach (var post in posts)
{
    postStmt.Bind("id", post.Id);
    postStmt.Bind("title", post.Title);
    postStmt.Bind("content", post.Content);
    postStmt.BindTimestamp("created_at", post.CreatedAt);
    postStmt.Execute();
}

// Create relationships
connection.NonQuery("MATCH (a:User), (b:User) WHERE a.name = 'Alice' AND b.name = 'Bob' CREATE (a)-[:Follows {since: '2023-01-01'}]->(b)");
connection.NonQuery("MATCH (a:User), (p:Post) WHERE a.name = 'Alice' AND p.title = 'Hello World' CREATE (a)-[:Authored]->(p)");
connection.NonQuery("MATCH (b:User), (p:Post) WHERE b.name = 'Bob' AND p.title = 'Hello World' CREATE (b)-[:Likes {timestamp: $timestamp}]->(p)");

// Query social network
using var socialResult = connection.Query(@"
    MATCH (follower:User)-[:Follows]->(author:User)-[:Authored]->(post:Post)
    RETURN follower.name, author.name, post.title
");

Console.WriteLine("Social Network Activity:");
while (socialResult.HasNext())
{
    using var row = socialResult.GetNext();
    var follower = row.GetValueAs<string>(0);
    var author = row.GetValueAs<string>(1);
    var postTitle = row.GetValueAs<string>(2);
    
    Console.WriteLine($"{follower} follows {author} who authored '{postTitle}'");
}
```

### Corporate Hierarchy

```csharp
// Create corporate hierarchy schema
connection.NonQuery(@"
    CREATE NODE TABLE Employee(id INT64, name STRING, role STRING, department STRING, PRIMARY KEY(id))
    CREATE REL TABLE Manages(FROM Employee TO Employee, since STRING)
    CREATE REL TABLE WorksIn(FROM Employee TO Department, start_date STRING)
");

// Insert departments
connection.NonQuery("CREATE (:Department {id: 1, name: 'Engineering'})");
connection.NonQuery("CREATE (:Department {id: 2, name: 'Sales'})");

// Insert employees
var employees = new[]
{
    new { Id = 1L, Name = "Alice Manager", Role = "Engineering Manager", Department = "Engineering" },
    new { Id = 2L, Name = "Bob Developer", Role = "Senior Developer", Department = "Engineering" },
    new { Id = 3L, Name = "Carol Analyst", Role = "Business Analyst", Department = "Engineering" },
    new { Id = 4L, Name = "Dave Sales", Role = "Sales Manager", Department = "Sales" }
};

using var empStmt = connection.Prepare("CREATE (:Employee {id: $id, name: $name, role: $role, department: $department})");
foreach (var emp in employees)
{
    empStmt.Bind(emp);
    empStmt.Execute();
}

// Create management relationships
connection.NonQuery("MATCH (m:Employee), (e:Employee) WHERE m.name = 'Alice Manager' AND e.name = 'Bob Developer' CREATE (m)-[:Manages {since: '2023-01-01'}]->(e)");
connection.NonQuery("MATCH (m:Employee), (e:Employee) WHERE m.name = 'Alice Manager' AND e.name = 'Carol Analyst' CREATE (m)-[:Manages {since: '2023-01-01'}]->(e)");

// Query hierarchy
using var hierarchyResult = connection.Query(@"
    MATCH (manager:Employee)-[:Manages]->(employee:Employee)
    RETURN manager.name, employee.name, employee.role
");

Console.WriteLine("Management Hierarchy:");
while (hierarchyResult.HasNext())
{
    using var row = hierarchyResult.GetNext();
    var manager = row.GetValueAs<string>(0);
    var employee = row.GetValueAs<string>(1);
    var role = row.GetValueAs<string>(2);
    
    Console.WriteLine($"{manager} manages {employee} ({role})");
}
```

## Async Operations

### Async Query Execution

```csharp
// Async query execution
using var result = await connection.QueryAsync("MATCH (p:Person) RETURN p.name, p.age");

Console.WriteLine("Async query results:");
while (result.HasNext())
{
    using var row = result.GetNext();
    var name = row.GetValueAs<string>(0);
    var age = row.GetValueAs<int>(1);
    Console.WriteLine($"Person: {name} (Age: {age})");
}
```

### Async Prepared Statements

```csharp
// Async prepared statement
using var stmt = await connection.PrepareAsync("MATCH (p:Person) WHERE p.age >= $minAge RETURN p.name, p.age");

stmt.Bind("minAge", 25);
using var result = await connection.ExecuteAsync(stmt);

Console.WriteLine("Persons 25 and older:");
while (result.HasNext())
{
    using var row = result.GetNext();
    var name = row.GetValueAs<string>(0);
    var age = row.GetValueAs<int>(1);
    Console.WriteLine($"  {name} ({age} years old)");
}
```

### Async POCO Mapping

```csharp
// Async POCO mapping
var people = await connection.QueryAsync<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age");

Console.WriteLine("Async POCO results:");
foreach (var person in people)
{
    Console.WriteLine($"Person: {person.Name} (ID: {person.Id}, Age: {person.Age})");
}
```

### Cancellation Support

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5)); // Cancel after 5 seconds

try
{
    using var result = await connection.QueryAsync("MATCH (n) RETURN n", cts.Token);
    // Process results...
}
catch (OperationCanceledException)
{
    Console.WriteLine("Query was cancelled");
}
```

## Error Handling

### Basic Error Handling

```csharp
try
{
    using var result = connection.Query("MATCH (n:NonExistentTable) RETURN n");
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

### Safe Query Execution

```csharp
// Using TryQuery for safe execution
if (connection.TryQuery("MATCH (p:Person) RETURN p.name", out var result, out var errorMessage))
{
    using (result)
    {
        Console.WriteLine("Query executed successfully:");
        while (result.HasNext())
        {
            using var row = result.GetNext();
            var name = row.GetValueAs<string>(0);
            Console.WriteLine($"  {name}");
        }
    }
}
else
{
    Console.WriteLine($"Query failed: {errorMessage}");
}
```

### Safe Prepared Statement Execution

```csharp
// Safe prepared statement execution
if (connection.TryPrepare("MATCH (p:Person) WHERE p.age >= $minAge RETURN p.name", out var stmt, out var prepareError))
{
    using (stmt)
    {
        stmt.Bind("minAge", 25);
        using var result = stmt.Execute();
        
        Console.WriteLine("Persons 25 and older:");
        while (result.HasNext())
        {
            using var row = result.GetNext();
            var name = row.GetValueAs<string>(0);
            Console.WriteLine($"  {name}");
        }
    }
}
else
{
    Console.WriteLine($"Prepare failed: {prepareError}");
}
```

### Resource Disposal Safety

```csharp
// Safe resource disposal pattern
Database? database = null;
Connection? connection = null;
QueryResult? result = null;

try
{
    database = Database.FromMemory();
    connection = database.Connect();
    result = connection.Query("MATCH (p:Person) RETURN p.name");
    
    while (result.HasNext())
    {
        using var row = result.GetNext();
        var name = row.GetValueAs<string>(0);
        Console.WriteLine($"Person: {name}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    result?.Dispose();
    connection?.Dispose();
    database?.Dispose();
}
```

## Performance Patterns

### Batch Operations

```csharp
// Efficient batch insert using prepared statements
using var insertStmt = connection.Prepare("CREATE (:Person {id: $id, name: $name, age: $age})");

var startTime = DateTime.UtcNow;
const int batchSize = 1000;

for (int i = 1; i <= batchSize; i++)
{
    insertStmt.Bind("id", i);
    insertStmt.Bind("name", $"Person{i}");
    insertStmt.Bind("age", 20 + (i % 50));
    
    insertStmt.Execute();
    
    if (i % 100 == 0)
    {
        Console.WriteLine($"Inserted {i} records...");
    }
}

var elapsed = DateTime.UtcNow - startTime;
Console.WriteLine($"Batch insert completed: {batchSize} records in {elapsed.TotalMilliseconds:F0}ms");
```

### Connection Pooling Pattern

```csharp
public class DatabaseService
{
    private readonly Database _database;
    
    public DatabaseService(string databasePath)
    {
        _database = Database.FromPath(databasePath);
    }
    
    public async Task<T> ExecuteQuery<T>(Func<Connection, T> queryFunc)
    {
        using var connection = _database.Connect();
        return await Task.Run(() => queryFunc(connection));
    }
    
    public void Dispose()
    {
        _database.Dispose();
    }
}

// Usage
using var dbService = new DatabaseService("./mygraph.db");

var people = await dbService.ExecuteQuery(conn => 
    conn.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age"));
```

### Performance Monitoring

```csharp
// Register performance interceptor
KuzuInterceptorRegistry.Register(new TimingLoggingInterceptor());

// All queries will be monitored
using var result = connection.Query("MATCH (p:Person) RETURN p.name");
// Timing information will be logged automatically
```

## Real-World Scenarios

### E-commerce Product Catalog

```csharp
// E-commerce schema
connection.NonQuery(@"
    CREATE NODE TABLE Product(id INT64, name STRING, price DOUBLE, category STRING, PRIMARY KEY(id))
    CREATE NODE TABLE Customer(id INT64, name STRING, email STRING, PRIMARY KEY(id))
    CREATE NODE TABLE Order(id INT64, order_date TIMESTAMP, total DOUBLE, PRIMARY KEY(id))
    CREATE REL TABLE Contains(FROM Order TO Product, quantity INT32, unit_price DOUBLE)
    CREATE REL TABLE PlacedBy(FROM Order TO Customer)
");

// Insert products
var products = new[]
{
    new { Id = 1L, Name = "Laptop", Price = 999.99, Category = "Electronics" },
    new { Id = 2L, Name = "Mouse", Price = 29.99, Category = "Electronics" },
    new { Id = 3L, Name = "Book", Price = 19.99, Category = "Books" }
};

using var productStmt = connection.Prepare("CREATE (:Product {id: $id, name: $name, price: $price, category: $category})");
foreach (var product in products)
{
    productStmt.Bind(product);
    productStmt.Execute();
}

// Insert customers
var customers = new[]
{
    new { Id = 1L, Name = "John Doe", Email = "john@example.com" },
    new { Id = 2L, Name = "Jane Smith", Email = "jane@example.com" }
};

using var customerStmt = connection.Prepare("CREATE (:Customer {id: $id, name: $name, email: $email})");
foreach (var customer in customers)
{
    customerStmt.Bind(customer);
    customerStmt.Execute();
}

// Create orders
connection.NonQuery("CREATE (:Order {id: 1, order_date: $date, total: 1029.98})");
connection.NonQuery("MATCH (o:Order), (c:Customer) WHERE o.id = 1 AND c.id = 1 CREATE (o)-[:PlacedBy]->(c)");
connection.NonQuery("MATCH (o:Order), (p:Product) WHERE o.id = 1 AND p.id = 1 CREATE (o)-[:Contains {quantity: 1, unit_price: 999.99}]->(p)");
connection.NonQuery("MATCH (o:Order), (p:Product) WHERE o.id = 1 AND p.id = 2 CREATE (o)-[:Contains {quantity: 1, unit_price: 29.99}]->(p)");

// Query order details
using var orderResult = connection.Query(@"
    MATCH (c:Customer)-[:PlacedBy]-(o:Order)-[:Contains]->(p:Product)
    RETURN c.name, o.id, o.total, p.name, p.price
");

Console.WriteLine("Order Details:");
while (orderResult.HasNext())
{
    using var row = orderResult.GetNext();
    var customerName = row.GetValueAs<string>(0);
    var orderId = row.GetValueAs<long>(1);
    var orderTotal = row.GetValueAs<double>(2);
    var productName = row.GetValueAs<string>(3);
    var productPrice = row.GetValueAs<double>(4);
    
    Console.WriteLine($"Customer: {customerName}, Order: {orderId}, Total: ${orderTotal:F2}");
    Console.WriteLine($"  Product: {productName} (${productPrice:F2})");
}
```

### Recommendation System

```csharp
// Recommendation system schema
connection.NonQuery(@"
    CREATE NODE TABLE User(id INT64, name STRING, age INT32, PRIMARY KEY(id))
    CREATE NODE TABLE Movie(id INT64, title STRING, genre STRING, year INT32, PRIMARY KEY(id))
    CREATE REL TABLE Rated(FROM User TO Movie, rating DOUBLE, timestamp TIMESTAMP)
    CREATE REL TABLE Similar(FROM Movie TO Movie, similarity DOUBLE)
");

// Insert users and movies
var users = new[]
{
    new { Id = 1L, Name = "Alice", Age = 25 },
    new { Id = 2L, Name = "Bob", Age = 30 },
    new { Id = 3L, Name = "Charlie", Age = 35 }
};

var movies = new[]
{
    new { Id = 1L, Title = "The Matrix", Genre = "Sci-Fi", Year = 1999 },
    new { Id = 2L, Title = "Inception", Genre = "Sci-Fi", Year = 2010 },
    new { Id = 3L, Title = "Titanic", Genre = "Romance", Year = 1997 }
};

using var userStmt = connection.Prepare("CREATE (:User {id: $id, name: $name, age: $age})");
using var movieStmt = connection.Prepare("CREATE (:Movie {id: $id, title: $title, genre: $genre, year: $year})");

foreach (var user in users) { userStmt.Bind(user); userStmt.Execute(); }
foreach (var movie in movies) { movieStmt.Bind(movie); movieStmt.Execute(); }

// Insert ratings
var ratings = new[]
{
    new { UserId = 1L, MovieId = 1L, Rating = 5.0, Timestamp = DateTime.UtcNow },
    new { UserId = 1L, MovieId = 2L, Rating = 4.5, Timestamp = DateTime.UtcNow },
    new { UserId = 2L, MovieId = 1L, Rating = 4.0, Timestamp = DateTime.UtcNow },
    new { UserId = 2L, MovieId = 3L, Rating = 3.5, Timestamp = DateTime.UtcNow }
};

using var ratingStmt = connection.Prepare("MATCH (u:User), (m:Movie) WHERE u.id = $userId AND m.id = $movieId CREATE (u)-[:Rated {rating: $rating, timestamp: $timestamp}]->(m)");
foreach (var rating in ratings)
{
    ratingStmt.Bind("userId", rating.UserId);
    ratingStmt.Bind("movieId", rating.MovieId);
    ratingStmt.Bind("rating", rating.Rating);
    ratingStmt.BindTimestamp("timestamp", rating.Timestamp);
    ratingStmt.Execute();
}

// Find similar users based on ratings
using var similarUsersResult = connection.Query(@"
    MATCH (u1:User)-[r1:Rated]->(m:Movie)<-[r2:Rated]-(u2:User)
    WHERE u1.id = 1 AND u2.id != 1 AND ABS(r1.rating - r2.rating) <= 1.0
    RETURN u2.name, m.title, r1.rating, r2.rating
");

Console.WriteLine("Users with similar ratings to Alice:");
while (similarUsersResult.HasNext())
{
    using var row = similarUsersResult.GetNext();
    var userName = row.GetValueAs<string>(0);
    var movieTitle = row.GetValueAs<string>(1);
    var aliceRating = row.GetValueAs<double>(2);
    var userRating = row.GetValueAs<double>(3);
    
    Console.WriteLine($"{userName} rated '{movieTitle}' {userRating} (Alice rated it {aliceRating})");
}
```

This comprehensive examples guide demonstrates the full range of KuzuDot capabilities, from basic operations to complex real-world scenarios. Each example includes proper resource management and error handling patterns.
