using SM4C.Model.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class InjectDataActionExtensions
    {
        public static Task<JToken> ExecuteAsync(this InjectDataAction action,
                                                StateMachineContext context,
                                                JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            return Task.FromResult(action.Expression.EvalExpr(input, context) ?? JValue.CreateNull());
        }
    }
}
