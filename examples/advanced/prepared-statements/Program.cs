using System;
using KuzuDot;

namespace KuzuDot.Examples.Advanced
{
    /// <summary>
    /// Prepared statements example demonstrating parameterized queries
    /// </summary>
    public class PreparedStatements
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Prepared Statements Example ===");
            
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

            // Demonstrate prepared statements
            Console.WriteLine("\n=== Prepared Statements Examples ===");
            DemonstratePreparedStatements(connection);

            Console.WriteLine("\n=== Prepared Statements Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            connection.NonQuery(@"
                CREATE NODE TABLE User(
                    id INT64, 
                    username STRING, 
                    email STRING, 
                    age INT32,
                    city STRING,
                    country STRING,
                    created_at TIMESTAMP,
                    is_active BOOLEAN,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Product(
                    id INT64, 
                    name STRING, 
                    category STRING,
                    price DOUBLE,
                    stock_quantity INT32,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Purchased(
                    FROM User TO Product,
                    purchase_date TIMESTAMP,
                    quantity INT32,
                    total_amount DOUBLE
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert users
            var users = new[]
            {
                new { Id = 1L, Username = "alice_j", Email = "alice@example.com", Age = 28, City = "New York", Country = "USA", CreatedAt = DateTime.UtcNow.AddDays(-30), IsActive = true },
                new { Id = 2L, Username = "bob_smith", Email = "bob@example.com", Age = 32, City = "San Francisco", Country = "USA", CreatedAt = DateTime.UtcNow.AddDays(-25), IsActive = true },
                new { Id = 3L, Username = "charlie_b", Email = "charlie@example.com", Age = 25, City = "London", Country = "UK", CreatedAt = DateTime.UtcNow.AddDays(-20), IsActive = true },
                new { Id = 4L, Username = "diana_p", Email = "diana@example.com", Age = 30, City = "Toronto", Country = "Canada", CreatedAt = DateTime.UtcNow.AddDays(-15), IsActive = false },
                new { Id = 5L, Username = "eve_w", Email = "eve@example.com", Age = 27, City = "Sydney", Country = "Australia", CreatedAt = DateTime.UtcNow.AddDays(-10), IsActive = true }
            };

            using var userStmt = connection.Prepare(@"
                CREATE (:User {
                    id: $id, 
                    username: $username, 
                    email: $email, 
                    age: $age,
                    city: $city,
                    country: $country,
                    created_at: $created_at,
                    is_active: $is_active
                })");

            foreach (var user in users)
            {
                userStmt.Bind("id", user.Id);
                userStmt.Bind("username", user.Username);
                userStmt.Bind("email", user.Email);
                userStmt.Bind("age", user.Age);
                userStmt.Bind("city", user.City);
                userStmt.Bind("country", user.Country);
                userStmt.BindTimestamp("created_at", user.CreatedAt);
                userStmt.Bind("is_active", user.IsActive);
                userStmt.Execute();
                Console.WriteLine($"  Created user: {user.Username}");
            }

            // Insert products
            var products = new[]
            {
                new { Id = 1L, Name = "Laptop Pro", Category = "Electronics", Price = 1299.99, StockQuantity = 50, CreatedAt = DateTime.UtcNow.AddDays(-60) },
                new { Id = 2L, Name = "Wireless Mouse", Category = "Electronics", Price = 29.99, StockQuantity = 200, CreatedAt = DateTime.UtcNow.AddDays(-45) },
                new { Id = 3L, Name = "Programming Book", Category = "Books", Price = 49.99, StockQuantity = 100, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new { Id = 4L, Name = "Coffee Mug", Category = "Accessories", Price = 12.99, StockQuantity = 150, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new { Id = 5L, Name = "Desk Lamp", Category = "Furniture", Price = 89.99, StockQuantity = 75, CreatedAt = DateTime.UtcNow.AddDays(-15) }
            };

            using var productStmt = connection.Prepare(@"
                CREATE (:Product {
                    id: $id, 
                    name: $name, 
                    category: $category,
                    price: $price,
                    stock_quantity: $stock_quantity,
                    created_at: $created_at
                })");

            foreach (var product in products)
            {
                productStmt.Bind("id", product.Id);
                productStmt.Bind("name", product.Name);
                productStmt.Bind("category", product.Category);
                productStmt.Bind("price", product.Price);
                productStmt.Bind("stock_quantity", product.StockQuantity);
                productStmt.BindTimestamp("created_at", product.CreatedAt);
                productStmt.Execute();
                Console.WriteLine($"  Created product: {product.Name}");
            }

            // Create purchase relationships
            var purchases = new[]
            {
                new { UserId = 1L, ProductId = 1L, PurchaseDate = DateTime.UtcNow.AddDays(-5), Quantity = 1, TotalAmount = 1299.99 },
                new { UserId = 1L, ProductId = 2L, PurchaseDate = DateTime.UtcNow.AddDays(-3), Quantity = 2, TotalAmount = 59.98 },
                new { UserId = 2L, ProductId = 3L, PurchaseDate = DateTime.UtcNow.AddDays(-7), Quantity = 1, TotalAmount = 49.99 },
                new { UserId = 2L, ProductId = 4L, PurchaseDate = DateTime.UtcNow.AddDays(-2), Quantity = 3, TotalAmount = 38.97 },
                new { UserId = 3L, ProductId = 1L, PurchaseDate = DateTime.UtcNow.AddDays(-10), Quantity = 1, TotalAmount = 1299.99 },
                new { UserId = 3L, ProductId = 5L, PurchaseDate = DateTime.UtcNow.AddDays(-1), Quantity = 1, TotalAmount = 89.99 },
                new { UserId = 5L, ProductId = 2L, PurchaseDate = DateTime.UtcNow.AddDays(-4), Quantity = 1, TotalAmount = 29.99 },
                new { UserId = 5L, ProductId = 3L, PurchaseDate = DateTime.UtcNow.AddDays(-6), Quantity = 2, TotalAmount = 99.98 }
            };

            using var purchaseStmt = connection.Prepare(@"
                MATCH (u:User), (p:Product) 
                WHERE u.id = $user_id AND p.id = $product_id 
                CREATE (u)-[:Purchased {
                    purchase_date: $purchase_date, 
                    quantity: $quantity, 
                    total_amount: $total_amount
                }]->(p)");

            foreach (var purchase in purchases)
            {
                purchaseStmt.Bind("user_id", purchase.UserId);
                purchaseStmt.Bind("product_id", purchase.ProductId);
                purchaseStmt.BindTimestamp("purchase_date", purchase.PurchaseDate);
                purchaseStmt.Bind("quantity", purchase.Quantity);
                purchaseStmt.Bind("total_amount", purchase.TotalAmount);
                purchaseStmt.Execute();
            }

            Console.WriteLine("  Created purchase relationships");
        }

        private static void DemonstratePreparedStatements(Connection connection)
        {
            // 1. Basic prepared statement with single parameter
            Console.WriteLine("1. Basic prepared statement - Find user by ID:");
            using var userByIdStmt = connection.Prepare("MATCH (u:User) WHERE u.id = $id RETURN u.username, u.email, u.age");
            
            var userIds = new[] { 1L, 3L, 5L };
            foreach (var id in userIds)
            {
                userByIdStmt.Bind("id", id);
                using var result = userByIdStmt.Execute();
                
                while (result.HasNext())
                {
                    using var row = result.GetNext();
                    var username = row.GetValueAs<string>(0);
                    var email = row.GetValueAs<string>(1);
                    var age = row.GetValueAs<int>(2);
                    
                    Console.WriteLine($"  User {id}: {username} ({email}), age {age}");
                }
            }

            // 2. Prepared statement with multiple parameters
            Console.WriteLine("\n2. Multiple parameters - Find users by age range and country:");
            using var userByAgeCountryStmt = connection.Prepare(@"
                MATCH (u:User) 
                WHERE u.age >= $min_age AND u.age <= $max_age AND u.country = $country 
                RETURN u.username, u.age, u.city
                ORDER BY u.age");

            var ageRanges = new[]
            {
                new { MinAge = 25, MaxAge = 30, Country = "USA" },
                new { MinAge = 25, MaxAge = 35, Country = "UK" }
            };

            foreach (var range in ageRanges)
            {
                userByAgeCountryStmt.Bind("min_age", range.MinAge);
                userByAgeCountryStmt.Bind("max_age", range.MaxAge);
                userByAgeCountryStmt.Bind("country", range.Country);
                
                using var result = userByAgeCountryStmt.Execute();
                Console.WriteLine($"  Users aged {range.MinAge}-{range.MaxAge} in {range.Country}:");
                
                while (result.HasNext())
                {
                    using var row = result.GetNext();
                    var username = row.GetValueAs<string>(0);
                    var age = row.GetValueAs<int>(1);
                    var city = row.GetValueAs<string>(2);
                    
                    Console.WriteLine($"    {username}, age {age}, from {city}");
                }
            }

            // 3. Object binding
            Console.WriteLine("\n3. Object binding - Insert new user:");
            using var insertUserStmt = connection.Prepare(@"
                CREATE (:User {
                    id: $id, 
                    username: $username, 
                    email: $email, 
                    age: $age,
                    city: $city,
                    country: $country,
                    created_at: $created_at,
                    is_active: $is_active
                })");

            var newUser = new UserData
            {
                Id = 6,
                Username = "frank_m",
                Email = "frank@example.com",
                Age = 35,
                City = "Berlin",
                Country = "Germany",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            insertUserStmt.Bind(newUser);
            insertUserStmt.Execute();
            Console.WriteLine($"  Created new user: {newUser.Username}");

            // 4. Dynamic query building
            Console.WriteLine("\n4. Dynamic query building - Search products:");
            using var searchProductsStmt = connection.Prepare(@"
                MATCH (p:Product) 
                WHERE p.name CONTAINS $search_term 
                   AND p.price >= $min_price 
                   AND p.price <= $max_price
                   AND p.stock_quantity > 0
                RETURN p.name, p.category, p.price, p.stock_quantity
                ORDER BY p.price");

            var searchCriteria = new[]
            {
                new { SearchTerm = "Laptop", MinPrice = 1000.0, MaxPrice = 2000.0 },
                new { SearchTerm = "Book", MinPrice = 0.0, MaxPrice = 100.0 },
                new { SearchTerm = "", MinPrice = 0.0, MaxPrice = 50.0 } // All products under $50
            };

            foreach (var criteria in searchCriteria)
            {
                searchProductsStmt.Bind("search_term", criteria.SearchTerm);
                searchProductsStmt.Bind("min_price", criteria.MinPrice);
                searchProductsStmt.Bind("max_price", criteria.MaxPrice);
                
                using var result = searchProductsStmt.Execute();
                Console.WriteLine($"  Products matching '{criteria.SearchTerm}' (${criteria.MinPrice}-${criteria.MaxPrice}):");
                
                while (result.HasNext())
                {
                    using var row = result.GetNext();
                    var name = row.GetValueAs<string>(0);
                    var category = row.GetValueAs<string>(1);
                    var price = row.GetValueAs<double>(2);
                    var stock = row.GetValueAs<int>(3);
                    
                    Console.WriteLine($"    {name} ({category}) - ${price:F2}, stock: {stock}");
                }
            }

            // 5. Batch operations
            Console.WriteLine("\n5. Batch operations - Update product prices:");
            using var updatePriceStmt = connection.Prepare("MATCH (p:Product) WHERE p.id = $id SET p.price = $new_price");
            
            var priceUpdates = new[]
            {
                new { Id = 1L, NewPrice = 1199.99 }, // Laptop discount
                new { Id = 2L, NewPrice = 24.99 },   // Mouse discount
                new { Id = 3L, NewPrice = 39.99 }    // Book discount
            };

            foreach (var update in priceUpdates)
            {
                updatePriceStmt.Bind("id", update.Id);
                updatePriceStmt.Bind("new_price", update.NewPrice);
                updatePriceStmt.Execute();
                Console.WriteLine($"  Updated product {update.Id} price to ${update.NewPrice:F2}");
            }

            // 6. Complex relationship queries
            Console.WriteLine("\n6. Complex relationship queries - User purchase history:");
            using var purchaseHistoryStmt = connection.Prepare(@"
                MATCH (u:User)-[p:Purchased]->(prod:Product) 
                WHERE u.id = $user_id 
                RETURN prod.name, p.quantity, p.total_amount, p.purchase_date
                ORDER BY p.purchase_date DESC");

            var userIdsForHistory = new[] { 1L, 2L, 3L };
            foreach (var userId in userIdsForHistory)
            {
                purchaseHistoryStmt.Bind("user_id", userId);
                using var result = purchaseHistoryStmt.Execute();
                
                Console.WriteLine($"  Purchase history for user {userId}:");
                while (result.HasNext())
                {
                    using var row = result.GetNext();
                    var productName = row.GetValueAs<string>(0);
                    var quantity = row.GetValueAs<int>(1);
                    var totalAmount = row.GetValueAs<double>(2);
                    var purchaseDate = row.GetValueAs<DateTime>(3);
                    
                    Console.WriteLine($"    {productName} x{quantity} - ${totalAmount:F2} on {purchaseDate:yyyy-MM-dd}");
                }
            }

            // 7. Aggregation queries
            Console.WriteLine("\n7. Aggregation queries - Sales by category:");
            using var salesByCategoryStmt = connection.Prepare(@"
                MATCH (u:User)-[p:Purchased]->(prod:Product) 
                WHERE p.purchase_date >= $start_date 
                RETURN prod.category, 
                       COUNT(p) as purchase_count, 
                       SUM(p.total_amount) as total_revenue,
                       AVG(p.total_amount) as avg_purchase
                ORDER BY total_revenue DESC");

            var startDate = DateTime.UtcNow.AddDays(-30);
            salesByCategoryStmt.Bind("start_date", startDate);
            
            using var salesResult = salesByCategoryStmt.Execute();
            Console.WriteLine($"  Sales by category (last 30 days):");
            
            while (salesResult.HasNext())
            {
                using var row = salesResult.GetNext();
                var category = row.GetValueAs<string>(0);
                var purchaseCount = row.GetValueAs<long>(1);
                var totalRevenue = row.GetValueAs<double>(2);
                var avgPurchase = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {category}: {purchaseCount} purchases, ${totalRevenue:F2} revenue, avg: ${avgPurchase:F2}");
            }

            // 8. Error handling with prepared statements
            Console.WriteLine("\n8. Error handling - Invalid parameter binding:");
            using var errorStmt = connection.Prepare("MATCH (u:User) WHERE u.id = $id RETURN u.username");
            
            try
            {
                // This will work
                errorStmt.Bind("id", 1L);
                using var result = errorStmt.Execute();
                Console.WriteLine("  Valid parameter binding succeeded");
                
                // This will fail - binding wrong parameter name
                errorStmt.Bind("invalid_param", 2L);
                using var errorResult = errorStmt.Execute();
            }
            catch (KuzuException ex)
            {
                Console.WriteLine($"  Expected error caught: {ex.Message}");
            }

            // 9. Performance comparison
            Console.WriteLine("\n9. Performance comparison - Prepared vs Direct queries:");
            
            // Direct query (slower for repeated operations)
            var directStart = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                using var directResult = connection.Query($"MATCH (u:User) WHERE u.id = {i % 5 + 1} RETURN u.username");
                // Process result
            }
            var directTime = DateTime.UtcNow - directStart;
            
            // Prepared statement (faster for repeated operations)
            using var perfStmt = connection.Prepare("MATCH (u:User) WHERE u.id = $id RETURN u.username");
            var preparedStart = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                perfStmt.Bind("id", i % 5 + 1);
                using var preparedResult = perfStmt.Execute();
                // Process result
            }
            var preparedTime = DateTime.UtcNow - preparedStart;
            
            Console.WriteLine($"  Direct queries (100 iterations): {directTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Prepared statements (100 iterations): {preparedTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Performance improvement: {(directTime.TotalMilliseconds / preparedTime.TotalMilliseconds):F1}x faster");
        }
    }

    // Helper class for object binding
    public class UserData
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
