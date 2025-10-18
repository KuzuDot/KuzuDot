namespace KuzuDot
{
    /// <summary>
    /// Defines how property names should be transformed when binding POCO objects to prepared statement parameters.
    /// </summary>
    public enum NamingStrategy
    {
        /// <summary>
        /// Convert to lowercase: "BirthYear" → "birthyear"
        /// </summary>
        Lowercase,
        
        /// <summary>
        /// Convert to snake_case: "BirthYear" → "birth_year"
        /// </summary>
        SnakeCase,
        
        /// <summary>
        /// Convert to camelCase: "BirthYear" → "birthYear"
        /// </summary>
        CamelCase,
        
        /// <summary>
        /// Keep PascalCase: "BirthYear" → "BirthYear"
        /// </summary>
        PascalCase,
        
        /// <summary>
        /// Use exact property name without transformation
        /// </summary>
        Exact
    }
}
