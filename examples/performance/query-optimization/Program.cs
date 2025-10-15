using System;
using KuzuDot;

namespace KuzuDot.Examples.Performance
{
    /// <summary>
    /// Query optimization example demonstrating optimizing query performance
    /// </summary>
    public class QueryOptimization
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Query Optimization Example ===");
            
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

            // Create schema
            Console.WriteLine("Creating schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate query optimization
            Console.WriteLine("\n=== Query Optimization Examples ===");
            DemonstrateQueryOptimization(connection);

            Console.WriteLine("\n=== Query Optimization Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            connection.NonQuery(@"
                CREATE NODE TABLE Person(
                    id INT64, 
                    name STRING, 
                    age INT32,
                    city STRING,
                    country STRING,
                    email STRING,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Company(
                    id INT64, 
                    name STRING, 
                    industry STRING,
                    size STRING,
                    founded_year INT32,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Project(
                    id INT64, 
                    name STRING, 
                    status STRING,
                    budget DOUBLE,
                    start_date DATE,
                    end_date DATE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE REL TABLE WorksFor(
                    FROM Person TO Company,
                    position STRING,
                    salary DOUBLE,
                    start_date DATE
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Manages(
                    FROM Person TO Person,
                    since DATE
                )");

            connection.NonQuery(@"
                CREATE REL TABLE WorksOn(
                    FROM Person TO Project,
                    role STRING,
                    hours_per_week INT32
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Sponsors(
                    FROM Company TO Project,
                    investment DOUBLE
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert persons
            var persons = GeneratePersons(1000);
            using var personStmt = connection.Prepare(@"
                CREATE (:Person {
                    id: $id, 
                    name: $name, 
                    age: $age,
                    city: $city,
                    country: $country,
                    email: $email,
                    created_at: $created_at
                })");

            foreach (var person in persons)
            {
                personStmt.Bind(person);
                personStmt.Execute();
            }

            // Insert companies
            var companies = GenerateCompanies(100);
            using var companyStmt = connection.Prepare(@"
                CREATE (:Company {
                    id: $id, 
                    name: $name, 
                    industry: $industry,
                    size: $size,
                    founded_year: $founded_year
                })");

            foreach (var company in companies)
            {
                companyStmt.Bind(company);
                companyStmt.Execute();
            }

            // Insert projects
            var projects = GenerateProjects(200);
            using var projectStmt = connection.Prepare(@"
                CREATE (:Project {
                    id: $id, 
                    name: $name, 
                    status: $status,
                    budget: $budget,
                    start_date: $start_date,
                    end_date: $end_date
                })");

            foreach (var project in projects)
            {
                projectStmt.Bind(project);
                projectStmt.Execute();
            }

            // Create relationships
            CreateRelationships(connection);

            Console.WriteLine("  Created 1000 persons, 100 companies, 200 projects, and relationships");
        }

        private static void CreateRelationships(Connection connection)
        {
            // WorksFor relationships
            using var worksForStmt = connection.Prepare(@"
                MATCH (p:Person), (c:Company) 
                WHERE p.id = $person_id AND c.id = $company_id 
                CREATE (p)-[:WorksFor {position: $position, salary: $salary, start_date: $start_date}]->(c)");

            for (int i = 1; i <= 1000; i++)
            {
                var companyId = (i % 100) + 1;
                var position = GetRandomPosition();
                var salary = Random.Shared.NextDouble() * 100000 + 30000;
                var startDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(365 * 5));

                worksForStmt.Bind("person_id", i);
                worksForStmt.Bind("company_id", companyId);
                worksForStmt.Bind("position", position);
                worksForStmt.Bind("salary", salary);
                worksForStmt.BindDate("start_date", startDate);
                worksForStmt.Execute();
            }

            // Manages relationships
            using var managesStmt = connection.Prepare(@"
                MATCH (m:Person), (e:Person) 
                WHERE m.id = $manager_id AND e.id = $employee_id 
                CREATE (m)-[:Manages {since: $since}]->(e)");

            for (int i = 1; i <= 100; i++)
            {
                var managerId = i;
                var employeeId = i + 100;
                var since = DateTime.UtcNow.AddDays(-Random.Shared.Next(365 * 2));

                managesStmt.Bind("manager_id", managerId);
                managesStmt.Bind("employee_id", employeeId);
                managesStmt.BindDate("since", since);
                managesStmt.Execute();
            }

            // WorksOn relationships
            using var worksOnStmt = connection.Prepare(@"
                MATCH (p:Person), (pr:Project) 
                WHERE p.id = $person_id AND pr.id = $project_id 
                CREATE (p)-[:WorksOn {role: $role, hours_per_week: $hours}]->(pr)");

            for (int i = 1; i <= 1000; i++)
            {
                var projectCount = Random.Shared.Next(1, 4);
                for (int j = 0; j < projectCount; j++)
                {
                    var projectId = Random.Shared.Next(1, 201);
                    var role = GetRandomRole();
                    var hours = Random.Shared.Next(10, 40);

                    worksOnStmt.Bind("person_id", i);
                    worksOnStmt.Bind("project_id", projectId);
                    worksOnStmt.Bind("role", role);
                    worksOnStmt.Bind("hours", hours);
                    worksOnStmt.Execute();
                }
            }

            // Sponsors relationships
            using var sponsorsStmt = connection.Prepare(@"
                MATCH (c:Company), (pr:Project) 
                WHERE c.id = $company_id AND pr.id = $project_id 
                CREATE (c)-[:Sponsors {investment: $investment}]->(pr)");

            for (int i = 1; i <= 200; i++)
            {
                var companyId = Random.Shared.Next(1, 101);
                var investment = Random.Shared.NextDouble() * 1000000 + 100000;

                sponsorsStmt.Bind("company_id", companyId);
                sponsorsStmt.Bind("project_id", i);
                sponsorsStmt.Bind("investment", investment);
                sponsorsStmt.Execute();
            }
        }

        private static void DemonstrateQueryOptimization(Connection connection)
        {
            // 1. Index usage optimization
            Console.WriteLine("1. Index usage optimization:");
            await IndexUsageOptimization(connection);

            // 2. Query structure optimization
            Console.WriteLine("\n2. Query structure optimization:");
            await QueryStructureOptimization(connection);

            // 3. Join optimization
            Console.WriteLine("\n3. Join optimization:");
            await JoinOptimization(connection);

            // 4. Aggregation optimization
            Console.WriteLine("\n4. Aggregation optimization:");
            await AggregationOptimization(connection);

            // 5. Subquery optimization
            Console.WriteLine("\n5. Subquery optimization:");
            await SubqueryOptimization(connection);

            // 6. Performance monitoring
            Console.WriteLine("\n6. Performance monitoring:");
            await PerformanceMonitoring(connection);
        }

        private static async Task IndexUsageOptimization(Connection connection)
        {
            // Query without optimization
            var startTime = DateTime.UtcNow;
            using var unoptimizedResult = connection.Query(@"
                MATCH (p:Person) 
                WHERE p.age > 30 AND p.city = 'New York' 
                RETURN p.name, p.age");
            
            var unoptimizedCount = 0;
            while (unoptimizedResult.HasNext())
            {
                unoptimizedResult.GetNext();
                unoptimizedCount++;
            }
            var unoptimizedTime = DateTime.UtcNow - startTime;

            // Query with optimization (using primary key)
            startTime = DateTime.UtcNow;
            using var optimizedResult = connection.Query(@"
                MATCH (p:Person) 
                WHERE p.id IN [1, 2, 3, 4, 5] AND p.age > 30 
                RETURN p.name, p.age");
            
            var optimizedCount = 0;
            while (optimizedResult.HasNext())
            {
                optimizedResult.GetNext();
                optimizedCount++;
            }
            var optimizedTime = DateTime.UtcNow - startTime;

            Console.WriteLine($"  Unoptimized query: {unoptimizedTime.TotalMilliseconds:F2}ms, {unoptimizedCount} results");
            Console.WriteLine($"  Optimized query: {optimizedTime.TotalMilliseconds:F2}ms, {optimizedCount} results");
            Console.WriteLine($"  Performance improvement: {(unoptimizedTime.TotalMilliseconds / optimizedTime.TotalMilliseconds):F1}x faster");
        }

        private static async Task QueryStructureOptimization(Connection connection)
        {
            // Inefficient query structure
            var startTime = DateTime.UtcNow;
            using var inefficientResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company)
                WHERE p.age > 25 AND p.age < 35 AND c.industry = 'Technology'
                RETURN p.name, c.name, p.age");
            
            var inefficientCount = 0;
            while (inefficientResult.HasNext())
            {
                inefficientResult.GetNext();
                inefficientCount++;
            }
            var inefficientTime = DateTime.UtcNow - startTime;

            // Efficient query structure
            startTime = DateTime.UtcNow;
            using var efficientResult = connection.Query(@"
                MATCH (c:Company)
                WHERE c.industry = 'Technology'
                MATCH (p:Person)-[:WorksFor]->(c)
                WHERE p.age > 25 AND p.age < 35
                RETURN p.name, c.name, p.age");
            
            var efficientCount = 0;
            while (efficientResult.HasNext())
            {
                efficientResult.GetNext();
                efficientCount++;
            }
            var efficientTime = DateTime.UtcNow - startTime;

            Console.WriteLine($"  Inefficient structure: {inefficientTime.TotalMilliseconds:F2}ms, {inefficientCount} results");
            Console.WriteLine($"  Efficient structure: {efficientTime.TotalMilliseconds:F2}ms, {efficientCount} results");
            Console.WriteLine($"  Performance improvement: {(inefficientTime.TotalMilliseconds / efficientTime.TotalMilliseconds):F1}x faster");
        }

        private static async Task JoinOptimization(Connection connection)
        {
            // Multiple joins without optimization
            var startTime = DateTime.UtcNow;
            using var unoptimizedJoinResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company),
                      (p)-[:WorksOn]->(pr:Project),
                      (c)-[:Sponsors]->(pr)
                RETURN p.name, c.name, pr.name, pr.budget");
            
            var unoptimizedJoinCount = 0;
            while (unoptimizedJoinResult.HasNext())
            {
                unoptimizedJoinResult.GetNext();
                unoptimizedJoinCount++;
            }
            var unoptimizedJoinTime = DateTime.UtcNow - startTime;

            // Optimized joins with filtering
            startTime = DateTime.UtcNow;
            using var optimizedJoinResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company)
                WHERE c.industry = 'Technology'
                MATCH (p)-[:WorksOn]->(pr:Project)
                WHERE pr.budget > 100000
                MATCH (c)-[:Sponsors]->(pr)
                RETURN p.name, c.name, pr.name, pr.budget");
            
            var optimizedJoinCount = 0;
            while (optimizedJoinResult.HasNext())
            {
                optimizedJoinResult.GetNext();
                optimizedJoinCount++;
            }
            var optimizedJoinTime = DateTime.UtcNow - startTime;

            Console.WriteLine($"  Unoptimized joins: {unoptimizedJoinTime.TotalMilliseconds:F2}ms, {unoptimizedJoinCount} results");
            Console.WriteLine($"  Optimized joins: {optimizedJoinTime.TotalMilliseconds:F2}ms, {optimizedJoinCount} results");
            Console.WriteLine($"  Performance improvement: {(unoptimizedJoinTime.TotalMilliseconds / optimizedJoinTime.TotalMilliseconds):F1}x faster");
        }

        private static async Task AggregationOptimization(Connection connection)
        {
            // Inefficient aggregation
            var startTime = DateTime.UtcNow;
            using var inefficientAggResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company)
                RETURN c.name, COUNT(p) as employee_count, AVG(p.age) as avg_age");
            
            var inefficientAggCount = 0;
            while (inefficientAggResult.HasNext())
            {
                inefficientAggResult.GetNext();
                inefficientAggCount++;
            }
            var inefficientAggTime = DateTime.UtcNow - startTime;

            // Efficient aggregation with filtering
            startTime = DateTime.UtcNow;
            using var efficientAggResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company)
                WHERE p.age > 25
                RETURN c.name, COUNT(p) as employee_count, AVG(p.age) as avg_age
                ORDER BY employee_count DESC
                LIMIT 10");
            
            var efficientAggCount = 0;
            while (efficientAggResult.HasNext())
            {
                efficientAggResult.GetNext();
                efficientAggCount++;
            }
            var efficientAggTime = DateTime.UtcNow - startTime;

            Console.WriteLine($"  Inefficient aggregation: {inefficientAggTime.TotalMilliseconds:F2}ms, {inefficientAggCount} results");
            Console.WriteLine($"  Efficient aggregation: {efficientAggTime.TotalMilliseconds:F2}ms, {efficientAggCount} results");
            Console.WriteLine($"  Performance improvement: {(inefficientAggTime.TotalMilliseconds / efficientAggTime.TotalMilliseconds):F1}x faster");
        }

        private static async Task SubqueryOptimization(Connection connection)
        {
            // Inefficient subquery
            var startTime = DateTime.UtcNow;
            using var inefficientSubResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company)
                WHERE c.industry IN ['Technology', 'Finance', 'Healthcare']
                RETURN p.name, c.name, p.age
                ORDER BY p.age DESC");
            
            var inefficientSubCount = 0;
            while (inefficientSubResult.HasNext())
            {
                inefficientSubResult.GetNext();
                inefficientSubCount++;
            }
            var inefficientSubTime = DateTime.UtcNow - startTime;

            // Efficient query without subquery
            startTime = DateTime.UtcNow;
            using var efficientSubResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company)
                WHERE c.industry = 'Technology' OR c.industry = 'Finance' OR c.industry = 'Healthcare'
                RETURN p.name, c.name, p.age
                ORDER BY p.age DESC
                LIMIT 50");
            
            var efficientSubCount = 0;
            while (efficientSubResult.HasNext())
            {
                efficientSubResult.GetNext();
                efficientSubCount++;
            }
            var efficientSubTime = DateTime.UtcNow - startTime;

            Console.WriteLine($"  Inefficient subquery: {inefficientSubTime.TotalMilliseconds:F2}ms, {inefficientSubCount} results");
            Console.WriteLine($"  Efficient query: {efficientSubTime.TotalMilliseconds:F2}ms, {efficientSubCount} results");
            Console.WriteLine($"  Performance improvement: {(inefficientSubTime.TotalMilliseconds / efficientSubTime.TotalMilliseconds):F1}x faster");
        }

        private static async Task PerformanceMonitoring(Connection connection)
        {
            Console.WriteLine("  Performance monitoring examples:");

            // Query execution time monitoring
            var queries = new[]
            {
                "MATCH (p:Person) RETURN COUNT(p)",
                "MATCH (c:Company) RETURN COUNT(c)",
                "MATCH (pr:Project) RETURN COUNT(pr)",
                "MATCH (p:Person)-[:WorksFor]->(c:Company) RETURN COUNT(p)",
                "MATCH (p:Person)-[:WorksOn]->(pr:Project) RETURN COUNT(p)"
            };

            foreach (var query in queries)
            {
                var startTime = DateTime.UtcNow;
                using var result = connection.Query(query);
                var count = result.HasNext() ? result.GetNext().GetValueAs<long>(0) : 0;
                var executionTime = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"    Query: {query.Substring(0, Math.Min(50, query.Length))}...");
                Console.WriteLine($"    Execution time: {executionTime.TotalMilliseconds:F2}ms, Result: {count}");
            }

            // Memory usage monitoring
            Console.WriteLine("    Memory usage monitoring:");
            var memoryStart = DateTime.UtcNow;
            using var memoryResult = connection.Query(@"
                MATCH (p:Person)-[:WorksFor]->(c:Company),
                      (p)-[:WorksOn]->(pr:Project)
                RETURN p.name, c.name, pr.name, pr.budget
                ORDER BY pr.budget DESC");
            
            var memoryCount = 0;
            while (memoryResult.HasNext())
            {
                memoryResult.GetNext();
                memoryCount++;
            }
            var memoryTime = DateTime.UtcNow - memoryStart;
            
            Console.WriteLine($"    Large result set: {memoryCount} rows in {memoryTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"    Average time per row: {(memoryTime.TotalMilliseconds / memoryCount):F2}ms");
        }

        // Helper methods
        private static List<PersonData> GeneratePersons(int count)
        {
            var persons = new List<PersonData>();
            var cities = new[] { "New York", "London", "Tokyo", "Paris", "Sydney", "Berlin", "Toronto", "Mumbai" };
            var countries = new[] { "USA", "UK", "Japan", "France", "Australia", "Germany", "Canada", "India" };

            for (int i = 1; i <= count; i++)
            {
                persons.Add(new PersonData
                {
                    Id = i,
                    Name = $"Person{i}",
                    Age = Random.Shared.Next(22, 65),
                    City = cities[i % cities.Length],
                    Country = countries[i % countries.Length],
                    Email = $"person{i}@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(365 * 2))
                });
            }

            return persons;
        }

        private static List<CompanyData> GenerateCompanies(int count)
        {
            var companies = new List<CompanyData>();
            var industries = new[] { "Technology", "Finance", "Healthcare", "Manufacturing", "Retail", "Education", "Energy", "Transportation" };
            var sizes = new[] { "Small", "Medium", "Large", "Enterprise" };

            for (int i = 1; i <= count; i++)
            {
                companies.Add(new CompanyData
                {
                    Id = i,
                    Name = $"Company{i}",
                    Industry = industries[i % industries.Length],
                    Size = sizes[i % sizes.Length],
                    FoundedYear = Random.Shared.Next(1950, 2020)
                });
            }

            return companies;
        }

        private static List<ProjectData> GenerateProjects(int count)
        {
            var projects = new List<ProjectData>();
            var statuses = new[] { "Planning", "In Progress", "Completed", "On Hold", "Cancelled" };

            for (int i = 1; i <= count; i++)
            {
                var startDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(365));
                var endDate = startDate.AddDays(Random.Shared.Next(30, 365));
                
                projects.Add(new ProjectData
                {
                    Id = i,
                    Name = $"Project{i}",
                    Status = statuses[i % statuses.Length],
                    Budget = Random.Shared.NextDouble() * 1000000 + 10000,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }

            return projects;
        }

        private static string GetRandomPosition()
        {
            var positions = new[] { "Developer", "Manager", "Analyst", "Designer", "Engineer", "Consultant", "Director", "Coordinator" };
            return positions[Random.Shared.Next(positions.Length)];
        }

        private static string GetRandomRole()
        {
            var roles = new[] { "Lead", "Senior", "Junior", "Architect", "Specialist", "Coordinator" };
            return roles[Random.Shared.Next(roles.Length)];
        }
    }

    // Data classes
    public class PersonData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CompanyData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int FoundedYear { get; set; }
    }

    public class ProjectData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Budget { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
