using SM4C.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class StateExtensions
    {
        public static async Task<State?> ExecuteAsync(this State state, StateMachineContext context)
        {
            state.CheckArgNull(nameof(state));
            context.CheckArgNull(nameof(context));

            var data = state.InputFilter?.EvalExpr(context.Input, context) ?? context.Input;

            Debug.Assert(data != null);

            if (state.EnterAction != null)
            {
                var result = await state.EnterAction.ExecuteAsync(context, data);

                Debug.Assert(result != null);

                result.Merge(context.Output, state.EnterResultHandler, context);
            }

            State? nextState = null;

            while (true)
            {
                var transition = await state.ResolveTransitionAsync(context, data);

                if (transition == null)
                {
                    break;
                }

                if (transition.Action != null)
                {
                    var result = await transition.Action.ExecuteAsync(context, data);

                    Debug.Assert(result != null);

                    result.Merge(context.Output, transition.ResultHandler, context);
                }

                nextState = context.Workflow.ResolveStateByName(transition.NextState);

                if (nextState != null)
                {
                    break;
                }
            }

            if (state.ExitAction != null)
            {
                var result = await state.ExitAction.ExecuteAsync(context, data);

                Debug.Assert(result != null);

                result.Merge(context.Output, state.ExitResultHandler, context);
            }

            return nextState;
        }

        private static async Task<Transition?> ResolveTransitionAsync(this State state, StateMachineContext context, JToken data)
        {
            if (state.Transitions.Count == 0)
            {
                return null;
            }

            Transition? transition = state.ResolveImplicitTransition();

            if (transition == null)
            {
                transition = state.ResolveConditionalTransition(context, data);
            }

            if (transition == null)
            {
                transition = await state.ResolveEventTransitionAsync(context, data);
            }

            Debug.Assert(transition != null);

            return transition;
        }

        private static async Task<Transition> ResolveEventTransitionAsync(this State state, StateMachineContext context, JToken data)
        {
            Debug.Assert(state != null);
            Debug.Assert(context != null);
            Debug.Assert(data != null);

            var eventSubscriptions = state.Transitions
                                          .Where(t => (t.EventGroups != null && t.EventGroups.Count > 0) &&
                                                      string.IsNullOrWhiteSpace(t.Condition) &&
                                                      t.Timeout == null)
                                          .Select(t => new EventSubscription { TargetTransition = t })
                                          .ToArray();

            Func<CancellationToken, Task<Transition>> getTransitionFunc =
                async token =>
                {
                    var matches = await EventSubscription.WaitForFirstMatchedSubscriptionAsync(eventSubscriptions, context, data, token);

                    Debug.Assert(matches != null);
                    Debug.Assert(matches.Length > 0);

                    foreach (var match in matches)
                    {
                        var json = match.EventInstance.ToJson();

                        Debug.Assert(json != null);

                        json.Merge(context.Output, match.Group.ResultHandler, context);
                    }

                    return matches.First().Transition;
                };

            var timeoutTransition = state.Transitions.SingleOrDefault(t => (!string.IsNullOrWhiteSpace(t.NextState) || t.Action != null) &&
                                                                           string.IsNullOrWhiteSpace(t.Condition) &&
                                                                           (t.EventGroups == null || t.EventGroups.Count == 0) &&
                                                                           t.Timeout != null);

            Transition next = null;

            if (timeoutTransition != null)
            {
                using var localTimeoutCancelTokenSource = new CancellationTokenSource();

                using var combined = CancellationTokenSource.CreateLinkedTokenSource(
                        localTimeoutCancelTokenSource.Token, context.CancelToken);

                Task<Transition> timeoutTask = context.Host.DelayAsync(timeoutTransition.Timeout.Value, combined.Token)
                                                           .ContinueWith(_ => timeoutTransition);

                Debug.Assert(timeoutTask != null);

                next = await Task.WhenAny(timeoutTask, getTransitionFunc(combined.Token)).Unwrap();

                if (!timeoutTask.IsCompleted)
                {
                    localTimeoutCancelTokenSource.Cancel();
                }
            }
            else
            {
                next = await getTransitionFunc(context.CancelToken);
            }

            Debug.Assert(next != null);

            return next;
        }

        private static Transition? ResolveConditionalTransition(this State state, StateMachineContext context, JToken data)
        {
            Debug.Assert(state != null);
            Debug.Assert(context != null);
            Debug.Assert(data != null);

            var candidates = state.Transitions.Where(t => !string.IsNullOrWhiteSpace(t.Condition) &&
                                                          (t.EventGroups == null || t.EventGroups.Count == 0) &&
                                                          t.Timeout == null);

            foreach (var candidate in candidates)
            {
                if (candidate.Condition.EvalPredicateExpr(data, context))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transition? ResolveImplicitTransition(this State state)
        {
            Debug.Assert(state != null);

            return state.Transitions.SingleOrDefault(t => !string.IsNullOrWhiteSpace(t.NextState) &&
                                                          string.IsNullOrWhiteSpace(t.Condition) &&
                                                          (t.EventGroups == null || t.EventGroups.Count == 0) &&
                                                          t.Timeout == null);
        }

        private static State? ResolveStateByName(this StateMachine workflow, string name)
        {
            workflow.CheckArgNull(nameof(workflow));

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return workflow.States.SingleOrDefault(s => s.Name.IsEqualTo(name));
        }
    }
}
