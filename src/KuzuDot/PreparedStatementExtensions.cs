using System;
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
    }
}
