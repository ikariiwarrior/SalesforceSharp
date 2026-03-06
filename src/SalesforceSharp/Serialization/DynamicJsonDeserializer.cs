﻿using Newtonsoft.Json;

namespace SalesforceSharp.Serialization
{
    /// <summary>
    /// Deserializes a JSON string to a <c>dynamic</c> object.
    /// </summary>
    internal class DynamicJsonDeserializer
    {
        #region Methods
        /// <summary>
        /// Deserializes <paramref name="content"/> to <typeparamref name="T"/>
        /// (typically <c>dynamic</c>).
        /// </summary>
        /// <typeparam name="T">The target type — use <c>dynamic</c> for schema-free access.</typeparam>
        /// <param name="content">Raw JSON string to deserialize.</param>
        public T Deserialize<T>(string content)
        {
            return JsonConvert.DeserializeObject<dynamic>(content);
        }
        #endregion
    }
}
