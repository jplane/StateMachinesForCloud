using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace SM4C.Model.Actions
{
    public abstract class Action
    {
        internal Action()
        {
        }

        /// <summary>Action name</summary>
        [JsonProperty("name", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>Action type</summary>
        [JsonProperty("type", Required = Required.Always)]
        [Required]
        protected abstract ActionType Type { get; set; }

        /// <summary>Error handling policies for this action</summary>
        [JsonProperty("errorHandlers", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<ErrorPolicy> ErrorHandlers { get; set; }
    }
}
