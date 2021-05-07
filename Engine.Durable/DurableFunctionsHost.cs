namespace SM4C.Engine.Durable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Newtonsoft.Json.Linq;
    using SM4C.Integration;

    public class DurableFunctionsHost : IStateMachineHost
    {
        readonly IDurableOrchestrationContext orchestrationContext;
        readonly Random deterministicRandom;
        readonly DateTimeOffset start = DateTimeOffset.UtcNow;
        readonly string? telemetryUri;

        public DurableFunctionsHost(IDurableOrchestrationContext orchestrationContext, string? telemetryUri)
        {
            this.orchestrationContext = orchestrationContext ?? throw new ArgumentNullException(nameof(orchestrationContext));
            this.deterministicRandom = new Random(GetDeterministicRandomSeed(orchestrationContext));
            this.telemetryUri = telemetryUri;
        }

        public IEvent CreateEventInstance(string name, string type, string source, JToken data, IDictionary<string, string> contextAttributes)
        {
            throw new NotImplementedException();
        }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancelToken)
        {
            DateTime fireAt = this.orchestrationContext.CurrentUtcDateTime.Add(delay);
            return this.orchestrationContext.CreateTimer(fireAt, cancelToken);
        }

        public Task<JObject> ExecuteSubflowAsync(string workflowId, JToken input, CancellationToken cancelToken, bool waitForCompletion = true)
        {
            throw new NotImplementedException();
        }

        public bool GetRandomBool()
        {
            return this.deterministicRandom.Next(1) == 0;
        }

        public double GetRandomDouble()
        {
            // REVIEW: Is this really the right thing to do instead of Random.NextDouble()?
            // Source: https://stackoverflow.com/a/3365388/2069
            double mantissa = (this.deterministicRandom.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, this.deterministicRandom.Next(-126, 128));
            return mantissa * exponent;
        }

        public async Task<JObject> InvokeAsync(string operation, IDictionary<string, object> parameters, CancellationToken cancelToken, bool waitForCompletion = true)
        {
            Task<JObject> activityTask = this.orchestrationContext.CallActivityAsync<JObject>(
                ServerlessWorkflowFunctions.RESTfulServiceInvokerFunctionName,
                new InvokeFunctionArgs(operation, parameters));

            Task cancellationTask = Task.Delay(Timeout.Infinite, cancelToken);

            if (activityTask == await Task.WhenAny(activityTask, cancellationTask))
            {
                return await activityTask;
            }
            else
            {
                // Awaiting the cancellation task should throw
                await cancellationTask;
                throw new TaskCanceledException();
            }
        }

        public Task SendEventsAsync(IEnumerable<IEvent> events, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEvent> WaitForEventAsync(CancellationToken cancelToken, TimeSpan? timeout = null)
        {
            return await this.orchestrationContext.WaitForExternalEvent<WorkflowEvent>(
                name: WorkflowEvent.ExternalEventName,
                timeout ?? TimeSpan.FromDays(365), // Infinite timeouts aren't supported by DF
                cancelToken);
        }

        static int GetDeterministicRandomSeed(IDurableOrchestrationContext context)
        {
            string key = $"{context.Name}|{context.InstanceId}|{context.CurrentUtcDateTime:s}";
            return key.GetHashCode();
        }

        public string GetInstanceId()
        {
            return this.orchestrationContext.InstanceId;
        }

        public DateTimeOffset GetStartTime()
        {
            return this.start;
        }

        public Task OnObservableEventAsync(IReadOnlyDictionary<string, object> eventData)
        {
            var args = (this.telemetryUri, eventData);

            return this.orchestrationContext.CallActivityAsync<bool>(ServerlessWorkflowFunctions.PublishTelemetryFunctionName, args);
        }
    }
}
