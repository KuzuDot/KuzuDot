using System;
using KuzuDot;

namespace KuzuDot.Examples.Advanced
{
    /// <summary>
    /// Async operations example demonstrating asynchronous query execution
    /// </summary>
    public class AsyncOperations
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Async Operations Example ===");
            
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

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            await InsertSampleDataAsync(connection);

            // Demonstrate async operations
            Console.WriteLine("\n=== Async Operations Examples ===");
            await DemonstrateAsyncOperations(connection);

            Console.WriteLine("\n=== Async Operations Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            connection.NonQuery(@"
                CREATE NODE TABLE Article(
                    id INT64, 
                    title STRING, 
                    content STRING, 
                    author STRING,
                    category STRING,
                    published_at TIMESTAMP,
                    read_count INT64,
                    likes INT64,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Comment(
                    id INT64, 
                    content STRING, 
                    author STRING,
                    created_at TIMESTAMP,
                    likes INT64,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE REL TABLE CommentsOn(
                    FROM Comment TO Article,
                    created_at TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE LikesArticle(
                    FROM User TO Article,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE User(
                    id INT64, 
                    username STRING, 
                    email STRING,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");
        }

        private static async Task InsertSampleDataAsync(Connection connection)
        {
            // Insert users
            var users = new[]
            {
                new { Id = 1L, Username = "alice_writer", Email = "alice@blog.com", CreatedAt = DateTime.UtcNow.AddDays(-60) },
                new { Id = 2L, Username = "bob_reader", Email = "bob@email.com", CreatedAt = DateTime.UtcNow.AddDays(-45) },
                new { Id = 3L, Username = "charlie_dev", Email = "charlie@dev.com", CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new { Id = 4L, Username = "diana_tech", Email = "diana@tech.com", CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new { Id = 5L, Username = "eve_blogger", Email = "eve@blog.com", CreatedAt = DateTime.UtcNow.AddDays(-15) }
            };

            using var userStmt = connection.Prepare("CREATE (:User {id: $id, username: $username, email: $email, created_at: $created_at})");
            foreach (var user in users)
            {
                userStmt.Bind("id", user.Id);
                userStmt.Bind("username", user.Username);
                userStmt.Bind("email", user.Email);
                userStmt.BindTimestamp("created_at", user.CreatedAt);
                userStmt.Execute();
            }

            // Insert articles
            var articles = new[]
            {
                new { Id = 1L, Title = "Introduction to Graph Databases", Content = "Graph databases are...", Author = "alice_writer", Category = "Technology", PublishedAt = DateTime.UtcNow.AddDays(-10), ReadCount = 1250L, Likes = 45L },
                new { Id = 2L, Title = "Async Programming in C#", Content = "Asynchronous programming...", Author = "charlie_dev", Category = "Programming", PublishedAt = DateTime.UtcNow.AddDays(-8), ReadCount = 980L, Likes = 32L },
                new { Id = 3L, Title = "Machine Learning Basics", Content = "Machine learning is...", Author = "diana_tech", Category = "AI", PublishedAt = DateTime.UtcNow.AddDays(-5), ReadCount = 2100L, Likes = 78L },
                new { Id = 4L, Title = "Web Development Trends", Content = "Modern web development...", Author = "eve_blogger", Category = "Web", PublishedAt = DateTime.UtcNow.AddDays(-3), ReadCount = 750L, Likes = 28L },
                new { Id = 5L, Title = "Database Optimization", Content = "Optimizing database performance...", Author = "alice_writer", Category = "Technology", PublishedAt = DateTime.UtcNow.AddDays(-1), ReadCount = 450L, Likes = 15L }
            };

            using var articleStmt = connection.Prepare(@"
                CREATE (:Article {
                    id: $id, 
                    title: $title, 
                    content: $content, 
                    author: $author,
                    category: $category,
                    published_at: $published_at,
                    read_count: $read_count,
                    likes: $likes
                })");

            foreach (var article in articles)
            {
                articleStmt.Bind("id", article.Id);
                articleStmt.Bind("title", article.Title);
                articleStmt.Bind("content", article.Content);
                articleStmt.Bind("author", article.Author);
                articleStmt.Bind("category", article.Category);
                articleStmt.BindTimestamp("published_at", article.PublishedAt);
                articleStmt.Bind("read_count", article.ReadCount);
                articleStmt.Bind("likes", article.Likes);
                articleStmt.Execute();
            }

            // Insert comments
            var comments = new[]
            {
                new { Id = 1L, Content = "Great article!", Author = "bob_reader", CreatedAt = DateTime.UtcNow.AddDays(-9), Likes = 5L },
                new { Id = 2L, Content = "Very informative", Author = "charlie_dev", CreatedAt = DateTime.UtcNow.AddDays(-8), Likes = 3L },
                new { Id = 3L, Content = "Thanks for sharing", Author = "diana_tech", CreatedAt = DateTime.UtcNow.AddDays(-7), Likes = 2L },
                new { Id = 4L, Content = "Looking forward to more", Author = "eve_blogger", CreatedAt = DateTime.UtcNow.AddDays(-6), Likes = 4L },
                new { Id = 5L, Content = "Excellent explanation", Author = "bob_reader", CreatedAt = DateTime.UtcNow.AddDays(-5), Likes = 6L }
            };

            using var commentStmt = connection.Prepare(@"
                CREATE (:Comment {
                    id: $id, 
                    content: $content, 
                    author: $author,
                    created_at: $created_at,
                    likes: $likes
                })");

            foreach (var comment in comments)
            {
                commentStmt.Bind("id", comment.Id);
                commentStmt.Bind("content", comment.Content);
                commentStmt.Bind("author", comment.Author);
                commentStmt.BindTimestamp("created_at", comment.CreatedAt);
                commentStmt.Bind("likes", comment.Likes);
                commentStmt.Execute();
            }

            Console.WriteLine("  Created users, articles, and comments");
        }

        private static async Task DemonstrateAsyncOperations(Connection connection)
        {
            // 1. Basic async query
            Console.WriteLine("1. Basic async query - Get all articles:");
            using var articlesResult = await connection.QueryAsync("MATCH (a:Article) RETURN a.title, a.author, a.read_count ORDER BY a.read_count DESC");
            
            while (articlesResult.HasNext())
            {
                using var row = articlesResult.GetNext();
                var title = row.GetValueAs<string>(0);
                var author = row.GetValueAs<string>(1);
                var readCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"  '{title}' by {author} - {readCount} reads");
            }

            // 2. Async prepared statement
            Console.WriteLine("\n2. Async prepared statement - Find articles by category:");
            using var categoryStmt = await connection.PrepareAsync("MATCH (a:Article) WHERE a.category = $category RETURN a.title, a.likes ORDER BY a.likes DESC");
            
            var categories = new[] { "Technology", "Programming", "AI" };
            foreach (var category in categories)
            {
                categoryStmt.Bind("category", category);
                using var result = await connection.ExecuteAsync(categoryStmt);
                
                Console.WriteLine($"  Articles in {category}:");
                while (result.HasNext())
                {
                    using var row = result.GetNext();
                    var title = row.GetValueAs<string>(0);
                    var likes = row.GetValueAs<long>(1);
                    
                    Console.WriteLine($"    '{title}' - {likes} likes");
                }
            }

            // 3. Async POCO mapping
            Console.WriteLine("\n3. Async POCO mapping - Get articles as objects:");
            var articles = await connection.QueryAsync<Article>("MATCH (a:Article) RETURN a.id, a.title, a.author, a.category, a.read_count, a.likes, a.published_at");
            
            foreach (var article in articles)
            {
                Console.WriteLine($"  {article.Title} by {article.Author} ({article.Category}) - {article.ReadCount} reads, {article.Likes} likes");
            }

            // 4. Parallel async operations
            Console.WriteLine("\n4. Parallel async operations - Multiple queries:");
            var tasks = new[]
            {
                GetArticleCountAsync(connection),
                GetTotalReadsAsync(connection),
                GetMostPopularArticleAsync(connection),
                GetRecentArticlesAsync(connection)
            };

            var results = await Task.WhenAll(tasks);
            
            Console.WriteLine($"  Total articles: {results[0]}");
            Console.WriteLine($"  Total reads: {results[1]}");
            Console.WriteLine($"  Most popular: {results[2]}");
            Console.WriteLine($"  Recent articles: {results[3]}");

            // 5. Async with cancellation
            Console.WriteLine("\n5. Async with cancellation - Long running query:");
            using var cts = new CancellationTokenSource();
            
            // Cancel after 2 seconds
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            
            try
            {
                using var longResult = await connection.QueryAsync(@"
                    MATCH (a:Article)
                    WITH a, a.read_count * a.likes as engagement_score
                    RETURN a.title, engagement_score
                    ORDER BY engagement_score DESC", cts.Token);
                
                Console.WriteLine("  Long running query completed:");
                while (longResult.HasNext())
                {
                    using var row = longResult.GetNext();
                    var title = row.GetValueAs<string>(0);
                    var score = row.GetValueAs<double>(1);
                    
                    Console.WriteLine($"    {title}: {score:F0} engagement score");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("  Query was cancelled (as expected)");
            }

            // 6. Async batch operations
            Console.WriteLine("\n6. Async batch operations - Update article statistics:");
            var updateTasks = new List<Task>();
            
            for (int i = 1; i <= 5; i++)
            {
                var articleId = i;
                var newReadCount = 1000 + (i * 100);
                var newLikes = 50 + (i * 10);
                
                var updateTask = Task.Run(async () =>
                {
                    using var updateStmt = await connection.PrepareAsync(@"
                        MATCH (a:Article) 
                        WHERE a.id = $id 
                        SET a.read_count = $read_count, a.likes = $likes");
                    
                    updateStmt.Bind("id", articleId);
                    updateStmt.Bind("read_count", newReadCount);
                    updateStmt.Bind("likes", newLikes);
                    
                    await connection.ExecuteAsync(updateStmt);
                    Console.WriteLine($"    Updated article {articleId}: {newReadCount} reads, {newLikes} likes");
                });
                
                updateTasks.Add(updateTask);
            }
            
            await Task.WhenAll(updateTasks);
            Console.WriteLine("  All batch updates completed");

            // 7. Async error handling
            Console.WriteLine("\n7. Async error handling:");
            try
            {
                using var errorResult = await connection.QueryAsync("MATCH (n:NonExistentTable) RETURN n");
            }
            catch (KuzuException ex)
            {
                Console.WriteLine($"  Caught KuzuDB error: {ex.Message}");
            }

            // 8. Performance comparison
            Console.WriteLine("\n8. Performance comparison - Sync vs Async:");
            
            // Sync operations
            var syncStart = DateTime.UtcNow;
            for (int i = 0; i < 10; i++)
            {
                using var syncResult = connection.Query("MATCH (a:Article) RETURN COUNT(a)");
                // Process result
            }
            var syncTime = DateTime.UtcNow - syncStart;
            
            // Async operations
            var asyncStart = DateTime.UtcNow;
            var asyncTasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                asyncTasks.Add(connection.QueryAsync("MATCH (a:Article) RETURN COUNT(a)"));
            }
            await Task.WhenAll(asyncTasks);
            var asyncTime = DateTime.UtcNow - asyncStart;
            
            Console.WriteLine($"  Sync operations (10 queries): {syncTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Async operations (10 queries): {asyncTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Async improvement: {(syncTime.TotalMilliseconds / asyncTime.TotalMilliseconds):F1}x faster");

            // 9. Complex async workflow
            Console.WriteLine("\n9. Complex async workflow - Article analysis:");
            await PerformComplexAnalysisAsync(connection);
        }

        private static async Task<long> GetArticleCountAsync(Connection connection)
        {
            using var result = await connection.QueryAsync("MATCH (a:Article) RETURN COUNT(a)");
            return result.HasNext() ? result.GetNext().GetValueAs<long>(0) : 0;
        }

        private static async Task<long> GetTotalReadsAsync(Connection connection)
        {
            using var result = await connection.QueryAsync("MATCH (a:Article) RETURN SUM(a.read_count)");
            return result.HasNext() ? result.GetNext().GetValueAs<long>(0) : 0;
        }

        private static async Task<string> GetMostPopularArticleAsync(Connection connection)
        {
            using var result = await connection.QueryAsync(@"
                MATCH (a:Article) 
                RETURN a.title 
                ORDER BY a.read_count DESC 
                LIMIT 1");
            
            return result.HasNext() ? result.GetNext().GetValueAs<string>(0) : "None";
        }

        private static async Task<long> GetRecentArticlesAsync(Connection connection)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            using var stmt = await connection.PrepareAsync("MATCH (a:Article) WHERE a.published_at >= $cutoff_date RETURN COUNT(a)");
            stmt.BindTimestamp("cutoff_date", cutoffDate);
            
            using var result = await connection.ExecuteAsync(stmt);
            return result.HasNext() ? result.GetNext().GetValueAs<long>(0) : 0;
        }

        private static async Task PerformComplexAnalysisAsync(Connection connection)
        {
            Console.WriteLine("  Performing complex article analysis...");
            
            // Step 1: Get article statistics
            var statsTask = connection.QueryAsync<ArticleStats>(@"
                MATCH (a:Article) 
                RETURN a.category, 
                       COUNT(a) as article_count, 
                       AVG(a.read_count) as avg_reads,
                       SUM(a.likes) as total_likes
                ORDER BY total_likes DESC");

            // Step 2: Get top authors
            var authorsTask = connection.QueryAsync<AuthorStats>(@"
                MATCH (a:Article) 
                RETURN a.author, 
                       COUNT(a) as article_count,
                       SUM(a.read_count) as total_reads
                ORDER BY total_reads DESC
                LIMIT 3");

            // Step 3: Get engagement metrics
            var engagementTask = connection.QueryAsync(@"
                MATCH (a:Article) 
                RETURN a.title, 
                       a.read_count, 
                       a.likes,
                       (a.read_count * 1.0 / a.likes) as reads_per_like
                ORDER BY reads_per_like DESC");

            // Wait for all tasks to complete
            var stats = await statsTask;
            var authors = await authorsTask;
            var engagementResult = await engagementTask;

            Console.WriteLine("    Category Statistics:");
            foreach (var stat in stats)
            {
                Console.WriteLine($"      {stat.Category}: {stat.ArticleCount} articles, {stat.AverageReads:F0} avg reads, {stat.TotalLikes} total likes");
            }

            Console.WriteLine("    Top Authors:");
            foreach (var author in authors)
            {
                Console.WriteLine($"      {author.Author}: {author.ArticleCount} articles, {author.TotalReads} total reads");
            }

            Console.WriteLine("    Engagement Metrics:");
            while (engagementResult.HasNext())
            {
                using var row = engagementResult.GetNext();
                var title = row.GetValueAs<string>(0);
                var reads = row.GetValueAs<long>(1);
                var likes = row.GetValueAs<long>(2);
                var readsPerLike = row.GetValueAs<double>(3);
                
                Console.WriteLine($"      '{title}': {reads} reads, {likes} likes, {readsPerLike:F1} reads/like");
            }
        }
    }

    // POCO classes for async operations
    public class Article
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public long ReadCount { get; set; }
        public long Likes { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public class ArticleStats
    {
        public string Category { get; set; } = string.Empty;
        public long ArticleCount { get; set; }
        public double AverageReads { get; set; }
        public long TotalLikes { get; set; }
    }

    public class AuthorStats
    {
        public string Author { get; set; } = string.Empty;
        public long ArticleCount { get; set; }
        public long TotalReads { get; set; }
    }
}
