using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace SM4C.Model.Actions
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActionType
    {
        [EnumMember(Value = @"delay")]
        Delay = 0,

        [EnumMember(Value = @"sendEvent")]
        SendEvent = 1,

        [EnumMember(Value = @"invokeFunction")]
        InvokeFunction = 2,

        [EnumMember(Value = @"parallel")]
        Parallel = 3,

        [EnumMember(Value = @"sequence")]
        Sequence = 4,

        [EnumMember(Value = @"invokeSubflow")]
        InvokeSubflow = 5,

        [EnumMember(Value = @"injectData")]
        InjectData = 6,

        [EnumMember(Value = @"forEach")]
        ForEach = 7
    }

    public sealed class ActionConverter : JsonConverter<Action>
    {
        public override Action ReadJson(JsonReader reader,
                                        Type objectType,
                                        Action existingValue,
                                        bool hasExistingValue,
                                        JsonSerializer serializer)
        {
            var json = JObject.Load(reader);

            Debug.Assert(json != null);

            if (!Enum.TryParse(json["type"].Value<string>(), true, out ActionType type))
            {
                throw new InvalidOperationException("Unable to deserialize JSON to ActionType.");
            }

            Action action = null;

            switch (type)
            {
                case ActionType.Delay:
                    action = json.ToObject<DelayAction>();
                    break;

                case ActionType.SendEvent:
                    action = json.ToObject<SendEventAction>();
                    break;

                case ActionType.ForEach:
                    action = json.ToObject<ForEachAction>();
                    break;

                case ActionType.InjectData:
                    action = json.ToObject<InjectDataAction>();
                    break;

                case ActionType.InvokeFunction:
                    action = json.ToObject<InvokeFunctionAction>();
                    break;

                case ActionType.Parallel:
                    action = json.ToObject<ParallelAction>();
                    break;

                case ActionType.Sequence:
                    action = json.ToObject<SequenceAction>();
                    break;

                case ActionType.InvokeSubflow:
                    action = json.ToObject<InvokeSubflowAction>();
                    break;

                default:
                    Debug.Fail("Unexpected action type: " + type);
                    break;
            }

            Debug.Assert(action != null);

            return action;
        }

        public override void WriteJson(JsonWriter writer, Action value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
