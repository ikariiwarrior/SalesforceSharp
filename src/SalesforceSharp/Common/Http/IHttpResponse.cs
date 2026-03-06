using System;
using System.Net;

namespace SalesforceSharp.Common.Http
{
    /// <summary>
    /// Represents the result of an HTTP request made by <see cref="ISalesforceHttpClient"/>.
    /// Replaces RestSharp's <c>IRestResponse</c> and <c>IRestResponse{T}</c>.
    /// </summary>
    public interface IHttpResponse
    {
        /// <summary>HTTP status code returned by the server.</summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>Response body as a decoded string.</summary>
        string Content { get; }

        /// <summary>Raw response body bytes.</summary>
        byte[] RawBytes { get; }

        /// <summary>
        /// Any transport-level exception (connection failure, timeout, etc.).
        /// <c>null</c> when the request completed successfully at the HTTP layer.
        /// </summary>
        Exception ErrorException { get; }

        /// <summary>Gets the value of a named response header, or <c>null</c> if absent.</summary>
        /// <param name="name">Header name (case-insensitive).</param>
        string GetHeader(string name);
    }
}
