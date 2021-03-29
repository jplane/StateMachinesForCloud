using SM4C.Model.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class InvokeSubflowActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this InvokeSubflowAction action,
                                              StateMachineContext context,
                                              JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            Func<CancellationToken, Task<JToken>> invokeTask = async token =>
            {
                var jobj = await context.Host.ExecuteSubflowAsync(action.SubflowName, input, token, action.WaitForCompletion);

                Debug.Assert(jobj != null);

                return jobj;
            };

            JToken output;

            if (action.Timeout != null)
            {
                using var localTimeoutCancelTokenSource = new CancellationTokenSource();

                using var combined = CancellationTokenSource.CreateLinkedTokenSource(
                        localTimeoutCancelTokenSource.Token, context.CancelToken);

                Task<JToken> timeoutTask = context.Host.DelayAsync(action.Timeout.Value, combined.Token)
                                              .ContinueWith(_ =>
                                              {
                                                  return (JToken)JValue.CreateNull();
                                              });

                Debug.Assert(timeoutTask != null);

                output = await Task.WhenAny(timeoutTask, invokeTask(combined.Token)).Unwrap();

                if (!timeoutTask.IsCompleted)
                {
                    localTimeoutCancelTokenSource.Cancel();
                }
            }
            else
            {
                output = await invokeTask(context.CancelToken);
            }

            Debug.Assert(output != null);

            return output;
        }
    }
}
