using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace SalesforceSharp.Common.Http
{
    /// <summary>
    /// <see cref="ISalesforceHttpClient"/> implementation backed by
    /// <see cref="System.Net.Http.HttpClient"/>. Replaces RestSharp's
    /// <c>RestClient</c> for all HTTP operations in this library.
    /// </summary>
    /// <remarks>
    /// A single <see cref="System.Net.Http.HttpClient"/> instance is shared for the
    /// lifetime of this object. Callers that manage <see cref="SalesforceClient"/>
    /// as a singleton (the recommended pattern) will naturally share the underlying
    /// socket pool and avoid socket exhaustion.
    /// </remarks>
    internal sealed class SalesforceHttpClient : ISalesforceHttpClient, IDisposable
    {
        private readonly HttpClient m_http;
        private bool m_disposed;

        /// <summary>
        /// Initializes the client, ensuring TLS 1.2 is available on the legacy
        /// <see cref="ServicePointManager"/> stack (required on .NET 4.x).
        /// </summary>
        public SalesforceHttpClient()
        {
            EnsureTls12();

            m_http = new HttpClient();
        }

        /// <summary>
        /// Internal constructor that accepts a pre-configured <see cref="HttpClient"/>,
        /// allowing unit tests to inject an <see cref="HttpMessageHandler"/> fake.
        /// </summary>
        internal SalesforceHttpClient(HttpClient httpClient)
        {
            m_http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // ------------------------------------------------------------------ //
        //  ISalesforceHttpClient
        // ------------------------------------------------------------------ //

        /// <inheritdoc/>
        public IHttpResponse<T> Execute<T>(
            string url,
            HttpVerb method,
            string bearerToken,
            string jsonBody = null) where T : new()
        {
            var raw = Execute(url, method, bearerToken, jsonBody);

            T data = default;
            if (!string.IsNullOrEmpty(raw.Content) && raw.ErrorException == null)
            {
                data = JsonConvert.DeserializeObject<T>(raw.Content);
            }

            return new HttpResponseWrapper<T>(raw, data);
        }

        /// <inheritdoc/>
        public IHttpResponse Execute(
            string url,
            HttpVerb method,
            string bearerToken,
            string jsonBody = null)
        {
            using (var request = BuildRequest(url, method, bearerToken, jsonBody))
            {
                return Send(request);
            }
        }

        /// <inheritdoc/>
        public IHttpResponse PostForm(
            string url,
            IEnumerable<KeyValuePair<string, string>> formFields)
        {
            using (var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url))
            using (var form   = new FormUrlEncodedContent(formFields))
            {
                request.Content = form;
                return Send(request);
            }
        }

        // ------------------------------------------------------------------ //
        //  IDisposable
        // ------------------------------------------------------------------ //

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!m_disposed)
            {
                m_http.Dispose();
                m_disposed = true;
            }
        }

        // ------------------------------------------------------------------ //
        //  Internals
        // ------------------------------------------------------------------ //

        private HttpRequestMessage BuildRequest(
            string url,
            HttpVerb method,
            string bearerToken,
            string jsonBody)
        {
            var request = new HttpRequestMessage(ToHttpMethod(method), url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + bearerToken);

            if (!string.IsNullOrEmpty(jsonBody))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private IHttpResponse Send(HttpRequestMessage request)
        {
            try
            {
                // Synchronous path — mirrors the original RestSharp usage and
                // avoids introducing async throughout the existing public API.
                var httpResponse = m_http.SendAsync(request).GetAwaiter().GetResult();
                var rawBytes     = httpResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                var content      = Encoding.UTF8.GetString(rawBytes);
                var headers      = ReadHeaders(httpResponse);

                return new HttpResponseWrapper(
                    httpResponse.StatusCode,
                    content,
                    rawBytes,
                    headers);
            }
            catch (Exception ex)
            {
                return new HttpResponseWrapper(
                    HttpStatusCode.ServiceUnavailable,
                    string.Empty,
                    Array.Empty<byte>(),
                    null,
                    ex);
            }
        }

        private static Dictionary<string, string> ReadHeaders(HttpResponseMessage response)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in response.Headers)
            {
                dict[header.Key] = string.Join(",", header.Value);
            }

            foreach (var header in response.Content.Headers)
            {
                dict[header.Key] = string.Join(",", header.Value);
            }

            return dict;
        }

        private static System.Net.Http.HttpMethod ToHttpMethod(HttpVerb verb)
        {
            switch (verb)
            {
                case HttpVerb.POST:   return System.Net.Http.HttpMethod.Post;
                case HttpVerb.PATCH:  return new System.Net.Http.HttpMethod("PATCH");
                case HttpVerb.DELETE: return System.Net.Http.HttpMethod.Delete;
                default:              return System.Net.Http.HttpMethod.Get;
            }
        }

        private static void EnsureTls12()
        {
            if (ServicePointManager.SecurityProtocol != 0)
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
        }
    }
}
