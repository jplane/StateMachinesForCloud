using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class ErrorPolicy
    {
        /// <summary>JQ expression that defines a predicate to match against error info received from an action invocation</summary>
        [JsonProperty("condition", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Condition { get; set; }

        /// <summary>Error result merge JQ expression</summary>
        [JsonProperty("resultHandler", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string ResultHandler { get; set; }

        /// <summary>Reference to a defined retry policy</summary>
        [JsonProperty("retryPolicy", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string RetryPolicy { get; set; }
    }
}
