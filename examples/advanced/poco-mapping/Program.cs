using System;
using KuzuDot;
using KuzuDot.Value;

namespace KuzuDot.Examples.Advanced
{
    /// <summary>
    /// POCO mapping example demonstrating object-relational mapping with KuzuDot
    /// </summary>
    public class PocoMapping
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot POCO Mapping Example ===");
            
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

            // Demonstrate POCO mapping
            Console.WriteLine("\n=== POCO Mapping Examples ===");
            DemonstratePocoMapping(connection);

            // Demonstrate naming strategies
            DemonstrateNamingStrategies(connection);

            Console.WriteLine("\n=== POCO Mapping Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            connection.NonQuery(@"
                CREATE NODE TABLE Employee(
                    id INT64, 
                    name STRING, 
                    email STRING, 
                    department STRING,
                    salary DOUBLE,
                    hire_date DATE,
                    is_active BOOLEAN,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Department(
                    id INT64, 
                    name STRING, 
                    budget DOUBLE,
                    manager_id INT64,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE REL TABLE WorksIn(
                    FROM Employee TO Department,
                    start_date DATE,
                    role STRING
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert departments
            var departments = new[]
            {
                new { Id = 1L, Name = "Engineering", Budget = 1000000.0, ManagerId = 1L },
                new { Id = 2L, Name = "Sales", Budget = 500000.0, ManagerId = 4L },
                new { Id = 3L, Name = "Marketing", Budget = 300000.0, ManagerId = 6L }
            };

            using var deptStmt = connection.Prepare("CREATE (:Department {id: $id, name: $name, budget: $budget, manager_id: $manager_id})");
            foreach (var dept in departments)
            {
                deptStmt.BindSnakeCase(dept);
                deptStmt.Execute();
            }

            // Insert employees
            var employees = new[]
            {
                new { Id = 1L, Name = "Alice Johnson", Email = "alice@company.com", Department = "Engineering", Salary = 95000.0, HireDate = new DateTime(2020, 3, 15), IsActive = true },
                new { Id = 2L, Name = "Bob Smith", Email = "bob@company.com", Department = "Engineering", Salary = 85000.0, HireDate = new DateTime(2021, 6, 1), IsActive = true },
                new { Id = 3L, Name = "Charlie Brown", Email = "charlie@company.com", Department = "Engineering", Salary = 90000.0, HireDate = new DateTime(2020, 9, 10), IsActive = true },
                new { Id = 4L, Name = "Diana Prince", Email = "diana@company.com", Department = "Sales", Salary = 75000.0, HireDate = new DateTime(2019, 1, 20), IsActive = true },
                new { Id = 5L, Name = "Eve Wilson", Email = "eve@company.com", Department = "Sales", Salary = 70000.0, HireDate = new DateTime(2022, 2, 14), IsActive = true },
                new { Id = 6L, Name = "Frank Miller", Email = "frank@company.com", Department = "Marketing", Salary = 80000.0, HireDate = new DateTime(2021, 4, 5), IsActive = true },
                new { Id = 7L, Name = "Grace Lee", Email = "grace@company.com", Department = "Marketing", Salary = 75000.0, HireDate = new DateTime(2020, 11, 30), IsActive = false }
            };

            using var empStmt = connection.Prepare(@"
                CREATE (:Employee {
                    id: $id, 
                    name: $name, 
                    email: $email, 
                    department: $department,
                    salary: $salary,
                    hire_date: $hire_date,
                    is_active: $is_active
                })");

            foreach (var emp in employees)
            {
                empStmt.Bind("id", emp.Id);
                empStmt.Bind("name", emp.Name);
                empStmt.Bind("email", emp.Email);
                empStmt.Bind("department", emp.Department);
                empStmt.Bind("salary", emp.Salary);
                empStmt.BindDate("hire_date", emp.HireDate);
                empStmt.Bind("is_active", emp.IsActive);
                empStmt.Execute();
            }

            // Create relationships
            var workRelations = new[]
            {
                new { EmployeeId = 1L, DepartmentId = 1L, StartDate = new DateTime(2020, 3, 15), Role = "Senior Engineer" },
                new { EmployeeId = 2L, DepartmentId = 1L, StartDate = new DateTime(2021, 6, 1), Role = "Engineer" },
                new { EmployeeId = 3L, DepartmentId = 1L, StartDate = new DateTime(2020, 9, 10), Role = "Engineer" },
                new { EmployeeId = 4L, DepartmentId = 2L, StartDate = new DateTime(2019, 1, 20), Role = "Sales Manager" },
                new { EmployeeId = 5L, DepartmentId = 2L, StartDate = new DateTime(2022, 2, 14), Role = "Sales Rep" },
                new { EmployeeId = 6L, DepartmentId = 3L, StartDate = new DateTime(2021, 4, 5), Role = "Marketing Manager" },
                new { EmployeeId = 7L, DepartmentId = 3L, StartDate = new DateTime(2020, 11, 30), Role = "Marketing Specialist" }
            };

            using var workStmt = connection.Prepare(@"
                MATCH (e:Employee), (d:Department) 
                WHERE e.id = $employee_id AND d.id = $department_id 
                CREATE (e)-[:WorksIn {start_date: $start_date, role: $role}]->(d)");

            foreach (var work in workRelations)
            {
                workStmt.BindSnakeCase(work);
                workStmt.Execute();
            }
        }

        private static void DemonstratePocoMapping(Connection connection)
        {
            // 1. Basic POCO mapping
            Console.WriteLine("\n1. Basic POCO mapping - All employees:");
            var employees = connection.Query<Employee>("MATCH (e:Employee) RETURN e.id AS id, e.name AS name, e.email AS email, e.department AS department, e.salary AS salary, e.hire_date AS hire_date, e.is_active AS is_active");
            
            foreach (var emp in employees)
            {
                Console.WriteLine($"  {emp.Name} ({emp.Email}) - {emp.Department}, ${emp.Salary:F0}, hired {emp.HireDate:yyyy-MM-dd}, active: {emp.IsActive}");
            }

            // 2. POCO mapping with custom column names
            Console.WriteLine("\n2. POCO mapping with custom column names:");
            var departments = connection.Query<Department>("MATCH (d:Department) RETURN d.id AS id, d.name AS name, d.budget AS budget, d.manager_id AS manager_id");
            
            foreach (var dept in departments)
            {
                Console.WriteLine($"  {dept.Name} - Budget: ${dept.Budget:F0}, Manager ID: {dept.ManagerId}");
            }

            // 3. POCO mapping with filtering
            Console.WriteLine("\n3. Active employees only:");
            var activeEmployees = connection.Query<Employee>("MATCH (e:Employee) WHERE e.is_active = true RETURN e.id AS id, e.name AS name, e.email AS email, e.department AS department, e.salary AS salary, e.hire_date AS hire_date, e.is_active AS is_active");
            
            foreach (var emp in activeEmployees)
            {
                Console.WriteLine($"  {emp.Name} - {emp.Department}, ${emp.Salary:F0}");
            }

            // 4. POCO mapping with aggregation
            Console.WriteLine("\n4. Department statistics:");
            var deptStats = connection.Query<DepartmentStats>("MATCH (e:Employee)-[:WorksIn]->(d:Department) RETURN d.name AS department_name, COUNT(e) AS employee_count, AVG(e.salary) AS avg_salary, MAX(e.salary) AS max_salary");
            
            foreach (var stat in deptStats)
            {
                Console.WriteLine($"  {stat.DepartmentName}: {stat.EmployeeCount} employees, avg salary: ${stat.AverageSalary:F0}, max salary: ${stat.MaxSalary:F0}");
            }

            // 5. POCO mapping with joins
            Console.WriteLine("\n5. Employee with department information:");
            var empDeptInfo = connection.Query<EmployeeDepartmentInfo>(@"
                MATCH (e:Employee)-[w:WorksIn]->(d:Department) 
                RETURN e.name AS employee_name, e.salary AS salary, d.name AS department_name, w.role AS role, w.start_date AS start_date");

            foreach (var info in empDeptInfo)
            {
                Console.WriteLine($"  {info.EmployeeName} - {info.Role} in {info.DepartmentName}, ${info.Salary:F0}, started {info.StartDate:yyyy-MM-dd}");
            }

            // 6. POCO mapping with complex queries
            Console.WriteLine("\n6. High-earning employees by department:");
            var highEarners = connection.Query<Employee>(@"
                MATCH (e:Employee)-[:WorksIn]->(d:Department) 
                WHERE e.salary > 80000 
                RETURN e.id AS id, e.name AS name, e.email AS email, e.department AS department, e.salary AS salary, e.hire_date AS hire_date, e.is_active AS is_active
                ORDER BY e.salary DESC");

            foreach (var emp in highEarners)
            {
                Console.WriteLine($"  {emp.Name} - {emp.Department}, ${emp.Salary:F0}");
            }

            // 7. POCO mapping with nullable types
            Console.WriteLine("\n7. Employees with nullable salary (demonstrating nullable types):");
            var employeesWithNullable = connection.Query<EmployeeWithNullable>("MATCH (e:Employee) RETURN e.id AS id, e.name AS name, e.salary AS salary, e.hire_date AS hire_date");
            
            foreach (var emp in employeesWithNullable)
            {
                var salaryText = emp.Salary?.ToString("F0") ?? "Not specified";
                var hireDateText = emp.HireDate?.ToString("yyyy-MM-dd") ?? "Not specified";
                Console.WriteLine($"  {emp.Name} - Salary: ${salaryText}, Hired: {hireDateText}");
            }

            // 8. Custom projector function
            Console.WriteLine("\n8. Custom projector function - Employee summaries:");
            var summaries = connection.Query(
                "MATCH (e:Employee) RETURN e.name, e.salary, e.department",
                row => new EmployeeSummary
                {
                    Name = row.GetValueAs<string>(0),
                    Salary = row.GetValueAs<double>(1),
                    Department = row.GetValueAs<string>(2),
                    SalaryCategory = row.GetValueAs<double>(1) > 85000 ? "High" : "Standard"
                });

            foreach (var summary in summaries)
            {
                Console.WriteLine($"  {summary.Name} - {summary.Department}, ${summary.Salary:F0} ({summary.SalaryCategory})");
            }
        }

        private static void DemonstrateNamingStrategies(Connection connection)
        {
            Console.WriteLine("\n=== Naming Strategy Examples ===");
            Console.WriteLine("1. Snake Case Naming Strategy:");
            
            // Create a test table with snake_case columns
            connection.NonQuery(@"
                CREATE NODE TABLE TestSnakeCase(
                    user_id INT64,
                    first_name STRING,
                    last_name STRING,
                    birth_year INT32,
                    email_address STRING,
                    PRIMARY KEY(user_id)
                )");

            // POCO class without KuzuName attributes
            var snakeTestUser = new TestUser
            {
                UserId = 1,
                FirstName = "John",
                LastName = "Doe",
                BirthYear = 1990,
                EmailAddress = "john.doe@example.com"
            };

            using var snakeStmt = connection.Prepare(@"
                CREATE (:TestSnakeCase {
                    user_id: $user_id,
                    first_name: $first_name,
                    last_name: $last_name,
                    birth_year: $birth_year,
                    email_address: $email_address
                })");

            // Use snake case naming strategy
            snakeStmt.BindSnakeCase(snakeTestUser);
            snakeStmt.Execute();
            Console.WriteLine("  ✓ Successfully inserted user with snake_case naming strategy");

            Console.WriteLine("\n2. Camel Case Naming Strategy:");
            
            // Create a test table with camelCase columns
            connection.NonQuery(@"
                CREATE NODE TABLE TestCamelCase(
                    userId INT64,
                    firstName STRING,
                    lastName STRING,
                    birthYear INT32,
                    emailAddress STRING,
                    PRIMARY KEY(userId)
                )");

            var camelTestUser = new TestUser
            {
                UserId = 2,
                FirstName = "Jane",
                LastName = "Smith",
                BirthYear = 1985,
                EmailAddress = "jane.smith@example.com"
            };

            using var camelStmt = connection.Prepare(@"
                CREATE (:TestCamelCase {
                    userId: $userId,
                    firstName: $firstName,
                    lastName: $lastName,
                    birthYear: $birthYear,
                    emailAddress: $emailAddress
                })");

            // Use camel case naming strategy
            camelStmt.BindCamelCase(camelTestUser);
            camelStmt.Execute();
            Console.WriteLine("  ✓ Successfully inserted user with camelCase naming strategy");

            Console.WriteLine("\n3. Pascal Case Naming Strategy:");
            
            // Create a test table with PascalCase columns
            connection.NonQuery(@"
                CREATE NODE TABLE TestPascalCase(
                    UserId INT64,
                    FirstName STRING,
                    LastName STRING,
                    BirthYear INT32,
                    EmailAddress STRING,
                    PRIMARY KEY(UserId)
                )");

            var pascalTestUser = new TestUser
            {
                UserId = 3,
                FirstName = "Bob",
                LastName = "Johnson",
                BirthYear = 1992,
                EmailAddress = "bob.johnson@example.com"
            };

            using var pascalStmt = connection.Prepare(@"
                CREATE (:TestPascalCase {
                    UserId: $UserId,
                    FirstName: $FirstName,
                    LastName: $LastName,
                    BirthYear: $BirthYear,
                    EmailAddress: $EmailAddress
                })");

            // Use pascal case naming strategy
            pascalStmt.BindPascalCase(pascalTestUser);
            pascalStmt.Execute();
            Console.WriteLine("  ✓ Successfully inserted user with PascalCase naming strategy");

            Console.WriteLine("\n4. Mixed Strategy with KuzuName Override:");
            
            var mixedUser = new MixedNamingUser
            {
                Id = 1,
                Name = "Jane Smith",
                BirthYear = 1985,
                EmailAddress = "jane.smith@example.com"
            };

            // Create a test table with mixed naming
            connection.NonQuery(@"
                CREATE NODE TABLE TestMixedNaming(
                    id INT64,
                    full_name STRING,
                    birth_year INT32,
                    email STRING,
                    PRIMARY KEY(id)
                )");

            using var mixedStmt = connection.Prepare(@"
                CREATE (:TestMixedNaming {
                    id: $id,
                    full_name: $full_name,
                    birth_year: $birth_year,
                    email: $email
                })");

            // Use snake case strategy, but KuzuName attributes override specific properties
            mixedStmt.BindSnakeCase(mixedUser);
            mixedStmt.Execute();
            Console.WriteLine("  ✓ Successfully inserted user with mixed naming (snake_case + KuzuName overrides)");
        }
    }

    // Test POCO classes for naming strategy demonstration
    public class TestUser
    {
        public long UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int BirthYear { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
    }

    public class MixedNamingUser
    {
        public long Id { get; set; }
        
        [KuzuName("full_name")]  // Override snake_case strategy
        public string Name { get; set; } = string.Empty;
        
        public int BirthYear { get; set; }  // Uses snake_case strategy: birth_year
        
        [KuzuName("email")]  // Override snake_case strategy
        public string EmailAddress { get; set; } = string.Empty;
    }

    // POCO classes for mapping
    public class Employee
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public double Salary { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class Department
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Budget { get; set; }
        public long ManagerId { get; set; }
    }

    public class DepartmentStats
    {
        [KuzuName("department_name")]
        public string DepartmentName { get; set; } = string.Empty;
        
        [KuzuName("employee_count")]
        public long EmployeeCount { get; set; }
        
        [KuzuName("avg_salary")]
        public double AverageSalary { get; set; }
        
        [KuzuName("max_salary")]
        public double MaxSalary { get; set; }
    }

    public class EmployeeDepartmentInfo
    {
        [KuzuName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;
        
        public double Salary { get; set; }
        
        [KuzuName("department_name")]
        public string DepartmentName { get; set; } = string.Empty;
        
        public string Role { get; set; } = string.Empty;
        
        [KuzuName("start_date")]
        public DateTime StartDate { get; set; }
    }

    public class EmployeeWithNullable
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double? Salary { get; set; }  // Nullable double
        public DateTime? HireDate { get; set; }  // Nullable DateTime
    }

    public class EmployeeSummary
    {
        public string Name { get; set; } = string.Empty;
        public double Salary { get; set; }
        public string Department { get; set; } = string.Empty;
        public string SalaryCategory { get; set; } = string.Empty;
    }
}
