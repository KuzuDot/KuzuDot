using System;
using System.Collections.Generic;
using System.Text;

namespace KuzuDot.Utils
{
    internal static class KuzuStringExtensions
    {

#if NETSTANDARD2_0
        /// <summary>
        /// Helper method to avoid .NET Framework issues with String.Contains and StringComparison
        /// </summary>
        /// <param name="source">Source String</param>
        /// <param name="toCheck">String to check for</param>
        /// <param name="comp">Comparision Type</param>
        /// <returns>true if the source string contains the check string</returns>
        internal static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        internal static bool Contains(this string source, char toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck) >= 0;
        }
#endif
    }
}
