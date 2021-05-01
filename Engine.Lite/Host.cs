using SM4C.Integration;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SM4C.Engine.Lite
{
    public class Host : IStateMachineHost
    {
        private static readonly Random Random = new Random(Environment.TickCount);

        private readonly AsyncLock _lock = new AsyncLock();
        private readonly Queue<Event> _inputQueue = new Queue<Event>();
        private readonly Queue<Event> _outputQueue = new Queue<Event>();
        private readonly Guid _instanceId = Guid.NewGuid();
        private readonly DateTimeOffset _start = DateTimeOffset.UtcNow;

        public Host()
        {
            this.Functions = new Dictionary<string, Delegate>();
        }

        public IDictionary<string, Delegate> Functions { get; }

        public void Enqueue(Event evt)
        {
            using var syncLock = _lock.Lock();
            _inputQueue.Enqueue(evt);
        }

        public IEvent Dequeue()
        {
            using var syncLock = _lock.Lock();

            _outputQueue.TryDequeue(out Event evt);

            return evt;
        }

        public IEvent CreateEventInstance(string name,
                                          string type,
                                          string source,
                                          JToken data,
                                          IDictionary<string, string> contextAttributes)
        {
            var evt = new Event
            {
                EventName = name,
                EventType = type,
                EventSource = source,
                Data = data
            };

            foreach (var pair in contextAttributes ?? new Dictionary<string, string>())
            {
                evt.ContextAttributes.Add(pair.Key, pair.Value);
            }

            return evt;
        }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancelToken)
        {
            return Task.Delay(delay, cancelToken);
        }

        public Task<JObject> ExecuteSubflowAsync(string workflowId,
                                                 JToken input,
                                                 CancellationToken cancelToken,
                                                 bool waitForCompletion = true)
        {
            return Task.FromResult(new JObject
            {
                ["workflowId"] = workflowId,
                ["input"] = input
            });
        }

        public bool GetRandomBool()
        {
            return Random.Next() % 2 == 0;
        }

        public double GetRandomDouble()
        {
            try
            {
                return double.MaxValue / Random.NextDouble();
            }
            catch (DivideByZeroException)
            {
                return 0d;
            }
        }

        public Task<JObject> InvokeAsync(string operation,
                                         IDictionary<string, object> parameters,
                                         CancellationToken cancelToken,
                                         bool waitForCompletion = true)
        {
            if (this.Functions.TryGetValue(operation, out Delegate function))
            {
                try
                {
                    var task = (Task<JObject>)function.Method.Invoke(null, parameters.Select(pair => pair.Value).ToArray());

                    Debug.Assert(task != null);

                    return task;
                }
                catch(TargetInvocationException tie)
                {
                    throw new Exception(tie.InnerException.Message, tie.InnerException);
                }
            }
            else
            {
                throw new InvalidOperationException("Operation does not exist: " + operation);
            }
        }

        public async Task SendEventsAsync(IEnumerable<IEvent> events, CancellationToken cancelToken)
        {
            using var asyncLock = await _lock.LockAsync(cancelToken);

            foreach (var evt in events)
            {
                Debug.Assert(evt is Event);

                _outputQueue.Enqueue((Event) evt);
            }
        }

        public Task<IEvent> WaitForEventAsync(CancellationToken cancelToken, TimeSpan? timeout = null)
        {
            async Task<IEvent> getNextEventAsync()
            {
                while (true)
                {
                    using (var asyncLock = await _lock.LockAsync(cancelToken))
                    {
                        if (_inputQueue.TryDequeue(out Event evt))
                        {
                            return evt;
                        }
                    }

                    await Task.Delay(1000, cancelToken);
                }
            }

            var nextEventTask = getNextEventAsync();

            if (timeout != null)
            {
                var timeoutTask = Task.Delay(timeout.Value, cancelToken).ContinueWith(_ => (IEvent) null);

                nextEventTask = Task.WhenAny(nextEventTask, timeoutTask).Unwrap();
            }

            return nextEventTask;
        }

        public string GetInstanceId()
        {
            return _instanceId.ToString();
        }

        public DateTimeOffset GetStartTime()
        {
            return _start;
        }

        public Task OnObservableEventAsync(IReadOnlyDictionary<string, object> data)
        {
            var json = JsonConvert.SerializeObject(data);

            Debug.Assert(json != null);

            Console.WriteLine(json);

            return Task.CompletedTask;
        }
    }
}
