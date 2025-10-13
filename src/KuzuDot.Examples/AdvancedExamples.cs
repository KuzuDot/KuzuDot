namespace KuzuDot.Examples
{
    /// <summary>
    /// Advanced examples demonstrating more complex KuzuDot usage patterns
    /// including error handling, transaction-like operations, and performance considerations.
    /// </summary>
    internal static class AdvancedExamples
    {
        /// <summary>
        /// Demonstrates comprehensive error handling patterns
        /// </summary>
        public static void ErrorHandlingExample()
        {
            ExampleLog.Title("Advanced Error Handling Example");

            try
            {
                using var db = Database.FromMemory();
                using var conn = db.Connect();

                // Example 1: Handling table already exists
                try
                {
                    conn.NonQuery("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name))");
                    ExampleLog.Success("Created Person table");

                    // This will fail because table already exists
                    conn.NonQuery("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name))");
                }
                catch (KuzuException ex)
                {
                    ExampleLog.Success($"Expected error creating duplicate table: {ex.Message}");
                }

                // Example 2: Using IF NOT EXISTS for idempotent operations
                conn.NonQuery("CREATE NODE TABLE IF NOT EXISTS Person(name STRING, PRIMARY KEY(name))");
                ExampleLog.Success("Used IF NOT EXISTS for safe table creation");

                // Example 3: Handling constraint violations
                try
                {
                    conn.NonQuery("CREATE (:Person {name: 'Alice'})");
                    ExampleLog.Success("Inserted Alice");

                    // This will fail due to primary key constraint
                    conn.NonQuery("CREATE (:Person {name: 'Alice'})");
                }
                catch (KuzuException ex)
                {
                    ExampleLog.Success($"Expected primary key violation: {ex.Message}");
                }

                ExampleLog.Success("Error handling example completed successfully");
            }
            catch (KuzuException ex)
            {
                // Top-level Kuzu exceptions reported distinctly
                ExampleLog.Error($"Top-level Kuzu error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                ExampleLog.Error($"Invalid operation: {ex.Message}");
            }
            ExampleLog.Separator();
        }

        /// <summary>
        /// Demonstrates batch operations and performance considerations
        /// </summary>
        public static void BatchOperationsExample()
        {
            ExampleLog.Title("Batch Operations Example");

            using var database = Database.FromMemory();
            using var conn = database.Connect();

            // Setup
            conn.NonQuery(
                "CREATE NODE TABLE IF NOT EXISTS BatchTest(id INT64, data STRING, PRIMARY KEY(id))");
            ExampleLog.Success("Created BatchTest table");

            // Example 1: Efficient batch insert using prepared statements
            using var insertStatement = conn.Prepare("CREATE (:BatchTest {id: $id, data: $data})");

            var startTime = DateTime.UtcNow;
            const int batchSize = 100;

            for (int i = 1; i <= batchSize; i++)
            {
                insertStatement.BindInt64("id", i);
                insertStatement.BindString("data", $"Data item {i}");

                using var result = insertStatement.Execute();

                if (i % 20 == 0)
                {
                    ExampleLog.Info($"Inserted {i} records...");
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            ExampleLog.Success($"Batch insert completed: {batchSize} records in {elapsed.TotalMilliseconds:F0}ms");

            // Example 2: Batch query operations

            // Ergonomic: ExecuteScalar
            long total = conn.ExecuteScalar<long>("MATCH (b:BatchTest) RETURN COUNT(*)");
            ExampleLog.Success($"Counted total records using ExecuteScalar: {total}");

            // Example 3: Range-based queries
            // Async query example
            var asyncTask = conn.QueryAsync("MATCH (b:BatchTest) WHERE b.id >= 50 AND b.id <= 70 RETURN b.id, b.data");
            asyncTask.Wait();
            using var asyncResult = asyncTask.Result;
            ExampleLog.Success("Executed async range query (ID 50-70)");

            ExampleLog.Separator();
        }

        /// <summary>
        /// Demonstrates complex relationship queries and graph traversal
        /// </summary>
        public static void ComplexGraphQueriesExample()
        {
            ExampleLog.Title("Complex Graph Queries Example");

            using var db = Database.FromMemory();
            using var conn = db.Connect();

            // Setup a more complex graph schema
            conn.NonQuery(@"
                CREATE NODE TABLE IF NOT EXISTS Company(
                    id INT64, 
                    name STRING, 
                    industry STRING, 
                    PRIMARY KEY(id)
                )");

            conn.NonQuery(@"
                CREATE NODE TABLE IF NOT EXISTS Employee(
                    id INT64, 
                    name STRING, 
                    role STRING, 
                    PRIMARY KEY(id)
                )");

            conn.NonQuery(@"
                CREATE REL TABLE IF NOT EXISTS WorksFor(
                    FROM Employee TO Company, 
                    start_date STRING, 
                    salary INT64
                )");

            conn.NonQuery(@"
                CREATE REL TABLE IF NOT EXISTS Manages(
                    FROM Employee TO Employee, 
                    since STRING
                )");

            ExampleLog.Success("Created complex graph schema (Company, Employee, relationships)");

            // Insert sample data
            var companies = new[]
            {
                (1, "TechCorp", "Technology"),
                (2, "DataSys", "Technology"),
                (3, "RetailPlus", "Retail")
            };

            foreach (var (id, name, industry) in companies)
            {
                conn.NonQuery($"CREATE (:Company {{id: {id}, name: '{name}', industry: '{industry}'}})");
            }

            var employees = new[]
            {
                (1, "Alice Manager", "Manager"),
                (2, "Bob Developer", "Developer"),
                (3, "Carol Analyst", "Analyst"),
                (4, "Dave Developer", "Developer"),
                (5, "Eve Manager", "Manager")
            };

            foreach (var (id, name, role) in employees)
            {
                conn.NonQuery($"CREATE (:Employee {{id: {id}, name: '{name}', role: '{role}'}})");
            }

            ExampleLog.Success("Inserted sample companies and employees");

            // Create relationships
            conn.NonQuery(@"
                MATCH (e:Employee), (c:Company) 
                WHERE (e.id = 1 AND c.id = 1) OR 
                      (e.id = 2 AND c.id = 1) OR 
                      (e.id = 3 AND c.id = 1) OR 
                      (e.id = 4 AND c.id = 2) OR 
                      (e.id = 5 AND c.id = 2)
                CREATE (e)-[:WorksFor {start_date: '2023-01-01', salary: 75000}]->(c)");

            conn.NonQuery(@"
                MATCH (m:Employee), (e:Employee) 
                WHERE (m.id = 1 AND e.id = 2) OR 
                      (m.id = 1 AND e.id = 3) OR 
                      (m.id = 5 AND e.id = 4)
                CREATE (m)-[:Manages {since: '2023-06-01'}]->(e)");

            ExampleLog.Success("Created work and management relationships");

            // Complex query examples
            conn.NonQuery(@"
                MATCH (manager:Employee)-[:Manages]->(employee:Employee)-[:WorksFor]->(company:Company)
                RETURN manager.name, employee.name, company.name");
            ExampleLog.Success("Executed hierarchy query: managers -> employees -> companies");

            conn.NonQuery(@"
                MATCH (e:Employee)-[:WorksFor]->(c:Company)
                WHERE c.industry = 'Technology'
                RETURN c.name, COUNT(e) as employee_count");
            ExampleLog.Success("Executed aggregation query: employee count by tech company");

            conn.NonQuery(@"
                MATCH (e:Employee)
                WHERE e.role = 'Manager' AND EXISTS {
                    MATCH (e)-[:Manages]->(:Employee)
                }
                RETURN e.name");
            ExampleLog.Success("Executed exists query: managers who actually manage people");

            ExampleLog.Separator();
        }

        /// <summary>
        /// Demonstrates schema evolution and data migration patterns
        /// </summary>
        public static void SchemaEvolutionExample()
        {
            ExampleLog.Title("Schema Evolution Example");

            using var database = Database.FromMemory();
            using var connection = database.Connect();

            // Initial schema
            connection.NonQuery(@"
                CREATE NODE TABLE IF NOT EXISTS Product_V1(
                    id INT64, 
                    name STRING, 
                    price STRING, 
                    PRIMARY KEY(id)
                )");
            ExampleLog.Success("Created initial schema (Product_V1)");

            // Insert some initial data
            connection.NonQuery(@"
                CREATE (:Product_V1 {id: 1, name: 'Widget A', price: '19.99'})");
            ExampleLog.Success("Inserted initial data");

            // Schema evolution: Add new fields
            connection.NonQuery(@"
                CREATE NODE TABLE IF NOT EXISTS Product_V2(
                    id INT64, 
                    name STRING, 
                    price STRING, 
                    category STRING, 
                    is_active BOOLEAN, 
                    PRIMARY KEY(id)
                )");
            ExampleLog.Success("Created evolved schema (Product_V2)");

            // Data migration pattern
            connection.NonQuery(@"
                MATCH (p1:Product_V1)
                CREATE (:Product_V2 {
                    id: p1.id, 
                    name: p1.name, 
                    price: p1.price, 
                    category: 'General', 
                    is_active: true
                })");
            ExampleLog.Success("Migrated data from V1 to V2 with default values");

            // Verify migration
            connection.NonQuery(@"
                MATCH (p:Product_V2) 
                RETURN p.id, p.name, p.category, p.is_active");
            ExampleLog.Success("Verified migrated data");

            ExampleLog.Success("Schema evolution example completed");
            ExampleLog.Separator();
        }
    }
}