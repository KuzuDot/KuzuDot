using System;
using System.Collections.Generic;
using System.Linq;
using KuzuDot;
using KuzuDot.Value;

namespace KuzuDot.Examples.Basic
{
    /// <summary>
    /// Data types example demonstrating various KuzuDB data types
    /// </summary>
    public class DataTypes
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Data Types Example ===");
            
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
            // Create an in-memory database
            Console.WriteLine("Creating in-memory database...");
            using var database = Database.FromMemory();
            using var connection = database.Connect();

            // Create schema with various data types
            Console.WriteLine("Creating schema with various data types...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate data type operations
            Console.WriteLine("\n=== Data Type Operations ===");
            DemonstrateDataTypes(connection);

            Console.WriteLine("\n=== Data Types Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            connection.NonQuery(@"
                CREATE NODE TABLE DataTypeDemo(
                    id INT64,
                    name STRING,
                    age INT32,
                    height FLOAT,
                    weight DOUBLE,
                    is_active BOOLEAN,
                    birth_date DATE,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            using var stmt = connection.Prepare(@"
                CREATE (:DataTypeDemo {
                    id: $id,
                    name: $name,
                    age: $age,
                    height: $height,
                    weight: $weight,
                    is_active: $is_active,
                    birth_date: $birth_date,
                    created_at: $created_at
                })");

            var sampleData = new[]
            {
                new {
                    Id = 1L,
                    Name = "Alice",
                    Age = 30,
                    Height = 5.6f,
                    Weight = 140.5,
                    IsActive = true,
                    BirthDate = new DateTime(1994, 3, 15),
                    CreatedAt = DateTime.UtcNow
                },
                new {
                    Id = 2L,
                    Name = "Bob",
                    Age = 25,
                    Height = 6.0f,
                    Weight = 180.0,
                    IsActive = false,
                    BirthDate = new DateTime(1999, 7, 22),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            foreach (var data in sampleData)
            {
                stmt.Bind("id", data.Id);
                stmt.Bind("name", data.Name);
                stmt.Bind("age", data.Age);
                stmt.Bind("height", data.Height);
                stmt.Bind("weight", data.Weight);
                stmt.Bind("is_active", data.IsActive);
                stmt.BindDate("birth_date", data.BirthDate.Date);
                stmt.BindTimestamp("created_at", data.CreatedAt);
                
                stmt.Execute();
                Console.WriteLine($"  Inserted: {data.Name}");
            }
        }

        private static void DemonstrateDataTypes(Connection connection)
        {
            // Query all data types
            Console.WriteLine("1. All data types:");
            using var result = connection.Query(@"
                MATCH (d:DataTypeDemo) 
                RETURN d.id, d.name, d.age, d.height, d.weight, d.is_active, 
                       d.birth_date, d.created_at");

            while (result.HasNext())
            {
                using var row = result.GetNext();
                
                var id = row.GetValueAs<long>(0);
                var name = row.GetValueAs<string>(1);
                var age = row.GetValueAs<int>(2);
                var height = row.GetValueAs<float>(3);
                var weight = row.GetValueAs<double>(4);
                var isActive = row.GetValueAs<bool>(5);
                var birthDate = row.GetValue<KuzuDate>(6).ToString();
                var createdAt = row.GetValueAs<DateTime>(7);
                
                Console.WriteLine($"  ID: {id}, Name: {name}, Age: {age}");
                Console.WriteLine($"    Height: {height:F1}ft, Weight: {weight:F1}lbs, Active: {isActive}");
                Console.WriteLine($"    Birth Date: {birthDate}, Created: {createdAt:yyyy-MM-dd HH:mm:ss}");
            }

            // Demonstrate type-specific queries
            Console.WriteLine("\n2. Type-specific queries:");
            
            // Boolean queries
            Console.WriteLine("   Active users:");
            using var activeResult = connection.Query("MATCH (d:DataTypeDemo) WHERE d.is_active = true RETURN d.name, d.age");
            while (activeResult.HasNext())
            {
                using var row = activeResult.GetNext();
                var name = row.GetValueAs<string>(0);
                var age = row.GetValueAs<int>(1);
                Console.WriteLine($"     {name} (age {age})");
            }

            // Numeric range queries
            Console.WriteLine("   Users aged 25-30:");
            using var ageResult = connection.Query("MATCH (d:DataTypeDemo) WHERE d.age >= 25 AND d.age <= 30 RETURN d.name, d.age");
            while (ageResult.HasNext())
            {
                using var row = ageResult.GetNext();
                var name = row.GetValueAs<string>(0);
                var age = row.GetValueAs<int>(1);
                Console.WriteLine($"     {name} (age {age})");
            }

            // Date queries
            Console.WriteLine("   Users born after 1995:");
            using var dateResult = connection.Query("MATCH (d:DataTypeDemo) WHERE d.birth_date > DATE('1995-01-01') RETURN d.name, d.birth_date");
            while (dateResult.HasNext())
            {
                using var row = dateResult.GetNext();
                var name = row.GetValueAs<string>(0);
                var birthDate = row.GetValue<KuzuDate>(1).ToString();
                Console.WriteLine($"     {name} (born {birthDate})");
            }

            // String pattern matching
            Console.WriteLine("   Users with 'a' in name:");
            using var stringResult = connection.Query("MATCH (d:DataTypeDemo) WHERE d.name CONTAINS 'a' RETURN d.name");
            while (stringResult.HasNext())
            {
                using var row = stringResult.GetNext();
                var name = row.GetValueAs<string>(0);
                Console.WriteLine($"     {name}");
            }

            // Aggregate functions
            Console.WriteLine("\n3. Aggregate functions:");
            var avgAge = connection.ExecuteScalar<double>("MATCH (d:DataTypeDemo) RETURN AVG(d.age)");
            var maxHeight = connection.ExecuteScalar<float>("MATCH (d:DataTypeDemo) RETURN MAX(d.height)");
            var minWeight = connection.ExecuteScalar<double>("MATCH (d:DataTypeDemo) RETURN MIN(d.weight)");
            var count = connection.ExecuteScalar<long>("MATCH (d:DataTypeDemo) RETURN COUNT(d)");
            
            Console.WriteLine($"   Average age: {avgAge:F1}");
            Console.WriteLine($"   Max height: {maxHeight:F1}ft");
            Console.WriteLine($"   Min weight: {minWeight:F1}lbs");
            Console.WriteLine($"   Total count: {count}");

            // Type checking
            Console.WriteLine("\n4. Type checking:");
            using var typeResult = connection.Query("MATCH (d:DataTypeDemo) RETURN d.id, d.name, d.age, d.height, d.weight");
            while (typeResult.HasNext())
            {
                using var row = typeResult.GetNext();
                
                using var idValue = row.GetValue(0);
                using var nameValue = row.GetValue(1);
                using var ageValue = row.GetValue(2);
                using var heightValue = row.GetValue(3);
                using var weightValue = row.GetValue(4);
                
                Console.WriteLine($"   ID type: {idValue.DataTypeId} (KuzuInt64: {idValue is KuzuInt64})");
                Console.WriteLine($"   Name type: {nameValue.DataTypeId} (KuzuString: {nameValue is KuzuString})");
                Console.WriteLine($"   Age type: {ageValue.DataTypeId} (KuzuInt32: {ageValue is KuzuInt32})");
                Console.WriteLine($"   Height type: {heightValue.DataTypeId} (KuzuFloat: {heightValue is KuzuFloat})");
                Console.WriteLine($"   Weight type: {weightValue.DataTypeId} (KuzuDouble: {weightValue is KuzuDouble})");
            }
        }
    }
}
