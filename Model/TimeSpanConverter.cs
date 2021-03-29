using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace SM4C.Model
{
    internal sealed class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public static TimeSpan GetTimeSpanFromString(string s)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(s));

            if (TimeSpan.TryParse(s, out TimeSpan ts))
            {
                return ts;
            }
            else
            {
                try
                {
                    return XmlConvert.ToTimeSpan(s); // ISO 8601
                }
                catch
                {
                    throw new InvalidOperationException("Unable to parse input string to TimeSpan: " + s);
                }
            }
        }

        public override TimeSpan ReadJson(JsonReader reader,
                                          Type objectType,
                                          [AllowNull] TimeSpan existingValue,
                                          bool hasExistingValue,
                                          JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            Debug.Assert(token.Type == JTokenType.String);

            var json = token.Value<string>();

            Debug.Assert(!string.IsNullOrWhiteSpace(json));

            return GetTimeSpanFromString(json);
        }

        public override void WriteJson(JsonWriter writer,
                                       [AllowNull] TimeSpan value,
                                       JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
