using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class InvokeFunctionAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.InvokeFunction;

        /// <summary>Unique function name</summary>
        [JsonProperty("functionName", Required = Required.Always)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        [Required]
        public string FunctionName { get; set; }

        /// <summary>Time period to wait for function execution to complete</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("timeout", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Timeout { get; set; }

        /// <summary>Workflow execution must wait for function to finish before continuing</summary>
        [JsonProperty("waitForCompletion", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool WaitForCompletion { get; set; } = false;

        /// <summary>Function argument name/value pairs. Values can be JQ expressions that operate against state data.</summary>
        [JsonProperty("arguments", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
    }
}
