using System;

namespace SalesforceSharp.Common
{
    /// <summary>
    /// Guard-clause helpers that cover the subset of HelperSharp.ExceptionHelper
    /// used by this library. Built on BCL types only — no external dependencies.
    /// </summary>
    internal static class ExceptionHelper
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is <c>null</c>.
        /// </summary>
        /// <param name="paramName">Parameter name surfaced in the exception.</param>
        /// <param name="value">The reference to test.</param>
        public static void ThrowIfNull(string paramName, object value)
        {
            _ = value ?? throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws when <paramref name="value"/> is <c>null</c>, empty, or whitespace.
        /// <list type="bullet">
        ///   <item><see cref="ArgumentNullException"/> — value is <c>null</c>.</item>
        ///   <item><see cref="ArgumentException"/> — value is empty or whitespace.</item>
        /// </list>
        /// </summary>
        /// <param name="paramName">Parameter name surfaced in the exception.</param>
        /// <param name="value">The string to test.</param>
        public static void ThrowIfNullOrEmpty(string paramName, string value)
        {
            _ = value ?? throw new ArgumentNullException(paramName);

            if (value.Trim().Length == 0)
            {
                throw new ArgumentException($"'{paramName}' must not be empty or whitespace.", paramName);
            }
        }
    }
}
