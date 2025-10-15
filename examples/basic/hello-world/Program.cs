using System;
using KuzuDot;
using KuzuDot.Value;

namespace KuzuDot.Examples.Basic
{
    /// <summary>
    /// Hello World example demonstrating basic KuzuDot operations
    /// </summary>
    public class HelloWorld
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Hello World Example ===");
            
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

            // Display version information
            Console.WriteLine($"KuzuDB Version: {Version.GetVersion()}");
            Console.WriteLine($"Storage Version: {Version.GetStorageVersion()}");

            // Create a simple table
            Console.WriteLine("\nCreating Person table...");
            connection.NonQuery("CREATE NODE TABLE Person(name STRING, age INT32, city STRING, PRIMARY KEY(name))");

            // Insert some sample data
            Console.WriteLine("Inserting sample data...");
            var people = new[]
            {
                new { Name = "Alice", Age = 30, City = "New York" },
                new { Name = "Bob", Age = 25, City = "San Francisco" },
                new { Name = "Charlie", Age = 35, City = "Chicago" },
                new { Name = "Diana", Age = 28, City = "Boston" }
            };

            using var insertStmt = connection.Prepare("CREATE (:Person {name: $name, age: $age, city: $city})");
            foreach (var person in people)
            {
                insertStmt.Bind("name", person.Name);
                insertStmt.Bind("age", person.Age);
                insertStmt.Bind("city", person.City);
                insertStmt.Execute();
                Console.WriteLine($"  Inserted: {person.Name}");
            }

            // Query all persons
            Console.WriteLine("\nQuerying all persons:");
            using var result = connection.Query("MATCH (p:Person) RETURN p.name, p.age, p.city ORDER BY p.age");
            
            while (result.HasNext())
            {
                using var row = result.GetNext();
                var name = row.GetValueAs<string>(0);
                var age = row.GetValueAs<int>(1);
                var city = row.GetValueAs<string>(2);
                
                Console.WriteLine($"  {name} is {age} years old and lives in {city}");
            }

            // Query with conditions
            Console.WriteLine("\nPersons over 30:");
            using var olderResult = connection.Query("MATCH (p:Person) WHERE p.age > 30 RETURN p.name, p.age");
            
            while (olderResult.HasNext())
            {
                using var row = olderResult.GetNext();
                var name = row.GetValueAs<string>(0);
                var age = row.GetValueAs<int>(1);
                
                Console.WriteLine($"  {name} is {age} years old");
            }

            // Get statistics
            Console.WriteLine("\nStatistics:");
            using var countResult = connection.Query("MATCH (p:Person) RETURN COUNT(p)");
            long totalPersons = 0;
            if (countResult.HasNext())
            {
                using var countRow = countResult.GetNext();
                totalPersons = countRow.GetValueAs<long>(0);
            }
            
            using var avgResult = connection.Query("MATCH (p:Person) RETURN AVG(p.age)");
            double averageAge = 0;
            if (avgResult.HasNext())
            {
                using var avgRow = avgResult.GetNext();
                averageAge = avgRow.GetValueAs<double>(0);
            }
            
            using var maxResult = connection.Query("MATCH (p:Person) RETURN MAX(p.age)");
            int maxAge = 0;
            if (maxResult.HasNext())
            {
                using var maxRow = maxResult.GetNext();
                maxAge = maxRow.GetValueAs<int>(0);
            }
            
            using var minResult = connection.Query("MATCH (p:Person) RETURN MIN(p.age)");
            int minAge = 0;
            if (minResult.HasNext())
            {
                using var minRow = minResult.GetNext();
                minAge = minRow.GetValueAs<int>(0);
            }

            Console.WriteLine($"  Total persons: {totalPersons}");
            Console.WriteLine($"  Average age: {averageAge:F1}");
            Console.WriteLine($"  Max age: {maxAge}");
            Console.WriteLine($"  Min age: {minAge}");

            Console.WriteLine("\n=== Example completed successfully! ===");
        }
    }
}
