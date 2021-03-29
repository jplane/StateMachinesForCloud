using SM4C.Model.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SM4C.Engine.Extensions
{
    internal static class SequenceActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this SequenceAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            var output = new JObject();

            for(var idx = 0; idx < action.Actions.Count; idx++)
            {
                var childAction = action.Actions.ElementAt(idx);

                Debug.Assert(childAction != null);

                var id = string.IsNullOrWhiteSpace(childAction.Name) ? idx.ToString() : childAction.Name;

                Debug.Assert(!string.IsNullOrWhiteSpace(id));

                output[id] = await childAction.ExecuteAsync(context, input);
            }

            return output;
        }
    }
}
