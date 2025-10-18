using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KuzuDot;

namespace KuzuDot.Examples.RealWorld
{
    /// <summary>
    /// Network analysis example demonstrating analyzing network topologies
    /// </summary>
    public class NetworkAnalysis
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Network Analysis Example ===");
            
            try
            {
                await RunExample();
            }
            catch (KuzuException ex)
            {
                Console.WriteLine($"KuzuDB Error: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Binding Error: {ex.Message}");
                Console.WriteLine("This indicates an incorrect usage of PreparedStatement.Bind()");
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
            using var connection = database.Connect();

            // Create schema
            Console.WriteLine("Creating network analysis schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate network analysis
            Console.WriteLine("\n=== Network Analysis Examples ===");
            await DemonstrateNetworkAnalysis(connection);

            Console.WriteLine("\n=== Network Analysis Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            connection.NonQuery(@"
                CREATE NODE TABLE Node(
                    id INT64, 
                    name STRING, 
                    node_type STRING,
                    capacity DOUBLE,
                    location STRING,
                    status STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Router(
                    id INT64, 
                    name STRING, 
                    model STRING,
                    ip_address STRING,
                    location STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Switch(
                    id INT64, 
                    name STRING, 
                    model STRING,
                    port_count INT32,
                    location STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Server(
                    id INT64, 
                    name STRING, 
                    os STRING,
                    cpu_cores INT32,
                    memory_gb INT32,
                    location STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE User(
                    id INT64, 
                    username STRING, 
                    department STRING,
                    role STRING,
                    last_login TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            // Create relationship tables
            connection.NonQuery(@"
                CREATE REL TABLE ConnectedTo(
                    FROM Router TO Router,
                    connection_type STRING,
                    bandwidth DOUBLE,
                    latency DOUBLE,
                    status STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE SwitchToRouter(
                    FROM Switch TO Router,
                    connection_type STRING,
                    bandwidth DOUBLE,
                    latency DOUBLE,
                    status STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE ServerToSwitch(
                    FROM Server TO Switch,
                    connection_type STRING,
                    bandwidth DOUBLE,
                    latency DOUBLE,
                    status STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE RoutesThrough(
                    FROM Router TO Router,
                    hop_count INT32,
                    cost DOUBLE,
                    status STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE HostsOn(
                    FROM Server TO Switch,
                    port_number INT32,
                    vlan_id INT32,
                    status STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Accesses(
                    FROM User TO Server,
                    access_type STRING,
                    timestamp TIMESTAMP,
                    duration_minutes INT32
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Manages(
                    FROM User TO Node,
                    permission_level STRING,
                    since TIMESTAMP
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert routers
            using var routerStmt = connection.Prepare(@"
                CREATE (:Router {
                    id: $id, 
                    name: $name, 
                    model: $model,
                    ip_address: $ip_address,
                    location: $location
                })");

            var routers = GenerateRouters(20);
            var routerCount = routerStmt.BindAndExecuteBatch(routers);
            Console.WriteLine($"  Inserted {routerCount} routers");

            // Insert switches
            using var switchStmt = connection.Prepare(@"
                CREATE (:Switch {
                    id: $id, 
                    name: $name, 
                    model: $model,
                    port_count: $port_count,
                    location: $location
                })");

            var switches = GenerateSwitches(30);
            var switchCount = switchStmt.BindAndExecuteBatch(switches);
            Console.WriteLine($"  Inserted {switchCount} switches");

            // Insert servers
            using var serverStmt = connection.Prepare(@"
                CREATE (:Server {
                    id: $id, 
                    name: $name, 
                    os: $os,
                    cpu_cores: $cpu_cores,
                    memory_gb: $memory_gb,
                    location: $location
                })");

            var servers = GenerateServers(50);
            var serverCount = serverStmt.BindAndExecuteBatch(servers);
            Console.WriteLine($"  Inserted {serverCount} servers");

            // Insert users
            var users = GenerateUsers(100);
            using var userStmt = connection.Prepare(@"
                CREATE (:User {
                    id: $id, 
                    username: $username, 
                    department: $department,
                    role: $role,
                    last_login: $last_login
                })");

            var userCount = userStmt.BindAndExecuteBatch(users);
            Console.WriteLine($"  Inserted {userCount} users");

            Console.WriteLine("  Created routers, switches, servers, and users");

            // Create relationships
            CreateRelationships(connection);
        }

        private static void CreateRelationships(Connection connection)
        {
            Console.WriteLine("Creating relationships...");

            // Router-Router connections
            using var routerConnectionStmt = connection.Prepare(@"
                MATCH (r1:Router), (r2:Router) 
                WHERE r1.id = $router_id1 AND r2.id = $router_id2 
                CREATE (r1)-[:ConnectedTo {connection_type: 'Router', bandwidth: $bandwidth, latency: $latency, status: $status}]->(r2)");

            for (int i = 1; i <= 20; i++)
            {
                for (int j = i + 1; j <= Math.Min(i + 3, 20); j++)
                {
                    routerConnectionStmt.Bind("router_id1", i);
                    routerConnectionStmt.Bind("router_id2", j);
                    routerConnectionStmt.Bind("bandwidth", Random.Shared.NextDouble() * 1000 + 100);
                    routerConnectionStmt.Bind("latency", Random.Shared.NextDouble() * 10 + 1);
                    routerConnectionStmt.Bind("status", "Active");
                    routerConnectionStmt.Execute();
                }
            }

            // Switch-Router connections
            using var switchRouterStmt = connection.Prepare(@"
                MATCH (s:Switch), (r:Router) 
                WHERE s.id = $switch_id AND r.id = $router_id 
                CREATE (s)-[:SwitchToRouter {connection_type: 'Router', bandwidth: $bandwidth, latency: $latency, status: $status}]->(r)");

            for (int i = 1; i <= 30; i++)
            {
                var routerId = Random.Shared.Next(1, 21);
                switchRouterStmt.Bind("switch_id", i);
                switchRouterStmt.Bind("router_id", routerId);
                switchRouterStmt.Bind("bandwidth", Random.Shared.NextDouble() * 100 + 10);
                switchRouterStmt.Bind("latency", Random.Shared.NextDouble() * 5 + 0.5);
                switchRouterStmt.Bind("status", "Active");
                switchRouterStmt.Execute();
            }

            // Server-Switch connections
            using var serverSwitchStmt = connection.Prepare(@"
                MATCH (s:Server), (sw:Switch) 
                WHERE s.id = $server_id AND sw.id = $switch_id 
                CREATE (s)-[:ServerToSwitch {connection_type: 'Switch', bandwidth: $bandwidth, latency: $latency, status: $status}]->(sw)");

            for (int i = 1; i <= 50; i++)
            {
                var switchId = Random.Shared.Next(1, 31);
                serverSwitchStmt.Bind("server_id", i);
                serverSwitchStmt.Bind("switch_id", switchId);
                serverSwitchStmt.Bind("bandwidth", Random.Shared.NextDouble() * 10 + 1);
                serverSwitchStmt.Bind("latency", Random.Shared.NextDouble() * 2 + 0.1);
                serverSwitchStmt.Bind("status", "Active");
                serverSwitchStmt.Execute();
            }

            // User-Server access relationships
            using var userAccessStmt = connection.Prepare(@"
                MATCH (u:User), (s:Server) 
                WHERE u.id = $user_id AND s.id = $server_id 
                CREATE (u)-[:Accesses {access_type: $access_type, timestamp: $timestamp, duration_minutes: $duration}]->(s)");

            for (int i = 1; i <= 100; i++)
            {
                var serverCount = Random.Shared.Next(1, 6);
                for (int j = 0; j < serverCount; j++)
                {
                    var serverId = Random.Shared.Next(1, 51);
                    var accessType = Random.Shared.Next(0, 2) == 0 ? "SSH" : "HTTP";
                    var timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(30));
                    var duration = Random.Shared.Next(5, 120);

                    userAccessStmt.Bind("user_id", i);
                    userAccessStmt.Bind("server_id", serverId);
                    userAccessStmt.Bind("access_type", accessType);
                    userAccessStmt.BindTimestamp("timestamp", timestamp);
                    userAccessStmt.Bind("duration", duration);
                    userAccessStmt.Execute();
                }
            }

            // User-Node management relationships
            using var userManageStmt = connection.Prepare(@"
                MATCH (u:User), (n:Node) 
                WHERE u.id = $user_id AND n.id = $node_id 
                CREATE (u)-[:Manages {permission_level: $permission_level, since: $since}]->(n)");

            for (int i = 1; i <= 100; i++)
            {
                var nodeCount = Random.Shared.Next(1, 4);
                for (int j = 0; j < nodeCount; j++)
                {
                    var nodeId = Random.Shared.Next(1, 101);
                    var permissionLevel = Random.Shared.Next(0, 3) switch
                    {
                        0 => "Read",
                        1 => "Write",
                        _ => "Admin"
                    };
                    var since = DateTime.UtcNow.AddDays(-Random.Shared.Next(365));

                    userManageStmt.Bind("user_id", i);
                    userManageStmt.Bind("node_id", nodeId);
                    userManageStmt.Bind("permission_level", permissionLevel);
                    userManageStmt.BindTimestamp("since", since);
                    userManageStmt.Execute();
                }
            }

            Console.WriteLine("  Created all relationships");
        }

        private static async Task DemonstrateNetworkAnalysis(Connection connection)
        {
            // 1. Network topology analysis
            Console.WriteLine("1. Network topology analysis:");
            await AnalyzeNetworkTopology(connection);

            // 2. Connectivity analysis
            Console.WriteLine("\n2. Connectivity analysis:");
            await AnalyzeConnectivity(connection);

            // 3. Performance analysis
            Console.WriteLine("\n3. Performance analysis:");
            await AnalyzePerformance(connection);

            // 4. Security analysis
            Console.WriteLine("\n4. Security analysis:");
            await AnalyzeSecurity(connection);

            // 5. Capacity planning
            Console.WriteLine("\n5. Capacity planning:");
            await AnalyzeCapacity(connection);

            // 6. Fault tolerance analysis
            Console.WriteLine("\n6. Fault tolerance analysis:");
            await AnalyzeFaultTolerance(connection);

            // 7. User behavior analysis
            Console.WriteLine("\n7. User behavior analysis:");
            await AnalyzeUserBehavior(connection);

            // 8. Network optimization
            Console.WriteLine("\n8. Network optimization:");
            await AnalyzeNetworkOptimization(connection);
        }

        private static async Task AnalyzeNetworkTopology(Connection connection)
        {
            // Find network hubs (nodes with most connections)
            using var hubsResult = connection.Query(@"
                MATCH (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                RETURN n.name, LABELS(n)[0] as node_type, COUNT(connected) as connection_count
                ORDER BY connection_count DESC
                LIMIT 10");

            Console.WriteLine("  Network hubs (most connected nodes):");
            while (hubsResult.HasNext())
            {
                using var row = hubsResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var nodeType = row.GetValueAs<string>(1);
                var connectionCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {nodeName} ({nodeType}): {connectionCount} connections");
            }

            // Find isolated nodes
            using var isolatedResult = connection.Query(@"
                MATCH (n)
                WHERE NOT (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->() 
                AND NOT ()-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n)
                RETURN n.name, LABELS(n)[0] as node_type
                LIMIT 10");

            Console.WriteLine("  Isolated nodes:");
            while (isolatedResult.HasNext())
            {
                using var row = isolatedResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var nodeType = row.GetValueAs<string>(1);
                
                Console.WriteLine($"    {nodeName} ({nodeType})");
            }

            // Find network segments
            using var segmentsResult = connection.Query(@"
                MATCH (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, COLLECT(connected) as connections
                WHERE SIZE(connections) > 5
                RETURN n.name, SIZE(connections) as segment_size
                ORDER BY segment_size DESC
                LIMIT 10");

            Console.WriteLine("  Network segments:");
            while (segmentsResult.HasNext())
            {
                using var row = segmentsResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var segmentSize = row.GetValueAs<long>(1);
                
                Console.WriteLine($"    {nodeName}: {segmentSize} nodes in segment");
            }
        }

        private static async Task AnalyzeConnectivity(Connection connection)
        {
            // Find shortest paths between routers
            using var shortestPathResult = connection.Query(@"
                MATCH path = (r1:Router)-[:ConnectedTo*1..3]->(r2:Router)
                WHERE r1.id = 1 AND r2.id = 10
                RETURN LENGTH(path) as path_length
                ORDER BY path_length
                LIMIT 5");

            Console.WriteLine("  Shortest paths between routers 1 and 10:");
            while (shortestPathResult.HasNext())
            {
                using var row = shortestPathResult.GetNext();
                var pathLength = row.GetValueAs<long>(0);
                
                Console.WriteLine($"    Path length: {pathLength}");
            }

            // Find critical nodes (nodes whose removal would disconnect the network)
            using var criticalResult = connection.Query(@"
                MATCH (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, COUNT(connected) as outbound_connections
                MATCH (connected)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n)
                WITH n, outbound_connections, COUNT(connected) as inbound_connections
                WHERE outbound_connections > 3 OR inbound_connections > 3
                RETURN n.name, outbound_connections, inbound_connections
                ORDER BY (outbound_connections + inbound_connections) DESC
                LIMIT 10");

            Console.WriteLine("  Critical nodes (high connectivity):");
            while (criticalResult.HasNext())
            {
                using var row = criticalResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var outboundConnections = row.GetValueAs<long>(1);
                var inboundConnections = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {nodeName}: {outboundConnections} outbound, {inboundConnections} inbound");
            }

            // Find network bridges
            using var bridgesResult = connection.Query(@"
                MATCH (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, connected, COUNT(*) as connection_count
                WHERE connection_count = 1
                RETURN n.name, connected.name, connection_count
                LIMIT 10");

            Console.WriteLine("  Network bridges (single connections):");
            while (bridgesResult.HasNext())
            {
                using var row = bridgesResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var connectedName = row.GetValueAs<string>(1);
                var connectionCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {nodeName} -> {connectedName} ({connectionCount} connection)");
            }
        }

        private static async Task AnalyzePerformance(Connection connection)
        {
            // Find high-latency connections
            using var latencyResult = connection.Query(@"
                MATCH (n1)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n2)
                WHERE c.latency > 5.0
                RETURN n1.name, n2.name, c.latency, c.bandwidth
                ORDER BY c.latency DESC
                LIMIT 10");

            Console.WriteLine("  High-latency connections:");
            while (latencyResult.HasNext())
            {
                using var row = latencyResult.GetNext();
                var node1Name = row.GetValueAs<string>(0);
                var node2Name = row.GetValueAs<string>(1);
                var latency = row.GetValueAs<double>(2);
                var bandwidth = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {node1Name} -> {node2Name}: {latency:F2}ms latency, {bandwidth:F0}Mbps");
            }

            // Find low-bandwidth connections
            using var bandwidthResult = connection.Query(@"
                MATCH (n1)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n2)
                WHERE c.bandwidth < 50.0
                RETURN n1.name, n2.name, c.bandwidth, c.latency
                ORDER BY c.bandwidth ASC
                LIMIT 10");

            Console.WriteLine("  Low-bandwidth connections:");
            while (bandwidthResult.HasNext())
            {
                using var row = bandwidthResult.GetNext();
                var node1Name = row.GetValueAs<string>(0);
                var node2Name = row.GetValueAs<string>(1);
                var bandwidth = row.GetValueAs<double>(2);
                var latency = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {node1Name} -> {node2Name}: {bandwidth:F0}Mbps bandwidth, {latency:F2}ms latency");
            }

            // Find performance bottlenecks
            using var bottleneckResult = connection.Query(@"
                MATCH (n)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, AVG(c.latency) as avg_latency, AVG(c.bandwidth) as avg_bandwidth
                WHERE avg_latency > 3.0 OR avg_bandwidth < 100.0
                RETURN n.name, avg_latency, avg_bandwidth
                ORDER BY avg_latency DESC
                LIMIT 10");

            Console.WriteLine("  Performance bottlenecks:");
            while (bottleneckResult.HasNext())
            {
                using var row = bottleneckResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var avgLatency = row.GetValueAs<double>(1);
                var avgBandwidth = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {nodeName}: {avgLatency:F2}ms avg latency, {avgBandwidth:F0}Mbps avg bandwidth");
            }
        }

        private static async Task AnalyzeSecurity(Connection connection)
        {
            // Find users with admin access
            using var adminResult = connection.Query(@"
                MATCH (u:User)-[m:Manages]->(n:Node)
                WHERE m.permission_level = 'Admin'
                RETURN u.username, u.department, COUNT(n) as admin_nodes
                ORDER BY admin_nodes DESC
                LIMIT 10");

            Console.WriteLine("  Users with admin access:");
            while (adminResult.HasNext())
            {
                using var row = adminResult.GetNext();
                var username = row.GetValueAs<string>(0);
                var department = row.GetValueAs<string>(1);
                var adminNodes = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {username} ({department}): {adminNodes} admin nodes");
            }

            // Find users with access to multiple servers
            using var multiServerResult = connection.Query(@"
                MATCH (u:User)-[:Accesses]->(s:Server)
                WITH u, COUNT(s) as server_count
                WHERE server_count > 5
                RETURN u.username, u.department, server_count
                ORDER BY server_count DESC
                LIMIT 10");

            Console.WriteLine("  Users with access to multiple servers:");
            while (multiServerResult.HasNext())
            {
                using var row = multiServerResult.GetNext();
                var username = row.GetValueAs<string>(0);
                var department = row.GetValueAs<string>(1);
                var serverCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {username} ({department}): {serverCount} servers");
            }

            // Find inactive users (simplified)
            using var inactiveResult = connection.Query(@"
                MATCH (u:User)
                RETURN u.username, u.department, u.last_login
                ORDER BY u.last_login ASC
                LIMIT 10");

            Console.WriteLine("  Inactive users (no login in 30 days):");
            while (inactiveResult.HasNext())
            {
                using var row = inactiveResult.GetNext();
                var username = row.GetValueAs<string>(0);
                var department = row.GetValueAs<string>(1);
                var lastLogin = row.GetValueAs<DateTime>(2);
                
                Console.WriteLine($"    {username} ({department}): last login {lastLogin:yyyy-MM-dd}");
            }
        }

        private static async Task AnalyzeCapacity(Connection connection)
        {
            // Find servers with high CPU usage
            using var cpuResult = connection.Query(@"
                MATCH (s:Server)
                WHERE s.cpu_cores < 8
                RETURN s.name, s.cpu_cores, s.memory_gb, s.location
                ORDER BY s.cpu_cores ASC
                LIMIT 10");

            Console.WriteLine("  Servers with low CPU capacity:");
            while (cpuResult.HasNext())
            {
                using var row = cpuResult.GetNext();
                var serverName = row.GetValueAs<string>(0);
                var cpuCores = row.GetValueAs<int>(1);
                var memoryGb = row.GetValueAs<int>(2);
                var location = row.GetValueAs<string>(3);
                
                Console.WriteLine($"    {serverName} ({location}): {cpuCores} cores, {memoryGb}GB RAM");
            }

            // Find switches with low port capacity
            using var portResult = connection.Query(@"
                MATCH (s:Switch)
                WHERE s.port_count < 24
                RETURN s.name, s.port_count, s.location
                ORDER BY s.port_count ASC
                LIMIT 10");

            Console.WriteLine("  Switches with low port capacity:");
            while (portResult.HasNext())
            {
                using var row = portResult.GetNext();
                var switchName = row.GetValueAs<string>(0);
                var portCount = row.GetValueAs<int>(1);
                var location = row.GetValueAs<string>(2);
                
                Console.WriteLine($"    {switchName} ({location}): {portCount} ports");
            }

            // Find network capacity utilization
            using var capacityResult = connection.Query(@"
                MATCH (n1)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n2)
                WITH n1, AVG(c.bandwidth) as avg_bandwidth
                WHERE avg_bandwidth < 100.0
                RETURN n1.name, avg_bandwidth
                ORDER BY avg_bandwidth ASC
                LIMIT 10");

            Console.WriteLine("  Nodes with low average bandwidth:");
            while (capacityResult.HasNext())
            {
                using var row = capacityResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var avgBandwidth = row.GetValueAs<double>(1);
                
                Console.WriteLine($"    {nodeName}: {avgBandwidth:F0}Mbps avg bandwidth");
            }
        }

        private static async Task AnalyzeFaultTolerance(Connection connection)
        {
            // Find single points of failure
            using var spofResult = connection.Query(@"
                MATCH (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, COUNT(connected) as outbound_connections
                MATCH (connected)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n)
                WITH n, outbound_connections, COUNT(connected) as inbound_connections
                WHERE outbound_connections = 1 OR inbound_connections = 1
                RETURN n.name, outbound_connections, inbound_connections
                ORDER BY (outbound_connections + inbound_connections) ASC
                LIMIT 10");

            Console.WriteLine("  Single points of failure:");
            while (spofResult.HasNext())
            {
                using var row = spofResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var outboundConnections = row.GetValueAs<long>(1);
                var inboundConnections = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {nodeName}: {outboundConnections} outbound, {inboundConnections} inbound");
            }

            // Find redundant connections
            using var redundantResult = connection.Query(@"
                MATCH (n1)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n2)
                MATCH (n2)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n1)
                RETURN n1.name, n2.name, 'Bidirectional' as connection_type
                LIMIT 10");

            Console.WriteLine("  Redundant connections:");
            while (redundantResult.HasNext())
            {
                using var row = redundantResult.GetNext();
                var node1Name = row.GetValueAs<string>(0);
                var node2Name = row.GetValueAs<string>(1);
                var connectionType = row.GetValueAs<string>(2);
                
                Console.WriteLine($"    {node1Name} <-> {node2Name} ({connectionType})");
            }

            // Find network resilience
            using var resilienceResult = connection.Query(@"
                MATCH (n)-[:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, COUNT(connected) as connection_count
                WHERE connection_count > 3
                RETURN n.name, connection_count
                ORDER BY connection_count DESC
                LIMIT 10");

            Console.WriteLine("  Highly resilient nodes:");
            while (resilienceResult.HasNext())
            {
                using var row = resilienceResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var connectionCount = row.GetValueAs<long>(1);
                
                Console.WriteLine($"    {nodeName}: {connectionCount} connections");
            }
        }

        private static async Task AnalyzeUserBehavior(Connection connection)
        {
            // Find most active users
            using var activeResult = connection.Query(@"
                MATCH (u:User)-[:Accesses]->(s:Server)
                WITH u, COUNT(s) as access_count
                RETURN u.username, u.department, access_count
                ORDER BY access_count DESC
                LIMIT 10");

            Console.WriteLine("  Most active users:");
            while (activeResult.HasNext())
            {
                using var row = activeResult.GetNext();
                var username = row.GetValueAs<string>(0);
                var department = row.GetValueAs<string>(1);
                var accessCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {username} ({department}): {accessCount} server accesses");
            }

            // Find users by department activity
            using var deptResult = connection.Query(@"
                MATCH (u:User)-[:Accesses]->(s:Server)
                RETURN u.department, COUNT(s) as total_accesses, COUNT(DISTINCT u) as user_count
                ORDER BY total_accesses DESC");

            Console.WriteLine("  Department activity:");
            while (deptResult.HasNext())
            {
                using var row = deptResult.GetNext();
                var department = row.GetValueAs<string>(0);
                var totalAccesses = row.GetValueAs<long>(1);
                var userCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {department}: {totalAccesses} accesses by {userCount} users");
            }

            // Find access patterns
            using var patternResult = connection.Query(@"
                MATCH (u:User)-[a:Accesses]->(s:Server)
                RETURN a.access_type, COUNT(a) as access_count, AVG(a.duration_minutes) as avg_duration
                ORDER BY access_count DESC");

            Console.WriteLine("  Access patterns:");
            while (patternResult.HasNext())
            {
                using var row = patternResult.GetNext();
                var accessType = row.GetValueAs<string>(0);
                var accessCount = row.GetValueAs<long>(1);
                var avgDuration = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {accessType}: {accessCount} accesses, {avgDuration:F1} minutes avg duration");
            }
        }

        private static async Task AnalyzeNetworkOptimization(Connection connection)
        {
            // Find optimization opportunities
            using var optimizationResult = connection.Query(@"
                MATCH (n1)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(n2)
                WHERE c.latency > 5.0 AND c.bandwidth < 100.0
                RETURN n1.name, n2.name, c.latency, c.bandwidth,
                       (c.latency * c.bandwidth) as optimization_score
                ORDER BY optimization_score DESC
                LIMIT 10");

            Console.WriteLine("  Optimization opportunities:");
            while (optimizationResult.HasNext())
            {
                using var row = optimizationResult.GetNext();
                var node1Name = row.GetValueAs<string>(0);
                var node2Name = row.GetValueAs<string>(1);
                var latency = row.GetValueAs<double>(2);
                var bandwidth = row.GetValueAs<double>(3);
                var optimizationScore = row.GetValueAs<double>(4);
                
                Console.WriteLine($"    {node1Name} -> {node2Name}: {latency:F2}ms, {bandwidth:F0}Mbps (score: {optimizationScore:F1})");
            }

            // Find load balancing opportunities
            using var loadBalanceResult = connection.Query(@"
                MATCH (n)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, COUNT(connected) as connection_count, AVG(c.bandwidth) as avg_bandwidth
                WHERE connection_count > 5 AND avg_bandwidth < 50.0
                RETURN n.name, connection_count, avg_bandwidth
                ORDER BY connection_count DESC
                LIMIT 10");

            Console.WriteLine("  Load balancing opportunities:");
            while (loadBalanceResult.HasNext())
            {
                using var row = loadBalanceResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var connectionCount = row.GetValueAs<long>(1);
                var avgBandwidth = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {nodeName}: {connectionCount} connections, {avgBandwidth:F0}Mbps avg bandwidth");
            }

            // Find network efficiency metrics
            using var efficiencyResult = connection.Query(@"
                MATCH (n)-[c:ConnectedTo|:SwitchToRouter|:ServerToSwitch]->(connected)
                WITH n, COUNT(connected) as connection_count, 
                     AVG(c.bandwidth) as avg_bandwidth, 
                     AVG(c.latency) as avg_latency
                RETURN n.name, connection_count, avg_bandwidth, avg_latency,
                       (avg_bandwidth / avg_latency) as efficiency_ratio
                ORDER BY efficiency_ratio DESC
                LIMIT 10");

            Console.WriteLine("  Network efficiency metrics:");
            while (efficiencyResult.HasNext())
            {
                using var row = efficiencyResult.GetNext();
                var nodeName = row.GetValueAs<string>(0);
                var connectionCount = row.GetValueAs<long>(1);
                var avgBandwidth = row.GetValueAs<double>(2);
                var avgLatency = row.GetValueAs<double>(3);
                var efficiencyRatio = row.GetValueAs<double>(4);
                
                Console.WriteLine($"    {nodeName}: {connectionCount} connections, {avgBandwidth:F0}Mbps/{avgLatency:F2}ms (ratio: {efficiencyRatio:F1})");
            }
        }

        // Helper methods to generate test data
        private static List<RouterData> GenerateRouters(int count)
        {
            var routers = new List<RouterData>();
            var models = new[] { "Cisco 2901", "Juniper MX240", "Huawei NE40E", "Arista 7280" };
            var locations = new[] { "Data Center A", "Data Center B", "Office Building 1", "Office Building 2", "Remote Site 1" };

            for (int i = 1; i <= count; i++)
            {
                routers.Add(new RouterData
                {
                    Id = i,
                    Name = $"Router{i}",
                    Model = models[i % models.Length],
                    IpAddress = $"10.0.{i / 255}.{i % 255}",
                    Location = locations[i % locations.Length]
                });
            }

            return routers;
        }

        private static List<SwitchData> GenerateSwitches(int count)
        {
            var switches = new List<SwitchData>();
            var models = new[] { "Cisco 2960", "HP ProCurve", "Juniper EX2300", "Arista 7050" };
            var locations = new[] { "Data Center A", "Data Center B", "Office Building 1", "Office Building 2", "Remote Site 1" };

            for (int i = 1; i <= count; i++)
            {
                switches.Add(new SwitchData
                {
                    Id = i,
                    Name = $"Switch{i}",
                    Model = models[i % models.Length],
                    PortCount = Random.Shared.Next(8, 49),
                    Location = locations[i % locations.Length]
                });
            }

            return switches;
        }

        private static List<ServerData> GenerateServers(int count)
        {
            var servers = new List<ServerData>();
            var osTypes = new[] { "Linux", "Windows", "Unix", "FreeBSD" };
            var locations = new[] { "Data Center A", "Data Center B", "Office Building 1", "Office Building 2", "Remote Site 1" };

            for (int i = 1; i <= count; i++)
            {
                servers.Add(new ServerData
                {
                    Id = i,
                    Name = $"Server{i}",
                    Os = osTypes[i % osTypes.Length],
                    CpuCores = Random.Shared.Next(2, 32),
                    MemoryGb = Random.Shared.Next(4, 128),
                    Location = locations[i % locations.Length]
                });
            }

            return servers;
        }

        private static List<UserData> GenerateUsers(int count)
        {
            var users = new List<UserData>();
            var departments = new[] { "IT", "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations" };
            var roles = new[] { "Admin", "User", "Manager", "Developer", "Analyst" };

            for (int i = 1; i <= count; i++)
            {
                users.Add(new UserData
                {
                    Id = i,
                    Username = $"user{i}",
                    Department = departments[i % departments.Length],
                    Role = roles[i % roles.Length],
                    LastLogin = DateTime.UtcNow.AddDays(-Random.Shared.Next(30))
                });
            }

            return users;
        }
    }

    // Data classes
    public class RouterData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class SwitchData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int PortCount { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    public class ServerData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
        public int CpuCores { get; set; }
        public int MemoryGb { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    public class UserData
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
    }
}
