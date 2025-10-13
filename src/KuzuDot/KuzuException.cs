using System;

namespace KuzuDot
{
    /// <summary>
    /// Exceptions for errors thrown by KuzuDB operations.
    /// </summary>
    public class KuzuException : Exception
    {
        public KuzuException() { }
        public KuzuException(string message) : base(message) { }
        public KuzuException(string message, Exception innerException) : base(message, innerException) { }
    }
}