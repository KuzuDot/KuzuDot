using System;
using KuzuDot;

namespace KuzuDot.Examples.RealWorld
{
    /// <summary>
    /// Recommendation system example demonstrating movie recommendation engine
    /// </summary>
    public class RecommendationSystem
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Recommendation System Example ===");
            
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
            Console.WriteLine("Creating recommendation system schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate recommendation algorithms
            Console.WriteLine("\n=== Recommendation Algorithms ===");
            DemonstrateRecommendations(connection);

            Console.WriteLine("\n=== Recommendation System Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            connection.NonQuery(@"
                CREATE NODE TABLE User(
                    id INT64, 
                    name STRING, 
                    age INT32,
                    gender STRING,
                    location STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Movie(
                    id INT64, 
                    title STRING, 
                    genre STRING,
                    year INT32,
                    rating DOUBLE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Genre(
                    id INT64, 
                    name STRING, 
                    description STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Director(
                    id INT64, 
                    name STRING, 
                    birth_year INT32,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Actor(
                    id INT64, 
                    name STRING, 
                    birth_year INT32,
                    PRIMARY KEY(id)
                )");

            // Create relationship tables
            connection.NonQuery(@"
                CREATE REL TABLE Rated(
                    FROM User TO Movie,
                    rating DOUBLE,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Watched(
                    FROM User TO Movie,
                    watch_date TIMESTAMP,
                    duration_minutes INT32
                )");

            connection.NonQuery(@"
                CREATE REL TABLE BelongsTo(
                    FROM Movie TO Genre
                )");

            connection.NonQuery(@"
                CREATE REL TABLE DirectedBy(
                    FROM Movie TO Director
                )");

            connection.NonQuery(@"
                CREATE REL TABLE StarredIn(
                    FROM Actor TO Movie,
                    role STRING
                )");

            connection.NonQuery(@"
                CREATE REL TABLE SimilarTo(
                    FROM Movie TO Movie,
                    similarity_score DOUBLE
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert users
            var users = new[]
            {
                new { Id = 1L, Name = "Alice", Age = 28, Gender = "Female", Location = "New York" },
                new { Id = 2L, Name = "Bob", Age = 32, Gender = "Male", Location = "San Francisco" },
                new { Id = 3L, Name = "Charlie", Age = 25, Gender = "Male", Location = "London" },
                new { Id = 4L, Name = "Diana", Age = 30, Gender = "Female", Location = "Toronto" },
                new { Id = 5L, Name = "Eve", Age = 27, Gender = "Female", Location = "Sydney" },
                new { Id = 6L, Name = "Frank", Age = 35, Gender = "Male", Location = "Berlin" },
                new { Id = 7L, Name = "Grace", Age = 29, Gender = "Female", Location = "Paris" },
                new { Id = 8L, Name = "Henry", Age = 31, Gender = "Male", Location = "Tokyo" }
            };

            using var userStmt = connection.Prepare("CREATE (:User {id: $id, name: $name, age: $age, gender: $gender, location: $location})");
            foreach (var user in users)
            {
                userStmt.Bind(user);
                userStmt.Execute();
            }

            // Insert genres
            var genres = new[]
            {
                new { Id = 1L, Name = "Action", Description = "High-energy films with physical stunts" },
                new { Id = 2L, Name = "Comedy", Description = "Humorous and entertaining films" },
                new { Id = 3L, Name = "Drama", Description = "Serious plot-driven presentations" },
                new { Id = 4L, Name = "Horror", Description = "Intended to frighten or unsettle viewers" },
                new { Id = 5L, Name = "Romance", Description = "Love stories and romantic relationships" },
                new { Id = 6L, Name = "Sci-Fi", Description = "Science fiction and futuristic themes" },
                new { Id = 7L, Name = "Thriller", Description = "Suspenseful and exciting plots" }
            };

            using var genreStmt = connection.Prepare("CREATE (:Genre {id: $id, name: $name, description: $description})");
            foreach (var genre in genres)
            {
                genreStmt.Bind(genre);
                genreStmt.Execute();
            }

            // Insert directors
            var directors = new[]
            {
                new { Id = 1L, Name = "Christopher Nolan", BirthYear = 1970 },
                new { Id = 2L, Name = "Steven Spielberg", BirthYear = 1946 },
                new { Id = 3L, Name = "Quentin Tarantino", BirthYear = 1963 },
                new { Id = 4L, Name = "Martin Scorsese", BirthYear = 1942 },
                new { Id = 5L, Name = "Ridley Scott", BirthYear = 1937 }
            };

            using var directorStmt = connection.Prepare("CREATE (:Director {id: $id, name: $name, birth_year: $birth_year})");
            foreach (var director in directors)
            {
                directorStmt.Bind(director);
                directorStmt.Execute();
            }

            // Insert actors
            var actors = new[]
            {
                new { Id = 1L, Name = "Leonardo DiCaprio", BirthYear = 1974 },
                new { Id = 2L, Name = "Tom Hanks", BirthYear = 1956 },
                new { Id = 3L, Name = "Meryl Streep", BirthYear = 1949 },
                new { Id = 4L, Name = "Brad Pitt", BirthYear = 1963 },
                new { Id = 5L, Name = "Emma Stone", BirthYear = 1988 },
                new { Id = 6L, Name = "Ryan Gosling", BirthYear = 1980 },
                new { Id = 7L, Name = "Scarlett Johansson", BirthYear = 1984 },
                new { Id = 8L, Name = "Robert Downey Jr.", BirthYear = 1965 }
            };

            using var actorStmt = connection.Prepare("CREATE (:Actor {id: $id, name: $name, birth_year: $birth_year})");
            foreach (var actor in actors)
            {
                actorStmt.Bind(actor);
                actorStmt.Execute();
            }

            // Insert movies
            var movies = new[]
            {
                new { Id = 1L, Title = "Inception", Genre = "Sci-Fi", Year = 2010, Rating = 8.8 },
                new { Id = 2L, Title = "The Dark Knight", Genre = "Action", Year = 2008, Rating = 9.0 },
                new { Id = 3L, Title = "Interstellar", Genre = "Sci-Fi", Year = 2014, Rating = 8.6 },
                new { Id = 4L, Title = "Saving Private Ryan", Genre = "Drama", Year = 1998, Rating = 8.6 },
                new { Id = 5L, Title = "Pulp Fiction", Genre = "Crime", Year = 1994, Rating = 8.9 },
                new { Id = 6L, Title = "Goodfellas", Genre = "Crime", Year = 1990, Rating = 8.7 },
                new { Id = 7L, Title = "Alien", Genre = "Horror", Year = 1979, Rating = 8.5 },
                new { Id = 8L, Title = "Blade Runner", Genre = "Sci-Fi", Year = 1982, Rating = 8.1 },
                new { Id = 9L, Title = "The Wolf of Wall Street", Genre = "Drama", Year = 2013, Rating = 8.2 },
                new { Id = 10L, Title = "La La Land", Genre = "Romance", Year = 2016, Rating = 8.0 }
            };

            using var movieStmt = connection.Prepare("CREATE (:Movie {id: $id, title: $title, genre: $genre, year: $year, rating: $rating})");
            foreach (var movie in movies)
            {
                movieStmt.Bind(movie);
                movieStmt.Execute();
            }

            Console.WriteLine("  Created users, genres, directors, actors, and movies");

            // Create relationships
            CreateRelationships(connection);
        }

        private static void CreateRelationships(Connection connection)
        {
            Console.WriteLine("Creating relationships...");

            // User ratings
            var ratings = new[]
            {
                new { UserId = 1L, MovieId = 1L, Rating = 5.0, Timestamp = DateTime.UtcNow.AddDays(-10) },
                new { UserId = 1L, MovieId = 2L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-8) },
                new { UserId = 1L, MovieId = 3L, Rating = 5.0, Timestamp = DateTime.UtcNow.AddDays(-5) },
                new { UserId = 2L, MovieId = 1L, Rating = 4.0, Timestamp = DateTime.UtcNow.AddDays(-12) },
                new { UserId = 2L, MovieId = 4L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-9) },
                new { UserId = 2L, MovieId = 5L, Rating = 5.0, Timestamp = DateTime.UtcNow.AddDays(-6) },
                new { UserId = 3L, MovieId = 2L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-7) },
                new { UserId = 3L, MovieId = 6L, Rating = 4.0, Timestamp = DateTime.UtcNow.AddDays(-4) },
                new { UserId = 4L, MovieId = 3L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-11) },
                new { UserId = 4L, MovieId = 7L, Rating = 3.5, Timestamp = DateTime.UtcNow.AddDays(-8) },
                new { UserId = 5L, MovieId = 1L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-6) },
                new { UserId = 5L, MovieId = 9L, Rating = 4.0, Timestamp = DateTime.UtcNow.AddDays(-3) },
                new { UserId = 6L, MovieId = 2L, Rating = 5.0, Timestamp = DateTime.UtcNow.AddDays(-9) },
                new { UserId = 6L, MovieId = 8L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-5) },
                new { UserId = 7L, MovieId = 10L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-7) },
                new { UserId = 7L, MovieId = 1L, Rating = 4.0, Timestamp = DateTime.UtcNow.AddDays(-4) },
                new { UserId = 8L, MovieId = 3L, Rating = 4.5, Timestamp = DateTime.UtcNow.AddDays(-8) },
                new { UserId = 8L, MovieId = 5L, Rating = 4.0, Timestamp = DateTime.UtcNow.AddDays(-2) }
            };

            using var ratingStmt = connection.Prepare(@"
                MATCH (u:User), (m:Movie) 
                WHERE u.id = $user_id AND m.id = $movie_id 
                CREATE (u)-[:Rated {rating: $rating, timestamp: $timestamp}]->(m)");

            foreach (var rating in ratings)
            {
                ratingStmt.Bind("user_id", rating.UserId);
                ratingStmt.Bind("movie_id", rating.MovieId);
                ratingStmt.Bind("rating", rating.Rating);
                ratingStmt.BindTimestamp("timestamp", rating.Timestamp);
                ratingStmt.Execute();
            }

            // Movie-genre relationships
            var movieGenres = new[]
            {
                new { MovieId = 1L, GenreId = 6L }, // Inception -> Sci-Fi
                new { MovieId = 2L, GenreId = 1L }, // Dark Knight -> Action
                new { MovieId = 3L, GenreId = 6L }, // Interstellar -> Sci-Fi
                new { MovieId = 4L, GenreId = 3L }, // Saving Private Ryan -> Drama
                new { MovieId = 5L, GenreId = 3L }, // Pulp Fiction -> Crime
                new { MovieId = 6L, GenreId = 3L }, // Goodfellas -> Crime
                new { MovieId = 7L, GenreId = 4L }, // Alien -> Horror
                new { MovieId = 8L, GenreId = 6L }, // Blade Runner -> Sci-Fi
                new { MovieId = 9L, GenreId = 3L }, // Wolf of Wall Street -> Drama
                new { MovieId = 10L, GenreId = 5L } // La La Land -> Romance
            };

            using var movieGenreStmt = connection.Prepare(@"
                MATCH (m:Movie), (g:Genre) 
                WHERE m.id = $movie_id AND g.id = $genre_id 
                CREATE (m)-[:BelongsTo]->(g)");

            foreach (var mg in movieGenres)
            {
                movieGenreStmt.Bind("movie_id", mg.MovieId);
                movieGenreStmt.Bind("genre_id", mg.GenreId);
                movieGenreStmt.Execute();
            }

            // Movie-director relationships
            var movieDirectors = new[]
            {
                new { MovieId = 1L, DirectorId = 1L }, // Inception -> Nolan
                new { MovieId = 2L, DirectorId = 1L }, // Dark Knight -> Nolan
                new { MovieId = 3L, DirectorId = 1L }, // Interstellar -> Nolan
                new { MovieId = 4L, DirectorId = 2L }, // Saving Private Ryan -> Spielberg
                new { MovieId = 5L, DirectorId = 3L }, // Pulp Fiction -> Tarantino
                new { MovieId = 6L, DirectorId = 4L }, // Goodfellas -> Scorsese
                new { MovieId = 7L, DirectorId = 5L }, // Alien -> Scott
                new { MovieId = 8L, DirectorId = 5L }, // Blade Runner -> Scott
                new { MovieId = 9L, DirectorId = 4L }, // Wolf of Wall Street -> Scorsese
                new { MovieId = 10L, DirectorId = 1L } // La La Land -> Nolan
            };

            using var movieDirectorStmt = connection.Prepare(@"
                MATCH (m:Movie), (d:Director) 
                WHERE m.id = $movie_id AND d.id = $director_id 
                CREATE (m)-[:DirectedBy]->(d)");

            foreach (var md in movieDirectors)
            {
                movieDirectorStmt.Bind("movie_id", md.MovieId);
                movieDirectorStmt.Bind("director_id", md.DirectorId);
                movieDirectorStmt.Execute();
            }

            // Movie-actor relationships
            var movieActors = new[]
            {
                new { MovieId = 1L, ActorId = 1L, Role = "Cobb" }, // Inception -> DiCaprio
                new { MovieId = 2L, ActorId = 8L, Role = "Batman" }, // Dark Knight -> Downey Jr.
                new { MovieId = 4L, ActorId = 2L, Role = "Captain Miller" }, // Saving Private Ryan -> Hanks
                new { MovieId = 5L, ActorId = 4L, Role = "Vincent Vega" }, // Pulp Fiction -> Pitt
                new { MovieId = 9L, ActorId = 1L, Role = "Jordan Belfort" }, // Wolf of Wall Street -> DiCaprio
                new { MovieId = 10L, ActorId = 5L, Role = "Mia" }, // La La Land -> Stone
                new { MovieId = 10L, ActorId = 6L, Role = "Sebastian" } // La La Land -> Gosling
            };

            using var movieActorStmt = connection.Prepare(@"
                MATCH (m:Movie), (a:Actor) 
                WHERE m.id = $movie_id AND a.id = $actor_id 
                CREATE (a)-[:StarredIn {role: $role}]->(m)");

            foreach (var ma in movieActors)
            {
                movieActorStmt.Bind("movie_id", ma.MovieId);
                movieActorStmt.Bind("actor_id", ma.ActorId);
                movieActorStmt.Bind("role", ma.Role);
                movieActorStmt.Execute();
            }

            // Movie similarity relationships
            var similarities = new[]
            {
                new { MovieId1 = 1L, MovieId2 = 3L, Score = 0.9 }, // Inception <-> Interstellar
                new { MovieId1 = 2L, MovieId2 = 1L, Score = 0.8 }, // Dark Knight <-> Inception
                new { MovieId1 = 5L, MovieId2 = 6L, Score = 0.85 }, // Pulp Fiction <-> Goodfellas
                new { MovieId1 = 7L, MovieId2 = 8L, Score = 0.75 }, // Alien <-> Blade Runner
                new { MovieId1 = 9L, MovieId2 = 4L, Score = 0.7 } // Wolf of Wall Street <-> Saving Private Ryan
            };

            using var similarityStmt = connection.Prepare(@"
                MATCH (m1:Movie), (m2:Movie) 
                WHERE m1.id = $movie_id1 AND m2.id = $movie_id2 
                CREATE (m1)-[:SimilarTo {similarity_score: $score}]->(m2)");

            foreach (var sim in similarities)
            {
                similarityStmt.Bind("movie_id1", sim.MovieId1);
                similarityStmt.Bind("movie_id2", sim.MovieId2);
                similarityStmt.Bind("score", sim.Score);
                similarityStmt.Execute();
            }

            Console.WriteLine("  Created all relationships");
        }

        private static void DemonstrateRecommendations(Connection connection)
        {
            // 1. Collaborative Filtering - Find users with similar tastes
            Console.WriteLine("1. Collaborative Filtering - Users with similar tastes:");
            using var similarUsersResult = connection.Query(@"
                MATCH (u1:User)-[r1:Rated]->(m:Movie)<-[r2:Rated]-(u2:User)
                WHERE u1.id = 1 AND u2.id != 1 AND ABS(r1.rating - r2.rating) <= 0.5
                RETURN u2.name, m.title, r1.rating, r2.rating
                ORDER BY ABS(r1.rating - r2.rating)");

            while (similarUsersResult.HasNext())
            {
                using var row = similarUsersResult.GetNext();
                var userName = row.GetValueAs<string>(0);
                var movieTitle = row.GetValueAs<string>(1);
                var rating1 = row.GetValueAs<double>(2);
                var rating2 = row.GetValueAs<double>(3);
                
                Console.WriteLine($"  {userName} rated '{movieTitle}' {rating2} (Alice rated it {rating1})");
            }

            // 2. Content-Based Filtering - Movies similar to liked movies
            Console.WriteLine("\n2. Content-Based Filtering - Movies similar to liked movies:");
            using var similarMoviesResult = connection.Query(@"
                MATCH (u:User)-[:Rated]->(m1:Movie)-[:SimilarTo]->(m2:Movie)
                WHERE u.id = 1 AND m1.rating >= 4.5 AND NOT EXISTS {
                    MATCH (u)-[:Rated]->(m2)
                }
                RETURN m2.title, m2.genre, m2.rating, m1.title as similar_to
                ORDER BY m2.rating DESC");

            while (similarMoviesResult.HasNext())
            {
                using var row = similarMoviesResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var genre = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                var similarTo = row.GetValueAs<string>(3);
                
                Console.WriteLine($"  '{movieTitle}' ({genre}) - Rating: {rating}, Similar to '{similarTo}'");
            }

            // 3. Genre-Based Recommendations
            Console.WriteLine("\n3. Genre-Based Recommendations - Movies in liked genres:");
            using var genreRecommendationsResult = connection.Query(@"
                MATCH (u:User)-[:Rated]->(m1:Movie)-[:BelongsTo]->(g:Genre)<-[:BelongsTo]-(m2:Movie)
                WHERE u.id = 1 AND m1.rating >= 4.0 AND NOT EXISTS {
                    MATCH (u)-[:Rated]->(m2)
                }
                RETURN m2.title, g.name as genre, m2.rating
                ORDER BY m2.rating DESC");

            while (genreRecommendationsResult.HasNext())
            {
                using var row = genreRecommendationsResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var genre = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                
                Console.WriteLine($"  '{movieTitle}' ({genre}) - Rating: {rating}");
            }

            // 4. Director-Based Recommendations
            Console.WriteLine("\n4. Director-Based Recommendations - Movies by liked directors:");
            using var directorRecommendationsResult = connection.Query(@"
                MATCH (u:User)-[:Rated]->(m1:Movie)-[:DirectedBy]->(d:Director)<-[:DirectedBy]-(m2:Movie)
                WHERE u.id = 1 AND m1.rating >= 4.0 AND NOT EXISTS {
                    MATCH (u)-[:Rated]->(m2)
                }
                RETURN m2.title, d.name as director, m2.rating
                ORDER BY m2.rating DESC");

            while (directorRecommendationsResult.HasNext())
            {
                using var row = directorRecommendationsResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var director = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                
                Console.WriteLine($"  '{movieTitle}' by {director} - Rating: {rating}");
            }

            // 5. Actor-Based Recommendations
            Console.WriteLine("\n5. Actor-Based Recommendations - Movies with liked actors:");
            using var actorRecommendationsResult = connection.Query(@"
                MATCH (u:User)-[:Rated]->(m1:Movie)<-[:StarredIn]-(a:Actor)-[:StarredIn]->(m2:Movie)
                WHERE u.id = 1 AND m1.rating >= 4.0 AND NOT EXISTS {
                    MATCH (u)-[:Rated]->(m2)
                }
                RETURN m2.title, a.name as actor, m2.rating
                ORDER BY m2.rating DESC");

            while (actorRecommendationsResult.HasNext())
            {
                using var row = actorRecommendationsResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var actor = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                
                Console.WriteLine($"  '{movieTitle}' starring {actor} - Rating: {rating}");
            }

            // 6. Popular Movies Recommendation
            Console.WriteLine("\n6. Popular Movies Recommendation - Highly rated movies:");
            using var popularMoviesResult = connection.Query(@"
                MATCH (m:Movie)
                WHERE m.rating >= 8.5
                RETURN m.title, m.genre, m.rating, m.year
                ORDER BY m.rating DESC");

            while (popularMoviesResult.HasNext())
            {
                using var row = popularMoviesResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var genre = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                var year = row.GetValueAs<int>(3);
                
                Console.WriteLine($"  '{movieTitle}' ({genre}, {year}) - Rating: {rating}");
            }

            // 7. Demographic-Based Recommendations
            Console.WriteLine("\n7. Demographic-Based Recommendations - Movies liked by similar users:");
            using var demographicResult = connection.Query(@"
                MATCH (u1:User)-[:Rated]->(m:Movie)<-[:Rated]-(u2:User)
                WHERE u1.id = 1 AND u2.age BETWEEN 25 AND 35 AND u2.gender = u1.gender
                   AND m.rating >= 4.0 AND NOT EXISTS {
                    MATCH (u1)-[:Rated]->(m)
                }
                RETURN m.title, m.genre, m.rating, COUNT(u2) as similar_users
                ORDER BY similar_users DESC, m.rating DESC");

            while (demographicResult.HasNext())
            {
                using var row = demographicResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var genre = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                var similarUsers = row.GetValueAs<long>(3);
                
                Console.WriteLine($"  '{movieTitle}' ({genre}) - Rating: {rating}, Liked by {similarUsers} similar users");
            }

            // 8. Recommendation Score Calculation
            Console.WriteLine("\n8. Recommendation Score Calculation - Weighted recommendations:");
            using var weightedResult = connection.Query(@"
                MATCH (u:User)-[r:Rated]->(m1:Movie)-[:SimilarTo]->(m2:Movie)
                WHERE u.id = 1 AND NOT EXISTS {
                    MATCH (u)-[:Rated]->(m2)
                }
                RETURN m2.title, 
                       m2.rating as movie_rating,
                       AVG(r.rating) as user_avg_rating,
                       AVG(similarity_score) as avg_similarity,
                       (m2.rating * 0.4 + AVG(r.rating) * 0.3 + AVG(similarity_score) * 0.3) as recommendation_score
                ORDER BY recommendation_score DESC");

            while (weightedResult.HasNext())
            {
                using var row = weightedResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var movieRating = row.GetValueAs<double>(1);
                var userAvgRating = row.GetValueAs<double>(2);
                var avgSimilarity = row.GetValueAs<double>(3);
                var recommendationScore = row.GetValueAs<double>(4);
                
                Console.WriteLine($"  '{movieTitle}' - Score: {recommendationScore:F2} (Movie: {movieRating}, User Avg: {userAvgRating:F1}, Similarity: {avgSimilarity:F2})");
            }

            // 9. Cross-Recommendation Analysis
            Console.WriteLine("\n9. Cross-Recommendation Analysis - Movies that bridge different user groups:");
            using var crossRecommendationResult = connection.Query(@"
                MATCH (u1:User)-[:Rated]->(m:Movie)<-[:Rated]-(u2:User)
                WHERE u1.age < 30 AND u2.age >= 30 AND m.rating >= 4.0
                RETURN m.title, m.genre, m.rating, COUNT(DISTINCT u1) as young_users, COUNT(DISTINCT u2) as older_users
                ORDER BY (COUNT(DISTINCT u1) + COUNT(DISTINCT u2)) DESC");

            while (crossRecommendationResult.HasNext())
            {
                using var row = crossRecommendationResult.GetNext();
                var movieTitle = row.GetValueAs<string>(0);
                var genre = row.GetValueAs<string>(1);
                var rating = row.GetValueAs<double>(2);
                var youngUsers = row.GetValueAs<long>(3);
                var olderUsers = row.GetValueAs<long>(4);
                
                Console.WriteLine($"  '{movieTitle}' ({genre}) - Rating: {rating}, Liked by {youngUsers} young users and {olderUsers} older users");
            }
        }
    }
}
