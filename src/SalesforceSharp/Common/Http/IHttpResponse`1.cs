namespace SalesforceSharp.Common.Http
{
    /// <summary>
    /// Typed extension of <see cref="IHttpResponse"/> that carries a deserialized
    /// response body. Replaces RestSharp's <c>IRestResponse{T}</c>.
    /// </summary>
    /// <typeparam name="T">The deserialized response body type.</typeparam>
    public interface IHttpResponse<T> : IHttpResponse
    {
        /// <summary>The response body deserialized to <typeparamref name="T"/>.</summary>
        T Data { get; }
    }
}
