# KuzuDot Data Types

This document provides comprehensive information about working with KuzuDB data types in KuzuDot.

## Table of Contents

- [Overview](#overview)
- [Scalar Types](#scalar-types)
- [Date and Time Types](#date-and-time-types)
- [Complex Types](#complex-types)
- [Graph Types](#graph-types)
- [Type Conversion](#type-conversion)
- [Null Handling](#null-handling)
- [Best Practices](#best-practices)

## Overview

KuzuDot provides strongly-typed wrappers for all KuzuDB data types. Each type is represented by a class that inherits from `KuzuValue` and provides type-specific operations.

### Base KuzuValue Class

All KuzuDB values inherit from the abstract `KuzuValue` class:

```csharp
public abstract class KuzuValue : IDisposable
{
    public KuzuDataTypeId DataTypeId { get; }
    public bool IsNull() { get; }
    public void SetNull(bool isNull) { }
    public KuzuValue Clone() { }
    public void CopyFrom(KuzuValue other) { }
    public override string ToString() { }
}
```

## Scalar Types

### Numeric Types

#### Integer Types

| Class | .NET Type | Description |
| --- | --- | --- |
KuzuInt8 | sbyte | 8-bit signed integer
KuzuUInt8 | byte | 8-bit unsigned integer
KuzuInt16 | short | 16-bit signed integer
KuzuUInt16 | ushort | 16-bit unsigned integer
KuzuInt32 | int | 32-bit signed integer
KuzuUInt32 | uint | 32-bit unsigned integer
KuzuInt64 | long | 64-bit signed integer
KuzuUInt64 | ulong | 64-bit unsigned integer
KuzuInt128 | BigInteger | 128-bit signed integer

#### Floating Point Types


| Class | .NET Type | Description |
| --- | --- | --- |
| KuzuFloat | float | 32-bit float |
| KuzuDouble | double | 64-bit float |


#### Example Usage

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.id, p.age, p.height");

while (result.HasNext())
{
    using var row = result.GetNext();
    
    // Get typed values
    using var idValue = row.GetValue<KuzuInt64>(0);
    using var ageValue = row.GetValue<KuzuInt32>(1);
    using var heightValue = row.GetValue<KuzuFloat>(2);
    
    long id = idValue.Value;
    int age = ageValue.Value;
    float height = heightValue.Value;
    
    Console.WriteLine($"Person {id}: Age {age}, Height {height}");
}
```

### Boolean Type

```csharp
public sealed class KuzuBool : KuzuTypedValue<bool>
{
    public bool Value { get; }
}
```

#### Example Usage

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.is_active");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var activeValue = row.GetValue<KuzuBool>(0);
    
    bool isActive = activeValue.Value;
    Console.WriteLine($"Person is {(isActive ? "active" : "inactive")}");
}
```

### String Type

| Class | .NET Type | Description |
| --- | --- | --- |
| KuzuString | string | ASCII* String |

\*UTF8 might work, but isn't guaranteed.

#### Example Usage

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.name, p.email");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var nameValue = row.GetValue<KuzuString>(0);
    using var emailValue = row.GetValue<KuzuString>(1);
    
    string name = nameValue.Value;
    string email = emailValue.Value;
    
    Console.WriteLine($"Person: {name} ({email})");
}
```

## Date and Time Types

### Date Type

| Class | .NET Type | Description |
| --- | --- | --- |
KuzuDate | `DateTime` | Date (year, month, day)

*In .NET 8.0 or greater, `DateOnly` can also be used
```

#### Example Usage

```csharp
// Insert date
using var stmt = connection.Prepare("CREATE (:Person {birth_date: $birth_date})");
stmt.BindDate("birth_date", new DateTime(1990, 5, 15));

// Query date
using var result = connection.Query("MATCH (p:Person) RETURN p.birth_date");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var dateValue = row.GetValue<KuzuDate>(0);
    
    DateTime birthDate = dateValue.Value;
    Console.WriteLine($"Birth date: {birthDate:yyyy-MM-dd}");
}
```

### Timestamp Types

KuzuDot supports multiple timestamp precisions:

| Class | .NET Type | Description |
| --- | --- | --- |
KuzuTimestamp | DateTime | Microseconds
KuzuTimestampNs | DateTime | Nanoseconds
KuzuTimestampMs | DateTime | Milliseconds
KuzuTimestampSec | DateTime | Seconds
KuzuTimestampTz | DateTimeOffset | Timezone-aware

#### Example Usage

```csharp
// Insert timestamp
using var stmt = connection.Prepare("CREATE (:Event {created_at: $created_at})");
stmt.BindTimestamp("created_at", DateTime.UtcNow);

// Insert with specific precision
stmt.BindTimestampMicros("created_at", DateTimeOffset.UtcNow.ToUnixTimeMicroseconds());
stmt.BindTimestampMilliseconds("created_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// Query timestamp
using var result = connection.Query("MATCH (e:Event) RETURN e.created_at");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var timestampValue = row.GetValue<KuzuTimestamp>(0);
    
    DateTime createdAt = timestampValue.Value;
    long unixMicros = timestampValue.UnixMicros;
    
    Console.WriteLine($"Created at: {createdAt:yyyy-MM-dd HH:mm:ss.ffffff}");
    Console.WriteLine($"Unix microseconds: {unixMicros}");
}
```

### Interval Type

| Class | .NET Type | Description |
| --- | --- | --- |
KuzuInterval | TimeSpan | Date/Time Interval

#### Example Usage

```csharp
// Insert interval
using var stmt = connection.Prepare("CREATE (:Task {duration: $duration})");
stmt.BindInterval("duration", TimeSpan.FromHours(2.5));

// Query interval
using var result = connection.Query("MATCH (t:Task) RETURN t.duration");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var intervalValue = row.GetValue<KuzuInterval>(0);
    
    TimeSpan duration = intervalValue.Value;
    Console.WriteLine($"Duration: {duration.TotalHours} hours");
}
```

## Complex Types

### List Type

```csharp
public sealed class KuzuList : KuzuValue
{
    public ulong Size { get; }
    
    public KuzuValue GetValue(ulong index) { }
    public KuzuValue GetValue<T>(ulong index) where T : KuzuValue { }
}
```

#### Example Usage

```csharp
// Insert list
connection.NonQuery("CREATE (:Project {tags: ['web', 'javascript', 'react']})");

// Query list
using var result = connection.Query("MATCH (p:Project) RETURN p.tags");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var listValue = row.GetValue<KuzuList>(0);
    
    var tags = new List<string>();
    for (ulong i = 0; i < listValue.Size; i++)
    {
        using var tagValue = listValue.GetValue<KuzuString>(i);
        tags.Add(tagValue.Value);
    }
    
    Console.WriteLine($"Tags: {string.Join(", ", tags)}");
}
```

### Map Type

```csharp
public sealed class KuzuMap : KuzuValue
{
    public PropertyDictionary Properties { get; }
}
```

#### Example Usage

```csharp
// Insert map
connection.NonQuery("CREATE (:Product {metadata: {brand: 'TechCorp', model: 'TC-1000', warranty: '2 years'}})");

// Query map
using var result = connection.Query("MATCH (p:Product) RETURN p.metadata");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var mapValue = row.GetValue<KuzuMap>(0);
    
    Console.WriteLine("Product metadata:");
    foreach (var (key, value) in mapValue.Properties)
    {
        if (value is KuzuString stringValue)
        {
            Console.WriteLine($"  {key}: {stringValue.Value}");
        }
        value.Dispose();
    }
}
```

### Struct Type

```csharp
public sealed class KuzuStruct : KuzuValue
{
    public PropertyDictionary Properties { get; }
}
```

#### Example Usage

```csharp
// Insert struct
connection.NonQuery("CREATE (:Address {location: {street: '123 Main St', city: 'Anytown', zip: '12345'}})");

// Query struct
using var result = connection.Query("MATCH (a:Address) RETURN a.location");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var structValue = row.GetValue<KuzuStruct>(0);
    
    Console.WriteLine("Address:");
    foreach (var (key, value) in structValue.Properties)
    {
        if (value is KuzuString stringValue)
        {
            Console.WriteLine($"  {key}: {stringValue.Value}");
        }
        value.Dispose();
    }
}
```

### Array Type

```csharp
public sealed class KuzuArray : KuzuValue
{
    public ulong Size { get; }
    
    public KuzuValue GetValue(ulong index) { }
    public KuzuValue GetValue<T>(ulong index) where T : KuzuValue { }
}
```

#### Example Usage

```csharp
// Insert array
connection.NonQuery("CREATE (:Matrix {data: [1, 2, 3, 4, 5, 6]})");

// Query array
using var result = connection.Query("MATCH (m:Matrix) RETURN m.data");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var arrayValue = row.GetValue<KuzuArray>(0);
    
    var numbers = new List<int>();
    for (ulong i = 0; i < arrayValue.Size; i++)
    {
        using var numberValue = arrayValue.GetValue<KuzuInt32>(i);
        numbers.Add(numberValue.Value);
    }
    
    Console.WriteLine($"Array: [{string.Join(", ", numbers)}]");
}
```

## Graph Types

### Node Type

```csharp
public sealed class KuzuNode : KuzuValue
{
    public string Label { get; }
    public PropertyDictionary Properties { get; }
    public KuzuInternalId InternalId { get; }
}
```

#### Example Usage

```csharp
// Query nodes
using var result = connection.Query("MATCH (n:Person) RETURN n");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var nodeValue = row.GetValue<KuzuNode>(0);
    
    Console.WriteLine($"Node Label: {nodeValue.Label}");
    Console.WriteLine($"Internal ID: {nodeValue.InternalId}");
    
    Console.WriteLine("Properties:");
    foreach (var (key, value) in nodeValue.Properties)
    {
        Console.WriteLine($"  {key}: {value}");
        value.Dispose();
    }
}
```

### Relationship Type

```csharp
public sealed class KuzuRel : KuzuValue
{
    public string Label { get; }
    public PropertyDictionary Properties { get; }
    public KuzuInternalId SrcId { get; }
    public KuzuInternalId DstId { get; }
}
```

#### Example Usage

```csharp
// Query relationships
using var result = connection.Query("MATCH ()-[r:KNOWS]->() RETURN r");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var relValue = row.GetValue<KuzuRel>(0);
    
    Console.WriteLine($"Relationship Label: {relValue.Label}");
    Console.WriteLine($"Source ID: {relValue.SrcId}");
    Console.WriteLine($"Destination ID: {relValue.DstId}");
    
    Console.WriteLine("Properties:");
    foreach (var (key, value) in relValue.Properties)
    {
        Console.WriteLine($"  {key}: {value}");
        value.Dispose();
    }
}
```

### Recursive Relationship Type

```csharp
public sealed class KuzuRecursiveRel : KuzuValue
{
    public string Label { get; }
    public PropertyDictionary Properties { get; }
    public KuzuInternalId SrcId { get; }
    public KuzuInternalId DstId { get; }
    public KuzuInternalId RelId { get; }
}
```

### Internal ID Type

```csharp
public sealed class KuzuInternalId : KuzuTypedValue<long>
{
    public long Value { get; }
}
```

### UUID Type

```csharp
public sealed class KuzuUUID : KuzuTypedValue<Guid>
{
    public Guid Value { get; }
}
```

#### Example Usage

```csharp
// Insert UUID
using var stmt = connection.Prepare("CREATE (:Document {id: $id, content: $content})");
stmt.Bind("id", Guid.NewGuid());
stmt.Bind("content", "Document content");

// Query UUID
using var result = connection.Query("MATCH (d:Document) RETURN d.id");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var uuidValue = row.GetValue<KuzuUUID>(0);
    
    Guid documentId = uuidValue.Value;
    Console.WriteLine($"Document ID: {documentId}");
}
```

### Blob Type

```csharp
public sealed class KuzuBlob : KuzuValue
{
    public byte[] Value { get; }
    public ulong Size { get; }
}
```

#### Example Usage

```csharp
// Insert blob
var imageData = File.ReadAllBytes("image.jpg");
using var stmt = connection.Prepare("CREATE (:Image {data: $data})");
stmt.Bind("data", imageData);

// Query blob
using var result = connection.Query("MATCH (i:Image) RETURN i.data");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var blobValue = row.GetValue<KuzuBlob>(0);
    
    byte[] imageBytes = blobValue.Value;
    Console.WriteLine($"Image size: {imageBytes.Length} bytes");
}
```

## Type Conversion

### Automatic Type Conversion

KuzuDot provides automatic type conversion for common scenarios:

```csharp
// Using GetValueAs for automatic conversion
var id = row.GetValueAs<long>(0);        // KuzuInt64 -> long
var name = row.GetValueAs<string>(1);    // KuzuString -> string
var age = row.GetValueAs<int>(2);         // KuzuInt32 -> int
var height = row.GetValueAs<float>(3);    // KuzuFloat -> float
var isActive = row.GetValueAs<bool>(4);   // KuzuBool -> bool
```

### POCO Mapping Conversion

When using POCO mapping, KuzuDot automatically converts values:

```csharp
public class Person
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public float Height { get; set; }
    public bool IsActive { get; set; }
    public DateTime BirthDate { get; set; }
}

// Automatic conversion during POCO mapping
var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age, p.height, p.is_active, p.birth_date");
```

### Manual Type Conversion

For complex scenarios, you can manually convert types:

```csharp
using var value = row.GetValue(0);

// Type checking and conversion
if (value is KuzuString stringValue)
{
    string text = stringValue.Value;
}
else if (value is KuzuInt64 intValue)
{
    long number = intValue.Value;
}
else if (value is KuzuDate dateValue)
{
    DateTime date = dateValue.Value;
}
```

## Null Handling

### Checking for Null Values

```csharp
using var value = row.GetValue(0);

if (value.IsNull())
{
    Console.WriteLine("Value is null");
}
else
{
    // Process non-null value
    if (value is KuzuString stringValue)
    {
        Console.WriteLine($"Value: {stringValue.Value}");
    }
}
```

### Setting Null Values

```csharp
// Create a null value
using var nullValue = KuzuValueFactory.CreateNull();

// Set a value to null
using var value = row.GetValue(0);
value.SetNull(true);
```

### Nullable Types in POCOs

```csharp
public class Person
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }        // Nullable int
    public DateTime? BirthDate { get; set; }  // Nullable DateTime
}

// KuzuDot handles nullable types automatically
var people = connection.Query<Person>("MATCH (p:Person) RETURN p.id, p.name, p.age, p.birth_date");
```

## Best Practices

### Resource Management

Always dispose KuzuValue instances:

```csharp
using var result = connection.Query("MATCH (p:Person) RETURN p.name");

while (result.HasNext())
{
    using var row = result.GetNext();
    using var nameValue = row.GetValue<KuzuString>(0);
    
    string name = nameValue.Value;
    // nameValue is automatically disposed
}
```

### Type Safety

Use type checking before casting:

```csharp
using var value = row.GetValue(0);

if (value is KuzuString stringValue)
{
    string text = stringValue.Value;
}
else if (value is KuzuInt64 intValue)
{
    long number = intValue.Value;
}
else
{
    Console.WriteLine($"Unexpected type: {value.DataTypeId}");
}
```

### Performance Considerations

- Use `GetValueAs<T>()` for simple type conversions
- Use POCO mapping for large result sets
- Dispose values promptly to free native resources
- Use prepared statements for repeated operations

### Error Handling

```csharp
try
{
    using var value = row.GetValue(0);
    
    if (value.IsNull())
    {
        Console.WriteLine("Value is null");
        return;
    }
    
    if (value is KuzuString stringValue)
    {
        string text = stringValue.Value;
        // Process text
    }
    else
    {
        Console.WriteLine($"Expected string, got {value.DataTypeId}");
    }
}
catch (InvalidCastException ex)
{
    Console.WriteLine($"Type conversion failed: {ex.Message}");
}
catch (KuzuException ex)
{
    Console.WriteLine($"KuzuDB error: {ex.Message}");
}
```