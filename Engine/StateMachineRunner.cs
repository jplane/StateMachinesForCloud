using SM4C.Engine.Extensions;
using SM4C.Integration;
using SM4C.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Engine
{
    public static class StateMachineRunner
    {
        public static async Task<JToken> RunAsync(StateMachine workflow,
                                                  IStateMachineHost host,
                                                  JObject? input = null,
                                                  ObservableAction[]? targetActions = null,
                                                  CancellationToken cancelToken = default)
        {
            workflow.CheckArgNull(nameof(workflow));
            host.CheckArgNull(nameof(host));

            StateMachineContext? context = null;

            Func<CancellationToken, Task<JToken>> runTask = async token =>
            {
                context = new StateMachineContext(workflow, host, input, targetActions, token);

                await context.RecordObservableActionAsync(ObservableAction.EnterStateMachine);

                try
                {
                    return await RunAsync(context);
                }
                finally
                {
                    await context.RecordObservableActionAsync(ObservableAction.ExitStateMachine);
                }
            };

            JToken output;

            if (workflow.Timeout != null)
            {
                using var localTimeoutCancelTokenSource = new CancellationTokenSource();

                using var combined = CancellationTokenSource.CreateLinkedTokenSource(
                        localTimeoutCancelTokenSource.Token, cancelToken);

                Task<JToken> timeoutTask = host.DelayAsync(workflow.Timeout.Duration, combined.Token)
                                               .ContinueWith(_ =>
                                               {
                                                   return (JToken)JValue.CreateNull();
                                               });

                Debug.Assert(timeoutTask != null);

                output = await Task.WhenAny(timeoutTask, runTask(combined.Token)).Unwrap();

                if (!timeoutTask.IsCompleted)
                {
                    localTimeoutCancelTokenSource.Cancel();
                }
                else if (workflow.Timeout.Action != null)
                {
                    Debug.Assert(context != null);

                    await workflow.Timeout.Action.ExecuteAsync(context, context.Data);
                }
            }
            else
            {
                output = await runTask(cancelToken);
            }

            Debug.Assert(output != null);

            return output;
        }

        private static async Task<JToken> RunAsync(StateMachineContext context)
        {
            Debug.Assert(context != null);

            var state = context.Workflow.States.SingleOrDefault(s => s.Start);

            if (state == null)
            {
                throw new InvalidOperationException("Unable to resolve single start state in workflow.");
            }

            while (state != null)
            {
                state = await state.ExecuteAsync(context);
            }

            return context.Data;
        }
    }
}
