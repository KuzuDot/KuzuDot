using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace KuzuDot.Tests.PocoBinderTests
{
    [TestClass]
    public sealed class NamingStrategyTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly Database _db;
        private readonly Connection _conn;

        public NamingStrategyTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _db = Database.FromPath(_dbPath);
            _conn = _db.Connect();
            
            // Create test tables for different naming strategies
            _conn.Query(@"
                CREATE NODE TABLE SnakeCaseUser(
                    user_id INT64,
                    first_name STRING,
                    last_name STRING,
                    email_address STRING,
                    birth_year INT64,
                    is_active BOOL,
                    created_at TIMESTAMP,
                    PRIMARY KEY(user_id)
                );
            ");
            
            _conn.Query(@"
                CREATE NODE TABLE CamelCaseUser(
                    userId INT64,
                    firstName STRING,
                    lastName STRING,
                    emailAddress STRING,
                    birthYear INT64,
                    isActive BOOL,
                    createdAt TIMESTAMP,
                    PRIMARY KEY(userId)
                );
            ");
            
            _conn.Query(@"
                CREATE NODE TABLE PascalCaseUser(
                    UserId INT64,
                    FirstName STRING,
                    LastName STRING,
                    EmailAddress STRING,
                    BirthYear INT64,
                    IsActive BOOL,
                    CreatedAt TIMESTAMP,
                    PRIMARY KEY(UserId)
                );
            ");
            
            _conn.Query(@"
                CREATE NODE TABLE LowercaseUser(
                    userid INT64,
                    firstname STRING,
                    lastname STRING,
                    emailaddress STRING,
                    birthyear INT64,
                    isactive BOOL,
                    createdat TIMESTAMP,
                    PRIMARY KEY(userid)
                );
            ");
            
            _conn.Query(@"
                CREATE NODE TABLE ExactUser(
                    UserId INT64,
                    FirstName STRING,
                    LastName STRING,
                    EmailAddress STRING,
                    BirthYear INT64,
                    IsActive BOOL,
                    CreatedAt TIMESTAMP,
                    PRIMARY KEY(UserId)
                );
            ");
        }

        [TestMethod]
        public void BindSnakeCase_ConvertsPropertyNamesToSnakeCase()
        {
            using var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});");
            
            var user = new TestUser
            {
                UserId = 1001,
                FirstName = "Alice",
                LastName = "Johnson",
                EmailAddress = "alice@example.com",
                BirthYear = 1990,
                IsActive = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0)
            };
            
            ps.BindSnakeCase(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindCamelCase_ConvertsPropertyNamesToCamelCase()
        {
            // CamelCase: UserId -> userId, FirstName -> firstName, etc.
            using var ps = _conn.Prepare("CREATE (:CamelCaseUser {userId: $userId, firstName: $firstName, lastName: $lastName, emailAddress: $emailAddress, birthYear: $birthYear, isActive: $isActive, createdAt: $createdAt});");
            
            var user = new TestUser
            {
                UserId = 102L,
                FirstName = "Bob",
                LastName = "Smith",
                EmailAddress = "bob@example.com",
                BirthYear = 1985,
                IsActive = true,
                CreatedAt = new DateTime(2023, 2, 1, 11, 0, 0)
            };
            
            ps.BindCamelCase(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindPascalCase_KeepsPropertyNamesAsPascalCase()
        {
            // PascalCase: UserId -> UserId, FirstName -> FirstName, etc. (no change)
            using var ps = _conn.Prepare("CREATE (:PascalCaseUser {UserId: $UserId, FirstName: $FirstName, LastName: $LastName, EmailAddress: $EmailAddress, BirthYear: $BirthYear, IsActive: $IsActive, CreatedAt: $CreatedAt});");
            
            var user = new TestUser
            {
                UserId = 103L,
                FirstName = "Charlie",
                LastName = "Brown",
                EmailAddress = "charlie@example.com",
                BirthYear = 1992,
                IsActive = false,
                CreatedAt = new DateTime(2023, 3, 1, 12, 0, 0)
            };
            
            ps.BindPascalCase(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindLowercase_ConvertsPropertyNamesToLowercase()
        {
            // Lowercase: UserId -> userid, FirstName -> firstname, etc.
            using var ps = _conn.Prepare("CREATE (:LowercaseUser {userid: $userid, firstname: $firstname, lastname: $lastname, emailaddress: $emailaddress, birthyear: $birthyear, isactive: $isactive, createdat: $createdat});");
            
            var user = new TestUser
            {
                UserId = 104L,
                FirstName = "Diana",
                LastName = "Prince",
                EmailAddress = "diana@example.com",
                BirthYear = 1988,
                IsActive = true,
                CreatedAt = new DateTime(2023, 4, 1, 13, 0, 0)
            };
            
            ps.BindLowercase(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindExact_UsesExactPropertyNames()
        {
            // Exact: UserId -> UserId, FirstName -> FirstName, etc. (no change)
            using var ps = _conn.Prepare("CREATE (:ExactUser {UserId: $UserId, FirstName: $FirstName, LastName: $LastName, EmailAddress: $EmailAddress, BirthYear: $BirthYear, IsActive: $IsActive, CreatedAt: $CreatedAt});");
            
            var user = new TestUser
            {
                UserId = 105L,
                FirstName = "Eve",
                LastName = "Wilson",
                EmailAddress = "eve@example.com",
                BirthYear = 1995,
                IsActive = true,
                CreatedAt = new DateTime(2023, 5, 1, 14, 0, 0)
            };
            
            ps.BindExact(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindDefault_UsesSnakeCaseStrategy()
        {
            using var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});");
            
            var user = new TestUser
            {
                UserId = 6,
                FirstName = "Frank",
                LastName = "Miller",
                EmailAddress = "frank@example.com",
                BirthYear = 1987,
                IsActive = false,
                CreatedAt = new DateTime(2023, 6, 1, 15, 0, 0)
            };
            
            // Test default Bind method (should use snake_case)
            ps.Bind(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void QuerySnakeCase_MapsSnakeCaseColumnsToPascalCaseProperties()
        {
            // Insert test data using snake_case binding
            using (var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});"))
            {
                ps.BindSnakeCase(new TestUser
                {
                    UserId = 10,
                    FirstName = "Grace",
                    LastName = "Lee",
                    EmailAddress = "grace@example.com",
                    BirthYear = 1993,
                    IsActive = true,
                    CreatedAt = new DateTime(2023, 7, 1, 16, 0, 0)
                });
                using var result = ps.Execute();
                Assert.IsTrue(result.IsSuccess);
            }
            
            // Query and verify mapping works
            var users = _conn.Query<TestUser>("MATCH (u:SnakeCaseUser) RETURN u.user_id AS user_id, u.first_name AS first_name, u.last_name AS last_name, u.email_address AS email_address, u.birth_year AS birth_year, u.is_active AS is_active, u.created_at AS created_at");
            
            Assert.IsTrue(users.Count > 0);
            var user = users.First();
            Assert.AreEqual(10L, user.UserId);
            Assert.AreEqual("Grace", user.FirstName);
            Assert.AreEqual("Lee", user.LastName);
            Assert.AreEqual("grace@example.com", user.EmailAddress);
            Assert.AreEqual(1993L, user.BirthYear);
            Assert.IsTrue(user.IsActive);
            Assert.AreEqual(new DateTime(2023, 7, 1, 16, 0, 0), user.CreatedAt);
        }

        [TestMethod]
        public void QueryWithKuzuNameAttribute_OverridesAutomaticMapping()
        {
            // Insert test data
            using (var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});"))
            {
                ps.BindSnakeCase(new TestUser
                {
                    UserId = 11,
                    FirstName = "Henry",
                    LastName = "Davis",
                    EmailAddress = "henry@example.com",
                    BirthYear = 1989,
                    IsActive = false,
                    CreatedAt = new DateTime(2023, 8, 1, 17, 0, 0)
                });
                using var result = ps.Execute();
                Assert.IsTrue(result.IsSuccess);
            }
            
            // Query using POCO with KuzuName attributes
            var users = _conn.Query<TestUserWithAttributes>("MATCH (u:SnakeCaseUser) RETURN u.user_id AS user_id, u.first_name AS first_name, u.last_name AS last_name, u.email_address AS email_address, u.birth_year AS birth_year, u.is_active AS is_active, u.created_at AS created_at");
            
            Assert.IsTrue(users.Count > 0);
            var user = users.First();
            Assert.AreEqual(11L, user.Id); // Mapped via KuzuName attribute
            Assert.AreEqual("Henry", user.Name); // Mapped via KuzuName attribute
            Assert.AreEqual("Davis", user.Surname); // Mapped via KuzuName attribute
            Assert.AreEqual("henry@example.com", user.Email); // Mapped via KuzuName attribute
            Assert.AreEqual(1989L, user.YearOfBirth); // Mapped via KuzuName attribute
            Assert.IsFalse(user.Active); // Mapped via KuzuName attribute
            Assert.AreEqual(new DateTime(2023, 8, 1, 17, 0, 0), user.Created); // Mapped via KuzuName attribute
        }

        [TestMethod]
        public void QueryNullableTypes_MapsCorrectlyWithSnakeCase()
        {
            // Insert test data
            using (var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});"))
            {
                ps.BindSnakeCase(new TestUser
                {
                    UserId = 12,
                    FirstName = "Iris",
                    LastName = "Taylor",
                    EmailAddress = "iris@example.com",
                    BirthYear = 1991,
                    IsActive = true,
                    CreatedAt = new DateTime(2023, 9, 1, 18, 0, 0)
                });
                using var result = ps.Execute();
                Assert.IsTrue(result.IsSuccess);
            }
            
            // Query using POCO with nullable types
            var users = _conn.Query<TestUserWithNullable>("MATCH (u:SnakeCaseUser) RETURN u.user_id AS user_id, u.first_name AS first_name, u.last_name AS last_name, u.email_address AS email_address, u.birth_year AS birth_year, u.is_active AS is_active, u.created_at AS created_at");
            
            Assert.IsTrue(users.Count > 0);
            var user = users.First();
            Assert.AreEqual(12L, user.UserId);
            Assert.AreEqual("Iris", user.FirstName);
            Assert.AreEqual("Taylor", user.LastName);
            Assert.AreEqual("iris@example.com", user.EmailAddress);
            Assert.AreEqual(1991L, user.BirthYear);
            Assert.IsTrue(user.IsActive);
            Assert.AreEqual(new DateTime(2023, 9, 1, 18, 0, 0), user.CreatedAt);
        }

        [TestMethod]
        public void BindWithMixedNamingStrategies_WorksCorrectly()
        {
            // Test that different naming strategies work independently
            var testCases = new[]
            {
                new { Strategy = NamingStrategy.SnakeCase, Table = "SnakeCaseUser", UserId = 2001L, Query = "CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});" },
                new { Strategy = NamingStrategy.CamelCase, Table = "CamelCaseUser", UserId = 2002L, Query = "CREATE (:CamelCaseUser {userId: $userId, firstName: $firstName, lastName: $lastName, emailAddress: $emailAddress, birthYear: $birthYear, isActive: $isActive, createdAt: $createdAt});" },
                new { Strategy = NamingStrategy.PascalCase, Table = "PascalCaseUser", UserId = 2003L, Query = "CREATE (:PascalCaseUser {UserId: $UserId, FirstName: $FirstName, LastName: $LastName, EmailAddress: $EmailAddress, BirthYear: $BirthYear, IsActive: $IsActive, CreatedAt: $CreatedAt});" },
                new { Strategy = NamingStrategy.Lowercase, Table = "LowercaseUser", UserId = 2004L, Query = "CREATE (:LowercaseUser {userid: $userid, firstname: $firstname, lastname: $lastname, emailaddress: $emailaddress, birthyear: $birthyear, isactive: $isactive, createdat: $createdat});" },
                new { Strategy = NamingStrategy.Exact, Table = "ExactUser", UserId = 2005L, Query = "CREATE (:ExactUser {UserId: $UserId, FirstName: $FirstName, LastName: $LastName, EmailAddress: $EmailAddress, BirthYear: $BirthYear, IsActive: $IsActive, CreatedAt: $CreatedAt});" }
            };
            
            foreach (var testCase in testCases)
            {
                using var ps = _conn.Prepare(testCase.Query);
                
                var user = new TestUser
                {
                    UserId = testCase.UserId,
                    FirstName = $"User{testCase.UserId}",
                    LastName = "Test",
                    EmailAddress = $"user{testCase.UserId}@example.com",
                    BirthYear = 1990 + (int)(testCase.UserId - 2000),
                    IsActive = testCase.UserId % 2 == 0,
                    CreatedAt = new DateTime(2023, 1, 1).AddDays(testCase.UserId - 2000)
                };
                
                ps.Bind(user, testCase.Strategy);
                using var result = ps.Execute();
                Assert.IsTrue(result.IsSuccess, $"Failed for strategy {testCase.Strategy}");
            }
        }

        [TestMethod]
        public void BindWithDateTimeHandling_DetectsDateVsTimestampCorrectly()
        {
            using var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});");
            
            var user = new TestUser
            {
                UserId = 30,
                FirstName = "Jack",
                LastName = "Wilson",
                EmailAddress = "jack@example.com",
                BirthYear = 1986,
                IsActive = true,
                CreatedAt = new DateTime(2023, 10, 1, 19, 0, 0) // This should be bound as TIMESTAMP due to "created_at" name
            };
            
            ps.BindSnakeCase(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void BindWithComplexPropertyNames_ConvertsCorrectly()
        {
            using var ps = _conn.Prepare("CREATE (:SnakeCaseUser {user_id: $user_id, first_name: $first_name, last_name: $last_name, email_address: $email_address, birth_year: $birth_year, is_active: $is_active, created_at: $created_at});");
            
            var user = new TestUserWithComplexNames
            {
                UserId = 40,
                FirstName = "Kate",
                LastName = "Anderson",
                EmailAddress = "kate@example.com",
                BirthYear = 1994,
                IsActive = true,
                CreatedAt = new DateTime(2023, 11, 1, 20, 0, 0)
            };
            
            ps.BindSnakeCase(user);
            using var result = ps.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        // Test POCO classes
        private sealed class TestUser
        {
            public long UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string EmailAddress { get; set; } = string.Empty;
            public long BirthYear { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private sealed class TestUserWithAttributes
        {
            [KuzuName("user_id")]
            public long Id { get; set; }
            
            [KuzuName("first_name")]
            public string Name { get; set; } = string.Empty;
            
            [KuzuName("last_name")]
            public string Surname { get; set; } = string.Empty;
            
            [KuzuName("email_address")]
            public string Email { get; set; } = string.Empty;
            
            [KuzuName("birth_year")]
            public long YearOfBirth { get; set; }
            
            [KuzuName("is_active")]
            public bool Active { get; set; }
            
            [KuzuName("created_at")]
            public DateTime Created { get; set; }
        }

        private sealed class TestUserWithNullable
        {
            public long? UserId { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? EmailAddress { get; set; }
            public long? BirthYear { get; set; }
            public bool? IsActive { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        private sealed class TestUserWithComplexNames
        {
            public long UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string EmailAddress { get; set; } = string.Empty;
            public long BirthYear { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public void Dispose()
        {
            _conn?.Dispose();
            _db?.Dispose();
            try { if (Directory.Exists(_dbPath)) Directory.Delete(_dbPath, recursive: true); } catch (System.IO.IOException) { } catch (System.UnauthorizedAccessException) { }
        }
    }
}
