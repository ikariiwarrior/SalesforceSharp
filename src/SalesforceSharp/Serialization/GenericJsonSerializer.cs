﻿using Newtonsoft.Json;

namespace SalesforceSharp.Serialization
{
    /// <summary>
    /// Serializes an object to JSON using a <see cref="SalesforceContractResolver"/>
    /// to apply Salesforce-specific field mapping and ignore rules.
    /// </summary>
    internal class GenericJsonSerializer
    {
        private readonly SalesforceContractResolver salesForceContractResolver;

        public GenericJsonSerializer(SalesforceContractResolver salesForceContractResolver)
        {
            this.salesForceContractResolver = salesForceContractResolver;
        }

        /// <summary>Serializes <paramref name="obj"/> to an indented JSON string.</summary>
        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(
                obj,
                Formatting.Indented,
                new JsonSerializerSettings { ContractResolver = salesForceContractResolver });
        }
    }
}