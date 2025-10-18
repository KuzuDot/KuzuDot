using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KuzuDot;

namespace KuzuDot.Examples.Performance
{
    /// <summary>
    /// Batch operations example demonstrating efficient bulk operations
    /// </summary>
    public class BatchOperations
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Batch Operations Example ===");
            
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
            using var connection = database.Connect();

            // Create schema
            Console.WriteLine("Creating schema...");
            CreateSchema(connection);

            // Demonstrate batch operations
            Console.WriteLine("\n=== Batch Operations Examples ===");
            await DemonstrateBatchOperations(connection);

            Console.WriteLine("\n=== Batch Operations Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            connection.NonQuery(@"
                CREATE NODE TABLE Customer(
                    id INT64, 
                    name STRING, 
                    email STRING, 
                    city STRING,
                    country STRING,
                    registration_date DATE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE CustomerOrder(
                    id INT64, 
                    order_date TIMESTAMP,
                    total_amount DOUBLE,
                    status STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Product(
                    id INT64, 
                    name STRING, 
                    category STRING,
                    price DOUBLE,
                    stock_quantity INT32,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Places(
                    FROM Customer TO CustomerOrder,
                    order_date TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Contains(
                    FROM CustomerOrder TO Product,
                    quantity INT32,
                    unit_price DOUBLE
                )");
        }

        private static async Task DemonstrateBatchOperations(Connection connection)
        {
            // 1. Batch insert with prepared statements
            Console.WriteLine("1. Batch insert with prepared statements:");
            await BatchInsertCustomers(connection);

            // 2. Batch insert with object binding
            Console.WriteLine("\n2. Batch insert with object binding:");
            await BatchInsertProducts(connection);

            // 3. Batch update operations
            Console.WriteLine("\n3. Batch update operations:");
            await BatchUpdateOperations(connection);

            // 4. Batch relationship creation
            Console.WriteLine("\n4. Batch relationship creation:");
            await BatchCreateRelationships(connection);

            // 5. Performance comparison
            Console.WriteLine("\n5. Performance comparison:");
            await PerformanceComparison(connection);

            // 6. Large batch operations
            Console.WriteLine("\n6. Large batch operations:");
            await LargeBatchOperations(connection);
        }

        private static async Task BatchInsertCustomers(Connection connection)
        {
            using var customerStmt = connection.Prepare(@"
                CREATE (:Customer {
                    id: $id, 
                    name: $name, 
                    email: $email, 
                    city: $city,
                    country: $country,
                    registration_date: $registration_date
                })");

            var customers = GenerateCustomers(1000);
            var startTime = DateTime.UtcNow;

            foreach (var customer in customers)
            {
                customerStmt.Bind("id", customer.Id);
                customerStmt.Bind("name", customer.Name);
                customerStmt.Bind("email", customer.Email);
                customerStmt.Bind("city", customer.City);
                customerStmt.Bind("country", customer.Country);
                customerStmt.BindDate("registration_date", customer.RegistrationDate);
                customerStmt.Execute();
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"  Inserted {customers.Count} customers in {elapsed.TotalMilliseconds:F0}ms");
        }

        private static async Task BatchInsertProducts(Connection connection)
        {
            using var productStmt = connection.Prepare(@"
                CREATE (:Product {
                    id: $id, 
                    name: $name, 
                    category: $category,
                    price: $price,
                    stock_quantity: $stock_quantity
                })");

            var products = GenerateProducts(500);
            var startTime = DateTime.UtcNow;

            foreach (var product in products)
            {
                productStmt.Bind("id", product.Id);
                productStmt.Bind("name", product.Name);
                productStmt.Bind("category", product.Category);
                productStmt.Bind("price", product.Price);
                productStmt.Bind("stock_quantity", product.StockQuantity);
                productStmt.Execute();
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"  Inserted {products.Count} products in {elapsed.TotalMilliseconds:F0}ms");
        }

        private static async Task BatchUpdateOperations(Connection connection)
        {
            // Batch update product prices
            using var updatePriceStmt = connection.Prepare(@"
                MATCH (p:Product) 
                WHERE p.id = $id 
                SET p.price = $new_price");

            var priceUpdates = GeneratePriceUpdates(100);
            var startTime = DateTime.UtcNow;

            foreach (var update in priceUpdates)
            {
                updatePriceStmt.Bind("id", update.ProductId);
                updatePriceStmt.Bind("new_price", update.NewPrice);
                updatePriceStmt.Execute();
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"  Updated {priceUpdates.Count} product prices in {elapsed.TotalMilliseconds:F0}ms");

            // Batch update stock quantities
            using var updateStockStmt = connection.Prepare(@"
                MATCH (p:Product) 
                WHERE p.category = $category 
                SET p.stock_quantity = p.stock_quantity + $increment");

            var categories = new[] { "Electronics", "Books", "Clothing", "Home", "Sports" };
            foreach (var category in categories)
            {
                updateStockStmt.Bind("category", category);
                updateStockStmt.Bind("increment", 50);
                updateStockStmt.Execute();
            }

            Console.WriteLine($"  Updated stock for {categories.Length} categories");
        }

        private static async Task BatchCreateRelationships(Connection connection)
        {
            // Create orders
            using var orderStmt = connection.Prepare(@"
                CREATE (:CustomerOrder {
                    id: $id, 
                    order_date: $order_date,
                    total_amount: $total_amount,
                    status: $status
                })");

            var orders = GenerateOrders(200);
            foreach (var order in orders)
            {
                orderStmt.Bind("id", order.Id);
                orderStmt.Bind("order_date", order.OrderDate);
                orderStmt.Bind("total_amount", order.TotalAmount);
                orderStmt.Bind("status", order.Status);
                orderStmt.Execute();
            }

            // Create customer-order relationships
            using var placesStmt = connection.Prepare(@"
                MATCH (c:Customer), (o:CustomerOrder) 
                WHERE c.id = $customer_id AND o.id = $order_id 
                CREATE (c)-[:Places {order_date: $order_date}]->(o)");

            var startTime = DateTime.UtcNow;
            var relationshipCount = 0;

            for (int i = 0; i < orders.Count; i++)
            {
                var customerId = (i % 100) + 1; // Distribute orders among customers
                var order = orders[i];
                
                placesStmt.Bind("customer_id", customerId);
                placesStmt.Bind("order_id", order.Id);
                placesStmt.BindTimestamp("order_date", order.OrderDate);
                placesStmt.Execute();
                relationshipCount++;
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"  Created {relationshipCount} customer-order relationships in {elapsed.TotalMilliseconds:F0}ms");

            // Create order-product relationships
            using var containsStmt = connection.Prepare(@"
                MATCH (o:CustomerOrder), (p:Product) 
                WHERE o.id = $order_id AND p.id = $product_id 
                CREATE (o)-[:Contains {quantity: $quantity, unit_price: $unit_price}]->(p)");

            var orderProducts = GenerateOrderProducts(orders.Count, 500);
            startTime = DateTime.UtcNow;

            foreach (var orderProduct in orderProducts)
            {
                containsStmt.Bind("order_id", orderProduct.OrderId);
                containsStmt.Bind("product_id", orderProduct.ProductId);
                containsStmt.Bind("quantity", orderProduct.Quantity);
                containsStmt.Bind("unit_price", orderProduct.UnitPrice);
                containsStmt.Execute();
            }

            elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"  Created {orderProducts.Count} order-product relationships in {elapsed.TotalMilliseconds:F0}ms");
        }

        private static async Task PerformanceComparison(Connection connection)
        {
            Console.WriteLine("  Performance comparison - Individual vs Batch operations:");

            // Individual operations
            var individualStart = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                using var result = connection.Query($"MATCH (c:Customer) WHERE c.id = {i % 100 + 1} RETURN c.name");
                // Process result
            }
            var individualTime = DateTime.UtcNow - individualStart;

            // Batch operations with prepared statements
            using var batchStmt = connection.Prepare("MATCH (c:Customer) WHERE c.id = $id RETURN c.name");
            var batchStart = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                batchStmt.Bind("id", i % 100 + 1);
                using var result = batchStmt.Execute();
                // Process result
            }
            var batchTime = DateTime.UtcNow - batchStart;

            Console.WriteLine($"    Individual operations: {individualTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"    Batch operations: {batchTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"    Performance improvement: {(individualTime.TotalMilliseconds / batchTime.TotalMilliseconds):F1}x faster");
        }

        private static async Task LargeBatchOperations(Connection connection)
        {
            Console.WriteLine("  Large batch operations - Processing 10,000 records:");

            // Large batch insert
            using var largeInsertStmt = connection.Prepare(@"
                CREATE (:Customer {
                    id: $id, 
                    name: $name, 
                    email: $email, 
                    city: $city,
                    country: $country,
                    registration_date: $registration_date
                })");

            var largeDataset = GenerateCustomers(10000);
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < largeDataset.Count; i++)
            {
                var customer = largeDataset[i];
                // Offset IDs to avoid conflicts with previous batch
                largeInsertStmt.Bind("id", customer.Id + 10000);
                largeInsertStmt.Bind("name", customer.Name);
                largeInsertStmt.Bind("email", customer.Email);
                largeInsertStmt.Bind("city", customer.City);
                largeInsertStmt.Bind("country", customer.Country);
                largeInsertStmt.BindDate("registration_date", customer.RegistrationDate);
                largeInsertStmt.Execute();

                if (i % 1000 == 0 && i > 0)
                {
                    Console.WriteLine($"    Processed {i} records...");
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"  Inserted {largeDataset.Count} customers in {elapsed.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  Average: {(largeDataset.Count / elapsed.TotalSeconds):F0} records/second");

            // Verify the data
            var count = connection.ExecuteScalar<long>("MATCH (c:Customer) RETURN COUNT(c)");
            Console.WriteLine($"  Verified: {count} customers in database");
        }

        // Helper methods to generate test data
        private static List<CustomerData> GenerateCustomers(int count)
        {
            var customers = new List<CustomerData>();
            var cities = new[] { "New York", "London", "Tokyo", "Paris", "Sydney", "Berlin", "Toronto", "Mumbai" };
            var countries = new[] { "USA", "UK", "Japan", "France", "Australia", "Germany", "Canada", "India" };

            for (int i = 1; i <= count; i++)
            {
                customers.Add(new CustomerData
                {
                    Id = i,
                    Name = $"Customer{i}",
                    Email = $"customer{i}@example.com",
                    City = cities[i % cities.Length],
                    Country = countries[i % countries.Length],
                    RegistrationDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(365))
                });
            }

            return customers;
        }

        private static List<ProductData> GenerateProducts(int count)
        {
            var products = new List<ProductData>();
            var categories = new[] { "Electronics", "Books", "Clothing", "Home", "Sports", "Toys", "Beauty", "Automotive" };

            for (int i = 1; i <= count; i++)
            {
                products.Add(new ProductData
                {
                    Id = i,
                    Name = $"Product{i}",
                    Category = categories[i % categories.Length],
                    Price = Random.Shared.NextDouble() * 1000 + 10,
                    StockQuantity = Random.Shared.Next(1, 1000)
                });
            }

            return products;
        }

        private static List<OrderData> GenerateOrders(int count)
        {
            var orders = new List<OrderData>();
            var statuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

            for (int i = 1; i <= count; i++)
            {
                orders.Add(new OrderData
                {
                    Id = i,
                    OrderDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(30)),
                    TotalAmount = Random.Shared.NextDouble() * 500 + 10,
                    Status = statuses[Random.Shared.Next(statuses.Length)]
                });
            }

            return orders;
        }

        private static List<PriceUpdate> GeneratePriceUpdates(int count)
        {
            var updates = new List<PriceUpdate>();

            for (int i = 1; i <= count; i++)
            {
                updates.Add(new PriceUpdate
                {
                    ProductId = i,
                    NewPrice = Random.Shared.NextDouble() * 1000 + 10
                });
            }

            return updates;
        }

        private static List<OrderProduct> GenerateOrderProducts(int orderCount, int productCount)
        {
            var orderProducts = new List<OrderProduct>();

            for (int i = 1; i <= orderCount; i++)
            {
                var productCountForOrder = Random.Shared.Next(1, 5);
                for (int j = 0; j < productCountForOrder; j++)
                {
                    orderProducts.Add(new OrderProduct
                    {
                        OrderId = i,
                        ProductId = Random.Shared.Next(1, productCount + 1),
                        Quantity = Random.Shared.Next(1, 10),
                        UnitPrice = Random.Shared.NextDouble() * 100 + 5
                    });
                }
            }

            return orderProducts;
        }
    }

    // Data classes for batch operations
    public class CustomerData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    public class ProductData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
    }

    public class OrderData
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PriceUpdate
    {
        public long ProductId { get; set; }
        public double NewPrice { get; set; }
    }

    public class OrderProduct
    {
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }
}
