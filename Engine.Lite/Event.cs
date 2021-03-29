using SM4C.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SM4C.Engine.Lite
{
    public class Event : IEvent
    {
        public Event()
        {
            this.ContextAttributes = new Dictionary<string, JToken>();
        }

        public string EventId { get; set; }

        public string EventName { get; set; }

        public string EventSource { get; set; }

        public string EventType { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public JToken Data { get; set; }

        public IDictionary<string, JToken> ContextAttributes { get; }

        public JObject ToJson() => JObject.FromObject(this);
    }
}
