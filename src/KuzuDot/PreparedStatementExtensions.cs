using System;
using System.Collections.Generic;
using KuzuDot.Utils;

namespace KuzuDot
{
    /// <summary>
    /// Extension methods for PreparedStatement to provide convenient POCO binding with different naming strategies.
    /// </summary>
    public static class PreparedStatementExtensions
    {
        /// <summary>
        /// Binds a POCO object using lowercase naming strategy.
        /// Converts property names like "BirthYear" to "birthyear".
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <returns>The prepared statement for method chaining</returns>
        public static PreparedStatement BindLowercase(this PreparedStatement stmt, object parameters)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            return stmt.Bind(parameters, NamingStrategy.Lowercase);
        }

        /// <summary>
        /// Binds a POCO object using snake_case naming strategy (default).
        /// Converts property names like "BirthYear" to "birth_year".
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <returns>The prepared statement for method chaining</returns>
        public static PreparedStatement BindSnakeCase(this PreparedStatement stmt, object parameters)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            return stmt.Bind(parameters, NamingStrategy.SnakeCase);
        }

        /// <summary>
        /// Binds a POCO object using camelCase naming strategy.
        /// Converts property names like "BirthYear" to "birthYear".
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <returns>The prepared statement for method chaining</returns>
        public static PreparedStatement BindCamelCase(this PreparedStatement stmt, object parameters)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            return stmt.Bind(parameters, NamingStrategy.CamelCase);
        }

        /// <summary>
        /// Binds a POCO object using PascalCase naming strategy.
        /// Keeps property names like "BirthYear" as "BirthYear".
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <returns>The prepared statement for method chaining</returns>
        public static PreparedStatement BindPascalCase(this PreparedStatement stmt, object parameters)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            return stmt.Bind(parameters, NamingStrategy.PascalCase);
        }

        /// <summary>
        /// Binds a POCO object using exact naming strategy.
        /// Uses property names exactly as they are defined.
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <returns>The prepared statement for method chaining</returns>
        public static PreparedStatement BindExact(this PreparedStatement stmt, object parameters)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            return stmt.Bind(parameters, NamingStrategy.Exact);
        }

        /// <summary>
        /// Binds a POCO object and executes the statement.
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <returns>The query result</returns>
        public static QueryResult BindAndExecute(this PreparedStatement stmt, object parameters)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            stmt.Bind(parameters);
            return stmt.Execute();
        }

        /// <summary>
        /// Binds a POCO object with a specific naming strategy and executes the statement.
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="parameters">The POCO object to bind</param>
        /// <param name="strategy">The naming strategy to use</param>
        /// <returns>The query result</returns>
        public static QueryResult BindAndExecute(this PreparedStatement stmt, object parameters, NamingStrategy strategy)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            stmt.Bind(parameters, strategy);
            return stmt.Execute();
        }

        /// <summary>
        /// Binds and executes the statement for each item in the enumerable collection.
        /// This is useful for batch operations where you want to insert/update multiple records.
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="items">The collection of POCO objects to bind and execute</param>
        /// <returns>The number of items processed</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP017:Prefer using", Justification = "Manual disposal needed for error handling")]
        public static int BindAndExecuteBatch(this PreparedStatement stmt, IEnumerable<object> items)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            KuzuGuard.NotNull(items, nameof(items));

            int count = 0;
            foreach (var item in items)
            {
                stmt.Bind(item);
                var result = stmt.Execute();
                if (!result.IsSuccess)
                {
                    var errorMessage = result.ErrorMessage;
                    result.Dispose();
                    throw new KuzuException($"Batch execution failed at item {count}: {errorMessage}");
                }
                result.Dispose();
                count++;
            }
            return count;
        }

        /// <summary>
        /// Binds and executes the statement for each item in the enumerable collection using a specific naming strategy.
        /// This is useful for batch operations where you want to insert/update multiple records.
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="items">The collection of POCO objects to bind and execute</param>
        /// <param name="strategy">The naming strategy to use</param>
        /// <returns>The number of items processed</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP017:Prefer using", Justification = "Manual disposal needed for error handling")]
        public static int BindAndExecuteBatch(this PreparedStatement stmt, IEnumerable<object> items, NamingStrategy strategy)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            KuzuGuard.NotNull(items, nameof(items));

            int count = 0;
            foreach (var item in items)
            {
                stmt.Bind(item, strategy);
                var result = stmt.Execute();
                if (!result.IsSuccess)
                {
                    var errorMessage = result.ErrorMessage;
                    result.Dispose();
                    throw new KuzuException($"Batch execution failed at item {count}: {errorMessage}");
                }
                result.Dispose();
                count++;
            }
            return count;
        }

        /// <summary>
        /// Binds and executes the statement for each item in the enumerable collection, with error handling.
        /// This is useful for batch operations where you want to continue processing even if some items fail.
        /// </summary>
        /// <param name="stmt">The prepared statement</param>
        /// <param name="items">The collection of POCO objects to bind and execute</param>
        /// <param name="onError">Optional callback to handle errors for individual items. If null, errors are ignored.</param>
        /// <returns>The number of items successfully processed</returns>
        public static int BindAndExecuteBatchWithErrorHandling(this PreparedStatement stmt, IEnumerable<object> items, Action<object, int, Exception>? onError = null)
        {
            KuzuGuard.NotNull(stmt, nameof(stmt));
            KuzuGuard.NotNull(items, nameof(items));

            int count = 0;
            int successCount = 0;
            foreach (var item in items)
            {
                try
                {
                    stmt.Bind(item);
                    using var result = stmt.Execute();
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        var ex = new KuzuException($"Batch execution failed at item {count}: {result.ErrorMessage}");
                        onError?.Invoke(item, count, ex);
                    }
                }
                catch (KuzuException ex)
                {
                    onError?.Invoke(item, count, ex);
                }
                catch (ArgumentException ex)
                {
                    onError?.Invoke(item, count, ex);
                }
                catch (InvalidOperationException ex)
                {
                    onError?.Invoke(item, count, ex);
                }
                count++;
            }
            return successCount;
        }
    }
}
