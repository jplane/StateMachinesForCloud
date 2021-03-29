using SM4C.Engine;
using SM4C.Engine.Extensions;
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
    internal static class ParallelStateExtensions
    {
        public static async Task<JToken> ExecuteAsync(this ParallelAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            var output = new JObject();

            var tasks = action.Actions.Select(async (a, idx) =>
            {
                var item = await a.ExecuteAsync(context, input.DeepClone());

                Debug.Assert(item != null);

                var id = string.IsNullOrWhiteSpace(a.Name) ? idx.ToString() : a.Name;

                return (id, item);
            }).ToList();

            if (action.CompletionType == ParallelCompletionType.And)
            {
                var results = await Task.WhenAll(tasks);

                Array.ForEach(results, tuple => output[tuple.id] = tuple.item);
            }
            else if (action.CompletionType == ParallelCompletionType.Xor)
            {
                var resultTask = await Task.WhenAny(tasks);

                var tuple = await resultTask;

                output[tuple.id] = tuple.item;
            }
            else
            {
                Debug.Assert(action.CompletionType == ParallelCompletionType.N_of_M);
                Debug.Assert(action.N > 0);

                var resultCount = 0;

                while (resultCount < action.N && resultCount < tasks.Count)
                {
                    var resultTask = await Task.WhenAny(tasks);

                    tasks.Remove(resultTask);

                    var tuple = await resultTask;

                    output[tuple.id] = tuple.item;

                    resultCount++;
                }
            }

            return output;
        }
    }
}
