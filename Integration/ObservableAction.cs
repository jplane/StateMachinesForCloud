using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SM4C.Integration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ObservableAction
    {
        EnterStateMachine = 1,
        ExitStateMachine,
        EnterState,
        ExitState,
        BeforeAction,
        AfterAction
    }
}
