using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class SequenceAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.Sequence;

        /// <summary>Ordered list of actions that execute in sequence.</summary>
        [JsonProperty("actions", Required = Required.Always, ItemConverterType = typeof(ActionConverter))]
        [MinLength(1)]
        [Required]
        public ICollection<Action> Actions { get; set; } = new List<Action>();
    }
}
