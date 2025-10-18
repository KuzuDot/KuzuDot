using System;
using KuzuDot;
using KuzuDot.Value;

namespace KuzuDot.Examples.Graph
{
    /// <summary>
    /// Corporate hierarchy example demonstrating employee management structure
    /// </summary>
    public class CorporateHierarchy
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Corporate Hierarchy Example ===");
            
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
            Console.WriteLine("Creating corporate hierarchy schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate hierarchy queries
            Console.WriteLine("\n=== Hierarchy Queries ===");
            DemonstrateHierarchyQueries(connection);

            Console.WriteLine("\n=== Corporate Hierarchy Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            connection.NonQuery(@"
                CREATE NODE TABLE Company(
                    id INT64, 
                    name STRING, 
                    industry STRING, 
                    founded_year INT32,
                    headquarters STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Department(
                    id INT64, 
                    name STRING, 
                    budget DOUBLE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Employee(
                    id INT64, 
                    name STRING, 
                    email STRING, 
                    role STRING,
                    salary DOUBLE,
                    hire_date DATE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Project(
                    id INT64, 
                    name STRING, 
                    description STRING,
                    start_date DATE,
                    end_date DATE,
                    budget DOUBLE,
                    PRIMARY KEY(id)
                )");

            // Create relationship tables
            connection.NonQuery(@"
                CREATE REL TABLE WorksFor(
                    FROM Employee TO Company,
                    start_date DATE,
                    position STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE BelongsTo(
                    FROM Employee TO Department,
                    since DATE
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Manages(
                    FROM Employee TO Employee,
                    since DATE,
                    management_level STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE WorksOn(
                    FROM Employee TO Project,
                    role STRING,
                    hours_per_week INT32
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Owns(
                    FROM Company TO Department,
                    established_date DATE
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Sponsors(
                    FROM Company TO Project,
                    investment DOUBLE
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert companies
            var companies = new[]
            {
                new { Id = 1L, Name = "TechCorp", Industry = "Technology", FoundedYear = 2010, Headquarters = "San Francisco" },
                new { Id = 2L, Name = "DataSys", Industry = "Data Analytics", FoundedYear = 2015, Headquarters = "New York" },
                new { Id = 3L, Name = "CloudSoft", Industry = "Cloud Computing", FoundedYear = 2018, Headquarters = "Seattle" }
            };

            using var companyStmt = connection.Prepare("CREATE (:Company {id: $id, name: $name, industry: $industry, founded_year: $founded_year, headquarters: $headquarters})");
            foreach (var company in companies)
            {
                companyStmt.Bind("id", company.Id);
                companyStmt.Bind("name", company.Name);
                companyStmt.Bind("industry", company.Industry);
                companyStmt.Bind("founded_year", company.FoundedYear);
                companyStmt.Bind("headquarters", company.Headquarters);
                companyStmt.Execute();
                Console.WriteLine($"  Created company: {company.Name}");
            }

            // Insert departments
            var departments = new[]
            {
                new { Id = 1L, Name = "Engineering", Budget = 2000000.0 },
                new { Id = 2L, Name = "Sales", Budget = 800000.0 },
                new { Id = 3L, Name = "Marketing", Budget = 600000.0 },
                new { Id = 4L, Name = "HR", Budget = 400000.0 },
                new { Id = 5L, Name = "Finance", Budget = 300000.0 }
            };

            using var deptStmt = connection.Prepare("CREATE (:Department {id: $id, name: $name, budget: $budget})");
            foreach (var dept in departments)
            {
                deptStmt.Bind("id", dept.Id);
                deptStmt.Bind("name", dept.Name);
                deptStmt.Bind("budget", dept.Budget);
                deptStmt.Execute();
                Console.WriteLine($"  Created department: {dept.Name}");
            }

            // Insert employees
            var employees = new[]
            {
                new { Id = 1L, Name = "Alice Johnson", Email = "alice@techcorp.com", Role = "CEO", Salary = 300000.0, HireDate = new DateTime(2010, 1, 15) },
                new { Id = 2L, Name = "Bob Smith", Email = "bob@techcorp.com", Role = "CTO", Salary = 250000.0, HireDate = new DateTime(2010, 3, 1) },
                new { Id = 3L, Name = "Charlie Brown", Email = "charlie@techcorp.com", Role = "VP Engineering", Salary = 180000.0, HireDate = new DateTime(2011, 6, 15) },
                new { Id = 4L, Name = "Diana Prince", Email = "diana@techcorp.com", Role = "VP Sales", Salary = 160000.0, HireDate = new DateTime(2012, 2, 1) },
                new { Id = 5L, Name = "Eve Wilson", Email = "eve@techcorp.com", Role = "Senior Engineer", Salary = 120000.0, HireDate = new DateTime(2013, 8, 20) },
                new { Id = 6L, Name = "Frank Miller", Email = "frank@techcorp.com", Role = "Engineer", Salary = 95000.0, HireDate = new DateTime(2015, 4, 10) },
                new { Id = 7L, Name = "Grace Lee", Email = "grace@techcorp.com", Role = "Sales Manager", Salary = 110000.0, HireDate = new DateTime(2014, 9, 5) },
                new { Id = 8L, Name = "Henry Davis", Email = "henry@techcorp.com", Role = "Sales Rep", Salary = 80000.0, HireDate = new DateTime(2016, 1, 15) },
                new { Id = 9L, Name = "Ivy Chen", Email = "ivy@techcorp.com", Role = "Marketing Director", Salary = 130000.0, HireDate = new DateTime(2013, 11, 1) },
                new { Id = 10L, Name = "Jack Wilson", Email = "jack@techcorp.com", Role = "HR Director", Salary = 100000.0, HireDate = new DateTime(2012, 5, 20) }
            };

            using var empStmt = connection.Prepare("CREATE (:Employee {id: $id, name: $name, email: $email, role: $role, salary: $salary, hire_date: $hire_date})");
            foreach (var emp in employees)
            {
                empStmt.Bind("id", emp.Id);
                empStmt.Bind("name", emp.Name);
                empStmt.Bind("email", emp.Email);
                empStmt.Bind("role", emp.Role);
                empStmt.Bind("salary", emp.Salary);
                empStmt.BindDate("hire_date", emp.HireDate);
                empStmt.Execute();
            }

            // Insert projects
            var projects = new[]
            {
                new { Id = 1L, Name = "Mobile App", Description = "iOS and Android mobile application", StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2023, 12, 31), Budget = 500000.0 },
                new { Id = 2L, Name = "Cloud Migration", Description = "Migrate infrastructure to cloud", StartDate = new DateTime(2023, 3, 1), EndDate = new DateTime(2024, 6, 30), Budget = 800000.0 },
                new { Id = 3L, Name = "AI Integration", Description = "Integrate AI capabilities", StartDate = new DateTime(2023, 6, 1), EndDate = new DateTime(2024, 3, 31), Budget = 1200000.0 }
            };

            using var projStmt = connection.Prepare("CREATE (:Project {id: $id, name: $name, description: $description, start_date: $start_date, end_date: $end_date, budget: $budget})");
            foreach (var proj in projects)
            {
                projStmt.Bind("id", proj.Id);
                projStmt.Bind("name", proj.Name);
                projStmt.Bind("description", proj.Description);
                projStmt.BindDate("start_date", proj.StartDate);
                projStmt.BindDate("end_date", proj.EndDate);
                projStmt.Bind("budget", proj.Budget);
                projStmt.Execute();
            }

            Console.WriteLine("  Created all employees and projects");

            // Create relationships
            CreateRelationships(connection);
        }

        private static void CreateRelationships(Connection connection)
        {
            Console.WriteLine("Creating relationships...");

            // WorksFor relationships (employees to companies)
            var workRelations = new[]
            {
                new { EmployeeId = 1L, CompanyId = 1L, StartDate = new DateTime(2010, 1, 15), Position = "CEO" },
                new { EmployeeId = 2L, CompanyId = 1L, StartDate = new DateTime(2010, 3, 1), Position = "CTO" },
                new { EmployeeId = 3L, CompanyId = 1L, StartDate = new DateTime(2011, 6, 15), Position = "VP Engineering" },
                new { EmployeeId = 4L, CompanyId = 1L, StartDate = new DateTime(2012, 2, 1), Position = "VP Sales" },
                new { EmployeeId = 5L, CompanyId = 1L, StartDate = new DateTime(2013, 8, 20), Position = "Senior Engineer" },
                new { EmployeeId = 6L, CompanyId = 1L, StartDate = new DateTime(2015, 4, 10), Position = "Engineer" },
                new { EmployeeId = 7L, CompanyId = 1L, StartDate = new DateTime(2014, 9, 5), Position = "Sales Manager" },
                new { EmployeeId = 8L, CompanyId = 1L, StartDate = new DateTime(2016, 1, 15), Position = "Sales Rep" },
                new { EmployeeId = 9L, CompanyId = 1L, StartDate = new DateTime(2013, 11, 1), Position = "Marketing Director" },
                new { EmployeeId = 10L, CompanyId = 1L, StartDate = new DateTime(2012, 5, 20), Position = "HR Director" }
            };

            using var workStmt = connection.Prepare(@"
                MATCH (e:Employee), (c:Company) 
                WHERE e.id = $employee_id AND c.id = $company_id 
                CREATE (e)-[:WorksFor {start_date: $start_date, position: $position}]->(c)");

            foreach (var work in workRelations)
            {
                workStmt.Bind("employee_id", work.EmployeeId);
                workStmt.Bind("company_id", work.CompanyId);
                workStmt.BindDate("start_date", work.StartDate);
                workStmt.Bind("position", work.Position);
                workStmt.Execute();
            }

            // BelongsTo relationships (employees to departments)
            var deptRelations = new[]
            {
                new { EmployeeId = 1L, DepartmentId = 5L, Since = new DateTime(2010, 1, 15) }, // CEO -> Finance
                new { EmployeeId = 2L, DepartmentId = 1L, Since = new DateTime(2010, 3, 1) }, // CTO -> Engineering
                new { EmployeeId = 3L, DepartmentId = 1L, Since = new DateTime(2011, 6, 15) }, // VP Eng -> Engineering
                new { EmployeeId = 4L, DepartmentId = 2L, Since = new DateTime(2012, 2, 1) }, // VP Sales -> Sales
                new { EmployeeId = 5L, DepartmentId = 1L, Since = new DateTime(2013, 8, 20) }, // Senior Eng -> Engineering
                new { EmployeeId = 6L, DepartmentId = 1L, Since = new DateTime(2015, 4, 10) }, // Engineer -> Engineering
                new { EmployeeId = 7L, DepartmentId = 2L, Since = new DateTime(2014, 9, 5) }, // Sales Manager -> Sales
                new { EmployeeId = 8L, DepartmentId = 2L, Since = new DateTime(2016, 1, 15) }, // Sales Rep -> Sales
                new { EmployeeId = 9L, DepartmentId = 3L, Since = new DateTime(2013, 11, 1) }, // Marketing Dir -> Marketing
                new { EmployeeId = 10L, DepartmentId = 4L, Since = new DateTime(2012, 5, 20) } // HR Dir -> HR
            };

            using var deptStmt = connection.Prepare(@"
                MATCH (e:Employee), (d:Department) 
                WHERE e.id = $employee_id AND d.id = $department_id 
                CREATE (e)-[:BelongsTo {since: $since}]->(d)");

            foreach (var dept in deptRelations)
            {
                deptStmt.Bind("employee_id", dept.EmployeeId);
                deptStmt.Bind("department_id", dept.DepartmentId);
                deptStmt.BindDate("since", dept.Since);
                deptStmt.Execute();
            }

            // Manages relationships (management hierarchy)
            var manageRelations = new[]
            {
                new { ManagerId = 1L, EmployeeId = 2L, Since = new DateTime(2010, 3, 1), Level = "Direct Report" }, // CEO manages CTO
                new { ManagerId = 1L, EmployeeId = 4L, Since = new DateTime(2012, 2, 1), Level = "Direct Report" }, // CEO manages VP Sales
                new { ManagerId = 1L, EmployeeId = 9L, Since = new DateTime(2013, 11, 1), Level = "Direct Report" }, // CEO manages Marketing Dir
                new { ManagerId = 1L, EmployeeId = 10L, Since = new DateTime(2012, 5, 20), Level = "Direct Report" }, // CEO manages HR Dir
                new { ManagerId = 2L, EmployeeId = 3L, Since = new DateTime(2011, 6, 15), Level = "Direct Report" }, // CTO manages VP Eng
                new { ManagerId = 3L, EmployeeId = 5L, Since = new DateTime(2013, 8, 20), Level = "Direct Report" }, // VP Eng manages Senior Eng
                new { ManagerId = 3L, EmployeeId = 6L, Since = new DateTime(2015, 4, 10), Level = "Direct Report" }, // VP Eng manages Engineer
                new { ManagerId = 4L, EmployeeId = 7L, Since = new DateTime(2014, 9, 5), Level = "Direct Report" }, // VP Sales manages Sales Manager
                new { ManagerId = 7L, EmployeeId = 8L, Since = new DateTime(2016, 1, 15), Level = "Direct Report" } // Sales Manager manages Sales Rep
            };

            using var manageStmt = connection.Prepare(@"
                MATCH (m:Employee), (e:Employee) 
                WHERE m.id = $manager_id AND e.id = $employee_id 
                CREATE (m)-[:Manages {since: $since, management_level: $level}]->(e)");

            foreach (var manage in manageRelations)
            {
                manageStmt.Bind("manager_id", manage.ManagerId);
                manageStmt.Bind("employee_id", manage.EmployeeId);
                manageStmt.BindDate("since", manage.Since);
                manageStmt.Bind("level", manage.Level);
                manageStmt.Execute();
            }

            // WorksOn relationships (employees to projects)
            var projectRelations = new[]
            {
                new { EmployeeId = 2L, ProjectId = 1L, Role = "Technical Lead", HoursPerWeek = 40 },
                new { EmployeeId = 3L, ProjectId = 1L, Role = "Project Manager", HoursPerWeek = 30 },
                new { EmployeeId = 5L, ProjectId = 1L, Role = "Developer", HoursPerWeek = 40 },
                new { EmployeeId = 6L, ProjectId = 1L, Role = "Developer", HoursPerWeek = 40 },
                new { EmployeeId = 2L, ProjectId = 2L, Role = "Architect", HoursPerWeek = 20 },
                new { EmployeeId = 3L, ProjectId = 2L, Role = "Project Manager", HoursPerWeek = 25 },
                new { EmployeeId = 5L, ProjectId = 2L, Role = "DevOps Engineer", HoursPerWeek = 30 },
                new { EmployeeId = 2L, ProjectId = 3L, Role = "Technical Lead", HoursPerWeek = 30 },
                new { EmployeeId = 3L, ProjectId = 3L, Role = "Project Manager", HoursPerWeek = 20 },
                new { EmployeeId = 5L, ProjectId = 3L, Role = "ML Engineer", HoursPerWeek = 35 }
            };

            using var projStmt = connection.Prepare(@"
                MATCH (e:Employee), (p:Project) 
                WHERE e.id = $employee_id AND p.id = $project_id 
                CREATE (e)-[:WorksOn {role: $role, hours_per_week: $hours}]->(p)");

            foreach (var proj in projectRelations)
            {
                projStmt.Bind("employee_id", proj.EmployeeId);
                projStmt.Bind("project_id", proj.ProjectId);
                projStmt.Bind("role", proj.Role);
                projStmt.Bind("hours", proj.HoursPerWeek);
                projStmt.Execute();
            }

            // Owns relationships (companies to departments)
            using var ownsStmt = connection.Prepare(@"
                MATCH (c:Company), (d:Department) 
                WHERE c.id = $company_id AND d.id = $department_id 
                CREATE (c)-[:Owns {established_date: $established_date}]->(d)");

            var ownsRelations = new[]
            {
                new { CompanyId = 1L, DepartmentId = 1L, EstablishedDate = new DateTime(2010, 1, 15) },
                new { CompanyId = 1L, DepartmentId = 2L, EstablishedDate = new DateTime(2010, 1, 15) },
                new { CompanyId = 1L, DepartmentId = 3L, EstablishedDate = new DateTime(2010, 1, 15) },
                new { CompanyId = 1L, DepartmentId = 4L, EstablishedDate = new DateTime(2010, 1, 15) },
                new { CompanyId = 1L, DepartmentId = 5L, EstablishedDate = new DateTime(2010, 1, 15) }
            };

            foreach (var owns in ownsRelations)
            {
                ownsStmt.Bind("company_id", owns.CompanyId);
                ownsStmt.Bind("department_id", owns.DepartmentId);
                ownsStmt.BindDate("established_date", owns.EstablishedDate);
                ownsStmt.Execute();
            }

            // Sponsors relationships (companies to projects)
            using var sponsorStmt = connection.Prepare(@"
                MATCH (c:Company), (p:Project) 
                WHERE c.id = $company_id AND p.id = $project_id 
                CREATE (c)-[:Sponsors {investment: $investment}]->(p)");

            var sponsorRelations = new[]
            {
                new { CompanyId = 1L, ProjectId = 1L, Investment = 500000.0 },
                new { CompanyId = 1L, ProjectId = 2L, Investment = 800000.0 },
                new { CompanyId = 1L, ProjectId = 3L, Investment = 1200000.0 }
            };

            foreach (var sponsor in sponsorRelations)
            {
                sponsorStmt.Bind("company_id", sponsor.CompanyId);
                sponsorStmt.Bind("project_id", sponsor.ProjectId);
                sponsorStmt.Bind("investment", sponsor.Investment);
                sponsorStmt.Execute();
            }

            Console.WriteLine("  Created all relationships");
        }

        private static void DemonstrateHierarchyQueries(Connection connection)
        {
            // 1. Management hierarchy
            Console.WriteLine("1. Management hierarchy:");
            using var hierarchyResult = connection.Query(@"
                MATCH (manager:Employee)-[:Manages]->(employee:Employee)
                RETURN manager.name, manager.role, employee.name, employee.role
                ORDER BY manager.name");

            while (hierarchyResult.HasNext())
            {
                using var row = hierarchyResult.GetNext();
                var managerName = row.GetValueAs<string>(0);
                var managerRole = row.GetValueAs<string>(1);
                var employeeName = row.GetValueAs<string>(2);
                var employeeRole = row.GetValueAs<string>(3);
                
                Console.WriteLine($"  {managerName} ({managerRole}) manages {employeeName} ({employeeRole})");
            }

            // 2. Department structure
            Console.WriteLine("\n2. Department structure:");
            using var deptResult = connection.Query(@"
                MATCH (d:Department)<-[:BelongsTo]-(e:Employee)
                RETURN d.name, COUNT(e) as employee_count, AVG(e.salary) as avg_salary
                ORDER BY employee_count DESC");

            while (deptResult.HasNext())
            {
                using var row = deptResult.GetNext();
                var deptName = row.GetValueAs<string>(0);
                var empCount = row.GetValueAs<long>(1);
                var avgSalary = row.GetValueAs<double>(2);
                
                Console.WriteLine($"  {deptName}: {empCount} employees, avg salary: ${avgSalary:F0}");
            }

            // 3. Project assignments
            Console.WriteLine("\n3. Project assignments:");
            using var projectResult = connection.Query(@"
                MATCH (e:Employee)-[w:WorksOn]->(p:Project)
                RETURN e.name, p.name, w.role, w.hours_per_week
                ORDER BY e.name, p.name");

            while (projectResult.HasNext())
            {
                using var row = projectResult.GetNext();
                var empName = row.GetValueAs<string>(0);
                var projName = row.GetValueAs<string>(1);
                var role = row.GetValueAs<string>(2);
                var hours = row.GetValueAs<int>(3);
                
                Console.WriteLine($"  {empName} works on '{projName}' as {role} ({hours}h/week)");
            }

            // 4. Reporting chain
            Console.WriteLine("\n4. Reporting chain (CEO to all employees):");
            using var reportingResult = connection.Query(@"
                MATCH path = (ceo:Employee {role: 'CEO'})-[:Manages*]->(employee:Employee)
                RETURN employee.name, employee.role, LENGTH(path) as levels_from_ceo
                ORDER BY levels_from_ceo, employee.name");

            while (reportingResult.HasNext())
            {
                using var row = reportingResult.GetNext();
                var empName = row.GetValueAs<string>(0);
                var empRole = row.GetValueAs<string>(1);
                var levels = row.GetValueAs<long>(2);
                
                Console.WriteLine($"  {empName} ({empRole}) - {levels} levels from CEO");
            }

            // 5. High-level executives
            Console.WriteLine("\n5. High-level executives (salary > $150k):");
            using var execResult = connection.Query(@"
                MATCH (e:Employee)-[:BelongsTo]->(d:Department)
                WHERE e.salary > 150000
                RETURN e.name, e.role, e.salary, d.name AS department
                ORDER BY e.salary DESC");

            while (execResult.HasNext())
            {
                using var row = execResult.GetNext();
                var empName = row.GetValueAs<string>(0);
                var empRole = row.GetValueAs<string>(1);
                var salary = row.GetValueAs<double>(2);
                var dept = row.GetValueAs<string>(3);
                
                Console.WriteLine($"  {empName} ({empRole}) - ${salary:F0} - {dept}");
            }

            // 6. Project budget analysis
            Console.WriteLine("\n6. Project budget analysis:");
            using var budgetResult = connection.Query(@"
                MATCH (c:Company)-[s:Sponsors]->(p:Project)
                RETURN c.name, p.name, p.budget, s.investment, (s.investment / p.budget * 100) as percentage
                ORDER BY s.investment DESC");

            while (budgetResult.HasNext())
            {
                using var row = budgetResult.GetNext();
                var companyName = row.GetValueAs<string>(0);
                var projName = row.GetValueAs<string>(1);
                var budget = row.GetValueAs<double>(2);
                var investment = row.GetValueAs<double>(3);
                var percentage = row.GetValueAs<double>(4);
                
                Console.WriteLine($"  {companyName} sponsors '{projName}': ${investment:F0} of ${budget:F0} ({percentage:F1}%)");
            }

            // 7. Employee tenure analysis
            Console.WriteLine("\n7. Employee tenure analysis:");
            using var tenureResult = connection.Query(@"
                MATCH (e:Employee)
                RETURN e.name, e.hire_date, e.salary
                ORDER BY e.hire_date ASC");

            while (tenureResult.HasNext())
            {
                using var row = tenureResult.GetNext();
                var empName = row.GetValueAs<string>(0);
                using var hireDateValue = row.GetValue<KuzuDate>(1);
                var hireDate = hireDateValue.AsDateTime();
                var salary = row.GetValueAs<double>(2);
                
                Console.WriteLine($"  {empName}: hired {hireDate:yyyy-MM-dd}, salary: ${salary:F0}");
            }

            // 8. Cross-department collaboration
            Console.WriteLine("\n8. Cross-department collaboration:");
            using var collabResult = connection.Query(@"
                MATCH (e1:Employee)-[:BelongsTo]->(d1:Department),
                      (e2:Employee)-[:BelongsTo]->(d2:Department),
                      (e1)-[:WorksOn]->(p:Project)<-[:WorksOn]-(e2)
                WHERE d1.id < d2.id
                RETURN d1.name, d2.name, p.name, COUNT(DISTINCT e1) + COUNT(DISTINCT e2) as total_employees
                ORDER BY total_employees DESC");

            while (collabResult.HasNext())
            {
                using var row = collabResult.GetNext();
                var dept1 = row.GetValueAs<string>(0);
                var dept2 = row.GetValueAs<string>(1);
                var projName = row.GetValueAs<string>(2);
                var totalEmp = row.GetValueAs<long>(3);
                
                Console.WriteLine($"  {dept1} and {dept2} collaborate on '{projName}' ({totalEmp} employees)");
            }
        }
    }
}
