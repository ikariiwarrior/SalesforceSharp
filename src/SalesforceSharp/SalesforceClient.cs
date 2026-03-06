using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SalesforceSharp.Common;
using SalesforceSharp.Common.Http;
using SalesforceSharp.Models;
using SalesforceSharp.Security;
using SalesforceSharp.Serialization;

namespace SalesforceSharp
{
    /// <summary>
    /// The central point to communicate with Salesforce REST API.
    /// </summary>
    public class SalesforceClient
    {
        #region Fields
        private string m_accessToken;
        private DynamicJsonDeserializer m_deserializer;
        private ISalesforceHttpClient m_httpClient;
        private GenericJsonDeserializer genericJsonDeserializer;
        private GenericJsonSerializer updateJsonSerializer;
        private static readonly Regex apiUsageRegexp = new Regex(@"api-usage=(\d+)/(\d+)", RegexOptions.Compiled);
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SalesforceClient"/> class.
        /// </summary>
        public SalesforceClient()
            : this(new SalesforceHttpClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SalesforceClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        protected internal SalesforceClient(ISalesforceHttpClient httpClient)
        {
            m_httpClient = httpClient;
            ApiVersion = "v28.0";
            m_deserializer = new DynamicJsonDeserializer();
            genericJsonDeserializer = new GenericJsonDeserializer(new SalesforceContractResolver(false));
            updateJsonSerializer = new GenericJsonSerializer(new SalesforceContractResolver(true));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the API version.
        /// </summary>
        /// <remarks>
        /// The default value is v28.0.
        /// </remarks>
        /// <value>
        /// The API version.
        /// </value>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is authenticated; otherwise, <c>false</c>.
        /// </value>
        public bool IsAuthenticated { get; private set; }

        /// <summary>
        /// Gets the instance URL.
        /// </summary>
        /// <value>
        /// The instance URL.
        /// </value>
        public string InstanceUrl { get; private set; }

        /// <summary>
        /// Get current API calls number
        /// </summary>
        /// <value>
        /// current API calls number
        /// </value>        
        public int ApiCallsUsed { get; private set; }

        /// <summary>
        /// Get total API calls limit
        /// </summary>
        /// <value>
        /// API calls limit
        /// </value>        
        public int ApiCallsLimit { get; private set; }
        #endregion

        #region Methods
        /// <summary>
        /// Authenticates the client.
        /// </summary>
        /// <param name="authenticationFlow">The authentication flow which will be used to authenticate on REST API.</param>
        public void Authenticate(IAuthenticationFlow authenticationFlow)
        {
            var info = authenticationFlow.Authenticate();
            m_accessToken = info.AccessToken;
            InstanceUrl = info.InstanceUrl;
            IsAuthenticated = true;
        }

        /// <summary>
        /// Executes a SOQL query and returns the result.
        /// </summary>
        /// <param name="query">The SOQL query.</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>The API result for the query.</returns>
        public IList<T> Query<T>(string query, string altUrl = "") where T : new()
        {
            return QueryActionBatch<T>(query, s => { }, altUrl);
        }

        /// <summary>
        /// Executes a SOQL query and returns the result.
        /// </summary>
        /// <param name="query">The SOQL query.</param>
        /// <param name="action">Action to call after getting a non error response.</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>The API result for the query.</returns>
        public IList<T> QueryActionBatch<T>(string query, Action<IList<T>> action, string altUrl = "") where T : new()
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            ExceptionHelper.ThrowIfNullOrEmpty("query", query);

            var escapedQuery = query.UrlEncode();

            var url = "{0}?q={1}".With(string.IsNullOrEmpty(altUrl) ? GetUrl("query") : GetAltUrl(altUrl), escapedQuery);

            var returns = new List<T>();
            IHttpResponse<SalesforceQueryResult<T>> response = null;

            do
            {
                if (response != null)
                {
                    url = GetNextRecordsUrl(response);
                    response = null;
                }

                if (string.IsNullOrEmpty(url))
                {
                    break;
                }

                response = Request<SalesforceQueryResult<T>>(url);
                if (response == null || response.Data == null) continue;

                if (!response.Data.Records.Any()) continue;
                try
                {
                    var customResponse = genericJsonDeserializer.Deserialize<SalesforceQueryResult<T>>(response.Content);
                    if (customResponse == null) continue;
                    action(customResponse.Records);
                    returns.AddRange(customResponse.Records);
                }
                catch
                {
                    throw;
                }

            } while (response != null && response.Data != null && !response.Data.Done && !string.IsNullOrEmpty(response.Data.NextRecordsUrl));

            return returns;
        }

        private string GetNextRecordsUrl<T>(IHttpResponse<SalesforceQueryResult<T>> previousResponse) where T : new()
        {
            if (previousResponse == null || previousResponse.Data == null ||
                string.IsNullOrEmpty(previousResponse.Data.NextRecordsUrl))
            {
                return string.Empty;
            }
            return InstanceUrl + previousResponse.Data.NextRecordsUrl;
        }

        /// <summary>
        /// Finds a record by Id.
        /// </summary>
        /// <typeparam name="T">The record type.</typeparam>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="recordId">The record id.</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>The record with the specified id.</returns>
        public T FindById<T>(string objectName, string recordId, string altUrl) where T : new()
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);
            ExceptionHelper.ThrowIfNullOrEmpty("altUrl", altUrl);

