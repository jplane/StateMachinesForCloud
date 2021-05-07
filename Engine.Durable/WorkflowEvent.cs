namespace SM4C.Engine.Durable
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SM4C.Integration;

    // https://github.com/cloudevents/spec/blob/v1.0/spec.md#example
    public class WorkflowEvent : IEvent
    {
        internal static string ExternalEventName = "ServerlessWF::CloudEvent";

        [JsonProperty("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        [JsonProperty("id")]
        public string? EventId { get; set; }

        [JsonProperty("subject")]
        public string? EventName { get; set; }

        [JsonProperty("source")]
        public string? EventSource { get; set; }

        [JsonProperty("type")]
        public string? EventType { get; set; }

        [JsonProperty("time")]
        public DateTimeOffset Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("data")]
        public JToken? Data { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken>? ContextAttributes { get; set; }

        public JObject ToJson() => JObject.FromObject(this);
    }
}
