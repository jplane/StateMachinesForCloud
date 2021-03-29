using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class InvokeSubflowAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.InvokeSubflow;

        /// <summary>Workflow execution must wait for sub-workflow to finish before continuing</summary>
        [JsonProperty("waitForCompletion", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool WaitForCompletion { get; set; } = false;

        /// <summary>Time period to wait for function execution to complete</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("timeout", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Timeout { get; set; }

        /// <summary>Sub-workflow unique id</summary>
        [JsonProperty("subflowName", Required = Required.Always)]
        [Required]
        public string SubflowName { get; set; }

        /// <summary>Argument name/value pairs. Values can be JQ expressions that operate against state data.</summary>
        [JsonProperty("arguments", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
    }
}
