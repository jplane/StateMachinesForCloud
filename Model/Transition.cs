using SM4C.Model.Actions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model
{
    public sealed class Transition
    {
        /// <summary>Name of state to transition to. If empty, transition action can fire without a state change.</summary>
        [JsonProperty("nextState", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string NextState { get; set; }

        /// <summary>JQ expression evaluated against state data</summary>
        [JsonProperty("condition", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Condition { get; set; }

        /// <summary>Define event groups that trigger this transition. All defined groups must be satisfied to trigger the transition.</summary>
        [JsonProperty("eventGroups", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<EventGroup> EventGroups { get; set; }

        /// <summary>Action result merge JQ expression</summary>
        [JsonProperty("resultHandler", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string ResultHandler { get; set; }

        /// <summary>Action that fires when transition is triggered</summary>
        [JsonProperty("action", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ActionConverter))]
        public Actions.Action Action { get; set; }

        /// <summary>Timeout interval (ISO 8601 duration format). Transition is triggered if timeout is reached.</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("timeout", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Timeout { get; set; }

        public static implicit operator Transition(string nextState) => new Transition { NextState = nextState };
    }

    public sealed class EventGroup
    {
        /// <summary>Events that match this event group. Only one is required.</summary>
        [JsonProperty("events", Required = Required.Always)]
        [Required]
        [MinLength(1)]
        public ICollection<string> Events { get; set; } = new List<string>();

        /// <summary>Optional JQ expression evaluated against the single matched event</summary>
        [JsonProperty("condition", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Condition { get; set; }

        /// <summary>Matched event merge JQ expression</summary>
        [JsonProperty("resultHandler", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string ResultHandler { get; set; }
    }
}
