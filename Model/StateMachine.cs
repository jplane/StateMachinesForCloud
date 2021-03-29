using SM4C.Model.Actions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SM4C.Model
{
    /// <summary>
    /// Serverless Workflow specification - workflow schema
    /// </summary>
    public sealed class StateMachine
    {
        /// <summary>Workflow name</summary>
        [JsonProperty("name", Required = Required.Always)]
        [Required]
        public string Name { get; set; }

        /// <summary>Workflow description</summary>
        [JsonProperty("description", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(int.MaxValue, MinimumLength = 1)]
        public string Description { get; set; }

        /// <summary>Workflow version</summary>
        [JsonProperty("version", Required = Required.Always)]
        [Required]
        public string Version { get; set; }

        [JsonProperty("execTimeout", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public ExecutionTimeoutPolicy Timeout { get; set; }

        [JsonProperty("events", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        private JToken _Events
        {
            get
            {
                if (this.EventsUri != null)
                {
                    return JValue.CreateString(this.EventsUri);
                }
                else if (this.Events != null)
                {
                    return JToken.FromObject(this.Events);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value.Type == JTokenType.String)
                {
                    var uri = (string)((JValue)value).Value;

                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        throw new ArgumentException("Events URI is invalid.");
                    }

                    this.EventsUri = uri;
                    this.Events = null;
                }
                else if (value.Type == JTokenType.Array)
                {
                    this.Events = ((JArray)value).ToObject<ICollection<EventDefinition>>();
                    this.EventsUri = null;
                }
                else
                {
                    this.EventsUri = null;
                    this.Events = null;
                }
            }
        }

        [JsonIgnore]
        public string EventsUri { get; set; }

        [JsonIgnore]
        public ICollection<EventDefinition> Events { get; set; }

        [JsonProperty("functions", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        private JToken _Functions
        {
            get
            {
                if (this.FunctionsUri != null)
                {
                    return JValue.CreateString(this.FunctionsUri);
                }
                else if (this.Functions != null)
                {
                    return JToken.FromObject(this.Functions);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value.Type == JTokenType.String)
                {
                    var uri = (string)((JValue)value).Value;

                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        throw new ArgumentException("Functions URI is invalid.");
                    }

                    this.FunctionsUri = uri;
                    this.Functions = null;
                }
                else if (value.Type == JTokenType.Array)
                {
                    this.Functions = ((JArray)value).ToObject<ICollection<Function>>();
                    this.FunctionsUri = null;
                }
                else
                {
                    this.FunctionsUri = null;
                    this.Functions = null;
                }
            }
        }

        [JsonIgnore]
        public string FunctionsUri { get; set; }

        [JsonIgnore]
        public ICollection<Function> Functions { get; set; }

        [JsonProperty("retries", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        private JToken _Retries
        {
            get
            {
                if (this.RetriesUri != null)
                {
                    return JValue.CreateString(this.RetriesUri);
                }
                else if (this.Retries != null)
                {
                    return JToken.FromObject(this.Retries);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value.Type == JTokenType.String)
                {
                    var uri = (string)((JValue)value).Value;

                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        throw new ArgumentException("Retries URI is invalid.");
                    }

                    this.RetriesUri = uri;
                    this.Retries = null;
                }
                else if (value.Type == JTokenType.Array)
                {
                    this.Retries = ((JArray)value).ToObject<ICollection<RetryPolicy>>();
                    this.RetriesUri = null;
                }
                else
                {
                    this.RetriesUri = null;
                    this.Retries = null;
                }
            }
        }

        [JsonIgnore]
        public string RetriesUri { get; set; }

        [JsonIgnore]
        public ICollection<RetryPolicy> Retries { get; set; }

        [JsonProperty("states", Required = Required.Always)]
        [MinLength(1)]
        [Required]
        public ICollection<State> States { get; set; }
    }
}
