using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class ParallelAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.Parallel;

        /// <summary>Set of actions that execute in parallel. Runtimes are free to implement parallelism as they see fit (logical, thread-based, etc.)</summary>
        [JsonProperty("actions", Required = Required.Always, ItemConverterType = typeof(ActionConverter))]
        [MinLength(1)]
        [Required]
        public ICollection<Action> Actions { get; set; } = new List<Action>();

        /// <summary>Option types on how to complete branch execution.</summary>
        [JsonProperty("completionType", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public ParallelCompletionType CompletionType { get; set; } = ParallelCompletionType.And;

        /// <summary>Used when completionType is set to 'n_of_m' to specify the 'N' value</summary>
        [JsonProperty("n", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [Range(1, int.MaxValue)]
        public int N { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ParallelCompletionType
    {
        [EnumMember(Value = @"and")]
        And = 0,

        [EnumMember(Value = @"xor")]
        Xor = 1,

        [EnumMember(Value = @"n_of_m")]
        N_of_M = 2
    }
}
