using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace SM4C.Model
{
    /// <summary>
    /// Defines Workflow CloudEvents that can be consumed or produced
    /// </summary>
    public sealed class EventDefinition
    {
        /// <summary>Unique event name</summary>
        [JsonProperty("name", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>CloudEvent source</summary>
        [JsonProperty("source", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        /// <summary>CloudEvent type</summary>
        [JsonProperty("type", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>CloudEvent correlation definitions</summary>
        [JsonProperty("correlation", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [MinLength(1)]
        public ICollection<CorrelationInfo> Correlation { get; set; }
    }

    public sealed class CorrelationInfo
    {
        /// <summary>CloudEvent Extension Context Attribute name</summary>
        [JsonProperty("contextAttributeName", Required = Required.Always)]
        [Required]
        public string ContextAttributeName { get; set; }

        /// <summary>CloudEvent Extension Context Attribute value</summary>
        [JsonProperty("contextAttributeValue", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string ContextAttributeValue { get; set; }
    }
}
