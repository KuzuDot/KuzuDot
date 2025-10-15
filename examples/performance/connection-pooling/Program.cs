using System;
using KuzuDot;

namespace KuzuDot.Examples.Performance
{
    /// <summary>
    /// Connection pooling example demonstrating managing multiple connections
    /// </summary>
    public class ConnectionPooling
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Connection Pooling Example ===");
            
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
            
            // Create schema
            Console.WriteLine("Creating schema...");
            CreateSchema(database);

            // Demonstrate connection pooling
            Console.WriteLine("\n=== Connection Pooling Examples ===");
            DemonstrateConnectionPooling(database);

            Console.WriteLine("\n=== Connection Pooling Example completed successfully! ===");
        }

        private static void CreateSchema(Database database)
        {
            using var connection = database.Connect();
            connection.NonQuery(@"
                CREATE NODE TABLE Task(
                    id INT64, 
                    name STRING, 
                    priority INT32,
                    status STRING,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Worker(
                    id INT64, 
                    name STRING, 
                    department STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE REL TABLE AssignedTo(
                    FROM Task TO Worker,
                    assigned_at TIMESTAMP
                )");
        }

        private static void DemonstrateConnectionPooling(Database database)
        {
            // 1. Basic connection pooling
            Console.WriteLine("1. Basic connection pooling:");
            await BasicConnectionPooling(database);

            // 2. Concurrent operations
            Console.WriteLine("\n2. Concurrent operations:");
            await ConcurrentOperations(database);

            // 3. Connection lifecycle management
            Console.WriteLine("\n3. Connection lifecycle management:");
            await ConnectionLifecycleManagement(database);

            // 4. Performance comparison
            Console.WriteLine("\n4. Performance comparison:");
            await PerformanceComparison(database);

            // 5. Error handling with multiple connections
            Console.WriteLine("\n5. Error handling with multiple connections:");
            await ErrorHandlingWithConnections(database);
        }

        private static async Task BasicConnectionPooling(Database database)
        {
            const int connectionCount = 5;
            var connections = new List<Connection>();
            
            try
            {
                // Create multiple connections
                for (int i = 0; i < connectionCount; i++)
                {
                    var connection = database.Connect();
                    connections.Add(connection);
                    Console.WriteLine($"  Created connection {i + 1}");
                }

                // Use connections for different operations
                var tasks = new List<Task>();
                for (int i = 0; i < connectionCount; i++)
                {
                    var connectionIndex = i;
                    var task = Task.Run(() =>
                    {
                        var connection = connections[connectionIndex];
                        
                        // Insert tasks
                        for (int j = 0; j < 10; j++)
                        {
                            var taskId = connectionIndex * 10 + j + 1;
                            connection.NonQuery($"CREATE (:Task {{id: {taskId}, name: 'Task{taskId}', priority: {Random.Shared.Next(1, 6)}, status: 'Pending', created_at: datetime()}})");
                        }
                        
                        Console.WriteLine($"  Connection {connectionIndex + 1} inserted 10 tasks");
                    });
                    
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                Console.WriteLine($"  All {connectionCount} connections completed their operations");
            }
            finally
            {
                // Dispose all connections
                foreach (var connection in connections)
                {
                    connection.Dispose();
                }
                Console.WriteLine($"  Disposed {connectionCount} connections");
            }
        }

        private static async Task ConcurrentOperations(Database database)
        {
            const int workerCount = 3;
            var connections = new List<Connection>();
            
            try
            {
                // Create connections for workers
                for (int i = 0; i < workerCount; i++)
                {
                    var connection = database.Connect();
                    connections.Add(connection);
                    
                    // Create worker
                    connection.NonQuery($"CREATE (:Worker {{id: {i + 1}, name: 'Worker{i + 1}', department: 'Dept{(i % 3) + 1}'}})");
                }

                // Simulate concurrent task processing
                var tasks = new List<Task>();
                for (int i = 0; i < workerCount; i++)
                {
                    var connectionIndex = i;
                    var task = Task.Run(async () =>
                    {
                        var connection = connections[connectionIndex];
                        var workerId = connectionIndex + 1;
                        
                        // Process tasks assigned to this worker
                        for (int j = 0; j < 5; j++)
                        {
                            var taskId = connectionIndex * 5 + j + 1;
                            
                            // Assign task to worker
                            connection.NonQuery($"MATCH (t:Task), (w:Worker) WHERE t.id = {taskId} AND w.id = {workerId} CREATE (t)-[:AssignedTo {{assigned_at: datetime()}}]->(w)");
                            
                            // Update task status
                            connection.NonQuery($"MATCH (t:Task) WHERE t.id = {taskId} SET t.status = 'In Progress'");
                            
                            // Simulate work
                            await Task.Delay(100);
                            
                            // Complete task
                            connection.NonQuery($"MATCH (t:Task) WHERE t.id = {taskId} SET t.status = 'Completed'");
                            
                            Console.WriteLine($"  Worker {workerId} completed task {taskId}");
                        }
                    });
                    
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                Console.WriteLine("  All workers completed their tasks");
            }
            finally
            {
                foreach (var connection in connections)
                {
                    connection.Dispose();
                }
            }
        }

        private static async Task ConnectionLifecycleManagement(Database database)
        {
            Console.WriteLine("  Connection lifecycle management:");

            // 1. Using statement (automatic disposal)
            Console.WriteLine("    Using statement example:");
            using (var connection = database.Connect())
            {
                var taskCount = connection.ExecuteScalar<long>("MATCH (t:Task) RETURN COUNT(t)");
                Console.WriteLine($"      Found {taskCount} tasks using using statement");
            }
            Console.WriteLine("      Connection automatically disposed");

            // 2. Manual disposal
            Console.WriteLine("    Manual disposal example:");
            var connection2 = database.Connect();
            try
            {
                var workerCount = connection2.ExecuteScalar<long>("MATCH (w:Worker) RETURN COUNT(w)");
                Console.WriteLine($"      Found {workerCount} workers using manual disposal");
            }
            finally
            {
                connection2.Dispose();
                Console.WriteLine("      Connection manually disposed");
            }

            // 3. Connection reuse
            Console.WriteLine("    Connection reuse example:");
            using var connection3 = database.Connect();
            
            // First operation
            var pendingTasks = connection3.ExecuteScalar<long>("MATCH (t:Task) WHERE t.status = 'Pending' RETURN COUNT(t)");
            Console.WriteLine($"      Pending tasks: {pendingTasks}");
            
            // Second operation
            var completedTasks = connection3.ExecuteScalar<long>("MATCH (t:Task) WHERE t.status = 'Completed' RETURN COUNT(t)");
            Console.WriteLine($"      Completed tasks: {completedTasks}");
            
            // Third operation
            var inProgressTasks = connection3.ExecuteScalar<long>("MATCH (t:Task) WHERE t.status = 'In Progress' RETURN COUNT(t)");
            Console.WriteLine($"      In progress tasks: {inProgressTasks}");
            
            Console.WriteLine("      Same connection used for multiple operations");
        }

        private static async Task PerformanceComparison(Database database)
        {
            Console.WriteLine("  Performance comparison - Single vs Multiple connections:");

            const int operationCount = 100;

            // Single connection
            var singleConnectionStart = DateTime.UtcNow;
            using var singleConnection = database.Connect();
            for (int i = 0; i < operationCount; i++)
            {
                singleConnection.Query("MATCH (t:Task) RETURN COUNT(t)");
            }
            var singleConnectionTime = DateTime.UtcNow - singleConnectionStart;

            // Multiple connections
            const int connectionCount = 5;
            var multipleConnectionStart = DateTime.UtcNow;
            var connections = new List<Connection>();
            
            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    connections.Add(database.Connect());
                }

                var tasks = new List<Task>();
                for (int i = 0; i < connectionCount; i++)
                {
                    var connectionIndex = i;
                    var task = Task.Run(() =>
                    {
                        var connection = connections[connectionIndex];
                        for (int j = 0; j < operationCount / connectionCount; j++)
                        {
                            connection.Query("MATCH (t:Task) RETURN COUNT(t)");
                        }
                    });
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                foreach (var connection in connections)
                {
                    connection.Dispose();
                }
            }
            
            var multipleConnectionTime = DateTime.UtcNow - multipleConnectionStart;

            Console.WriteLine($"    Single connection ({operationCount} operations): {singleConnectionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"    Multiple connections ({operationCount} operations): {multipleConnectionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"    Performance ratio: {(singleConnectionTime.TotalMilliseconds / multipleConnectionTime.TotalMilliseconds):F1}x");
        }

        private static async Task ErrorHandlingWithConnections(Database database)
        {
            Console.WriteLine("  Error handling with multiple connections:");

            var connections = new List<Connection>();
            
            try
            {
                // Create multiple connections
                for (int i = 0; i < 3; i++)
                {
                    connections.Add(database.Connect());
                }

                // Simulate operations with potential errors
                var tasks = new List<Task>();
                for (int i = 0; i < 3; i++)
                {
                    var connectionIndex = i;
                    var task = Task.Run(() =>
                    {
                        var connection = connections[connectionIndex];
                        
                        try
                        {
                            if (connectionIndex == 1)
                            {
                                // This will cause an error
                                connection.Query("MATCH (n:NonExistentTable) RETURN n");
                            }
                            else
                            {
                                // This will succeed
                                var count = connection.ExecuteScalar<long>("MATCH (t:Task) RETURN COUNT(t)");
                                Console.WriteLine($"      Connection {connectionIndex + 1} successfully counted {count} tasks");
                            }
                        }
                        catch (KuzuException ex)
                        {
                            Console.WriteLine($"      Connection {connectionIndex + 1} caught error: {ex.Message}");
                        }
                    });
                    
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                Console.WriteLine("      All connections handled errors gracefully");
            }
            finally
            {
                foreach (var connection in connections)
                {
                    connection.Dispose();
                }
                Console.WriteLine("      All connections disposed after error handling");
            }
        }
    }
}
