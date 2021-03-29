using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SM4C.Integration
{
    public interface IStateMachineHost
    {
        bool GetRandomBool();
        double GetRandomDouble();

        Task DelayAsync(TimeSpan delay, CancellationToken cancelToken);
        Task<JObject> ExecuteSubflowAsync(string workflowId, JToken input, CancellationToken cancelToken, bool waitForCompletion = true);
        Task<JObject> InvokeAsync(string operation, IDictionary<string, object> parameters, CancellationToken cancelToken, bool waitForCompletion = true);
        Task<IEvent> WaitForEventAsync(CancellationToken cancelToken, TimeSpan? timeout = null);

        Task SendEventsAsync(IEnumerable<IEvent> events, CancellationToken cancelToken);

        IEvent CreateEventInstance(string name,
                                   string type,
                                   string source,
                                   JToken data,
                                   IDictionary<string, string> contextAttributes);
    }
}
