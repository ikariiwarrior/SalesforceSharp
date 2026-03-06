using System;
using System.Collections.Generic;
using System.Net;
using SalesforceSharp.Common;
using SalesforceSharp.Common.Http;
using SalesforceSharp.Serialization;

namespace SalesforceSharp.Security
{
    /// <summary>
    /// The username-password authentication flow can be used to authenticate when the consumer already has the user's credentials.
    /// In this flow, the user's credentials are used by the application to request an access token as shown in the following steps.
    /// <remarks>
    /// Warning: This OAuth authentication flow involves passing the user's credentials back and forth. Use this
    /// authentication flow only when necessary. No refresh token will be issued.
    /// 
    /// You should only use the password access grant type in situations such as an AUTONOMOUS CLIENT, where a user cannot be present 
    /// at application startup. In this instance, you should carefully set the API user's permissions to minimize its access as far as possible, 
    /// and protect the API user's stored credentials from unauthorized access.
    /// 
    /// More info at:
    /// http://wiki.developerforce.com/page/Digging_Deeper_into_OAuth_2.0_on_Force.com
    /// </remarks>
    /// </summary>
    public class UsernamePasswordAuthenticationFlow : IAuthenticationFlow
    {
        #region Fields
        private readonly ISalesforceHttpClient m_httpClient;
        private readonly string m_clientId;
        private readonly string m_clientSecret;
        private readonly string m_username;
        private readonly string m_password;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UsernamePasswordAuthenticationFlow"/> class.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="clientSecret">The client secret.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public UsernamePasswordAuthenticationFlow(string clientId, string clientSecret, string username, string password) :
            this(new SalesforceHttpClient(), clientId, clientSecret, username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsernamePasswordAuthenticationFlow"/> class.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="clientSecret">The client secret.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="tokenRequestEndpointUrl">The token request endpoint url.</param>
        public UsernamePasswordAuthenticationFlow(string clientId, string clientSecret, string username, string password, string tokenRequestEndpointUrl) :
            this(new SalesforceHttpClient(), clientId, clientSecret, username, password, tokenRequestEndpointUrl)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsernamePasswordAuthenticationFlow"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client which will be used.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="clientSecret">The client secret.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="tokenRequestEndpointUrl">The token request endpoint url.</param>
        internal UsernamePasswordAuthenticationFlow(ISalesforceHttpClient httpClient, string clientId, string clientSecret, string username, string password, string tokenRequestEndpointUrl = "https://login.salesforce.com/services/oauth2/token")
        {
            ExceptionHelper.ThrowIfNull("httpClient", httpClient);
            ExceptionHelper.ThrowIfNullOrEmpty("clientId", clientId);
            ExceptionHelper.ThrowIfNullOrEmpty("clientSecret", clientSecret);
            ExceptionHelper.ThrowIfNullOrEmpty("username", username);
            ExceptionHelper.ThrowIfNullOrEmpty("password", password);

            m_httpClient            = httpClient;
            m_clientId              = clientId;
            m_clientSecret          = clientSecret;
            m_username              = username;
            m_password              = password;
            TokenRequestEndpointUrl = tokenRequestEndpointUrl;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the token request endpoint url.
        /// </summary>
        /// <remarks>
        /// The default value is https://login.salesforce.com/services/oauth2/token.
        /// For sandbox use "https://test.salesforce.com/services/oauth2/token".
        /// </remarks>
        public string TokenRequestEndpointUrl { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Authenticate in the Salesforce REST's API.
        /// </summary>
        /// <returns>
        /// The authentication info with access token and instance url for further API calls.
        /// </returns>
        /// <remarks>
        /// If authentication fails a <see cref="SalesforceException"/> will be thrown.
        /// </remarks>
        public AuthenticationInfo Authenticate()
        {
            var formFields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type",    "password"),
                new KeyValuePair<string, string>("client_id",     m_clientId),
                new KeyValuePair<string, string>("client_secret", m_clientSecret),
                new KeyValuePair<string, string>("username",      m_username),
                new KeyValuePair<string, string>("password",      m_password)
            };

            var response        = m_httpClient.PostForm(TokenRequestEndpointUrl, formFields);
            var isAuthenticated = response.StatusCode == HttpStatusCode.OK;

            var deserializer = new GenericJsonDeserializer(new SalesforceContractResolver(false));
            var responseData = deserializer.Deserialize<dynamic>(response.Content);

            if (responseData == null)
            {
                var transportError = response.ErrorException?.Message ?? "Unknown transport error.";
                throw new SalesforceException(transportError, transportError);
            }

            if (isAuthenticated)
            {
                return new AuthenticationInfo(responseData.access_token.Value, responseData.instance_url.Value);
            }

            throw new SalesforceException(responseData.error.Value, responseData.error_description.Value);
        }
        #endregion
    }
}
