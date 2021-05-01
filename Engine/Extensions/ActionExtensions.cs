using SM4C.Model;
using SM4C.Model.Actions;
using ModelAction =SM4C.Model.Actions.Action;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SM4C.Integration;

namespace SM4C.Engine.Extensions
{
    internal static class ActionExtensions
    {
        public static async Task<JToken> ExecuteAsync(this ModelAction action,
                                                      StateMachineContext context,
                                                      JToken input)
        {
            action.CheckArgNull(nameof(action));
            context.CheckArgNull(nameof(context));
            input.CheckArgNull(nameof(input));

            await context.RecordObservableActionAsync(ObservableAction.BeforeAction,
                                                      () => new Dictionary<string, object>
                                                      {
                                                          { "actionName", action.Name },
                                                          { "actionType", action.GetType().FullName }
                                                      });

            Func<StateMachineContext, JToken, Task<JToken>> executeFunc = action switch
            {
                InjectDataAction inject => inject.ExecuteAsync,
                ParallelAction parallel => parallel.ExecuteAsync,
                SequenceAction sequence => sequence.ExecuteAsync,
                SendEventAction send => send.ExecuteAsync,
                DelayAction delay => delay.ExecuteAsync,
                InvokeSubflowAction subflow => subflow.ExecuteAsync,
                InvokeFunctionAction function => function.ExecuteAsync,
                ForEachAction forEach => forEach.ExecuteAsync,
                _ => throw new NotImplementedException("Action behavior not implemented: " + action.GetType().FullName)
            };

            Debug.Assert(executeFunc != null);

            var attempts = 0;
            DateTimeOffset? start = null;

            while (true)
            {
                try
                {
                    var result = await executeFunc(context, input);

                    await context.RecordObservableActionAsync(ObservableAction.AfterAction,
                                                              () => new Dictionary<string, object>
                                                              {
                                                                  { "actionName", action.Name },
                                                                  { "actionType", action.GetType().FullName }
                                                              });

                    return result;
                }
                catch (Exception ex)
                {
                    if (action.TryHandleError(JObject.FromObject(ex),
                                              context,
                                              out RetryPolicy? retryPolicy))
                    {
                        if (retryPolicy == null)
                        {
                            return JValue.CreateNull();
                        }
                        else
                        {
                            TimeSpan elapsedDelay;

                            if (start == null)
                            {
                                start = DateTimeOffset.UtcNow;
                                elapsedDelay = TimeSpan.Zero;
                            }
                            else
                            {
                                elapsedDelay = DateTimeOffset.UtcNow.Subtract(start.Value);
                            }

                            var retry = await retryPolicy.ShouldRetryAsync(context, ++attempts, elapsedDelay);

                            if (retry)
                            {
                                continue;
                            }
                        }
                    }

                    throw;
                }
            }
        }

        private static bool TryHandleError(this ModelAction action,
                                           JToken error,
                                           StateMachineContext context,
                                           out RetryPolicy? retry)
        {
            Debug.Assert(action != null);
            Debug.Assert(context != null);

            retry = null;

            if (action.ErrorHandlers != null)
            {
                foreach (var handler in action.ErrorHandlers)
                {
                    if (handler.Condition?.EvalPredicateExpr(error, context) ?? true)
                    {
                        error.Merge(context.Output, handler.ResultHandler, context);

                        retry = context.Workflow.Retries?.SingleOrDefault(r => r.Name.IsEqualTo(handler.RetryPolicy ?? string.Empty));

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
