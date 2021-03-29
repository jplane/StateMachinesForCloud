using SM4C.Engine.Extensions;
using SM4C.Integration;
using SM4C.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Engine
{
    internal class EventMatch
    {
        public IEvent EventInstance { get; set; }
        public EventDefinition EventDefinition { get; set; }
        public EventGroup Group { get; set; }
        public Transition Transition { get; set; }
    }

    internal sealed class EventSubscription
    {
        public static async Task<EventMatch[]> WaitForFirstMatchedSubscriptionAsync(
            IEnumerable<EventSubscription> subscriptions, StateMachineContext context, JToken data, CancellationToken cancelToken)
        {
            subscriptions.CheckArgNull(nameof(subscriptions));
            context.CheckArgNull(nameof(context));

            Func<ICollection<IEvent>, EventMatch[]> matchFinder = accumulated =>
            {
                foreach (var subscription in subscriptions)
                {
                    EventMatch[] matches = subscription.FindMatches(accumulated, context, data);

                    if (matches?.Length > 0)
                    {
                        return matches;
                    }
                }

                return null;
            };

            var accumulated = new List<IEvent>();

            EventMatch[] matches = null;

            while (matches == null && !cancelToken.IsCancellationRequested)
            {
                var evt = await context.Host.WaitForEventAsync(cancelToken);

                Debug.Assert(evt != null);

                accumulated.Add(evt);

                matches = matchFinder(accumulated);
            }

            return matches;
        }

        private EventMatch[] FindMatches(ICollection<IEvent> events, StateMachineContext context, JToken data)
        {
            var matches = new List<EventMatch>();

            foreach (var group in this.TargetTransition.EventGroups)
            {
                var match = FindGroupMatch(group, events, context, data);

                if (match == null)
                {
                    return null;
                }

                matches.Add(match);
            }

            return matches.ToArray();
        }

        private EventMatch FindGroupMatch(EventGroup group,
                                          ICollection<IEvent> events,
                                          StateMachineContext context,
                                          JToken data)
        {
            Debug.Assert(group != null);
            Debug.Assert(events != null);
            Debug.Assert(context != null);
            Debug.Assert(data != null);

            foreach (var evt in events)
            {
                Debug.Assert(evt != null);

                bool isDefinedAndNotAMatch(string eventDefAttribute, string incomingEventAttribute)
                {
                    return !(string.IsNullOrWhiteSpace(eventDefAttribute)) &&
                           !(eventDefAttribute.IsEqualTo(incomingEventAttribute));
                }

                var matchedEventName = group.Events.FirstOrDefault(e => e.IsEqualTo(evt.EventName));

                if (string.IsNullOrWhiteSpace(matchedEventName))
                {
                    continue;
                }

                var targetEvent = context.Workflow.Events.Single(e => e.Name.IsEqualTo(evt.EventName));

                if (isDefinedAndNotAMatch(targetEvent.Source, evt.EventSource))
                {
                    continue;
                }

                if (isDefinedAndNotAMatch(targetEvent.Type, evt.EventType))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(group.Condition) && !group.Condition.EvalPredicateExpr(data, context))
                {
                    continue;
                }

                return new EventMatch
                {
                    EventDefinition = targetEvent,
                    EventInstance = evt,
                    Transition = this.TargetTransition,
                    Group = group
                };
            }

            return null;
        }

        public Transition TargetTransition { get; set; }
    }
}
