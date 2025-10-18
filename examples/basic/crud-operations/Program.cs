using System;
using KuzuDot;
using KuzuDot.Value;

namespace KuzuDot.Examples.Basic
{
    /// <summary>
    /// CRUD operations example demonstrating Create, Read, Update, Delete operations
    /// </summary>
    public class CrudOperations
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot CRUD Operations Example ===");
            
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
            connection.NonQuery(@"
                CREATE NODE TABLE Product(
                    id INT64, 
                    name STRING, 
                    price DOUBLE, 
                    category STRING,
                    in_stock BOOLEAN,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            // CREATE operations
            Console.WriteLine("\n=== CREATE Operations ===");
            CreateProducts(connection);

            // READ operations
            Console.WriteLine("\n=== READ Operations ===");
            ReadProducts(connection);

            // UPDATE operations
            Console.WriteLine("\n=== UPDATE Operations ===");
            UpdateProducts(connection);

            // DELETE operations
            Console.WriteLine("\n=== DELETE Operations ===");
            DeleteProducts(connection);

            // Final state
            Console.WriteLine("\n=== Final State ===");
            ReadProducts(connection);

            Console.WriteLine("\n=== CRUD Example completed successfully! ===");
        }

        private static void CreateProducts(Connection connection)
        {
            var products = new[]
            {
                new { Id = 1L, Name = "Laptop", Price = 999.99, Category = "Electronics", InStock = true, CreatedAt = DateTime.UtcNow },
                new { Id = 2L, Name = "Mouse", Price = 29.99, Category = "Electronics", InStock = true, CreatedAt = DateTime.UtcNow },
                new { Id = 3L, Name = "Keyboard", Price = 79.99, Category = "Electronics", InStock = false, CreatedAt = DateTime.UtcNow },
                new { Id = 4L, Name = "Book", Price = 19.99, Category = "Books", InStock = true, CreatedAt = DateTime.UtcNow },
                new { Id = 5L, Name = "Pen", Price = 2.99, Category = "Office", InStock = true, CreatedAt = DateTime.UtcNow }
            };

            using var insertStmt = connection.Prepare(@"
                CREATE (:Product {
                    id: $id, 
                    name: $name, 
                    price: $price, 
                    category: $category,
                    in_stock: $in_stock,
                    created_at: $created_at
                })");

            foreach (var product in products)
            {
                insertStmt.Bind("id", product.Id);
                insertStmt.Bind("name", product.Name);
                insertStmt.Bind("price", product.Price);
                insertStmt.Bind("category", product.Category);
                insertStmt.Bind("in_stock", product.InStock);
                insertStmt.BindTimestamp("created_at", product.CreatedAt);
                
                insertStmt.Execute();
                Console.WriteLine($"  Created: {product.Name} (${product.Price:F2})");
            }
        }

        private static void ReadProducts(Connection connection)
        {
            Console.WriteLine("All products:");
            using var result = connection.Query("MATCH (p:Product) RETURN p.id, p.name, p.price, p.category, p.in_stock ORDER BY p.id");
            
            while (result.HasNext())
            {
                using var row = result.GetNext();
                var id = row.GetValueAs<long>(0);
                var name = row.GetValueAs<string>(1);
                var price = row.GetValueAs<double>(2);
                var category = row.GetValueAs<string>(3);
                var inStock = row.GetValueAs<bool>(4);
                
                Console.WriteLine($"  ID: {id}, Name: {name}, Price: ${price:F2}, Category: {category}, In Stock: {inStock}");
            }

            Console.WriteLine("\nProducts in stock:");
            using var inStockResult = connection.Query("MATCH (p:Product) WHERE p.in_stock = true RETURN p.name, p.price ORDER BY p.price");
            
            while (inStockResult.HasNext())
            {
                using var row = inStockResult.GetNext();
                var name = row.GetValueAs<string>(0);
                var price = row.GetValueAs<double>(1);
                
                Console.WriteLine($"  {name}: ${price:F2}");
            }

            Console.WriteLine("\nProducts by category:");
            using var categoryResult = connection.Query(@"
                MATCH (p:Product) 
                RETURN p.category, COUNT(p) as count, AVG(p.price) as avg_price 
                ORDER BY count DESC");

            while (categoryResult.HasNext())
            {
                using var row = categoryResult.GetNext();
                var category = row.GetValueAs<string>(0);
                var count = row.GetValueAs<long>(1);
                var avgPrice = row.GetValueAs<double>(2);
                
                Console.WriteLine($"  {category}: {count} products, avg price: ${avgPrice:F2}");
            }
        }

        private static void UpdateProducts(Connection connection)
        {
            Console.WriteLine("Updating product prices...");
            
            // Update laptop price
            connection.NonQuery("MATCH (p:Product) WHERE p.name = 'Laptop' SET p.price = 899.99");
            Console.WriteLine("  Updated Laptop price to $899.99");

            // Update mouse stock status
            connection.NonQuery("MATCH (p:Product) WHERE p.name = 'Mouse' SET p.in_stock = false");
            Console.WriteLine("  Set Mouse as out of stock");

            // Update keyboard stock status
            connection.NonQuery("MATCH (p:Product) WHERE p.name = 'Keyboard' SET p.in_stock = true");
            Console.WriteLine("  Set Keyboard as in stock");

            // Bulk update - increase prices by 10%
            connection.NonQuery("MATCH (p:Product) WHERE p.category = 'Electronics' SET p.price = p.price * 1.1");
            Console.WriteLine("  Increased Electronics prices by 10%");

            Console.WriteLine("\nUpdated products:");
            using var result = connection.Query("MATCH (p:Product) WHERE p.category = 'Electronics' RETURN p.name, p.price, p.in_stock ORDER BY p.name");
            
            while (result.HasNext())
            {
                using var row = result.GetNext();
                var name = row.GetValueAs<string>(0);
                var price = row.GetValueAs<double>(1);
                var inStock = row.GetValueAs<bool>(2);
                
                Console.WriteLine($"  {name}: ${price:F2}, In Stock: {inStock}");
            }
        }

        private static void DeleteProducts(Connection connection)
        {
            Console.WriteLine("Deleting products...");
            
            // Delete out of stock products
            long deletedCount = connection.ExecuteScalar<long>("MATCH (p:Product) WHERE p.in_stock = false RETURN COUNT(p)");
            connection.NonQuery("MATCH (p:Product) WHERE p.in_stock = false DELETE p");
            Console.WriteLine($"  Deleted {deletedCount} out of stock products");

            // Delete products with price less than $5
            long cheapCount = connection.ExecuteScalar<long>("MATCH (p:Product) WHERE p.price < 5.0 RETURN COUNT(p)");
            connection.NonQuery("MATCH (p:Product) WHERE p.price < 5.0 DELETE p");
            Console.WriteLine($"  Deleted {cheapCount} products with price < $5");

            Console.WriteLine("\nRemaining products:");
            using var result = connection.Query("MATCH (p:Product) RETURN p.name, p.price, p.category ORDER BY p.name");
            
            while (result.HasNext())
            {
                using var row = result.GetNext();
                var name = row.GetValueAs<string>(0);
                var price = row.GetValueAs<double>(1);
                var category = row.GetValueAs<string>(2);
                
                Console.WriteLine($"  {name}: ${price:F2} ({category})");
            }
        }
    }
}
