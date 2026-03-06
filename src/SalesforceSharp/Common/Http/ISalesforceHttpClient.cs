using System.Collections.Generic;

namespace SalesforceSharp.Common.Http
{
    /// <summary>
    /// Minimal HTTP client contract used by this library.
    /// Replaces RestSharp's <c>IRestClient</c> and is kept intentionally narrow
    /// so that tests can substitute a fake without pulling in RestSharp.
    /// </summary>
    public interface ISalesforceHttpClient
    {
        /// <summary>
        /// Executes a request and deserializes the response body to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target deserialization type.</typeparam>
        /// <param name="url">Full, absolute request URL.</param>
        /// <param name="method">HTTP verb.</param>
        /// <param name="bearerToken">OAuth bearer token added to the Authorization header.</param>
        /// <param name="jsonBody">Optional JSON body (used for POST / PATCH).</param>
        IHttpResponse<T> Execute<T>(string url, HttpVerb method, string bearerToken, string jsonBody = null) where T : new();

        /// <summary>
        /// Executes a request and returns the raw response without deserialization.
        /// </summary>
        /// <param name="url">Full, absolute request URL.</param>
        /// <param name="method">HTTP verb.</param>
        /// <param name="bearerToken">OAuth bearer token added to the Authorization header.</param>
        /// <param name="jsonBody">Optional JSON body (used for POST / PATCH).</param>
        IHttpResponse Execute(string url, HttpVerb method, string bearerToken, string jsonBody = null);

        /// <summary>
        /// Posts a form-encoded body and returns the raw response.
        /// Used exclusively by the OAuth token endpoint.
        /// </summary>
        /// <param name="url">Full, absolute endpoint URL.</param>
        /// <param name="formFields">Key-value pairs to send as application/x-www-form-urlencoded.</param>
        IHttpResponse PostForm(string url, IEnumerable<KeyValuePair<string, string>> formFields);
    }
}
