using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SM4C.Model.Actions
{
    public sealed class RetryPolicy
    {
        /// <summary>Unique retry strategy name</summary>
        [JsonProperty("name", Required = Required.Always)]
        [Required]
        public string Name { get; set; }

        /// <summary>Time delay between retry attempts (ISO 8601 duration format)</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("delay", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Delay { get; set; }

        /// <summary>Maximum time delay between retry attempts (ISO 8601 duration format)</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("maxDelay", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? MaxDelay { get; set; }

        /// <summary>Static value by which the delay increases during each attempt (ISO 8601 time format)</summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        [JsonProperty("increment", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Increment { get; set; }

        /// <summary>Numeric value, if specified the delay between retries is multiplied by this value.</summary>
        [JsonProperty("multiplier", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        [Range(0D, double.MaxValue)]
        public double? Multiplier { get; set; }

        /// <summary>Maximum number of retry attempts. Value of 0 means no retries are performed</summary>
        [JsonProperty("maxAttempts", Required = Required.Always)]
        [Required]
        [Range(0, int.MaxValue)]
        public int MaxAttempts { get; set; }

        [JsonProperty("jitter", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        private JValue _Jitter
        {
            get
            {
                if (this.Jitter != null)
                {
                    return new JValue(this.Jitter.Value);
                }
                else if (this.JitterTimeSpan != null)
                {
                    return (JValue) JToken.FromObject(this.JitterTimeSpan.Value);
                }
                else
                {
                    return JValue.CreateNull();
                }
            }

            set
            {
                if (value.Type == JTokenType.Float)
                {
                    this.Jitter = value.Value<double>();
                    this.JitterTimeSpan = null;
                }
                else if (value.Type == JTokenType.String)
                {
                    this.Jitter = null;
                    this.JitterTimeSpan = TimeSpanConverter.GetTimeSpanFromString(value.Value<string>());
                }
                else
                {
                    this.Jitter = null;
                    this.JitterTimeSpan = null;
                }
            }
        }

        /// <summary>Maximum amount of random time added or subtracted from the delay between each retry relative to total delay (between 0 and 1).</summary>
        [JsonIgnore]
        [Range(0D, double.MaxValue)]
        public double? Jitter { get; set; }

        /// <summary>Maximum amount of random time added or subtracted from the delay between each retry.</summary>
        [JsonIgnore]
        public TimeSpan? JitterTimeSpan { get; set; }
    }
}
