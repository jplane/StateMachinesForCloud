using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SM4C.Integration
{
    public interface IEvent
    {
        public string EventId { get; }
        public string EventName { get; }
        public string EventSource { get; }
        public string EventType { get; }
        public DateTimeOffset Timestamp { get; }
        public JToken Data { get; }
        public IDictionary<string, JToken> ContextAttributes { get; }
        JObject ToJson();
    }
}
