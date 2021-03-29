using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace SM4C.Model
{
    public sealed class Function
    {
        /// <summary>Unique function name</summary>
        [JsonProperty("name", Required = Required.Always)]
        [Required]
        public string Name { get; set; }

        /// <summary>If type `rest`, combination of the function/service OpenAPI definition URI and the operationID of the operation that needs to be invoked, separated by a '#'. If type is `expression` defines the workflow expression.</summary>
        [JsonProperty("operation", Required = Required.Always)]
        [Required]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Operation { get; set; }

        /// <summary>Defines the function type. Is either `rest` or `expression`. Default is `rest`</summary>
        [JsonProperty("type", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public FunctionsType Type { get; set; } = FunctionsType.Rest;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FunctionsType
    {
        [EnumMember(Value = @"rest")]
        Rest = 0,

        [EnumMember(Value = @"expression")]
        Expression = 1,

        [EnumMember(Value = @"rpc")]
        Rpc = 2,
    }
}
