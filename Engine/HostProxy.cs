using SM4C.Integration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Engine
{
    internal class HostProxy : IStateMachineHost
    {
        private readonly IStateMachineHost _host;

        public HostProxy(IStateMachineHost host)
        {
            _host = host;
        }

        public IEvent CreateEventInstance(string name,
                                          string type,
                                          string source,
                                          JToken data,
                                          IDictionary<string, string> contextAttributes)
        {
            return _host.CreateEventInstance(name, type, source, data, contextAttributes);
        }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancelToken)
        {
            var task = _host.DelayAsync(delay, cancelToken);

            cancelToken.ThrowIfCancellationRequested();

            return task;
        }

        public Task<JObject> ExecuteSubflowAsync(string workflowId,
                                                 JToken input,
                                                 CancellationToken cancelToken,
                                                 bool waitForCompletion = true)
        {
            var task = _host.ExecuteSubflowAsync(workflowId, input, cancelToken, waitForCompletion);

            cancelToken.ThrowIfCancellationRequested();

            return task;
        }

        public bool GetRandomBool()
        {
            return _host.GetRandomBool();
        }

        public double GetRandomDouble()
        {
            return _host.GetRandomDouble();
        }

        public string GetInstanceId()
        {
            return _host.GetInstanceId();
        }

        public DateTimeOffset GetStartTime()
        {
            return _host.GetStartTime();
        }

        public Task<JObject> InvokeAsync(string operation, IDictionary<string, object> parameters, CancellationToken cancelToken, bool waitForCompletion = true)
        {
            var task = _host.InvokeAsync(operation, parameters, cancelToken);

            cancelToken.ThrowIfCancellationRequested();

            return task;
        }

        public Task SendEventsAsync(IEnumerable<IEvent> events, CancellationToken cancelToken)
        {
            var task = _host.SendEventsAsync(events, cancelToken);

            cancelToken.ThrowIfCancellationRequested();

            return task;
        }

        public Task<IEvent> WaitForEventAsync(CancellationToken cancelToken, TimeSpan? timeout = null)
        {
            var task = _host.WaitForEventAsync(cancelToken, timeout);

            cancelToken.ThrowIfCancellationRequested();

            return task;
        }

        public Task OnObservableEventAsync(IReadOnlyDictionary<string, object> data)
        {
            return _host.OnObservableEventAsync(data);
        }
    }
}