            var result = Query<T>("SELECT {0} FROM {1} WHERE Id = '{2}'".With(GetRecordProjection(typeof(T)), objectName, SanitizeSoqlId(recordId)), altUrl);

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Finds a record by Id.
        /// </summary>
        /// <typeparam name="T">The record type.</typeparam>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="recordId">The record id.</param>
        /// <returns>The record with the specified id.</returns>
        public T FindById<T>(string objectName, string recordId) where T : new()
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);

            var result = Query<T>("SELECT {0} FROM {1} WHERE Id = '{2}'".With(GetRecordProjection(typeof(T)), objectName, SanitizeSoqlId(recordId)));

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Obtains a JSON representation of fields and meta data for a given object type.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <returns></returns>
        public string ReadMetaData(string objectName)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);

            var response = Request<object>(GetUrl("sobjects"), "{0}/describe/".With(objectName));

            return response.Content;
        }

        /// <summary>
        /// Creates a record.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="record">The record to be created.</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>The Id of created record.</returns>
        public string Create(string objectName, object record, string altUrl)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNull("record", record);
            ExceptionHelper.ThrowIfNullOrEmpty("altUrl", altUrl);

            var response = Request<object>(GetAltUrl(altUrl), objectName, record, HttpVerb.POST);
            return m_deserializer.Deserialize<dynamic>(response.Content).id.Value;
        }

        /// <summary>
        /// Creates a record.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="record">The record to be created.</param>
        /// <returns>The Id of created record.</returns>
        public string Create(string objectName, object record)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNull("record", record);

            var response = Request<object>(GetUrl("sobjects"), objectName, record, HttpVerb.POST);
            return m_deserializer.Deserialize<dynamic>(response.Content).id.Value;
        }

        /// <summary>
        /// Updates a record.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="recordId">The record id.</param>
        /// <param name="record">The record to be updated.</param>
        public bool Update(string objectName, string recordId, object record)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);
            ExceptionHelper.ThrowIfNull("record", record);

            var response = RequestRaw(GetUrl("sobjects"), "{0}/{1}".With(objectName, recordId), record, HttpVerb.PATCH);

            // HTTP status code 204 is returned if an existing record is updated.
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Updates a record.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="recordId">The record id.</param>
        /// <param name="record">The record to be updated.</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        public bool Update(string objectName, string recordId, object record, string altUrl)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);
            ExceptionHelper.ThrowIfNull("record", record);
            ExceptionHelper.ThrowIfNullOrEmpty("altUrl", altUrl);

            var response = RequestRaw(GetAltUrl(altUrl), "{0}/{1}".With(objectName, recordId), record, HttpVerb.PATCH);

            // HTTP status code 204 is returned if an existing record is updated.
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Deletes a record.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="recordId">The record id which will be deleted.</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>True if was deleted, otherwise false.</returns>
        public bool Delete(string objectName, string recordId, string altUrl)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);
            ExceptionHelper.ThrowIfNullOrEmpty("altUrl", altUrl);

            var response = Request<object>(GetAltUrl(altUrl), "{0}/{1}".With(objectName, recordId), null, HttpVerb.DELETE);

            // HTTP status code 204 is returned if an existing record is deleted.
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Deletes a record.
        /// </summary>
        /// <param name="objectName">The name of the object in Salesforce.</param>
        /// <param name="recordId">The record id which will be deleted.</param>
        /// <returns>True if was deleted, otherwise false.</returns>
        public bool Delete(string objectName, string recordId)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);

            var response = Request<object>(GetUrl("sobjects"), "{0}/{1}".With(objectName, recordId), null, HttpVerb.DELETE);

            // HTTP status code 204 is returned if an existing record is deleted.
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Get sObject Details.
        /// </summary>
        /// <param name="sobjectApiName">object Api Id</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns></returns>
        public SalesforceObject GetSObjectDetail(string sobjectApiName, string altUrl = "")
        {
            var url = "{0}/{1}/describe".With(string.IsNullOrEmpty(altUrl) ? GetUrl("sobjects") : GetAltUrl(altUrl), sobjectApiName);

            var response = Request<SalesforceObject>(url);
            return response.Data;
        }

        /// <summary>
        /// Returns the raw content of a GET request to the given object.
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="recordId">The record id</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>The returned content as a string</returns>
        public string GetRawContent(string objectName, string recordId, string altUrl)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);
            ExceptionHelper.ThrowIfNullOrEmpty("altUrl", altUrl);

            var response = RequestRaw(GetAltUrl(altUrl), "{0}/{1}".With(objectName, recordId));

            return response.Content;
        }

        /// <summary>
        /// Returns the raw content of a GET request to the given object.
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="recordId">The record id</param>
        /// <returns>The returned content as a string</returns>
        public string GetRawContent(string objectName, string recordId)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);

            var response = RequestRaw(GetUrl("sobjects"), "{0}/{1}".With(objectName, recordId));

            return response.Content;
        }

        /// <summary>
        /// Returns the raw byte array of a GET request to the given object.
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="recordId">The record id</param>
        /// <param name="altUrl">The url to use without the instance url</param>
        /// <returns>The returned binary content as a byte array</returns>
        public byte[] GetRawBytes(string objectName, string recordId, string altUrl)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);
            ExceptionHelper.ThrowIfNullOrEmpty("altUrl", altUrl);

            var response = RequestRaw(GetAltUrl(altUrl), "{0}/{1}".With(objectName, recordId));

            return response.RawBytes;
        }

        /// <summary>
        /// Returns the raw byte array of a GET request to the given object.
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="recordId">The record id</param>
        /// <returns>The returned binary content as a byte array</returns>
        public byte[] GetRawBytes(string objectName, string recordId)
        {
            ExceptionHelper.ThrowIfNullOrEmpty("objectName", objectName);
            ExceptionHelper.ThrowIfNullOrEmpty("recordId", recordId);

            var response = RequestRaw(GetUrl("sobjects"), "{0}/{1}".With(objectName, recordId));

            return response.RawBytes;
        }
        #endregion

        #region Requests
        /// <summary>
        /// Performs a typed request against Salesforce's REST API.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="objectName">The name of the object (appended to the base URL).</param>
        /// <param name="record">The record to serialize as the request body.</param>
        /// <param name="method">The HTTP verb.</param>
        /// <exception cref="System.InvalidOperationException">Please, execute Authenticate method before call any REST API operation.</exception>
        protected IHttpResponse<T> Request<T>(string baseUrl, string objectName = null, object record = null, HttpVerb method = HttpVerb.GET) where T : new()
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("Please, execute Authenticate method before call any REST API operation.");
            }

            var url      = BuildUrl(baseUrl, objectName);
            var jsonBody = record != null ? updateJsonSerializer.Serialize(record) : null;

            var response = m_httpClient.Execute<T>(url, method, m_accessToken, jsonBody);
            CheckApiException(response);

            return response;
        }

        /// <summary>
        /// Performs a raw (un-deserialized) request against Salesforce's REST API.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="objectName">The name of the object (appended to the base URL).</param>
        /// <param name="record">The record to serialize as the request body.</param>
        /// <param name="method">The HTTP verb.</param>
        /// <exception cref="System.InvalidOperationException">Please, execute Authenticate method before call any REST API operation.</exception>
        protected IHttpResponse RequestRaw(string baseUrl, string objectName = null, object record = null, HttpVerb method = HttpVerb.GET)
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("Please, execute Authenticate method before call any REST API operation.");
            }

            var url      = BuildUrl(baseUrl, objectName);
            var jsonBody = record != null ? updateJsonSerializer.Serialize(record) : null;

            var response = m_httpClient.Execute(url, method, m_accessToken, jsonBody);
            CheckApiException(response);
            ExtractLimitsInfo(response);

            return response;
        }

        /// <summary>
        /// Constructs the full request URL from a base URL and an optional object-name segment.
        /// </summary>
        private static string BuildUrl(string baseUrl, string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return baseUrl;
            }

            return baseUrl.TrimEnd('/') + "/" + objectName.TrimStart('/');
        }

        /// <summary>
        /// Checks if an API exception was thrown in the response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <exception cref="SalesforceException"></exception>
        private void CheckApiException(IHttpResponse response)
        {
            if ((int)response.StatusCode > 299)
            {
                var responseData = m_deserializer.Deserialize<dynamic>(response.Content);

                var error       = responseData[0];
                var fieldsArray = error.fields as JArray;

                if (fieldsArray == null)
                {
                    throw new SalesforceException(error.errorCode.Value, error.message.Value);
                }
                else
                {
                    throw new SalesforceException(error.errorCode.Value, error.message.Value, fieldsArray.Select(v => (string)v).ToArray());
                }
            }

            if (response.ErrorException != null)
            {
                throw new FormatException(
                    "{0}{1}{2}".With(response.ErrorException.Message, Environment.NewLine, response.Content));
            }
        }

        private void ExtractLimitsInfo(IHttpResponse response)
        {
            var limitHeader = response.GetHeader("Sforce-Limit-Info");
            if (limitHeader != null)
            {
                var match = apiUsageRegexp.Match(limitHeader);
                if (match.Success)
                {
                    ApiCallsUsed  = int.Parse(match.Groups[1].Value);
                    ApiCallsLimit = int.Parse(match.Groups[2].Value);
                }
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Gets the record projection fields.
        /// </summary>
        /// <param name="recordType">Type of the record.</param>
        /// <returns></returns>
        public static string GetRecordProjection(Type recordType)
        {
            var propNames = new List<string>();

            var props = recordType.GetProperties();
            foreach (var prop in props)
            {
                var sfAttrs = prop.GetCustomAttributes(typeof(SalesforceAttribute), true);
                // If Ignore then we shouldn't include it.
                if (sfAttrs.Any())
                {
                    var sfAttr = sfAttrs.FirstOrDefault() as SalesforceAttribute;
                    if (sfAttr != null)
                    {
                        if (sfAttr.Ignore)
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(sfAttr.FieldName))
                        {
                            propNames.Add(sfAttr.FieldName);
                            continue;
                        }
                    }
                }

                propNames.Add(prop.Name);
            }

            return String.Join(", ", propNames);
        }

        private static readonly Regex s_salesforceIdRegex = new Regex(@"^[a-zA-Z0-9]{15}([a-zA-Z0-9]{3})?$", RegexOptions.Compiled);

        /// <summary>
        /// Validates and sanitizes a Salesforce record ID before use in a SOQL query.
        /// Applies two independent layers of defence against SOQL injection:
        ///   1. Allowlist validation — rejects any value that is not exactly 15 or 18
        ///      alphanumeric characters, which is the only legal Salesforce ID format.
        ///   2. Single-quote escaping — replaces any residual single quotes with the SOQL
        ///      escape sequence (\') as a defence-in-depth measure. Under the current regex
        ///      this branch is unreachable for valid input, but it provides a safety net
        ///      against any future bypass technique that circumvents the allowlist.
        /// </summary>
        /// <param name="recordId">The record ID to validate and sanitize.</param>
        /// <returns>The sanitized record ID, safe for interpolation into a SOQL string literal.</returns>
        /// <exception cref="ArgumentException">Thrown when the record ID does not match the expected Salesforce ID format.</exception>
        private static string SanitizeSoqlId(string recordId)
        {
            if (!s_salesforceIdRegex.IsMatch(recordId))
            {
                throw new ArgumentException(
                    "The recordId contains invalid characters or is not a valid Salesforce ID format. " +
                    "A Salesforce ID must be exactly 15 or 18 alphanumeric characters.",
                    "recordId");
            }

            // Defence-in-depth: escape single quotes per SOQL convention so that even if a
            // future bypass technique were to pass the regex above, the value cannot break
            // out of the enclosing string literal in the query.
            return recordId.Replace("'", "\\'");
        }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns></returns>
        protected string GetUrl(string resourceName)
        {
            return "{0}/services/data/{1}/{2}".With(InstanceUrl, ApiVersion, resourceName);
        }

        /// <summary>
        /// Gets URL for use with a custom RESTful endpoint.
        /// </summary>
        /// <param name="url">URL of alternate service</param>
        /// <returns></returns>
        protected string GetAltUrl(string url)
        {
            return "{0}/{1}".With(InstanceUrl, url);
        }
        #endregion
    }
}
