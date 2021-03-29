using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class ForEachAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.ForEach;

        /// <summary>Action to execute for each item in the input collection</summary>
        [JsonProperty("action", Required = Required.Always)]
        [JsonConverter(typeof(ActionConverter))]
        [Required]
        public Action Action { get; set; }

        /// <summary>Specifies how upper bound on how many iterations may run in parallel</summary>
        [JsonProperty("maxParallel", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [Range(1, int.MaxValue)]
        public int? MaxParallel { get; set; }

        /// <summary>JQ expression selecting an array element of the states data</summary>
        [JsonProperty("input", Required = Required.Always)]
        [Required]
        public string Input { get; set; }
    }
}
