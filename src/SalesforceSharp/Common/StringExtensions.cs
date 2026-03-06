using System;
using System.Net;

namespace SalesforceSharp.Common
{
    /// <summary>
    /// String extension methods that cover the subset of HelperSharp and
    /// RestSharp.Extensions used by this library. No external dependencies.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Formats the template string using <see cref="string.Format(string, object[])"/>.
        /// Replaces HelperSharp's <c>StringExtensions.With</c>.
        /// </summary>
        /// <param name="template">The composite format string.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>The formatted string.</returns>
        public static string With(this string template, params object[] args)
        {
            return string.Format(template, args);
        }

        /// <summary>
        /// URL-encodes the string using <see cref="Uri.EscapeDataString"/>.
        /// Replaces RestSharp's <c>StringExtensions.UrlEncode</c>.
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>The URL-encoded string.</returns>
        public static string UrlEncode(this string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty);
        }
    }
}
