using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class InjectDataAction : Action
    {
        protected override ActionType Type { get; set; } = ActionType.InjectData;

        /// <summary>JQ expression to create arbitrary JSON</summary>
        [JsonProperty("expression", Required = Required.Always)]
        [Required]
        public string Expression { get; set; }
    }
}
