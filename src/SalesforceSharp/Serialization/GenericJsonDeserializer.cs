﻿﻿﻿using System;
using Newtonsoft.Json;

namespace SalesforceSharp.Serialization
{
    /// <summary>
    /// Deserializes a JSON string to a target type using a
    /// <see cref="SalesforceContractResolver"/> to handle Salesforce-specific
    /// field mapping and ignore rules.
    /// </summary>
    internal class GenericJsonDeserializer
    {
        private readonly SalesforceContractResolver salesForceContractResolver;

        public GenericJsonDeserializer(SalesforceContractResolver salesForceContractResolver)
        {
            if (salesForceContractResolver == null) throw new ArgumentNullException(nameof(salesForceContractResolver));
            this.salesForceContractResolver = salesForceContractResolver;
        }

        #region Methods
        /// <summary>
        /// Deserializes <paramref name="content"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target deserialization type.</typeparam>
        /// <param name="content">Raw JSON string to deserialize.</param>
        public T Deserialize<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(
                content,
                new JsonSerializerSettings { ContractResolver = salesForceContractResolver });
        }
        #endregion
    }
}