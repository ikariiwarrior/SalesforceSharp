using System;
using System.Collections.Generic;
using System.Net;

namespace SalesforceSharp.Common.Http
{
    /// <summary>
    /// Concrete <see cref="IHttpResponse"/> backed by data read from
    /// <see cref="System.Net.Http.HttpResponseMessage"/>. Immutable after construction.
    /// </summary>
    internal sealed class HttpResponseWrapper : IHttpResponse
    {
        private readonly IDictionary<string, string> m_headers;

        internal HttpResponseWrapper(
            HttpStatusCode statusCode,
            string content,
            byte[] rawBytes,
            IDictionary<string, string> headers,
            Exception errorException = null)
        {
            StatusCode     = statusCode;
            Content        = content ?? string.Empty;
            RawBytes       = rawBytes ?? Array.Empty<byte>();
            m_headers      = headers  ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ErrorException = errorException;
        }

        /// <inheritdoc/>
        public HttpStatusCode StatusCode { get; }

        /// <inheritdoc/>
        public string Content { get; }

        /// <inheritdoc/>
        public byte[] RawBytes { get; }

        /// <inheritdoc/>
        public Exception ErrorException { get; }

        /// <inheritdoc/>
        public string GetHeader(string name)
        {
            m_headers.TryGetValue(name, out var value);
            return value;
        }
    }

    /// <summary>
    /// Typed companion that adds a deserialized <see cref="Data"/> property.
    /// </summary>
    internal sealed class HttpResponseWrapper<T> : IHttpResponse<T>
    {
        private readonly IHttpResponse m_inner;

        internal HttpResponseWrapper(IHttpResponse inner, T data)
        {
            m_inner = inner;
            Data    = data;
        }

        /// <inheritdoc/>
        public T Data { get; }

        /// <inheritdoc/>
        public HttpStatusCode StatusCode     => m_inner.StatusCode;

        /// <inheritdoc/>
        public string Content                => m_inner.Content;

        /// <inheritdoc/>
        public byte[] RawBytes               => m_inner.RawBytes;

        /// <inheritdoc/>
        public Exception ErrorException      => m_inner.ErrorException;

        /// <inheritdoc/>
        public string GetHeader(string name) => m_inner.GetHeader(name);
    }
}
