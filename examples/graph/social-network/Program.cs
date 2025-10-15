using System;
using KuzuDot;

namespace KuzuDot.Examples.Graph
{
    /// <summary>
    /// Social network example demonstrating graph relationships and complex queries
    /// </summary>
    public class SocialNetwork
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Social Network Example ===");
            
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
            Console.WriteLine("Creating social network schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate various graph queries
            Console.WriteLine("\n=== Graph Queries ===");
            DemonstrateGraphQueries(connection);

            Console.WriteLine("\n=== Social Network Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            connection.NonQuery(@"
                CREATE NODE TABLE User(
                    id INT64, 
                    name STRING, 
                    email STRING, 
                    age INT32,
                    city STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Post(
                    id INT64, 
                    title STRING, 
                    content STRING, 
                    created_at TIMESTAMP,
                    likes_count INT32,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Comment(
                    id INT64, 
                    content STRING, 
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");

            // Create relationship tables
            connection.NonQuery(@"
                CREATE REL TABLE Follows(
                    FROM User TO User, 
                    since STRING,
                    notification_enabled BOOLEAN
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Authored(
                    FROM User TO Post,
                    published_at TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Likes(
                    FROM User TO Post,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE CommentsOn(
                    FROM User TO Post,
                    comment_id INT64,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE RepliesTo(
                    FROM User TO Comment,
                    timestamp TIMESTAMP
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert users
            var users = new[]
            {
                new { Id = 1L, Name = "Alice Johnson", Email = "alice@example.com", Age = 28, City = "New York" },
                new { Id = 2L, Name = "Bob Smith", Email = "bob@example.com", Age = 32, City = "San Francisco" },
                new { Id = 3L, Name = "Charlie Brown", Email = "charlie@example.com", Age = 25, City = "Chicago" },
                new { Id = 4L, Name = "Diana Prince", Email = "diana@example.com", Age = 30, City = "Boston" },
                new { Id = 5L, Name = "Eve Wilson", Email = "eve@example.com", Age = 27, City = "Seattle" }
            };

            using var userStmt = connection.Prepare("CREATE (:User {id: $id, name: $name, email: $email, age: $age, city: $city})");
            foreach (var user in users)
            {
                userStmt.Bind(user);
                userStmt.Execute();
                Console.WriteLine($"  Created user: {user.Name}");
            }

            // Insert posts
            var posts = new[]
            {
                new { Id = 1L, Title = "My First Post", Content = "Hello world!", CreatedAt = DateTime.UtcNow.AddDays(-5), LikesCount = 10 },
                new { Id = 2L, Title = "Graph Databases", Content = "Learning about graph databases is fascinating!", CreatedAt = DateTime.UtcNow.AddDays(-3), LikesCount = 25 },
                new { Id = 3L, Title = "Weekend Plans", Content = "Looking forward to the weekend!", CreatedAt = DateTime.UtcNow.AddDays(-1), LikesCount = 5 },
                new { Id = 4L, Title = "Tech News", Content = "Latest updates in technology", CreatedAt = DateTime.UtcNow.AddHours(-2), LikesCount = 15 }
            };

            using var postStmt = connection.Prepare("CREATE (:Post {id: $id, title: $title, content: $content, created_at: $created_at, likes_count: $likes_count})");
            foreach (var post in posts)
            {
                postStmt.Bind("id", post.Id);
                postStmt.Bind("title", post.Title);
                postStmt.Bind("content", post.Content);
                postStmt.BindTimestamp("created_at", post.CreatedAt);
                postStmt.Bind("likes_count", post.LikesCount);
                postStmt.Execute();
                Console.WriteLine($"  Created post: {post.Title}");
            }

            // Create relationships
            Console.WriteLine("Creating relationships...");
            
            // Follow relationships
            var follows = new[]
            {
                new { From = 1L, To = 2L, Since = "2023-01-01", Notifications = true },
                new { From = 1L, To = 3L, Since = "2023-02-15", Notifications = true },
                new { From = 2L, To = 1L, Since = "2023-01-05", Notifications = false },
                new { From = 2L, To = 4L, Since = "2023-03-01", Notifications = true },
                new { From = 3L, To = 1L, Since = "2023-02-20", Notifications = true },
                new { From = 4L, To = 2L, Since = "2023-03-05", Notifications = true },
                new { From = 5L, To = 1L, Since = "2023-04-01", Notifications = true }
            };

            using var followStmt = connection.Prepare(@"
                MATCH (u1:User), (u2:User) 
                WHERE u1.id = $from AND u2.id = $to 
                CREATE (u1)-[:Follows {since: $since, notification_enabled: $notifications}]->(u2)");

            foreach (var follow in follows)
            {
                followStmt.Bind("from", follow.From);
                followStmt.Bind("to", follow.To);
                followStmt.Bind("since", follow.Since);
                followStmt.Bind("notifications", follow.Notifications);
                followStmt.Execute();
            }

            // Author relationships
            var authors = new[]
            {
                new { UserId = 1L, PostId = 1L, PublishedAt = DateTime.UtcNow.AddDays(-5) },
                new { UserId = 2L, PostId = 2L, PublishedAt = DateTime.UtcNow.AddDays(-3) },
                new { UserId = 1L, PostId = 3L, PublishedAt = DateTime.UtcNow.AddDays(-1) },
                new { UserId = 4L, PostId = 4L, PublishedAt = DateTime.UtcNow.AddHours(-2) }
            };

            using var authorStmt = connection.Prepare(@"
                MATCH (u:User), (p:Post) 
                WHERE u.id = $userId AND p.id = $postId 
                CREATE (u)-[:Authored {published_at: $published_at}]->(p)");

            foreach (var author in authors)
            {
                authorStmt.Bind("userId", author.UserId);
                authorStmt.Bind("postId", author.PostId);
                authorStmt.BindTimestamp("published_at", author.PublishedAt);
                authorStmt.Execute();
            }

            // Like relationships
            var likes = new[]
            {
                new { UserId = 2L, PostId = 1L, Timestamp = DateTime.UtcNow.AddDays(-4) },
                new { UserId = 3L, PostId = 1L, Timestamp = DateTime.UtcNow.AddDays(-3) },
                new { UserId = 1L, PostId = 2L, Timestamp = DateTime.UtcNow.AddDays(-2) },
                new { UserId = 4L, PostId = 2L, Timestamp = DateTime.UtcNow.AddDays(-1) },
                new { UserId = 5L, PostId = 2L, Timestamp = DateTime.UtcNow.AddHours(-12) }
            };

            using var likeStmt = connection.Prepare(@"
                MATCH (u:User), (p:Post) 
                WHERE u.id = $userId AND p.id = $postId 
                CREATE (u)-[:Likes {timestamp: $timestamp}]->(p)");

            foreach (var like in likes)
            {
                likeStmt.Bind("userId", like.UserId);
                likeStmt.Bind("postId", like.PostId);
                likeStmt.BindTimestamp("timestamp", like.Timestamp);
                likeStmt.Execute();
            }

            Console.WriteLine("  Created all relationships");
        }

        private static void DemonstrateGraphQueries(Connection connection)
        {
            // 1. Find users and their posts
            Console.WriteLine("1. Users and their posts:");
            using var userPostsResult = connection.Query(@"
                MATCH (u:User)-[:Authored]->(p:Post)
                RETURN u.name, p.title, p.likes_count
                ORDER BY p.likes_count DESC");

            while (userPostsResult.HasNext())
            {
                using var row = userPostsResult.GetNext();
                var userName = row.GetValueAs<string>(0);
                var postTitle = row.GetValueAs<string>(1);
                var likesCount = row.GetValueAs<int>(2);
                
                Console.WriteLine($"  {userName} authored '{postTitle}' ({likesCount} likes)");
            }

            // 2. Find mutual follows
            Console.WriteLine("\n2. Mutual follows:");
            using var mutualFollowsResult = connection.Query(@"
                MATCH (u1:User)-[:Follows]->(u2:User)-[:Follows]->(u1)
                RETURN u1.name, u2.name
                ORDER BY u1.name");

            while (mutualFollowsResult.HasNext())
            {
                using var row = mutualFollowsResult.GetNext();
                var user1 = row.GetValueAs<string>(0);
                var user2 = row.GetValueAs<string>(1);
                
                Console.WriteLine($"  {user1} and {user2} follow each other");
            }

            // 3. Find most popular posts
            Console.WriteLine("\n3. Most popular posts:");
            using var popularPostsResult = connection.Query(@"
                MATCH (p:Post)
                RETURN p.title, p.likes_count, p.created_at
                ORDER BY p.likes_count DESC
                LIMIT 3");

            while (popularPostsResult.HasNext())
            {
                using var row = popularPostsResult.GetNext();
                var title = row.GetValueAs<string>(0);
                var likesCount = row.GetValueAs<int>(1);
                var createdAt = row.GetValueAs<DateTime>(2);
                
                Console.WriteLine($"  '{title}' - {likesCount} likes (posted {createdAt:yyyy-MM-dd})");
            }

            // 4. Find users who liked posts by their followers
            Console.WriteLine("\n4. Users who liked posts by their followers:");
            using var likedFollowersResult = connection.Query(@"
                MATCH (liker:User)-[:Likes]->(p:Post)<-[:Authored]-(author:User)<-[:Follows]-(liker)
                RETURN liker.name, author.name, p.title
                ORDER BY liker.name");

            while (likedFollowersResult.HasNext())
            {
                using var row = likedFollowersResult.GetNext();
                var liker = row.GetValueAs<string>(0);
                var author = row.GetValueAs<string>(1);
                var postTitle = row.GetValueAs<string>(2);
                
                Console.WriteLine($"  {liker} liked '{postTitle}' by {author} (whom they follow)");
            }

            // 5. Find users with most followers
            Console.WriteLine("\n5. Users with most followers:");
            using var mostFollowersResult = connection.Query(@"
                MATCH (u:User)<-[:Follows]-(follower:User)
                RETURN u.name, COUNT(follower) as follower_count
                ORDER BY follower_count DESC");

            while (mostFollowersResult.HasNext())
            {
                using var row = mostFollowersResult.GetNext();
                var userName = row.GetValueAs<string>(0);
                var followerCount = row.GetValueAs<long>(1);
                
                Console.WriteLine($"  {userName}: {followerCount} followers");
            }

            // 6. Find posts with engagement (likes from followers)
            Console.WriteLine("\n6. Posts with high engagement from followers:");
            using var engagementResult = connection.Query(@"
                MATCH (author:User)-[:Authored]->(p:Post)<-[:Likes]-(liker:User)<-[:Follows]-(author)
                RETURN author.name, p.title, COUNT(liker) as follower_likes
                ORDER BY follower_likes DESC");

            while (engagementResult.HasNext())
            {
                using var row = engagementResult.GetNext();
                var author = row.GetValueAs<string>(0);
                var postTitle = row.GetValueAs<string>(1);
                var followerLikes = row.GetValueAs<long>(2);
                
                Console.WriteLine($"  '{postTitle}' by {author}: {followerLikes} likes from followers");
            }

            // 7. Find users in the same city
            Console.WriteLine("\n7. Users in the same city:");
            using var sameCityResult = connection.Query(@"
                MATCH (u1:User), (u2:User)
                WHERE u1.city = u2.city AND u1.id < u2.id
                RETURN u1.city, u1.name, u2.name
                ORDER BY u1.city");

            while (sameCityResult.HasNext())
            {
                using var row = sameCityResult.GetNext();
                var city = row.GetValueAs<string>(0);
                var user1 = row.GetValueAs<string>(1);
                var user2 = row.GetValueAs<string>(2);
                
                Console.WriteLine($"  {city}: {user1} and {user2}");
            }

            // 8. Find users who haven't posted anything
            Console.WriteLine("\n8. Users who haven't posted anything:");
            using var noPostsResult = connection.Query(@"
                MATCH (u:User)
                WHERE NOT EXISTS { MATCH (u)-[:Authored]->(:Post) }
                RETURN u.name, u.city");

            while (noPostsResult.HasNext())
            {
                using var row = noPostsResult.GetNext();
                var userName = row.GetValueAs<string>(0);
                var city = row.GetValueAs<string>(1);
                
                Console.WriteLine($"  {userName} from {city} hasn't posted anything");
            }
        }
    }
}
