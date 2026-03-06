namespace SalesforceSharp.Common.Http
{
    /// <summary>
    /// HTTP verbs used by <see cref="ISalesforceHttpClient"/>.
    /// Replaces RestSharp's <c>Method</c> enum.
    /// </summary>
    public enum HttpVerb
    {
        /// <summary>HTTP GET</summary>
        GET,

        /// <summary>HTTP POST</summary>
        POST,

        /// <summary>HTTP PATCH</summary>
        PATCH,

        /// <summary>HTTP DELETE</summary>
        DELETE
    }
}
