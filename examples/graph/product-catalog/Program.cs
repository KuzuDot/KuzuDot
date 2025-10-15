using System;
using KuzuDot;

namespace KuzuDot.Examples.Graph
{
    /// <summary>
    /// Product catalog example demonstrating e-commerce product relationships
    /// </summary>
    public class ProductCatalog
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Product Catalog Example ===");
            
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
            Console.WriteLine("Creating product catalog schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate product catalog queries
            Console.WriteLine("\n=== Product Catalog Examples ===");
            DemonstrateProductCatalog(connection);

            Console.WriteLine("\n=== Product Catalog Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            connection.NonQuery(@"
                CREATE NODE TABLE Product(
                    id INT64, 
                    name STRING, 
                    description STRING,
                    price DOUBLE,
                    sku STRING,
                    category STRING,
                    brand STRING,
                    weight DOUBLE,
                    dimensions STRING,
                    in_stock BOOLEAN,
                    stock_quantity INT32,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Category(
                    id INT64, 
                    name STRING, 
                    description STRING,
                    parent_category STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Brand(
                    id INT64, 
                    name STRING, 
                    country STRING,
                    founded_year INT32,
                    website STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Customer(
                    id INT64, 
                    name STRING, 
                    email STRING,
                    phone STRING,
                    address STRING,
                    city STRING,
                    country STRING,
                    registration_date DATE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Order(
                    id INT64, 
                    order_date TIMESTAMP,
                    total_amount DOUBLE,
                    status STRING,
                    shipping_address STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Review(
                    id INT64, 
                    rating INT32,
                    comment STRING,
                    review_date TIMESTAMP,
                    verified BOOLEAN,
                    PRIMARY KEY(id)
                )");

            // Create relationship tables
            connection.NonQuery(@"
                CREATE REL TABLE BelongsTo(
                    FROM Product TO Category
                )");

            connection.NonQuery(@"
                CREATE REL TABLE ManufacturedBy(
                    FROM Product TO Brand
                )");

            connection.NonQuery(@"
                CREATE REL TABLE RelatedTo(
                    FROM Product TO Product,
                    relationship_type STRING,
                    strength DOUBLE
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Contains(
                    FROM Order TO Product,
                    quantity INT32,
                    unit_price DOUBLE
                )");

            connection.NonQuery(@"
                CREATE REL TABLE PlacedBy(
                    FROM Customer TO Order
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Reviews(
                    FROM Customer TO Product,
                    review_id INT64
                )");

            connection.NonQuery(@"
                CREATE REL TABLE HasReview(
                    FROM Product TO Review
                )");

            connection.NonQuery(@"
                CREATE REL TABLE WroteReview(
                    FROM Customer TO Review
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert categories
            var categories = GenerateCategories();
            using var categoryStmt = connection.Prepare(@"
                CREATE (:Category {
                    id: $id, 
                    name: $name, 
                    description: $description,
                    parent_category: $parent_category
                })");

            foreach (var category in categories)
            {
                categoryStmt.Bind(category);
                categoryStmt.Execute();
            }

            // Insert brands
            var brands = GenerateBrands();
            using var brandStmt = connection.Prepare(@"
                CREATE (:Brand {
                    id: $id, 
                    name: $name, 
                    country: $country,
                    founded_year: $founded_year,
                    website: $website
                })");

            foreach (var brand in brands)
            {
                brandStmt.Bind(brand);
                brandStmt.Execute();
            }

            // Insert products
            var products = GenerateProducts();
            using var productStmt = connection.Prepare(@"
                CREATE (:Product {
                    id: $id, 
                    name: $name, 
                    description: $description,
                    price: $price,
                    sku: $sku,
                    category: $category,
                    brand: $brand,
                    weight: $weight,
                    dimensions: $dimensions,
                    in_stock: $in_stock,
                    stock_quantity: $stock_quantity,
                    created_at: $created_at
                })");

            foreach (var product in products)
            {
                productStmt.Bind(product);
                productStmt.Execute();
            }

            // Insert customers
            var customers = GenerateCustomers();
            using var customerStmt = connection.Prepare(@"
                CREATE (:Customer {
                    id: $id, 
                    name: $name, 
                    email: $email,
                    phone: $phone,
                    address: $address,
                    city: $city,
                    country: $country,
                    registration_date: $registration_date
                })");

            foreach (var customer in customers)
            {
                customerStmt.Bind(customer);
                customerStmt.Execute();
            }

            // Insert orders
            var orders = GenerateOrders();
            using var orderStmt = connection.Prepare(@"
                CREATE (:Order {
                    id: $id, 
                    order_date: $order_date,
                    total_amount: $total_amount,
                    status: $status,
                    shipping_address: $shipping_address
                })");

            foreach (var order in orders)
            {
                orderStmt.Bind(order);
                orderStmt.Execute();
            }

            // Insert reviews
            var reviews = GenerateReviews();
            using var reviewStmt = connection.Prepare(@"
                CREATE (:Review {
                    id: $id, 
                    rating: $rating,
                    comment: $comment,
                    review_date: $review_date,
                    verified: $verified
                })");

            foreach (var review in reviews)
            {
                reviewStmt.Bind(review);
                reviewStmt.Execute();
            }

            Console.WriteLine("  Created categories, brands, products, customers, orders, and reviews");

            // Create relationships
            CreateRelationships(connection);
        }

        private static void CreateRelationships(Connection connection)
        {
            Console.WriteLine("Creating relationships...");

            // Product-Category relationships
            using var productCategoryStmt = connection.Prepare(@"
                MATCH (p:Product), (c:Category) 
                WHERE p.id = $product_id AND c.id = $category_id 
                CREATE (p)-[:BelongsTo]->(c)");

            var productCategories = new[]
            {
                new { ProductId = 1L, CategoryId = 1L }, // Laptop -> Electronics
                new { ProductId = 2L, CategoryId = 1L }, // Smartphone -> Electronics
                new { ProductId = 3L, CategoryId = 2L }, // T-Shirt -> Clothing
                new { ProductId = 4L, CategoryId = 2L }, // Jeans -> Clothing
                new { ProductId = 5L, CategoryId = 3L }, // Coffee Maker -> Home
                new { ProductId = 6L, CategoryId = 3L }, // Vacuum -> Home
                new { ProductId = 7L, CategoryId = 4L }, // Running Shoes -> Sports
                new { ProductId = 8L, CategoryId = 4L }, // Yoga Mat -> Sports
                new { ProductId = 9L, CategoryId = 5L }, // Novel -> Books
                new { ProductId = 10L, CategoryId = 5L } // Cookbook -> Books
            };

            foreach (var pc in productCategories)
            {
                productCategoryStmt.Bind("product_id", pc.ProductId);
                productCategoryStmt.Bind("category_id", pc.CategoryId);
                productCategoryStmt.Execute();
            }

            // Product-Brand relationships
            using var productBrandStmt = connection.Prepare(@"
                MATCH (p:Product), (b:Brand) 
                WHERE p.id = $product_id AND b.id = $brand_id 
                CREATE (p)-[:ManufacturedBy]->(b)");

            var productBrands = new[]
            {
                new { ProductId = 1L, BrandId = 1L }, // Laptop -> Apple
                new { ProductId = 2L, BrandId = 2L }, // Smartphone -> Samsung
                new { ProductId = 3L, BrandId = 3L }, // T-Shirt -> Nike
                new { ProductId = 4L, BrandId = 4L }, // Jeans -> Levi's
                new { ProductId = 5L, BrandId = 5L }, // Coffee Maker -> Breville
                new { ProductId = 6L, BrandId = 6L }, // Vacuum -> Dyson
                new { ProductId = 7L, BrandId = 3L }, // Running Shoes -> Nike
                new { ProductId = 8L, BrandId = 7L }, // Yoga Mat -> Lululemon
                new { ProductId = 9L, BrandId = 8L }, // Novel -> Penguin
                new { ProductId = 10L, BrandId = 9L } // Cookbook -> Williams Sonoma
            };

            foreach (var pb in productBrands)
            {
                productBrandStmt.Bind("product_id", pb.ProductId);
                productBrandStmt.Bind("brand_id", pb.BrandId);
                productBrandStmt.Execute();
            }

            // Product-Product relationships (related products)
            using var productRelatedStmt = connection.Prepare(@"
                MATCH (p1:Product), (p2:Product) 
                WHERE p1.id = $product_id1 AND p2.id = $product_id2 
                CREATE (p1)-[:RelatedTo {relationship_type: $type, strength: $strength}]->(p2)");

            var productRelations = new[]
            {
                new { ProductId1 = 1L, ProductId2 = 2L, Type = "Complementary", Strength = 0.8 }, // Laptop <-> Smartphone
                new { ProductId1 = 3L, ProductId2 = 4L, Type = "Complementary", Strength = 0.9 }, // T-Shirt <-> Jeans
                new { ProductId1 = 5L, ProductId2 = 6L, Type = "Same_Category", Strength = 0.7 }, // Coffee Maker <-> Vacuum
                new { ProductId1 = 7L, ProductId2 = 8L, Type = "Complementary", Strength = 0.8 }, // Running Shoes <-> Yoga Mat
                new { ProductId1 = 9L, ProductId2 = 10L, Type = "Same_Category", Strength = 0.6 } // Novel <-> Cookbook
            };

            foreach (var pr in productRelations)
            {
                productRelatedStmt.Bind("product_id1", pr.ProductId1);
                productRelatedStmt.Bind("product_id2", pr.ProductId2);
                productRelatedStmt.Bind("type", pr.Type);
                productRelatedStmt.Bind("strength", pr.Strength);
                productRelatedStmt.Execute();
            }

            // Customer-Order relationships
            using var customerOrderStmt = connection.Prepare(@"
                MATCH (c:Customer), (o:Order) 
                WHERE c.id = $customer_id AND o.id = $order_id 
                CREATE (c)-[:PlacedBy]->(o)");

            var customerOrders = new[]
            {
                new { CustomerId = 1L, OrderId = 1L },
                new { CustomerId = 1L, OrderId = 2L },
                new { CustomerId = 2L, OrderId = 3L },
                new { CustomerId = 3L, OrderId = 4L },
                new { CustomerId = 4L, OrderId = 5L },
                new { CustomerId = 5L, OrderId = 6L }
            };

            foreach (var co in customerOrders)
            {
                customerOrderStmt.Bind("customer_id", co.CustomerId);
                customerOrderStmt.Bind("order_id", co.OrderId);
                customerOrderStmt.Execute();
            }

            // Order-Product relationships
            using var orderProductStmt = connection.Prepare(@"
                MATCH (o:Order), (p:Product) 
                WHERE o.id = $order_id AND p.id = $product_id 
                CREATE (o)-[:Contains {quantity: $quantity, unit_price: $unit_price}]->(p)");

            var orderProducts = new[]
            {
                new { OrderId = 1L, ProductId = 1L, Quantity = 1, UnitPrice = 1299.99 },
                new { OrderId = 1L, ProductId = 2L, Quantity = 1, UnitPrice = 799.99 },
                new { OrderId = 2L, ProductId = 3L, Quantity = 2, UnitPrice = 29.99 },
                new { OrderId = 2L, ProductId = 4L, Quantity = 1, UnitPrice = 89.99 },
                new { OrderId = 3L, ProductId = 5L, Quantity = 1, UnitPrice = 199.99 },
                new { OrderId = 4L, ProductId = 6L, Quantity = 1, UnitPrice = 399.99 },
                new { OrderId = 5L, ProductId = 7L, Quantity = 1, UnitPrice = 129.99 },
                new { OrderId = 6L, ProductId = 8L, Quantity = 1, UnitPrice = 79.99 }
            };

            foreach (var op in orderProducts)
            {
                orderProductStmt.Bind("order_id", op.OrderId);
                orderProductStmt.Bind("product_id", op.ProductId);
                orderProductStmt.Bind("quantity", op.Quantity);
                orderProductStmt.Bind("unit_price", op.UnitPrice);
                orderProductStmt.Execute();
            }

            // Customer-Product review relationships
            using var customerReviewStmt = connection.Prepare(@"
                MATCH (c:Customer), (p:Product), (r:Review) 
                WHERE c.id = $customer_id AND p.id = $product_id AND r.id = $review_id 
                CREATE (c)-[:Reviews {review_id: $review_id}]->(p),
                       (c)-[:WroteReview]->(r),
                       (p)-[:HasReview]->(r)");

            var customerReviews = new[]
            {
                new { CustomerId = 1L, ProductId = 1L, ReviewId = 1L },
                new { CustomerId = 2L, ProductId = 2L, ReviewId = 2L },
                new { CustomerId = 3L, ProductId = 3L, ReviewId = 3L },
                new { CustomerId = 4L, ProductId = 4L, ReviewId = 4L },
                new { CustomerId = 5L, ProductId = 5L, ReviewId = 5L }
            };

            foreach (var cr in customerReviews)
            {
                customerReviewStmt.Bind("customer_id", cr.CustomerId);
                customerReviewStmt.Bind("product_id", cr.ProductId);
                customerReviewStmt.Bind("review_id", cr.ReviewId);
                customerReviewStmt.Execute();
            }

            Console.WriteLine("  Created all relationships");
        }

        private static void DemonstrateProductCatalog(Connection connection)
        {
            // 1. Product browsing by category
            Console.WriteLine("1. Product browsing by category:");
            await BrowseProductsByCategory(connection);

            // 2. Product recommendations
            Console.WriteLine("\n2. Product recommendations:");
            await GetProductRecommendations(connection);

            // 3. Customer purchase history
            Console.WriteLine("\n3. Customer purchase history:");
            await GetCustomerPurchaseHistory(connection);

            // 4. Product reviews and ratings
            Console.WriteLine("\n4. Product reviews and ratings:");
            await GetProductReviews(connection);

            // 5. Brand analysis
            Console.WriteLine("\n5. Brand analysis:");
            await AnalyzeBrands(connection);

            // 6. Order analysis
            Console.WriteLine("\n6. Order analysis:");
            await AnalyzeOrders(connection);

            // 7. Inventory management
            Console.WriteLine("\n7. Inventory management:");
            await AnalyzeInventory(connection);

            // 8. Cross-selling opportunities
            Console.WriteLine("\n8. Cross-selling opportunities:");
            await FindCrossSellingOpportunities(connection);
        }

        private static async Task BrowseProductsByCategory(Connection connection)
        {
            // Get products by category
            using var categoryResult = connection.Query(@"
                MATCH (p:Product)-[:BelongsTo]->(c:Category)
                RETURN c.name, p.name, p.price, p.brand
                ORDER BY c.name, p.price DESC");

            Console.WriteLine("  Products by category:");
            string currentCategory = "";
            while (categoryResult.HasNext())
            {
                using var row = categoryResult.GetNext();
                var categoryName = row.GetValueAs<string>(0);
                var productName = row.GetValueAs<string>(1);
                var price = row.GetValueAs<double>(2);
                var brand = row.GetValueAs<string>(3);
                
                if (categoryName != currentCategory)
                {
                    Console.WriteLine($"    {categoryName}:");
                    currentCategory = categoryName;
                }
                
                Console.WriteLine($"      {productName} by {brand} - ${price:F2}");
            }

            // Get category hierarchy
            using var hierarchyResult = connection.Query(@"
                MATCH (c:Category)
                RETURN c.name, c.parent_category
                ORDER BY c.name");

            Console.WriteLine("  Category hierarchy:");
            while (hierarchyResult.HasNext())
            {
                using var row = hierarchyResult.GetNext();
                var categoryName = row.GetValueAs<string>(0);
                var parentCategory = row.GetValueAs<string>(1);
                
                if (string.IsNullOrEmpty(parentCategory))
                {
                    Console.WriteLine($"    {categoryName} (root category)");
                }
                else
                {
                    Console.WriteLine($"    {categoryName} (parent: {parentCategory})");
                }
            }
        }

        private static async Task GetProductRecommendations(Connection connection)
        {
            // Get related products
            using var relatedResult = connection.Query(@"
                MATCH (p:Product)-[r:RelatedTo]->(related:Product)
                WHERE p.id = 1
                RETURN related.name, related.price, r.relationship_type, r.strength
                ORDER BY r.strength DESC");

            Console.WriteLine("  Related products for Laptop:");
            while (relatedResult.HasNext())
            {
                using var row = relatedResult.GetNext();
                var productName = row.GetValueAs<string>(0);
                var price = row.GetValueAs<double>(1);
                var relationshipType = row.GetValueAs<string>(2);
                var strength = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {productName} - ${price:F2} ({relationshipType}, strength: {strength:F1})");
            }

            // Get products bought together
            using var boughtTogetherResult = connection.Query(@"
                MATCH (o:Order)-[:Contains]->(p1:Product),
                      (o)-[:Contains]->(p2:Product)
                WHERE p1.id = 1 AND p2.id != 1
                RETURN p2.name, p2.price, COUNT(o) as order_count
                ORDER BY order_count DESC
                LIMIT 5");

            Console.WriteLine("  Products bought together with Laptop:");
            while (boughtTogetherResult.HasNext())
            {
                using var row = boughtTogetherResult.GetNext();
                var productName = row.GetValueAs<string>(0);
                var price = row.GetValueAs<double>(1);
                var orderCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {productName} - ${price:F2} (bought together {orderCount} times)");
            }

            // Get products by same brand
            using var sameBrandResult = connection.Query(@"
                MATCH (p:Product)-[:ManufacturedBy]->(b:Brand)<-[:ManufacturedBy]-(related:Product)
                WHERE p.id = 1 AND related.id != 1
                RETURN related.name, related.price, b.name as brand
                ORDER BY related.price DESC");

            Console.WriteLine("  Other products by same brand:");
            while (sameBrandResult.HasNext())
            {
                using var row = sameBrandResult.GetNext();
                var productName = row.GetValueAs<string>(0);
                var price = row.GetValueAs<double>(1);
                var brand = row.GetValueAs<string>(2);
                
                Console.WriteLine($"    {productName} by {brand} - ${price:F2}");
            }
        }

        private static async Task GetCustomerPurchaseHistory(Connection connection)
        {
            // Get customer purchase history
            using var purchaseHistoryResult = connection.Query(@"
                MATCH (c:Customer)-[:PlacedBy]->(o:Order)-[:Contains]->(p:Product)
                WHERE c.id = 1
                RETURN o.order_date, p.name, p.price, o.total_amount
                ORDER BY o.order_date DESC");

            Console.WriteLine("  Purchase history for Customer 1:");
            while (purchaseHistoryResult.HasNext())
            {
                using var row = purchaseHistoryResult.GetNext();
                var orderDate = row.GetValueAs<DateTime>(0);
                var productName = row.GetValueAs<string>(1);
                var price = row.GetValueAs<double>(2);
                var totalAmount = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {orderDate:yyyy-MM-dd}: {productName} - ${price:F2} (Order total: ${totalAmount:F2})");
            }

            // Get customer spending by category
            using var spendingResult = connection.Query(@"
                MATCH (c:Customer)-[:PlacedBy]->(o:Order)-[:Contains]->(p:Product)-[:BelongsTo]->(cat:Category)
                WHERE c.id = 1
                RETURN cat.name, SUM(p.price) as total_spent, COUNT(p) as product_count
                ORDER BY total_spent DESC");

            Console.WriteLine("  Spending by category for Customer 1:");
            while (spendingResult.HasNext())
            {
                using var row = spendingResult.GetNext();
                var categoryName = row.GetValueAs<string>(0);
                var totalSpent = row.GetValueAs<double>(1);
                var productCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {categoryName}: ${totalSpent:F2} ({productCount} products)");
            }

            // Get customer loyalty by brand
            using var loyaltyResult = connection.Query(@"
                MATCH (c:Customer)-[:PlacedBy]->(o:Order)-[:Contains]->(p:Product)-[:ManufacturedBy]->(b:Brand)
                WHERE c.id = 1
                RETURN b.name, COUNT(p) as product_count, SUM(p.price) as total_spent
                ORDER BY product_count DESC");

            Console.WriteLine("  Brand loyalty for Customer 1:");
            while (loyaltyResult.HasNext())
            {
                using var row = loyaltyResult.GetNext();
                var brandName = row.GetValueAs<string>(0);
                var productCount = row.GetValueAs<long>(1);
                var totalSpent = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {brandName}: {productCount} products, ${totalSpent:F2} spent");
            }
        }

        private static async Task GetProductReviews(Connection connection)
        {
            // Get product reviews
            using var reviewsResult = connection.Query(@"
                MATCH (p:Product)-[:HasReview]->(r:Review)<-[:WroteReview]-(c:Customer)
                WHERE p.id = 1
                RETURN c.name, r.rating, r.comment, r.review_date, r.verified
                ORDER BY r.review_date DESC");

            Console.WriteLine("  Reviews for Laptop:");
            while (reviewsResult.HasNext())
            {
                using var row = reviewsResult.GetNext();
                var customerName = row.GetValueAs<string>(0);
                var rating = row.GetValueAs<int>(1);
                var comment = row.GetValueAs<string>(2);
                var reviewDate = row.GetValueAs<DateTime>(3);
                var verified = row.GetValueAs<bool>(4);
                
                Console.WriteLine($"    {customerName} ({rating}/5 stars) - {reviewDate:yyyy-MM-dd}");
                Console.WriteLine($"      {comment}");
                Console.WriteLine($"      Verified: {verified}");
            }

            // Get average ratings by product
            using var avgRatingsResult = connection.Query(@"
                MATCH (p:Product)-[:HasReview]->(r:Review)
                RETURN p.name, AVG(r.rating) as avg_rating, COUNT(r) as review_count
                ORDER BY avg_rating DESC");

            Console.WriteLine("  Average ratings by product:");
            while (avgRatingsResult.HasNext())
            {
                using var row = avgRatingsResult.GetNext();
                var productName = row.GetValueAs<string>(0);
                var avgRating = row.GetValueAs<double>(1);
                var reviewCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {productName}: {avgRating:F1}/5 stars ({reviewCount} reviews)");
            }

            // Get verified vs unverified reviews
            using var verifiedResult = connection.Query(@"
                MATCH (r:Review)
                RETURN r.verified, COUNT(r) as review_count, AVG(r.rating) as avg_rating
                ORDER BY r.verified DESC");

            Console.WriteLine("  Verified vs unverified reviews:");
            while (verifiedResult.HasNext())
            {
                using var row = verifiedResult.GetNext();
                var verified = row.GetValueAs<bool>(0);
                var reviewCount = row.GetValueAs<long>(1);
                var avgRating = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {(verified ? "Verified" : "Unverified")}: {reviewCount} reviews, {avgRating:F1}/5 avg rating");
            }
        }

        private static async Task AnalyzeBrands(Connection connection)
        {
            // Get brand performance
            using var brandPerformanceResult = connection.Query(@"
                MATCH (b:Brand)<-[:ManufacturedBy]-(p:Product)
                RETURN b.name, b.country, COUNT(p) as product_count, AVG(p.price) as avg_price
                ORDER BY product_count DESC");

            Console.WriteLine("  Brand performance:");
            while (brandPerformanceResult.HasNext())
            {
                using var row = brandPerformanceResult.GetNext();
                var brandName = row.GetValueAs<string>(0);
                var country = row.GetValueAs<string>(1);
                var productCount = row.GetValueAs<long>(2);
                var avgPrice = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {brandName} ({country}): {productCount} products, ${avgPrice:F2} avg price");
            }

            // Get brand sales
            using var brandSalesResult = connection.Query(@"
                MATCH (b:Brand)<-[:ManufacturedBy]-(p:Product)<-[:Contains]-(o:Order)
                RETURN b.name, COUNT(o) as order_count, SUM(p.price) as total_sales
                ORDER BY total_sales DESC");

            Console.WriteLine("  Brand sales:");
            while (brandSalesResult.HasNext())
            {
                using var row = brandSalesResult.GetNext();
                var brandName = row.GetValueAs<string>(0);
                var orderCount = row.GetValueAs<long>(1);
                var totalSales = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {brandName}: {orderCount} orders, ${totalSales:F2} total sales");
            }

            // Get brand reviews
            using var brandReviewsResult = connection.Query(@"
                MATCH (b:Brand)<-[:ManufacturedBy]-(p:Product)-[:HasReview]->(r:Review)
                RETURN b.name, AVG(r.rating) as avg_rating, COUNT(r) as review_count
                ORDER BY avg_rating DESC");

            Console.WriteLine("  Brand reviews:");
            while (brandReviewsResult.HasNext())
            {
                using var row = brandReviewsResult.GetNext();
                var brandName = row.GetValueAs<string>(0);
                var avgRating = row.GetValueAs<double>(1);
                var reviewCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {brandName}: {avgRating:F1}/5 stars ({reviewCount} reviews)");
            }
        }

        private static async Task AnalyzeOrders(Connection connection)
        {
            // Get order statistics
            using var orderStatsResult = connection.Query(@"
                MATCH (o:Order)
                RETURN COUNT(o) as total_orders, 
                       AVG(o.total_amount) as avg_order_value,
                       MIN(o.total_amount) as min_order_value,
                       MAX(o.total_amount) as max_order_value");

            Console.WriteLine("  Order statistics:");
            while (orderStatsResult.HasNext())
            {
                using var row = orderStatsResult.GetNext();
                var totalOrders = row.GetValueAs<long>(0);
                var avgOrderValue = row.GetValueAs<double>(1);
                var minOrderValue = row.GetValueAs<double>(2);
                var maxOrderValue = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    Total orders: {totalOrders}");
                Console.WriteLine($"    Average order value: ${avgOrderValue:F2}");
                Console.WriteLine($"    Min order value: ${minOrderValue:F2}");
                Console.WriteLine($"    Max order value: ${maxOrderValue:F2}");
            }

            // Get order status distribution
            using var orderStatusResult = connection.Query(@"
                MATCH (o:Order)
                RETURN o.status, COUNT(o) as order_count, AVG(o.total_amount) as avg_amount
                ORDER BY order_count DESC");

            Console.WriteLine("  Order status distribution:");
            while (orderStatusResult.HasNext())
            {
                using var row = orderStatusResult.GetNext();
                var status = row.GetValueAs<string>(0);
                var orderCount = row.GetValueAs<long>(1);
                var avgAmount = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {status}: {orderCount} orders, ${avgAmount:F2} avg value");
            }

            // Get order trends by date
            using var orderTrendsResult = connection.Query(@"
                MATCH (o:Order)
                RETURN DATE(o.order_date) as order_date, COUNT(o) as order_count, AVG(o.total_amount) as avg_amount
                ORDER BY order_date DESC");

            Console.WriteLine("  Order trends by date:");
            while (orderTrendsResult.HasNext())
            {
                using var row = orderTrendsResult.GetNext();
                var orderDate = row.GetValueAs<DateTime>(0);
                var orderCount = row.GetValueAs<long>(1);
                var avgAmount = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {orderDate:yyyy-MM-dd}: {orderCount} orders, ${avgAmount:F2} avg value");
            }
        }

        private static async Task AnalyzeInventory(Connection connection)
        {
            // Get inventory status
            using var inventoryResult = connection.Query(@"
                MATCH (p:Product)
                RETURN p.name, p.stock_quantity, p.in_stock, p.price
                ORDER BY p.stock_quantity ASC");

            Console.WriteLine("  Inventory status:");
            while (inventoryResult.HasNext())
            {
                using var row = inventoryResult.GetNext();
                var productName = row.GetValueAs<string>(0);
                var stockQuantity = row.GetValueAs<int>(1);
                var inStock = row.GetValueAs<bool>(2);
                var price = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {productName}: {stockQuantity} units, ${price:F2} {(inStock ? "(in stock)" : "(out of stock)")}");
            }

            // Get low stock products
            using var lowStockResult = connection.Query(@"
                MATCH (p:Product)
                WHERE p.stock_quantity < 10
                RETURN p.name, p.stock_quantity, p.price
                ORDER BY p.stock_quantity ASC");

            Console.WriteLine("  Low stock products:");
            while (lowStockResult.HasNext())
            {
                using var row = lowStockResult.GetNext();
                var productName = row.GetValueAs<string>(0);
                var stockQuantity = row.GetValueAs<int>(1);
                var price = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    {productName}: {stockQuantity} units remaining, ${price:F2}");
            }

            // Get inventory value by category
            using var inventoryValueResult = connection.Query(@"
                MATCH (p:Product)-[:BelongsTo]->(c:Category)
                RETURN c.name, SUM(p.stock_quantity * p.price) as inventory_value, COUNT(p) as product_count
                ORDER BY inventory_value DESC");

            Console.WriteLine("  Inventory value by category:");
            while (inventoryValueResult.HasNext())
            {
                using var row = inventoryValueResult.GetNext();
                var categoryName = row.GetValueAs<string>(0);
                var inventoryValue = row.GetValueAs<double>(1);
                var productCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {categoryName}: ${inventoryValue:F2} ({productCount} products)");
            }
        }

        private static async Task FindCrossSellingOpportunities(Connection connection)
        {
            // Find products frequently bought together
            using var crossSellResult = connection.Query(@"
                MATCH (o:Order)-[:Contains]->(p1:Product),
                      (o)-[:Contains]->(p2:Product)
                WHERE p1.id < p2.id
                RETURN p1.name, p2.name, COUNT(o) as frequency
                ORDER BY frequency DESC
                LIMIT 10");

            Console.WriteLine("  Products frequently bought together:");
            while (crossSellResult.HasNext())
            {
                using var row = crossSellResult.GetNext();
                var product1Name = row.GetValueAs<string>(0);
                var product2Name = row.GetValueAs<string>(1);
                var frequency = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    {product1Name} + {product2Name}: {frequency} times");
            }

            // Find products in same category
            using var sameCategoryResult = connection.Query(@"
                MATCH (p1:Product)-[:BelongsTo]->(c:Category)<-[:BelongsTo]-(p2:Product)
                WHERE p1.id < p2.id
                RETURN p1.name, p2.name, c.name as category
                ORDER BY c.name");

            Console.WriteLine("  Products in same category:");
            while (sameCategoryResult.HasNext())
            {
                using var row = sameCategoryResult.GetNext();
                var product1Name = row.GetValueAs<string>(0);
                var product2Name = row.GetValueAs<string>(1);
                var category = row.GetValueAs<string>(2);
                
                Console.WriteLine($"    {product1Name} + {product2Name} ({category})");
            }

            // Find products by same brand
            using var sameBrandResult = connection.Query(@"
                MATCH (p1:Product)-[:ManufacturedBy]->(b:Brand)<-[:ManufacturedBy]-(p2:Product)
                WHERE p1.id < p2.id
                RETURN p1.name, p2.name, b.name as brand
                ORDER BY b.name");

            Console.WriteLine("  Products by same brand:");
            while (sameBrandResult.HasNext())
            {
                using var row = sameBrandResult.GetNext();
                var product1Name = row.GetValueAs<string>(0);
                var product2Name = row.GetValueAs<string>(1);
                var brand = row.GetValueAs<string>(2);
                
                Console.WriteLine($"    {product1Name} + {product2Name} ({brand})");
            }
        }

        // Helper methods to generate test data
        private static List<CategoryData> GenerateCategories()
        {
            return new List<CategoryData>
            {
                new() { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets", ParentCategory = "" },
                new() { Id = 2, Name = "Clothing", Description = "Apparel and fashion items", ParentCategory = "" },
                new() { Id = 3, Name = "Home", Description = "Home and kitchen appliances", ParentCategory = "" },
                new() { Id = 4, Name = "Sports", Description = "Sports and fitness equipment", ParentCategory = "" },
                new() { Id = 5, Name = "Books", Description = "Books and educational materials", ParentCategory = "" }
            };
        }

        private static List<BrandData> GenerateBrands()
        {
            return new List<BrandData>
            {
                new() { Id = 1, Name = "Apple", Country = "USA", FoundedYear = 1976, Website = "apple.com" },
                new() { Id = 2, Name = "Samsung", Country = "South Korea", FoundedYear = 1938, Website = "samsung.com" },
                new() { Id = 3, Name = "Nike", Country = "USA", FoundedYear = 1964, Website = "nike.com" },
                new() { Id = 4, Name = "Levi's", Country = "USA", FoundedYear = 1853, Website = "levi.com" },
                new() { Id = 5, Name = "Breville", Country = "Australia", FoundedYear = 1932, Website = "breville.com" },
                new() { Id = 6, Name = "Dyson", Country = "UK", FoundedYear = 1991, Website = "dyson.com" },
                new() { Id = 7, Name = "Lululemon", Country = "Canada", FoundedYear = 1998, Website = "lululemon.com" },
                new() { Id = 8, Name = "Penguin", Country = "UK", FoundedYear = 1935, Website = "penguin.com" },
                new() { Id = 9, Name = "Williams Sonoma", Country = "USA", FoundedYear = 1956, Website = "williams-sonoma.com" }
            };
        }

        private static List<ProductData> GenerateProducts()
        {
            return new List<ProductData>
            {
                new() { Id = 1, Name = "MacBook Pro", Description = "High-performance laptop", Price = 1299.99, Sku = "MBP001", Category = "Electronics", Brand = "Apple", Weight = 2.0, Dimensions = "13.3x9.0x0.6", InStock = true, StockQuantity = 50, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new() { Id = 2, Name = "Galaxy S21", Description = "Latest smartphone", Price = 799.99, Sku = "GS21", Category = "Electronics", Brand = "Samsung", Weight = 0.2, Dimensions = "6.2x2.9x0.3", InStock = true, StockQuantity = 100, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new() { Id = 3, Name = "Nike T-Shirt", Description = "Comfortable cotton t-shirt", Price = 29.99, Sku = "NTS001", Category = "Clothing", Brand = "Nike", Weight = 0.2, Dimensions = "M", InStock = true, StockQuantity = 200, CreatedAt = DateTime.UtcNow.AddDays(-15) },
                new() { Id = 4, Name = "Levi's Jeans", Description = "Classic denim jeans", Price = 89.99, Sku = "LJ001", Category = "Clothing", Brand = "Levi's", Weight = 0.8, Dimensions = "32x34", InStock = true, StockQuantity = 75, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new() { Id = 5, Name = "Coffee Maker", Description = "Automatic coffee maker", Price = 199.99, Sku = "CM001", Category = "Home", Brand = "Breville", Weight = 5.0, Dimensions = "12x8x10", InStock = true, StockQuantity = 25, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new() { Id = 6, Name = "Vacuum Cleaner", Description = "Powerful vacuum cleaner", Price = 399.99, Sku = "VC001", Category = "Home", Brand = "Dyson", Weight = 3.0, Dimensions = "15x10x8", InStock = true, StockQuantity = 30, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new() { Id = 7, Name = "Running Shoes", Description = "Comfortable running shoes", Price = 129.99, Sku = "RS001", Category = "Sports", Brand = "Nike", Weight = 0.5, Dimensions = "US 10", InStock = true, StockQuantity = 80, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new() { Id = 8, Name = "Yoga Mat", Description = "Non-slip yoga mat", Price = 79.99, Sku = "YM001", Category = "Sports", Brand = "Lululemon", Weight = 1.0, Dimensions = "72x24x0.2", InStock = true, StockQuantity = 60, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 9, Name = "Programming Book", Description = "Learn programming fundamentals", Price = 49.99, Sku = "PB001", Category = "Books", Brand = "Penguin", Weight = 0.8, Dimensions = "9x6x1", InStock = true, StockQuantity = 150, CreatedAt = DateTime.UtcNow.AddDays(-7) },
                new() { Id = 10, Name = "Cookbook", Description = "Delicious recipes collection", Price = 34.99, Sku = "CB001", Category = "Books", Brand = "Williams Sonoma", Weight = 1.2, Dimensions = "10x8x1.5", InStock = true, StockQuantity = 120, CreatedAt = DateTime.UtcNow.AddDays(-4) }
            };
        }

        private static List<CustomerData> GenerateCustomers()
        {
            return new List<CustomerData>
            {
                new() { Id = 1, Name = "Alice Johnson", Email = "alice@example.com", Phone = "555-0001", Address = "123 Main St", City = "New York", Country = "USA", RegistrationDate = DateTime.UtcNow.AddDays(-60) },
                new() { Id = 2, Name = "Bob Smith", Email = "bob@example.com", Phone = "555-0002", Address = "456 Oak Ave", City = "Los Angeles", Country = "USA", RegistrationDate = DateTime.UtcNow.AddDays(-45) },
                new() { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com", Phone = "555-0003", Address = "789 Pine Rd", City = "Chicago", Country = "USA", RegistrationDate = DateTime.UtcNow.AddDays(-30) },
                new() { Id = 4, Name = "Diana Prince", Email = "diana@example.com", Phone = "555-0004", Address = "321 Elm St", City = "Houston", Country = "USA", RegistrationDate = DateTime.UtcNow.AddDays(-20) },
                new() { Id = 5, Name = "Eve Wilson", Email = "eve@example.com", Phone = "555-0005", Address = "654 Maple Dr", City = "Phoenix", Country = "USA", RegistrationDate = DateTime.UtcNow.AddDays(-10) }
            };
        }

        private static List<OrderData> GenerateOrders()
        {
            return new List<OrderData>
            {
                new() { Id = 1, OrderDate = DateTime.UtcNow.AddDays(-10), TotalAmount = 2099.98, Status = "Completed", ShippingAddress = "123 Main St, New York, NY" },
                new() { Id = 2, OrderDate = DateTime.UtcNow.AddDays(-8), TotalAmount = 149.97, Status = "Shipped", ShippingAddress = "123 Main St, New York, NY" },
                new() { Id = 3, OrderDate = DateTime.UtcNow.AddDays(-5), TotalAmount = 199.99, Status = "Processing", ShippingAddress = "456 Oak Ave, Los Angeles, CA" },
                new() { Id = 4, OrderDate = DateTime.UtcNow.AddDays(-3), TotalAmount = 399.99, Status = "Completed", ShippingAddress = "789 Pine Rd, Chicago, IL" },
                new() { Id = 5, OrderDate = DateTime.UtcNow.AddDays(-1), TotalAmount = 129.99, Status = "Pending", ShippingAddress = "321 Elm St, Houston, TX" },
                new() { Id = 6, OrderDate = DateTime.UtcNow.AddDays(-1), TotalAmount = 79.99, Status = "Pending", ShippingAddress = "654 Maple Dr, Phoenix, AZ" }
            };
        }

        private static List<ReviewData> GenerateReviews()
        {
            return new List<ReviewData>
            {
                new() { Id = 1, Rating = 5, Comment = "Excellent laptop, fast and reliable!", ReviewDate = DateTime.UtcNow.AddDays(-5), Verified = true },
                new() { Id = 2, Rating = 4, Comment = "Great phone, good camera quality", ReviewDate = DateTime.UtcNow.AddDays(-4), Verified = true },
                new() { Id = 3, Rating = 5, Comment = "Very comfortable t-shirt, fits perfectly", ReviewDate = DateTime.UtcNow.AddDays(-3), Verified = false },
                new() { Id = 4, Rating = 4, Comment = "Good quality jeans, durable material", ReviewDate = DateTime.UtcNow.AddDays(-2), Verified = true },
                new() { Id = 5, Rating = 5, Comment = "Amazing coffee maker, makes perfect coffee", ReviewDate = DateTime.UtcNow.AddDays(-1), Verified = true }
            };
        }
    }

    // Data classes
    public class CategoryData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ParentCategory { get; set; } = string.Empty;
    }

    public class BrandData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int FoundedYear { get; set; }
        public string Website { get; set; } = string.Empty;
    }

    public class ProductData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public double Weight { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public bool InStock { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    public class OrderData
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class ReviewData
    {
        public long Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public bool Verified { get; set; }
    }
}
