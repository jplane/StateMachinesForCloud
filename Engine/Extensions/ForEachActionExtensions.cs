using SM4C.Model.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class ForEachActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this ForEachAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            Debug.Assert(!string.IsNullOrWhiteSpace(action.Input));

            var token = action.Input.EvalExpr(input, context);

            if (token == null || token.Type != JTokenType.Array)
            {
                throw new InvalidOperationException("Unable to resolve input collection for ForEach action.");
            }

            var inputCollection = (JArray)token;

            var max = action.MaxParallel ?? inputCollection.Count;

            Debug.Assert(action.Action != null);

            var outputs = new JArray();

            for (var i = 0; i < inputCollection.Count; i += max)
            {
                if (context.CancelToken.IsCancellationRequested)
                {
                    break;
                }

                var subsetTasks = inputCollection.Skip(i)
                                                 .Take(max)
                                                 .Select(json => action.Action.ExecuteAsync(context, json));

                var results = await Task.WhenAll(subsetTasks);

                Array.ForEach(results, outputs.Add);
            }

            return outputs;
        }
    }
}
