using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class SendEventAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.SendEvent;

        /// <summary>References a name of a defined event</summary>
        [JsonProperty("event", Required = Required.Always)]
        [Required]
        public string Event { get; set; }

        /// <summary>JQ expression that defines the body of the event</summary>
        [JsonProperty("expression", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string Expression { get; set; }

        /// <summary>Add additional extension context attributes to the produced event</summary>
        [JsonProperty("contextAttributes", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> ContextAttributes { get; set; }
    }
}
