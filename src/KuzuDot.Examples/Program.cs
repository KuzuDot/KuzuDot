namespace KuzuDot.Examples
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            ExampleLog.Title("KuzuDot Example Console Application");
            ExampleLog.Info("Demonstrating KuzuDB .NET Integration");

            try
            {
                ExampleLog.Section("Running Basic Examples");
                RunBasicExample();
                RunSocialNetworkExample();
                RunPreparedStatementExample();
                RunDataTypesExample();

                ExampleLog.Section("Running Advanced Examples");
                AdvancedExamples.ErrorHandlingExample();
                AdvancedExamples.BatchOperationsExample();
                AdvancedExamples.ComplexGraphQueriesExample();
                AdvancedExamples.SchemaEvolutionExample();

                ExampleLog.Section("Running Safe Usage Examples");
                SafeUsageExample.BasicUsageExample();
                SafeUsageExample.ExceptionSafetyExample();
                SafeUsageExample.MultipleConnectionsExample();
                SafeUsageExample.ManualDisposalPattern();

                ExampleLog.Section("Running Comprehensive New Features Example");
                ComprehensiveExample.Run();

                ExampleLog.Success("All Examples Completed Successfully");
            }
            catch (KuzuException ex)
            {
                ExampleLog.Error($"KuzuDB Error: {ex.Message}");
                ExampleLog.Info("Ensure kuzu_shared native library is accessible in output directory");
            }
            catch (InvalidOperationException ex)
            {
                ExampleLog.Error($"Invalid operation: {ex.Message}");
            }

            ExampleLog.Info("Press any key to exit...");
            Console.ReadKey();
        }

        static void RunBasicExample()
        {
            ExampleLog.Title("Basic Example");
            using var database = Database.FromMemory();
            using var connection = database.Connect();
            connection.NonQuery(
                "CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))");
            ExampleLog.Success("Created Person node table");
            connection.NonQuery("CREATE (:Person {name: 'Alice', age: 30})");
            ExampleLog.Success("Inserted Alice");
            connection.NonQuery("CREATE (:Person {name: 'Bob', age: 25})");
            ExampleLog.Success("Inserted Bob");
            // Ergonomic: scalar query
            long count = connection.ExecuteScalar<long>("MATCH (p:Person) RETURN COUNT(*)");
            ExampleLog.Info($"Person count (ExecuteScalar): {count}");
            // Ergonomic: POCO mapping
            var people = connection.Query<Person>("MATCH (p:Person) RETURN p.name AS name, p.age AS age");
            foreach (var p in people) ExampleLog.Info($"Person: {p.name}, Age: {p.age}");
            // Ergonomic2: POCO mapping
            var pet = connection.Query<Pet>("MATCH (p:Person) RETURN p");
            foreach (var p in pet) ExampleLog.Info($"Pet: {p.name}, Age: {p.age}");
            // Async query example
            var asyncTask = connection.QueryAsync("MATCH (p:Person) RETURN p.name, p.age");
            asyncTask.Wait();
            using var asyncResult = asyncTask.Result;
            ExampleLog.Info("Async query result (first row):");
            if (asyncResult.HasNext())
            {
                using var row = asyncResult.GetNext();
                ExampleLog.Info($"Name: {row.GetValue(0)}, Age: {row.GetValue(1)}");
            }
            ExampleLog.Separator();
        }

        static void RunSocialNetworkExample()
        {
            ExampleLog.Title("Social Network Example");
            using var database = Database.FromMemory();
            using var connection = database.Connect();
            connection.NonQuery(
                "CREATE NODE TABLE User(id INT64, name STRING, email STRING, PRIMARY KEY(id))");
            connection.NonQuery(
                "CREATE NODE TABLE Post(id INT64, title STRING, content STRING, timestamp INT64, PRIMARY KEY(id))");
            ExampleLog.Success("Created User and Post node tables");
            connection.NonQuery("CREATE REL TABLE Follows(FROM User TO User, since INT64)");
            connection.NonQuery("CREATE REL TABLE Authored(FROM User TO Post)");
            connection.NonQuery("CREATE REL TABLE Likes(FROM User TO Post, timestamp INT64)");
            ExampleLog.Success("Created relationship tables");
            var users = new[] { (1, "Alice Johnson", "alice@example.com"), (2, "Bob Smith", "bob@example.com"), (3, "Carol Davis", "carol@example.com"), (4, "David Wilson", "david@example.com") };
            foreach (var (id, name, email) in users) using (var r = connection.Query($"CREATE (:User {{id: {id}, name: '{name}', email: '{email}'}})")) { }
            ExampleLog.Success("Inserted sample users");
            var posts = new[] { (101, "Introduction to Graph Databases", "Graph databases are powerful for connected data...", 1640995200), (102, "KuzuDB Performance Tips", "Here are some tips to optimize your Kuzu queries...", 1641081600), (103, "Building Social Networks", "Social networks are natural graph structures...", 1641168000) };
            foreach (var (id, title, content, timestamp) in posts) using (var r = connection.Query($"CREATE (:Post {{id: {id}, title: '{title}', content: '{content}', timestamp: {timestamp}}})")) { }
            ExampleLog.Success("Inserted sample posts");
            connection.NonQuery("MATCH (u:User {id: 1}), (p:Post {id: 101}) CREATE (u)-[:Authored]->(p)");
            connection.NonQuery("MATCH (u:User {id: 2}), (p:Post {id: 102}) CREATE (u)-[:Authored]->(p)");
            connection.NonQuery("MATCH (u:User {id: 1}), (p:Post {id: 103}) CREATE (u)-[:Authored]->(p)");
            connection.NonQuery("MATCH (u1:User {id: 2}), (u2:User {id: 1}) CREATE (u1)-[:Follows {since: 1640908800}]->(u2)");
            connection.NonQuery("MATCH (u1:User {id: 3}), (u2:User {id: 1}) CREATE (u1)-[:Follows {since: 1640908800}]->(u2)");
            connection.NonQuery("MATCH (u1:User {id: 4}), (u2:User {id: 2}) CREATE (u1)-[:Follows {since: 1640908800}]->(u2)");
            ExampleLog.Success("Created relationships");
            connection.NonQuery(@"MATCH (follower:User)-[:Follows]->(author:User)-[:Authored]->(post:Post) RETURN follower.name, author.name, post.title");
            ExampleLog.Success("Executed social network query");
            ExampleLog.Separator();
        }

        static void RunPreparedStatementExample()
        {
            ExampleLog.Title("Prepared Statement Example");
            using var database = Database.FromMemory();
            using var connection = database.Connect();
            connection.NonQuery(
                "CREATE NODE TABLE Product(id INT64, name STRING, price STRING, category STRING, PRIMARY KEY(id))");
            ExampleLog.Success("Created Product table");
            using var insertStatement = connection.Prepare(
                "CREATE (:Product {id: $id, name: $name, price: $price, category: $category})");
            ExampleLog.Success("Prepared insert statement");
            var products = new[] { (1, "Laptop", "999.99", "Electronics"), (2, "Coffee Mug", "12.50", "Kitchen"), (3, "Running Shoes", "89.95", "Sports"), (4, "Desk Chair", "199.00", "Furniture"), (5, "Smartphone", "699.99", "Electronics") };
            foreach (var (id, name, price, category) in products) 
            { 
                insertStatement.BindInt64("id", id); 
                insertStatement.BindString("name", name); 
                insertStatement.BindString("price", price); 
                insertStatement.BindString("category", category); 
                using var r = insertStatement.Execute(); 
                ExampleLog.Info($"Inserted {name}"); 
            }
            using var queryStatement = connection.Prepare("MATCH (p:Product) WHERE p.category = $category RETURN p.name, p.price");
            queryStatement.BindString("category", "Electronics");
            using var queryResult = queryStatement.Execute();
            ExampleLog.Success("Executed parameterized query for Electronics products");
            ExampleLog.Separator();
        }

        static void RunDataTypesExample()
        {
            ExampleLog.Title("Data Types Example");
            using var database = Database.FromMemory();
            using var connection = database.Connect();
            connection.NonQuery(@"CREATE NODE TABLE DataSample(id INT64, name STRING, score STRING, is_active BOOLEAN, created_date STRING, updated_timestamp STRING, PRIMARY KEY(id))");
            ExampleLog.Success("Created DataSample table");
            using var insertStatement = connection.Prepare(@"CREATE (:DataSample { id: $id, name: $name, score: $score, is_active: $is_active, created_date: $created_date, updated_timestamp: $updated_timestamp })");
            var samples = new[] { (1L, "Sample A", "95.5", true, "2024-01-15", "2024-01-15 10:30:00"), (2L, "Sample B", "87.2", false, "2024-01-16", "2024-01-16 14:45:30"), (3L, "Sample C", "92.8", true, "2024-01-17", "2024-01-17 09:15:45") };
            foreach (var (id, name, score, isActive, createdDate, updatedTs) in samples) { insertStatement.BindInt64("id", id); insertStatement.BindString("name", name); insertStatement.BindString("score", score); insertStatement.BindBool("is_active", isActive); insertStatement.BindString("created_date", createdDate); insertStatement.BindString("updated_timestamp", updatedTs); using var r = insertStatement.Execute(); ExampleLog.Info($"Inserted {name}"); }
            connection.NonQuery(@"MATCH (d:DataSample) WHERE d.is_active = true RETURN d.name, d.score, d.created_date");
            ExampleLog.Success("Executed boolean-filtered query");
            connection.NonQuery(@"MATCH (d:DataSample) WHERE d.created_date >= '2024-01-16' RETURN d.name, d.created_date");
            ExampleLog.Success("Executed date-filtered query (string comparison)");
            ExampleLog.Separator();
        }
    }
}