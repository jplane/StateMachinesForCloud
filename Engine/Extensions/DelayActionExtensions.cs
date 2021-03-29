using SM4C.Model.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class DelayActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this DelayAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            await context.Host.DelayAsync(action.Timeout, context.CancelToken);

            return JValue.CreateNull();
        }
    }
}
