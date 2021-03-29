using SM4C.Model.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model
{
    public sealed class ExecutionTimeoutPolicy
    {
        /// <summary>Timeout interval (ISO 8601 duration format)</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("duration", Required = Required.Always)]
        [Required]
        public System.TimeSpan Duration { get; set; }

        /// <summary>An (optional) action to perform upon timeout expiration, immediately prior to ending workflow execution.</summary>
        [JsonProperty("action", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public Action Action { get; set; }
    }
}
