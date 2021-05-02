namespace SM4C.Engine.Durable
{
    using System;
    using Newtonsoft.Json.Linq;
    using SM4C.Integration;
    using SM4C.Model;

    public class StartWorkflowArgs
    {
        public StartWorkflowArgs(StateMachine definition, JObject? input, ObservableAction[]? actions, string? telemetryUri)
        {
            this.Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.Input = input;
            this.Actions = actions;
            this.TelemetryUri = telemetryUri;
        }

        public StateMachine Definition { get; protected set; }

        public JObject? Input { get; protected set; }

        public ObservableAction[]? Actions { get; protected set; }

        public string? TelemetryUri { get; protected set; }
    }
}
