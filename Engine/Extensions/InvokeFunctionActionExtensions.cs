using SM4C.Model;
using SM4C.Model.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class InvokeFunctionActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this InvokeFunctionAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            Func<CancellationToken, Task<JToken>> invokeTask = token => ExecuteFunctionAsync(action, context, input, token);

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

        private static async Task<JToken> ExecuteFunctionAsync(InvokeFunctionAction action,
                                                               StateMachineContext context,
                                                               JToken input,
                                                               CancellationToken cancelToken)
        {
            Debug.Assert(action != null);
            Debug.Assert(context != null);
            Debug.Assert(input != null);

            var function = context.Workflow.Functions?.SingleOrDefault(f => f.Name.IsEqualTo(action.FunctionName));

            if (function == null)
            {
                throw new InvalidOperationException("Unable to resolve function reference: " + action.FunctionName);
            }

            Debug.Assert(!string.IsNullOrWhiteSpace(function.Operation));

            JToken? result = null;

            switch (function.Type)
            {
                case FunctionsType.Rest:
                    result = await context.Host.InvokeAsync(function.Operation,
                                                            action.Arguments.ResolveExpressionArguments(input, context),
                                                            cancelToken,
                                                            action.WaitForCompletion);
                    break;

                case FunctionsType.Expression:
                    result = function.Operation.EvalExpr(input, context);
                    break;

                default:
                    Debug.Fail("Unexpected FunctionsType: " + function.Type);
                    break;
            }

            return result ?? JValue.CreateNull();
        }

        private static IDictionary<string, object> ResolveExpressionArguments(this IDictionary<string, object>? arguments,
                                                                              JToken data,
                                                                              StateMachineContext context)
        {
            Debug.Assert(data != null);
            Debug.Assert(context != null);

            if (arguments == null)
            {
                return ImmutableDictionary<string, object>.Empty;
            }

            object ResolveExpression(object argument)
            {
                if (argument is string s && s.IsJQExpression())
                {
                    return s.EvalExpr(data, context);
                }
                else
                {
                    return argument;
                }
            }

            return arguments.ToDictionary(pair => pair.Key, pair => ResolveExpression(pair.Value));
        }
    }
}
