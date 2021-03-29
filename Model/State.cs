using SM4C.Model.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SM4C.Model
{
    public sealed class State
    {
        /// <summary>State name</summary>
        [JsonProperty("name", Required = Required.Always)]
        [Required]
        public string Name { get; set; }

        [JsonProperty("start", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool Start { get; set; } = false;

        /// <summary>Next transition of the workflow</summary>
        [JsonProperty("transitions", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<Transition> Transitions { get; set; } = new List<Transition>();

        /// <summary>JQ definition that selects parts of the states data input to be the action data</summary>
        [JsonProperty("inputFilter", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string InputFilter { get; set; }

        /// <summary>Enter action result merge JQ expression</summary>
        [JsonProperty("enterResultHandler", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string EnterResultHandler { get; set; }

        /// <summary>Action that fires when transitioning into this state</summary>
        [JsonProperty("enterAction", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ActionConverter))]
        public Action EnterAction { get; set; }

        /// <summary>Exit action result merge JQ expression</summary>
        [JsonProperty("exitResultHandler", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string ExitResultHandler { get; set; }

        /// <summary>Action that fires when transitioning away from this state</summary>
        [JsonProperty("exitAction", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ActionConverter))]
        public Action ExitAction { get; set; }
    }
}
