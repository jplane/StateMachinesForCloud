using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class DelayAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.Delay;

        /// <summary>Amount of time (ISO 8601 format) to delay</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("timeout", Required = Required.Always)]
        [Required]
        public TimeSpan Timeout { get; set; }
    }
}
