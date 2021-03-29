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
    internal static class SendEventActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this SendEventAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            var eventDefinition = context.Workflow.Events.SingleOrDefault(ev => ev.Name.IsEqualTo(action.Event));

            if (eventDefinition == null)
            {
                throw new InvalidOperationException("Unable to resolve event definition: " + action.Event);
            }

            JToken payload = action.Expression?.EvalExpr(input, context) ?? new JObject();

            var evt = context.Host.CreateEventInstance(eventDefinition.Name,
                                                       eventDefinition.Type,
                                                       eventDefinition.Source,
                                                       payload,
                                                       action.ContextAttributes);

            Debug.Assert(evt != null);

            await context.Host.SendEventsAsync(new[] { evt }, context.CancelToken);

            return JValue.CreateNull();
        }
    }
}
