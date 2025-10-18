using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KuzuDot;

namespace KuzuDot.Examples.Performance
{
    /// <summary>
    /// Connection pooling example demonstrating managing multiple connections
    /// </summary>
    public class ConnectionPooling
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Connection Pooling Example ===");
            
            try
            {
                await RunExample();
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

        private static async Task RunExample()
        {
            // Create an in-memory database
            Console.WriteLine("Creating in-memory database...");
            using var database = Database.FromMemory();
            
            // Create schema
            Console.WriteLine("Creating schema...");
            CreateSchema(database);

            // Demonstrate connection pooling
            Console.WriteLine("\n=== Connection Pooling Examples ===");
            await DemonstrateConnectionPooling(database);

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

        private static async Task DemonstrateConnectionPooling(Database database)
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

                // Use connections sequentially (KuzuDB only allows one write transaction at a time)
                for (int i = 0; i < connectionCount; i++)
                {
                    var connection = connections[i];
                    
                    // Insert tasks using prepared statement
                    using var taskStmt = connection.Prepare(@"
                        CREATE (:Task {
                            id: $id, 
                            name: $name, 
                            priority: $priority,
                            status: $status,
                            created_at: $created_at
                        })");
                    
                    for (int j = 0; j < 10; j++)
                    {
                        var taskId = i * 10 + j + 1;
                        taskStmt.Bind("id", taskId);
                        taskStmt.Bind("name", $"Task{taskId}");
                        taskStmt.Bind("priority", Random.Shared.Next(1, 6));
                        taskStmt.Bind("status", "Pending");
                        taskStmt.BindTimestamp("created_at", DateTime.UtcNow);
                        taskStmt.Execute();
                    }
                    
                    Console.WriteLine($"  Connection {i + 1} inserted 10 tasks");
                }
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

                // Simulate sequential task processing (KuzuDB only allows one write transaction at a time)
                for (int i = 0; i < workerCount; i++)
                {
                    var connection = connections[i];
                    var workerId = i + 1;
                    
                    // Process tasks assigned to this worker
                    using var assignStmt = connection.Prepare(@"
                        MATCH (t:Task), (w:Worker) 
                        WHERE t.id = $task_id AND w.id = $worker_id 
                        CREATE (t)-[:AssignedTo {assigned_at: $assigned_at}]->(w)");
                    
                    using var updateStatusStmt = connection.Prepare(@"
                        MATCH (t:Task) 
                        WHERE t.id = $task_id 
                        SET t.status = $status");
                    
                    for (int j = 0; j < 5; j++)
                    {
                        var taskId = i * 5 + j + 1;
                        
                        // Assign task to worker
                        assignStmt.Bind("task_id", taskId);
                        assignStmt.Bind("worker_id", workerId);
                        assignStmt.BindTimestamp("assigned_at", DateTime.UtcNow);
                        assignStmt.Execute();
                        
                        // Update task status
                        updateStatusStmt.Bind("task_id", taskId);
                        updateStatusStmt.Bind("status", "In Progress");
                        updateStatusStmt.Execute();
                        
                        // Simulate work
                        await Task.Delay(100);
                        
                        // Complete task
                        updateStatusStmt.Bind("task_id", taskId);
                        updateStatusStmt.Bind("status", "Completed");
                        updateStatusStmt.Execute();
                        
                        Console.WriteLine($"  Worker {workerId} completed task {taskId}");
                    }
                }
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
